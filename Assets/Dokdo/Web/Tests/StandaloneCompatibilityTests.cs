using System;
using System.IO;
using System.Text.RegularExpressions;
using CodeStage.AntiCheat.ObscuredTypes;
using NUnit.Framework;
using UnityEngine;

namespace Dokdo.Web.Tests
{
    public sealed class StandaloneCompatibilityTests
    {
        [Serializable] private class Values { public ObscuredInt integer; public ObscuredFloat number; }

        [TestCase(0)] [TestCase(-7)] [TestCase(444444)] [TestCase(int.MaxValue)]
        public void IntegerPreservesSerializedBits(int value)
        {
            var json = "{\"integer\":{\"currentCryptoKey\":137,\"hiddenValue\":" + (value ^ 137) + ",\"inited\":true}}";
            var data = JsonUtility.FromJson<Values>(json);
            Assert.That((int)data.integer, Is.EqualTo(value));
            Assert.That((int)JsonUtility.FromJson<Values>(JsonUtility.ToJson(data)).integer, Is.EqualTo(value));
        }

        [TestCase(0f)] [TestCase(-1.25f)] [TestCase(60.5f)]
        public void FloatPreservesSerializedBits(float value)
        {
            int bits = BitConverter.SingleToInt32Bits(value) ^ 23;
            var json = "{\"number\":{\"currentCryptoKey\":23,\"hiddenValue\":{\"b1\":" + (byte)bits +
                ",\"b2\":" + (byte)(bits >> 8) + ",\"b3\":" + (byte)(bits >> 16) +
                ",\"b4\":" + (byte)(bits >> 24) + "},\"inited\":true}}";
            var data = JsonUtility.FromJson<Values>(json);
            Assert.That((float)data.number, Is.EqualTo(value));
            Assert.That((float)JsonUtility.FromJson<Values>(JsonUtility.ToJson(data)).number, Is.EqualTo(value));
        }

        [Test]
        public void ArithmeticAndDefaultValuesRetainGameplaySemantics()
        {
            ObscuredInt count = 12; count++; count -= 4;
            ObscuredFloat speed = 1.5f; speed *= 2; speed--;
            Assert.That((int)count, Is.EqualTo(9));
            Assert.That((float)speed, Is.EqualTo(2));
            Assert.That((int)default(ObscuredInt), Is.Zero);
            Assert.That((float)default(ObscuredFloat), Is.Zero);
        }

        [Test]
        public void StandaloneHasNoServiceRuntimeInitializers()
        {
            var assembly = typeof(BrowserPlatformBridge).Assembly;
            foreach (var type in assembly.GetTypes())
                Assert.That(type.Name, Does.Not.Match("Presence|Statistics|TreasureHunt|Promo"));
        }
    }
}
