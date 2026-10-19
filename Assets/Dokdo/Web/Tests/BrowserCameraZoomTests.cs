using NUnit.Framework;

namespace Dokdo.Web.Tests
{
    public sealed class BrowserCameraZoomTests
    {
        [Test]
        public void WheelUsesRecoveredPinchDirectionAndSpeed()
        {
            Assert.That(BrowserCameraZoom.CalculateOrthographicSize(
                15f, -100f, 0.03f, 3f, 40f),
                Is.EqualTo(12f).Within(0.0001f));
            Assert.That(BrowserCameraZoom.CalculateOrthographicSize(
                15f, 100f, 0.03f, 3f, 40f),
                Is.EqualTo(18f).Within(0.0001f));
        }

        [Test]
        public void WheelUsesRecoveredPinchLimits()
        {
            Assert.That(BrowserCameraZoom.CalculateOrthographicSize(
                4f, -100f, 0.03f, 3f, 40f), Is.EqualTo(3f));
            Assert.That(BrowserCameraZoom.CalculateOrthographicSize(
                39f, 100f, 0.03f, 3f, 40f), Is.EqualTo(40f));
        }

        [Test]
        public void OneWheelEventHasABoundedPinchEquivalent()
        {
            Assert.That(BrowserCameraZoom.CalculateOrthographicSize(
                15f, 1000f, 0.03f, 3f, 40f), Is.EqualTo(18f));
            Assert.That(BrowserCameraZoom.CalculateOrthographicSize(
                15f, -1000f, 0.03f, 3f, 40f), Is.EqualTo(12f));
        }

        [Test]
        public void InvalidWheelInputDoesNotChangeZoom()
        {
            Assert.That(BrowserCameraZoom.CalculateOrthographicSize(
                15f, float.NaN, 0.03f, 3f, 40f), Is.EqualTo(15f));
            Assert.That(BrowserCameraZoom.CalculateOrthographicSize(
                15f, 100f, -0.03f, 3f, 40f), Is.EqualTo(15f));
            Assert.That(BrowserCameraZoom.CalculateOrthographicSize(
                15f, 100f, 0.03f, 40f, 3f), Is.EqualTo(15f));
        }
    }
}
