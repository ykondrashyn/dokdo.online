using NUnit.Framework;

namespace Dokdo.Web.Tests
{
    public sealed class BrowserPerformanceSettingsTests
    {
        [Test]
        public void TimingRetainsRecoveredPresentationAndPhysics()
        {
            Assert.That(BrowserPerformanceSettings.TargetFrameRate,
                Is.EqualTo(60));
            Assert.That(BrowserPerformanceSettings.FixedTimestep,
                Is.EqualTo(0.02f));
            Assert.That(BrowserPerformanceSettings.MaximumWasmHeapMiB,
                Is.EqualTo(1024));
        }

        [Test]
        public void CoarsePointerSelectsSimpleWaterFallback()
        {
            Assert.That(
                BrowserPerformanceSettings.ShouldDisablePlanarReflection(
                    true),
                Is.True);
            Assert.That(
                BrowserPerformanceSettings.ShouldDisablePlanarReflection(
                    false),
                Is.False);
        }
    }
}
