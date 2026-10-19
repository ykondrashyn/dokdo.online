using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dokdo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dokdo.Tests.PlayMode
{
    public sealed class HarnessPlayModeTests : GameplayFixture
    {
        [UnityTest]
        public IEnumerator RuntimeTransitionsAndLifecyclePreserveFreshDefaults()
        {
            yield return LoadCanonicalScene(SceneIds.Load);

            EnableHeadlessRenderingGuard();
            yield return WaitForActiveScene(SceneIds.Intro);
            DisableHeadlessRenderingGuard();

            var gameManager = FindRequiredComponent("GameManager");
            Assert.That(ReadBoolField(gameManager, "noAds"), Is.True);
            Assert.That(ReadBoolField(gameManager, "pickboat"), Is.True);
            var gameManagerId = gameManager.GetInstanceID();

            var autoFade = FindRequiredComponent("AutoFade");
            EnableHeadlessRenderingGuard();
            InvokeInstanceMethod(autoFade, "StartGame");
            yield return WaitForActiveScene(SceneIds.Main);
            DisableHeadlessRenderingGuard();

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneIds.Main));
            Assert.That(FindRequiredComponent("GameManager").GetInstanceID(),
                Is.EqualTo(gameManagerId));

            var shipController = FindRequiredComponent("ShipController");
            InvokeInstanceMethod(shipController, "OnApplicationPause", true);
            InvokeInstanceMethod(shipController, "OnApplicationPause", false);
            Assert.That(((UnityEngine.MonoBehaviour)shipController)
                .IsInvoking("ReCalTime"), Is.True);
            ((UnityEngine.MonoBehaviour)shipController).CancelInvoke("ReCalTime");
            InvokeInstanceMethod(shipController, "OnApplicationQuit");

            WriteBoolField(gameManager, "noAds", false);
            InvokeInstanceMethod(gameManager, "Load");
            Assert.That(ReadBoolField(gameManager, "noAds"), Is.True);
        }

        [UnityTest]
        public IEnumerator CoreGameplayLoopRemainsBoundedAndOperable()
        {
            yield return LoadCanonicalScene(SceneIds.Main);

            var gameManager = FindRequiredComponent("GameManager");
            var ship = FindRequiredComponent("ShipController");
            var rigidbody = ReadField<Rigidbody>(ship, "rigid");
            var shipTransform = ReadField<Transform>(ship, "myShip");
            var controller = ReadField<Transform>(ship, "controller");

            WriteField(ship, "isParking", false);
            WriteField(ship, "isDead", false);
            WriteField(ship, "isTouch", true);
            controller.localEulerAngles = new Vector3(0f, 0f, 45f);
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            InvokeInstanceMethod(ship, "FixedUpdate");
            Assert.That(rigidbody.GetAccumulatedForce(
                    Time.fixedDeltaTime).sqrMagnitude,
                Is.GreaterThan(0.0001f), "ship did not accelerate");
            Assert.That(Mathf.Abs(rigidbody.GetAccumulatedTorque(
                    Time.fixedDeltaTime).y),
                Is.GreaterThan(0.0001f), "ship did not turn");

            InvokeInstanceMethod(ship, "outTouch");
            InvokeInstanceMethod(ship, "onTouch");
            InvokeInstanceMethod(ship, "outTouch");
            InvokeInstanceMethod(ship, "onTouch");
            Assert.That(ReadField<bool>(ship, "isDouble"), Is.True,
                "double-tap travel did not engage");

            InvokeInstanceMethod(ship, "Parking", 0);
            Assert.That(ReadField<bool>(ship, "isParking"), Is.True);
            Assert.That(rigidbody.linearVelocity, Is.EqualTo(Vector3.zero));
            WriteField(ship, "isParking", false);
            WriteField(ship, "isFishing", true);
            ReadField<List<Transform>>(ship, "NearEnemys").Clear();
            InvokeInstanceMethod(ship, "FixedUpdate");
            Assert.That(ReadField<GameObject>(ship, "ShowFishing").activeSelf,
                Is.True, "fishing state did not surface");

            var bulletPool = ReadField<Component>(ship, "bulletPool");
            InvokeInstanceMethod(ship, "ShootEach",
                shipTransform.position + Vector3.up,
                Quaternion.Euler(-20f, 0f, 0f));
            var activeBullets = (int)InvokeInstanceMethodWithResult(
                bulletPool, "ActiveObjectCount");
            var bulletObjects = ReadField<List<GameObject>>(
                bulletPool, "ObjectList");
            var bulletMaximum = (int)ReadProperty(
                bulletPool, "EffectiveMaxPoolSize");
            Assert.That(activeBullets, Is.GreaterThan(0));
            Assert.That(bulletObjects.Count, Is.LessThanOrEqualTo(bulletMaximum));

            var enemyPool = FindRequiredComponents("EZObjectPools.EZObjectPool")
                .Single(component =>
                    ReadField<string>(component, "PoolName") == "enemys");
            var checkout = enemyPool.GetType().GetMethods(
                    BindingFlags.Instance | BindingFlags.Public)
                .Single(method => method.Name == "TryGetNextObject" &&
                    method.ReturnType == typeof(bool));
            var checkoutArguments = new object[]
            {
                shipTransform.position + shipTransform.forward * 10f,
                Quaternion.identity,
                null
            };
            Assert.That((bool)checkout.Invoke(enemyPool, checkoutArguments),
                Is.True, "enemy pool checkout failed");
            var enemyObject = (GameObject)checkoutArguments[2];
            var enemy = enemyObject.GetComponent(
                enemyObject.GetComponents<Component>()
                    .Single(component =>
                        component.GetType().Name == "Enemy_Ship")
                    .GetType());
            WriteField(enemy, "isPirate", false);
            InvokeInstanceMethod(enemy, "SetHome", 0);
            InvokeInstanceMethod(enemy, "init", 0);
            enemy.transform.position = shipTransform.position +
                shipTransform.forward * 10f;
            InvokeInstanceMethod(enemy, "FixedUpdate");
            Assert.That(ReadField<bool>(enemy, "LockOn"), Is.True,
                "enemy did not pursue in detection range");

            var shooter = FindRequiredComponents("Shooter")
                .First(component => component.gameObject.activeInHierarchy &&
                    ReadField<Component>(component, "SC") != null);
            var parent = ReadField<Transform>(shooter, "ParentTrans");
            var head = ReadField<Transform>(enemy, "HEAD");
            enemy.transform.position = parent.position + parent.forward * 10f;
            ReadField<List<Transform>>(ship, "NearEnemys").Add(head);
            WriteField(shooter, "bulletLocalT", 0f);
            WriteField(shooter, "isTarget", true);
            InvokeInstanceMethod(shooter, "FixedUpdate");
            Assert.That(ReadField<float>(shooter, "bulletLocalT"),
                Is.EqualTo(2.8f).Within(0.0001f),
                "cannon reload interval differs");
            ((MonoBehaviour)shooter).CancelInvoke();

            enemy.transform.position = shipTransform.position +
                shipTransform.forward * 500f;
            InvokeInstanceMethod(enemy, "FixedUpdate");
            Assert.That(ReadField<bool>(enemy, "LockOn"), Is.False,
                "enemy did not leave detection range");
            enemyObject.SetActive(false);
            ReadField<List<Transform>>(ship, "NearEnemys").Clear();

            var islands = ReadField<bool[]>(gameManager, "IslandDone");
            islands[1] = false;
            InvokeInstanceMethod(gameManager, "GetIsland", 1);
            Assert.That(islands[1], Is.True, "island capture did not persist");

            var assistantObjects = ReadField<GameObject[]>(ship, "AssiShips");
            Assert.That(assistantObjects, Has.Length.EqualTo(3));
            InvokeInstanceMethod(ship, "SetAssi");

            InvokeInstanceMethod(gameManager, "FullHP");
            InvokeInstanceMethod(ship, "Hit", int.MaxValue);
            Assert.That(ReadField<bool>(ship, "isDead"), Is.True,
                "death state did not engage");
            InvokeInstanceMethod(ship, "Restart");
            Assert.That(ReadField<bool>(ship, "isDead"), Is.False,
                "revival did not restore the ship");

            var arenaDoor = FindRequiredComponents("ArenaDoor")
                .First(component => component.gameObject.activeInHierarchy);
            var arenaTrigger = FindRequiredComponents("Arena_trigger")
                .First(component => component.gameObject.activeInHierarchy &&
                    ReadField<Component>(component, "AD") == arenaDoor);
            WriteField(arenaDoor, "EnterPos",
                ReadField<Transform>(arenaTrigger, "enterPos"));
            InvokeInstanceMethod(arenaDoor, "Enter");
            yield return null;
            Assert.That(ReadField<bool>(ship, "isArena"), Is.True,
                "arena entry did not engage");
        }
    }
}
