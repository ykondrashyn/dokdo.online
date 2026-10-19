using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Dokdo.Web.Tests
{
    public sealed class BrowserLifecycleTests
    {
        [Test]
        public void TwentyHideResumeCyclesKeepRequiredOrdering()
        {
            var lifecycle = new BrowserLifecycleController();
            var events = new List<string>();

            for (var cycle = 0; cycle < 20; cycle++)
            {
                lifecycle.Hide(
                    () => events.Add("save"),
                    () => events.Add("pause"));
                lifecycle.Show(
                    () => events.Add("resume"),
                    () => events.Add("elapsed"));
            }

            Assert.That(events, Has.Count.EqualTo(80));
            Assert.That(lifecycle.BlockTransitionCount, Is.EqualTo(20));
            Assert.That(lifecycle.ResumeTransitionCount, Is.EqualTo(20));
            for (var index = 0; index < events.Count; index += 4)
            {
                CollectionAssert.AreEqual(
                    new[] { "save", "pause", "resume", "elapsed" },
                    events.GetRange(index, 4));
            }
            Assert.That(lifecycle.IsHidden, Is.False);
        }

        [Test]
        public void DuplicateLifecycleSignalsAreIdempotent()
        {
            var lifecycle = new BrowserLifecycleController();
            var saves = 0;
            var pauses = 0;
            var resumes = 0;
            var elapsed = 0;

            lifecycle.Hide(() => saves++, () => pauses++);
            lifecycle.Hide(() => saves++, () => pauses++);
            lifecycle.Show(() => resumes++, () => elapsed++);
            lifecycle.Show(() => resumes++, () => elapsed++);

            Assert.That(saves, Is.EqualTo(1));
            Assert.That(pauses, Is.EqualTo(1));
            Assert.That(resumes, Is.EqualTo(1));
            Assert.That(elapsed, Is.EqualTo(1));
        }

        [Test]
        public void VisibilityAndOrientationAreIndependentBlockers()
        {
            var lifecycle = new BrowserLifecycleController();
            var events = new List<string>();

            lifecycle.SetOrientationBlocked(true,
                () => events.Add("save"), () => events.Add("pause"),
                () => events.Add("resume"), () => events.Add("elapsed"));
            lifecycle.SetVisibilityBlocked(true,
                () => events.Add("save"), () => events.Add("pause"),
                () => events.Add("resume"), () => events.Add("elapsed"));
            lifecycle.SetOrientationBlocked(false,
                () => events.Add("save"), () => events.Add("pause"),
                () => events.Add("resume"), () => events.Add("elapsed"));

            CollectionAssert.AreEqual(
                new[] { "save", "pause" }, events);
            Assert.That(lifecycle.IsBlocked, Is.True);
            Assert.That(lifecycle.IsVisibilityBlocked, Is.True);
            Assert.That(lifecycle.IsOrientationBlocked, Is.False);

            lifecycle.SetVisibilityBlocked(false,
                () => events.Add("save"), () => events.Add("pause"),
                () => events.Add("resume"), () => events.Add("elapsed"));

            CollectionAssert.AreEqual(
                new[] { "save", "pause", "resume", "elapsed" }, events);
            Assert.That(lifecycle.IsBlocked, Is.False);
        }

        [Test]
        public void TwentyRotateAndBackgroundCyclesPauseAndResumeOnce()
        {
            var lifecycle = new BrowserLifecycleController();
            var events = new List<string>();
            void SetOrientation(bool value) =>
                lifecycle.SetOrientationBlocked(value,
                    () => events.Add("save"), () => events.Add("pause"),
                    () => events.Add("resume"), () => events.Add("elapsed"));
            void SetVisibility(bool value) =>
                lifecycle.SetVisibilityBlocked(value,
                    () => events.Add("save"), () => events.Add("pause"),
                    () => events.Add("resume"), () => events.Add("elapsed"));

            for (var cycle = 0; cycle < 20; cycle++)
            {
                SetOrientation(true);
                SetVisibility(true);
                SetOrientation(false);
                SetVisibility(false);
            }

            Assert.That(events, Has.Count.EqualTo(80));
            for (var index = 0; index < events.Count; index += 4)
            {
                CollectionAssert.AreEqual(
                    new[] { "save", "pause", "resume", "elapsed" },
                    events.GetRange(index, 4));
            }
        }

        [Test]
        public void JavaScriptUsesCacheFriendlyLifecycleEvents()
        {
            var source = File.ReadAllText(
                "Assets/Plugins/WebGL/DokdoBrowser.jslib") +
                File.ReadAllText(
                    "Assets/WebGLTemplates/DokdoStandalone/TemplateData/" +
                    "standalone.js");

            Assert.That(source, Does.Contain("visibilitychange"));
            Assert.That(source, Does.Contain("pagehide"));
            Assert.That(source, Does.Contain("OnOrientationBlocked"));
            Assert.That(source, Does.Not.Contain("beforeunload"));
            Assert.That(source, Does.Not.Contain("addEventListener(\"unload\""));
            Assert.That(source, Does.Not.Contain("execCommand"));
        }
    }
}
