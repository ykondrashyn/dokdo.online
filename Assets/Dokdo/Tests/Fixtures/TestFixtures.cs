using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Dokdo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dokdo.Tests
{
    public sealed class FixedClockFixture : IClock
    {
        public FixedClockFixture(DateTime utcNow)
        {
            if (utcNow.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("The fixed clock requires UTC.", nameof(utcNow));
            }

            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; private set; }

        public void Advance(TimeSpan elapsed)
        {
            if (elapsed < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsed));
            }

            UtcNow = UtcNow.Add(elapsed);
        }
    }

    public abstract class FreshSaveFixture
    {
        protected string SaveDirectory { get; private set; }

        [SetUp]
        public void ResetPrefsBeforeTest()
        {
            SaveRuntime.ResetDefaults();
            SaveDirectory = Path.Combine(Path.GetTempPath(),
                "dokdo-save-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(SaveDirectory);
            SaveRuntime.Store = new JsonFileSaveStore(SaveDirectory);
            SaveRuntime.Clock = new FixedClockFixture(
                new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        [TearDown]
        public void ResetPrefsAfterTest()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            SaveRuntime.ResetDefaults();
            if (Directory.Exists(SaveDirectory))
            {
                Directory.Delete(SaveDirectory, true);
            }
        }
    }

    public abstract class SceneLoadingFixture : FreshSaveFixture
    {
        protected static IEnumerator LoadCanonicalScene(string sceneId)
        {
            EnableHeadlessRenderingGuard();
            var operation = SceneManager.LoadSceneAsync(sceneId, LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null, $"Scene '{sceneId}' did not start loading.");

            while (!operation.isDone)
            {
                yield return null;
            }

            DisableHeadlessRenderingGuard();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneId));
        }

        protected static IEnumerator WaitForActiveScene(
            string sceneId,
            float timeoutSeconds = 5f)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (SceneManager.GetActiveScene().name != sceneId &&
                Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneId));
        }

        protected static void EnableHeadlessRenderingGuard()
        {
            SceneManager.sceneLoaded -= DisableRenderingForHeadlessTest;
            SceneManager.sceneLoaded += DisableRenderingForHeadlessTest;
        }

        protected static void DisableHeadlessRenderingGuard()
        {
            SceneManager.sceneLoaded -= DisableRenderingForHeadlessTest;
        }

        private static void DisableRenderingForHeadlessTest(
            Scene scene,
            LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                {
                    camera.enabled = false;
                }

                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    canvas.enabled = false;
                }

                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.enabled = false;
                }
            }
        }
    }

    public abstract class GameplayFixture : SceneLoadingFixture
    {
        protected static Component FindRequiredComponent(string typeName)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName, false))
                .FirstOrDefault(candidate => candidate != null);
            Assert.That(type, Is.Not.Null, $"Runtime type '{typeName}' was not loaded.");

            var component = Resources.FindObjectsOfTypeAll(type)
                .OfType<Component>()
                .FirstOrDefault(candidate => candidate.gameObject.scene.IsValid() ||
                    candidate.gameObject.scene.name == "DontDestroyOnLoad");
            Assert.That(component, Is.Not.Null, $"Component '{typeName}' was not found.");
            return component;
        }

        protected static bool ReadBoolField(Component component, string fieldName)
        {
            return ReadField<bool>(component, fieldName);
        }

        protected static T ReadField<T>(Component component, string fieldName)
        {
            var field = component.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null,
                $"Field '{component.GetType().Name}.{fieldName}' was not found.");
            return (T)field.GetValue(component);
        }

        protected static void WriteField<T>(Component component,
            string fieldName, T value)
        {
            var field = component.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null,
                $"Field '{component.GetType().Name}.{fieldName}' was not found.");
            field.SetValue(component, value);
        }

        protected static object ReadProperty(Component component,
            string propertyName)
        {
            var property = component.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null,
                $"Property '{component.GetType().Name}.{propertyName}' was not found.");
            return property.GetValue(component);
        }

        protected static void WriteBoolField(
            Component component,
            string fieldName,
            bool value)
        {
            var field = component.GetType().GetField(fieldName);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field.SetValue(component, value);
        }

        protected static Component[] FindRequiredComponents(string typeName)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName, false))
                .FirstOrDefault(candidate => candidate != null);
            Assert.That(type, Is.Not.Null,
                $"Runtime type '{typeName}' was not loaded.");
            var components = Resources.FindObjectsOfTypeAll(type)
                .OfType<Component>()
                .Where(candidate => candidate.gameObject.scene.IsValid() ||
                    candidate.gameObject.scene.name == "DontDestroyOnLoad")
                .ToArray();
            Assert.That(components, Is.Not.Empty,
                $"Components '{typeName}' were not found.");
            return components;
        }

        protected static object InvokeInstanceMethodWithResult(
            Component component,
            string methodName,
            params object[] arguments)
        {
            var method = component.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null,
                $"Method '{component.GetType().Name}.{methodName}' was not found.");
            return method.Invoke(component, arguments);
        }

        protected static void InvokeInstanceMethod(
            Component component,
            string methodName,
            params object[] arguments)
        {
            InvokeInstanceMethodWithResult(component, methodName, arguments);
        }
    }
}
