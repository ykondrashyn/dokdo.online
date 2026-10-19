using System;
using System.IO;
using System.Linq;
using Dokdo;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Dokdo.Tests.EditMode
{
    public sealed class HarnessEditModeTests : FreshSaveFixture
    {
        [Test]
        public void SceneIdsMatchEnabledBuildOrder()
        {
            var paths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            CollectionAssert.AreEqual(SceneIds.BuildPaths, paths);
            CollectionAssert.AreEqual(
                new[] { "load", "Intro", "Main" },
                SceneIds.BuildOrder);
        }

        [Test]
        public void FixedClockAdvancesDeterministically()
        {
            var start = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            var clock = new FixedClockFixture(start);

            clock.Advance(TimeSpan.FromMinutes(10));

            Assert.That(clock.UtcNow, Is.EqualTo(start.AddMinutes(10)));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => clock.Advance(TimeSpan.FromSeconds(-1)));
            Assert.That(SaveTime.ElapsedSeconds(
                start.AddHours(-1), start, 600), Is.Zero);
            Assert.That(SaveTime.ElapsedSeconds(
                start.AddHours(12), start, 600), Is.EqualTo(600));
            Assert.That(SaveTime.ElapsedSeconds(
                start.AddDays(1), start, 43200), Is.EqualTo(43200));
        }

        [Test]
        public void SaveRoundTripUsesValidBackupAfterPrimaryCorruption()
        {
            var store = (JsonFileSaveStore)SaveRuntime.Store;
            var first = SaveDataV1.CreateFresh("English", SaveRuntime.Clock.UtcNow);
            first.resources[0] = 111;
            store.Save(first);

            var second = SaveDataV1.CreateFresh("English", SaveRuntime.Clock.UtcNow);
            second.resources[0] = 222;
            store.Save(second);
            File.WriteAllText(store.PrimaryPath, "corrupt");

            Assert.That(store.TryLoad(out var recovered, out var error), Is.True);
            Assert.That(recovered.resources[0], Is.EqualTo(111));
            Assert.That(error, Does.Contain("backup"));
            Assert.That(store.TryLoad(out var restored, out _), Is.True);
            Assert.That(restored.resources[0], Is.EqualTo(111));
        }

        [Test]
        public void SaveValidationRejectsBadArraysAndChecksums()
        {
            var store = (JsonFileSaveStore)SaveRuntime.Store;
            var data = SaveDataV1.CreateFresh("English", SaveRuntime.Clock.UtcNow);
            store.Save(data);
            var exported = store.Export();
            var tampered = exported.Replace("\"payload\": \"",
                "\"payload\": \"tampered");

            Assert.That(store.TryImport(tampered, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("checksum"));

            data.resources = new int[1];
            Assert.Throws<InvalidDataException>(() => store.Save(data));
        }

        [Test]
        public void SaveExportImportAndResetRoundTrip()
        {
            var source = (JsonFileSaveStore)SaveRuntime.Store;
            var data = SaveDataV1.CreateFresh("Japanese", SaveRuntime.Clock.UtcNow);
            data.revivalCount = 17;
            source.Save(data);
            var exported = source.Export();

            var importDirectory = Path.Combine(SaveDirectory, "import");
            var destination = new JsonFileSaveStore(importDirectory);
            Assert.That(destination.TryImport(exported, out var imported,
                out var error), Is.True, error);
            Assert.That(imported.language, Is.EqualTo("Japanese"));
            Assert.That(imported.revivalCount, Is.EqualTo(17));

            destination.Reset();
            Assert.That(File.Exists(destination.PrimaryPath), Is.False);
            Assert.That(File.Exists(destination.BackupPath), Is.False);
        }

        [Test]
        public void FreshSaveFixtureStartsWithoutPrefs()
        {
            Assert.That(PlayerPrefs.HasKey("dokdo-test-sentinel"), Is.False);
            PlayerPrefs.SetInt("dokdo-test-sentinel", 1);
        }

        [Test]
        public void TestAssembliesAreAbsentFromReleaseCompilation()
        {
            var releaseAssemblies = CompilationPipeline
                .GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Select(assembly => assembly.name)
                .ToArray();

            Assert.That(releaseAssemblies, Does.Contain("Dokdo.Runtime.Core"));
            Assert.That(
                releaseAssemblies.Any(name => name.StartsWith("Dokdo.Tests.")),
                Is.False);
        }

        [Test]
        public void ForcedFailurePropagationProbe()
        {
            if (Environment.GetEnvironmentVariable("DOKDO_FORCE_TEST_FAILURE") == "1")
            {
                Assert.Fail("DOKDO forced test failure probe");
            }

            Assert.Pass();
        }
    }
}
