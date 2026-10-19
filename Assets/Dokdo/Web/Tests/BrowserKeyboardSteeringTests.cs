using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Dokdo.Web.Tests
{
    public sealed class BrowserKeyboardSteeringTests
    {
        private const BindingFlags Fields = BindingFlags.Instance |
            BindingFlags.Public | BindingFlags.NonPublic;
        private GameObject root;
        private Component ship;
        private BrowserKeyboardSteeringRuntime runtime;
        private Transform camera, steering;
        private Rigidbody body;
        private float timeScale;
        private readonly List<bool> availability = new();

        private T Read<T>(string name) => (T)ship.GetType().GetField(name, Fields).GetValue(ship);
        private void Write(string name, object value) => ship.GetType().GetField(name, Fields).SetValue(ship, value);
        private void Tick(string name) => typeof(BrowserKeyboardSteeringRuntime)
            .GetMethod(name, Fields).Invoke(runtime, null);
        private GameObject Child(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform);
            return child;
        }

        [SetUp]
        public void SetUp()
        {
            timeScale = Time.timeScale;
            Time.timeScale = 1;
            root = new GameObject("Keyboard steering test");
            // Use the actual recovered types and field names, without running
            // their scene startup or changing the recovered source assembly.
            ship = root.AddComponent(Type.GetType("ShipController, Assembly-CSharp", true));
            steering = Child("controller").transform;
            Write("controller", steering);
            Write("mousePos", Child("indicator").transform);
            Write("motorForce", 750f);
            Write("isParking", false);
            Write("isDead", false);
            Write("isOuting", false);
            body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            camera = Child("camera").transform;
            camera.gameObject.AddComponent<Camera>();
            camera.gameObject.AddComponent(Type.GetType("CameraZoom, Assembly-CSharp", true));
            runtime = root.AddComponent<BrowserKeyboardSteeringRuntime>();
            availability.Clear();
            runtime.Configure(availability.Add);
            runtime.Refresh();
            Tick("Update");
            Assert.That(availability[availability.Count - 1], Is.True, "required reflected bindings resolve");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Time.timeScale = timeScale;
        }

        [TestCase(1, 0)]
        [TestCase(9, 45)]
        [TestCase(8, 90)]
        [TestCase(12, 135)]
        [TestCase(4, 180)]
        [TestCase(6, -135)]
        [TestCase(2, -90)]
        [TestCase(3, -45)]
        public void EightHeadingsFollowCameraWithEqualManualPower(int mask, float heading)
        {
            camera.rotation = Quaternion.Euler(65, 32, 0);
            runtime.Receive(mask.ToString());
            // The exact conversion used by the recovered FixedUpdate.
            var target = Quaternion.AngleAxis(steering.localEulerAngles.z + 90, Vector3.down);
            Assert.That(Mathf.DeltaAngle(target.eulerAngles.y, heading + 32), Is.EqualTo(0).Within(.001f));
            Assert.That(BrowserKeyboardSteering.Direction(mask).magnitude, Is.EqualTo(1).Within(.00001f));
            Assert.That(Read<bool>("isTouch"), Is.True);
            Assert.That(Read<float>("motorForce"), Is.EqualTo(750f));
        }

        [TestCase(5)]
        [TestCase(10)]
        [TestCase(15)]
        public void OppositesReleaseEngineWhileRetainingKeyOwnership(int mask)
        {
            runtime.Receive("1");
            Write("v", 750f);
            runtime.Receive(mask.ToString());
            Assert.That(runtime.OwnsSteering, Is.True);
            Assert.That(Read<bool>("isTouch"), Is.False);
            Assert.That(Read<float>("v"), Is.Zero);
            Assert.That(BrowserKeyboardSteering.Direction(mask), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void AcquisitionCancelsAutoSailAndReleaseLeavesMomentumAlone()
        {
            Write("isDouble", true);
            Write("doubleclickT", .4f);
            body.linearVelocity = new Vector3(1, 0, 4);
            runtime.Receive("1");
            Assert.That(Read<bool>("isDouble"), Is.False);
            Assert.That(Read<float>("doubleclickT"), Is.Zero);
            Write("v", 750f);
            runtime.Receive("0");
            Assert.That(Read<bool>("isTouch"), Is.False);
            Assert.That(Read<float>("v"), Is.Zero);
            Assert.That(body.linearVelocity, Is.EqualTo(new Vector3(1, 0, 4)));
            // A subsequent mouse press owns these fields. Later zeroes and
            // lifecycle cancellation must not overwrite it.
            Write("isTouch", true);
            Write("isDouble", true);
            Write("v", 750f);
            runtime.Receive("0");
            runtime.SetLifecycleBlocked(true);
            Assert.That(Read<bool>("isTouch"), Is.True);
            Assert.That(Read<bool>("isDouble"), Is.True);
            Assert.That(Read<float>("v"), Is.EqualTo(750f));
        }

        [TestCase("isParking")]
        [TestCase("isDead")]
        [TestCase("isOuting")]
        public void ShipTransitionsCannotResumeStaleKeys(string field)
        {
            runtime.Receive("9");
            Write(field, true);
            Tick("Update");
            Assert.That(runtime.OwnsSteering, Is.False);
            Assert.That(availability[availability.Count - 1], Is.False);
            Write(field, false);
            Tick("FixedUpdate");
            Assert.That(Read<bool>("isTouch"), Is.False);
            runtime.Receive("2");
            Assert.That(Read<bool>("isTouch"), Is.True);
        }

        [Test]
        public void PauseLifecycleAndSceneRefreshRequireFreshState()
        {
            foreach (var reason in new[] {"pause", "lifecycle", "scene", "focus", "disabled"})
            {
                runtime.Receive("1");
                switch (reason)
                {
                    case "pause": Time.timeScale = 0; Tick("Update"); Time.timeScale = 1; break;
                    case "lifecycle": runtime.SetLifecycleBlocked(true); runtime.SetLifecycleBlocked(false); break;
                    case "scene": runtime.Refresh(); break;
                    case "focus":
                        typeof(BrowserKeyboardSteeringRuntime).GetMethod("OnApplicationFocus", Fields)
                            .Invoke(runtime, new object[] {false});
                        typeof(BrowserKeyboardSteeringRuntime).GetMethod("OnApplicationFocus", Fields)
                            .Invoke(runtime, new object[] {true});
                        break;
                    case "disabled": runtime.enabled = false; Tick("OnDisable"); runtime.enabled = true; break;
                }
                Tick("FixedUpdate");
                Assert.That(runtime.OwnsSteering, Is.False, reason);
                Assert.That(Read<bool>("isTouch"), Is.False, reason);
            }
        }

        [Test]
        public void PopupAndUnityTextEntryCancelSteering()
        {
            var popupType = Type.GetType("OfflinePopupView, Assembly-CSharp", true);
            var popup = Child("popup host").AddComponent(popupType);
            var instance = popupType.GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = instance.GetValue(null);
            try
            {
                instance.SetValue(null, popup);
                foreach (var field in new[] {"popupRoot", "preloaderRoot"})
                {
                    runtime.Receive("1");
                    var panel = Child(field);
                    popupType.GetField(field, Fields).SetValue(popup, panel);
                    Tick("Update");
                    Assert.That(runtime.OwnsSteering, Is.False, field);
                    Object.DestroyImmediate(panel);
                    Tick("FixedUpdate");
                    Assert.That(Read<bool>("isTouch"), Is.False, field);
                }
                var events = Child("event system").AddComponent<EventSystem>();
                var input = Child("input").AddComponent<InputField>();
                typeof(EventSystem).GetMethod("OnEnable", Fields).Invoke(events, null);
                EventSystem.current = events;
                events.SetSelectedGameObject(input.gameObject);
                // EditMode does not run the input module's activation loop.
                typeof(InputField).GetField("m_AllowInput", Fields).SetValue(input, true);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(input.gameObject));
                Assert.That(input.isFocused, Is.True);
                runtime.Receive("1");
                Assert.That(runtime.OwnsSteering, Is.False, "focused Unity input");
                typeof(EventSystem).GetMethod("OnDisable", Fields).Invoke(events, null);
            }
            finally { instance.SetValue(null, previous); }
        }

        [Test]
        public void RewardPanelBlocksBeforeItsPauseAnimationCompletes()
        {
            var panel = Child("reward");
            panel.SetActive(false);
            Write("IslandRewardPanel", panel);
            runtime.Refresh();
            runtime.Receive("1");
            panel.SetActive(true);
            Tick("Update");
            Assert.That(runtime.OwnsSteering, Is.False);
            panel.SetActive(false);
            Tick("FixedUpdate");
            Assert.That(Read<bool>("isTouch"), Is.False);
        }

        [TestCase(20)]
        [TestCase(40)]
        [TestCase(60)]
        public void InputAndHandoffReachNextPhysicsStepWithoutPublicationTimer(int fps)
        {
            // Simulate render/physics scheduling, with the real adapter callbacks.
            var nextPhysics = 0d;
            for (var frame = 0; frame < fps; frame++)
            {
                runtime.Receive(frame % 2 == 0 ? "1" : "8");
                Tick("Update");
                while (nextPhysics <= (frame + 1d) / fps)
                {
                    Tick("FixedUpdate");
                    Assert.That(Read<bool>("isTouch"), Is.True);
                    nextPhysics += .02;
                }
                runtime.Receive("0");
                Write("isTouch", true); // pointer takeover before its next step
                Tick("FixedUpdate");
                Assert.That(Read<bool>("isTouch"), Is.True);
                Write("isTouch", false);
            }
            Assert.That(MonoImporter.GetExecutionOrder(MonoScript.FromMonoBehaviour(runtime)), Is.LessThan(0));
        }

        [Test]
        public void MissingBindingDisablesAdapterAndIndicatorIsHiddenOnlyDuringKeyboardOwnership()
        {
            runtime.Receive("1");
            var indicator = Read<Transform>("mousePos").gameObject;
            indicator.SetActive(true); // recovered FixedUpdate shows it
            Tick("LateUpdate");
            Assert.That(indicator.activeSelf, Is.False);
            runtime.Receive("0");
            indicator.SetActive(true);
            Tick("LateUpdate");
            Assert.That(indicator.activeSelf, Is.True);
            Write("controller", null);
            runtime.Refresh();
            runtime.Receive("1");
            Assert.That(runtime.OwnsSteering, Is.False);
            Assert.That(availability[availability.Count - 1], Is.False);
        }
    }
}
