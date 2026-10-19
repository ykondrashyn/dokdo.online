using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EZObjectPools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dokdo.Editor
{
    public static class GameplayAudit
    {
        private const string MainScenePath =
            "Assets/0.my/Scenes/Main.unity";
        private const string EnemyPrefabPath =
            "Assets/GameObject/Enemy.prefab";

        [Serializable]
        private sealed class AuditReport
        {
            public int schemaVersion = 1;
            public string unityVersion;
            public Vector3 gravity;
            public float fixedTimestep;
            public int sceneRigidbodies;
            public int playerWheelColliders;
            public PlayerReport player;
            public EnemyReport enemyPrefab;
            public int islandCount;
            public int assistantCount;
            public int arenaDoorCount;
            public PoolReport[] pools;
            public string[] smokeChecks;
            public string[] problems;
        }

        [Serializable]
        private sealed class PlayerReport
        {
            public float motorForce;
            public float steerForce;
            public float shootPower;
            public float worldBoundary;
            public float islandPointerDistance;
            public float rigidbodyMass;
            public float rigidbodyLinearDamping;
            public float rigidbodyAngularDamping;
            public int rigidbodyConstraints;
        }

        [Serializable]
        private sealed class EnemyReport
        {
            public float attackRange;
            public float detectionRange;
            public float shootPower;
            public float motorForce;
            public float steerForce;
            public float rigidbodyMass;
            public float rigidbodyLinearDamping;
            public float rigidbodyAngularDamping;
            public int rigidbodyConstraints;
        }

        [Serializable]
        private sealed class PoolReport
        {
            public string name;
            public int initialSize;
            public bool autoResize;
            public int maximumSize;
        }

        public static void Run()
        {
            var problems = new List<string>();
            var smokeChecks = RunSmokeChecks(problems);
            var scene = EditorSceneManager.OpenScene(
                MainScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();

            var ships = Components<ShipController>(roots);
            Compare(problems, "player ship count", ships.Length, 1);
            var ship = ships.FirstOrDefault();
            var player = InspectPlayer(ship, problems);

            var pools = Components<EZObjectPool>(roots)
                .OrderBy(pool => pool.PoolName, StringComparer.Ordinal)
                .Select(pool => new PoolReport
                {
                    name = pool.PoolName,
                    initialSize = pool.PoolSize,
                    autoResize = pool.AutoResize,
                    maximumSize = pool.EffectiveMaxPoolSize
                })
                .ToArray();
            Compare(problems, "pool count", pools.Length, 11);
            foreach (var pool in pools)
            {
                if (pool.maximumSize < pool.initialSize)
                {
                    problems.Add("pool maximum is below initial size: " +
                        pool.name);
                }
                if (pool.maximumSize > 256)
                {
                    problems.Add("pool maximum exceeds hard ceiling: " +
                        pool.name);
                }
            }

            var report = new AuditReport
            {
                unityVersion = Application.unityVersion,
                gravity = Physics.gravity,
                fixedTimestep = Time.fixedDeltaTime,
                sceneRigidbodies = Components<Rigidbody>(roots).Length,
                playerWheelColliders = ship == null
                    ? 0
                    : ship.GetComponentsInChildren<WheelCollider>(true).Length,
                player = player,
                enemyPrefab = InspectEnemyPrefab(problems),
                islandCount = Components<IslandChecker>(roots).Length,
                assistantCount = Components<Assistant>(roots).Length,
                arenaDoorCount = Components<ArenaDoor>(roots).Length,
                pools = pools,
                smokeChecks = smokeChecks,
                problems = problems.OrderBy(
                        problem => problem, StringComparer.Ordinal)
                    .ToArray()
            };

            Compare(problems, "scene Rigidbody count", report.sceneRigidbodies,
                6);
            Compare(problems, "player WheelCollider count",
                report.playerWheelColliders, 4);
            Compare(problems, "island count", report.islandCount, 47);
            Compare(problems, "assistant count", report.assistantCount, 3);
            Compare(problems, "arena door count", report.arenaDoorCount, 2);
            if (Physics.gravity != new Vector3(0f, -9.81f, 0f))
            {
                problems.Add("3D gravity differs");
            }
            if (Mathf.Abs(Time.fixedDeltaTime - 0.02f) > 0.0001f)
            {
                problems.Add("fixed timestep differs");
            }

            report.problems = problems.OrderBy(
                    problem => problem, StringComparer.Ordinal)
                .ToArray();
            WriteReport(report);
            Debug.Log(string.Format(
                "DOKDO_GAMEPLAY_AUDIT rigidbodies={0} wheels={1} pools={2} " +
                "smoke={3} problems={4}", report.sceneRigidbodies,
                report.playerWheelColliders, pools.Length, smokeChecks.Length,
                problems.Count));
            if (problems.Count != 0)
            {
                throw new InvalidOperationException(
                    "DOKDO gameplay audit failed:\n" +
                    string.Join("\n", problems));
            }
        }

        private static PlayerReport InspectPlayer(ShipController ship,
            ICollection<string> problems)
        {
            if (ship == null)
            {
                problems.Add("player ship is missing");
                return new PlayerReport();
            }
            CompareFloat(problems, "player serialized motor force",
                ship.motorForce, 525f);
            CompareFloat(problems, "player serialized steer force",
                ship.steerForce, 370f);
            CompareFloat(problems, "player shoot power", ship.ShootPower,
                1200f);
            CompareFloat(problems, "world boundary", ship.OverWorld, 2000f);
            CompareFloat(problems, "island pointer distance",
                ship.ShowIslandPointLimit, 80f);
            if (ship.rigid == null)
            {
                problems.Add("player Rigidbody reference is missing");
                return new PlayerReport
                {
                    motorForce = ship.motorForce,
                    steerForce = ship.steerForce,
                    shootPower = ship.ShootPower,
                    worldBoundary = ship.OverWorld,
                    islandPointerDistance = ship.ShowIslandPointLimit
                };
            }
            CompareFloat(problems, "player Rigidbody mass", ship.rigid.mass,
                1000f);
            CompareFloat(problems, "player Rigidbody damping",
                ship.rigid.linearDamping, 2f);
            CompareFloat(problems, "player Rigidbody angular damping",
                ship.rigid.angularDamping, 1f);
            Compare(problems, "player Rigidbody constraints",
                (int)ship.rigid.constraints, 84);
            return new PlayerReport
            {
                motorForce = ship.motorForce,
                steerForce = ship.steerForce,
                shootPower = ship.ShootPower,
                worldBoundary = ship.OverWorld,
                islandPointerDistance = ship.ShowIslandPointLimit,
                rigidbodyMass = ship.rigid.mass,
                rigidbodyLinearDamping = ship.rigid.linearDamping,
                rigidbodyAngularDamping = ship.rigid.angularDamping,
                rigidbodyConstraints = (int)ship.rigid.constraints
            };
        }

        private static EnemyReport InspectEnemyPrefab(
            ICollection<string> problems)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                EnemyPrefabPath);
            var enemy = prefab == null ? null : prefab.GetComponent<Enemy_Ship>();
            var rigidbody = prefab == null ? null : prefab.GetComponent<Rigidbody>();
            if (enemy == null || rigidbody == null)
            {
                problems.Add("enemy prefab gameplay components are missing");
                return new EnemyReport();
            }
            CompareFloat(problems, "enemy attack range", enemy.AtkRange, 15f);
            CompareFloat(problems, "enemy detection range", enemy.DetactRange,
                60f);
            CompareFloat(problems, "enemy shoot power", enemy.ShootPower,
                1000f);
            CompareFloat(problems, "enemy serialized motor force",
                enemy.motorForce, 550f);
            CompareFloat(problems, "enemy serialized steer force",
                enemy.steerForce, 70f);
            CompareFloat(problems, "enemy Rigidbody mass", rigidbody.mass,
                500f);
            CompareFloat(problems, "enemy Rigidbody damping",
                rigidbody.linearDamping, 3f);
            CompareFloat(problems, "enemy Rigidbody angular damping",
                rigidbody.angularDamping, 1f);
            Compare(problems, "enemy Rigidbody constraints",
                (int)rigidbody.constraints, 84);
            return new EnemyReport
            {
                attackRange = enemy.AtkRange,
                detectionRange = enemy.DetactRange,
                shootPower = enemy.ShootPower,
                motorForce = enemy.motorForce,
                steerForce = enemy.steerForce,
                rigidbodyMass = rigidbody.mass,
                rigidbodyLinearDamping = rigidbody.linearDamping,
                rigidbodyAngularDamping = rigidbody.angularDamping,
                rigidbodyConstraints = (int)rigidbody.constraints
            };
        }

        private static string[] RunSmokeChecks(ICollection<string> problems)
        {
            var passed = new List<string>();
            RunPhysicsSmoke(passed, problems);
            RunPoolSmoke(passed, problems);
            return passed.OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static void RunPhysicsSmoke(ICollection<string> passed,
            ICollection<string> problems)
        {
            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var previousSimulationMode = Physics.simulationMode;
            GameObject boat = null;
            GameObject bullet = null;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                boat = new GameObject("DOKDO movement probe");
                var rigidbody = boat.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.mass = 1000f;
                rigidbody.linearDamping = 2f;
                rigidbody.angularDamping = 1f;
                rigidbody.constraints = (RigidbodyConstraints)84;
                for (var index = 0; index < 25; index++)
                {
                    rigidbody.AddForce(boat.transform.forward * 525f * 10f);
                    rigidbody.AddTorque(Vector3.up * 37f * 5f);
                    Physics.Simulate(0.02f);
                }
                if (rigidbody.linearVelocity.sqrMagnitude > 0.0001f &&
                    Mathf.Abs(rigidbody.angularVelocity.y) > 0.01f &&
                    IsFinite(rigidbody.linearVelocity) &&
                    IsFinite(rigidbody.angularVelocity))
                {
                    passed.Add("acceleration and turning");
                }
                else
                {
                    problems.Add(string.Format(
                        "acceleration or turning smoke differs: linear={0}, " +
                        "angular={1}", rigidbody.linearVelocity,
                        rigidbody.angularVelocity));
                }

                bullet = new GameObject("DOKDO cannon probe");
                bullet.transform.position = new Vector3(0f, 1f, 0f);
                bullet.transform.rotation = Quaternion.Euler(-20f, 0f, 0f);
                var bulletBody = bullet.AddComponent<Rigidbody>();
                bulletBody.mass = 1f;
                bulletBody.linearDamping = 0f;
                bulletBody.angularDamping = 0.05f;
                bulletBody.AddForce(bullet.transform.forward * 1200f);
                var steps = 0;
                do
                {
                    Physics.Simulate(0.02f);
                    steps++;
                }
                while (bullet.transform.position.y > 0f && steps < 500);
                var range = Mathf.Abs(bullet.transform.position.z);
                if (steps > 1 && steps < 500 && range > 20f && range < 100f)
                {
                    passed.Add("cannon ballistic range");
                }
                else
                {
                    problems.Add(string.Format(
                        "cannon ballistic range differs: {0:F3}", range));
                }
            }
            catch (Exception exception)
            {
                problems.Add("physics smoke failed: " +
                    exception.GetType().Name + ": " + exception.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(boat);
                UnityEngine.Object.DestroyImmediate(bullet);
                Physics.simulationMode = previousSimulationMode;
            }
        }

        private static void RunPoolSmoke(ICollection<string> passed,
            ICollection<string> problems)
        {
            GameObject template = null;
            EZObjectPool pool = null;
            try
            {
                template = new GameObject("DOKDO pool template");
                pool = EZObjectPool.CreateObjectPool(template,
                    "DOKDO bounded pool audit", 2, true, true, false);
                pool.MaxPoolSize = 3;
                var checkedOut = new List<GameObject>();
                for (var index = 0; index < 3; index++)
                {
                    if (!pool.TryGetNextObject(Vector3.zero,
                        Quaternion.identity, out var item))
                    {
                        problems.Add(string.Format(
                            "bounded pool exhausted at {0}: objects={1}, " +
                            "available={2}, maximum={3}", index,
                            pool.ObjectList.Count, pool.AvailableObjectCount(),
                            pool.EffectiveMaxPoolSize));
                        return;
                    }
                    checkedOut.Add(item);
                }
                var rejected = !pool.TryGetNextObject(Vector3.zero,
                    Quaternion.identity, out var overflow);
                if (rejected && overflow == null && pool.ObjectList.Count == 3)
                {
                    passed.Add("bounded pool exhaustion");
                }
                else
                {
                    problems.Add("bounded pool ceiling failed");
                }

                checkedOut[0].SetActive(false);
                pool.AddToAvailableObjects(checkedOut[0]);
                var reused = pool.TryGetNextObject(Vector3.one,
                    Quaternion.identity, out var recycled);
                if (reused && recycled == checkedOut[0] &&
                    pool.ObjectList.Count == 3)
                {
                    passed.Add("pool return and reuse");
                }
                else
                {
                    problems.Add("pool return and reuse failed");
                }
            }
            catch (Exception exception)
            {
                problems.Add("pool smoke failed: " +
                    exception.GetType().Name + ": " + exception.Message);
            }
            finally
            {
                if (pool != null)
                {
                    UnityEngine.Object.DestroyImmediate(pool.gameObject);
                }
                UnityEngine.Object.DestroyImmediate(template);
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static T[] Components<T>(IEnumerable<GameObject> roots)
            where T : Component
        {
            return roots.SelectMany(
                    root => root.GetComponentsInChildren<T>(true))
                .ToArray();
        }

        private static void Compare(ICollection<string> problems, string name,
            int actual, int expected)
        {
            if (actual != expected)
            {
                problems.Add(string.Format(
                    "{0} differs: expected {1}, got {2}", name, expected,
                    actual));
            }
        }

        private static void CompareFloat(ICollection<string> problems,
            string name, float actual, float expected)
        {
            if (Mathf.Abs(actual - expected) > 0.0001f)
            {
                problems.Add(string.Format(
                    "{0} differs: expected {1}, got {2}", name, expected,
                    actual));
            }
        }

        private static void WriteReport(AuditReport report)
        {
            var outputPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "../.artifacts/unity-gameplay-audit.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath,
                JsonUtility.ToJson(report, true) + "\n");
        }
    }
}
