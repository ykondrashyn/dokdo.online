// Serialization compatibility only. These adapters provide no anti-cheat service.
using System;
using UnityEngine;

namespace CodeStage.AntiCheat.ObscuredTypes
{
    [Serializable]
    public struct ObscuredInt : IEquatable<ObscuredInt>, IFormattable
    {
        [SerializeField] private int currentCryptoKey;
        [SerializeField] private int hiddenValue;
        [SerializeField] private bool inited;
        [SerializeField] private int fakeValue;
        [SerializeField] private bool fakeValueActive;
        private int Value => inited ? hiddenValue ^ currentCryptoKey : 0;
        public static implicit operator int(ObscuredInt value) => value.Value;
        public static implicit operator ObscuredInt(int value) => new ObscuredInt {
            currentCryptoKey = 444444, hiddenValue = value ^ 444444,
            inited = true, fakeValue = value, fakeValueActive = false
        };
        public static ObscuredInt operator ++(ObscuredInt value) => unchecked(value.Value + 1);
        public static ObscuredInt operator --(ObscuredInt value) => unchecked(value.Value - 1);
        public bool Equals(ObscuredInt other) => Value == other.Value;
        public override bool Equals(object other) => other is ObscuredInt value && Equals(value);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public string ToString(string format) => Value.ToString(format);
        public string ToString(IFormatProvider provider) => Value.ToString(provider);
        public string ToString(string format, IFormatProvider provider) => Value.ToString(format, provider);
    }

    [Serializable]
    public struct ObscuredFloat : IEquatable<ObscuredFloat>, IFormattable
    {
        [Serializable]
        private struct Bytes { public byte b1, b2, b3, b4; }
        [SerializeField] private int currentCryptoKey;
        [SerializeField] private Bytes hiddenValue;
        [SerializeField] private byte[] hiddenValueOld;
        [SerializeField] private bool inited;
        [SerializeField] private float fakeValue;
        [SerializeField] private bool fakeValueActive;
        private float Value {
            get {
                if (!inited) return 0;
                // The recovered Unity serialization stores little-endian IEEE bits.
                int bits = hiddenValue.b1 | hiddenValue.b2 << 8 |
                    hiddenValue.b3 << 16 | hiddenValue.b4 << 24;
                return BitConverter.Int32BitsToSingle(bits ^ currentCryptoKey);
            }
        }
        public static implicit operator float(ObscuredFloat value) => value.Value;
        public static implicit operator ObscuredFloat(float value) {
            int bits = BitConverter.SingleToInt32Bits(value) ^ 230887;
            return new ObscuredFloat {
                currentCryptoKey = 230887, inited = true,
                hiddenValue = new Bytes { b1 = (byte)bits, b2 = (byte)(bits >> 8),
                    b3 = (byte)(bits >> 16), b4 = (byte)(bits >> 24) },
                hiddenValueOld = null, fakeValue = value, fakeValueActive = false
            };
        }
        public static ObscuredFloat operator ++(ObscuredFloat value) => value.Value + 1;
        public static ObscuredFloat operator --(ObscuredFloat value) => value.Value - 1;
        public bool Equals(ObscuredFloat other) => Value.Equals(other.Value);
        public override bool Equals(object other) => other is ObscuredFloat value && Equals(value);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public string ToString(string format) => Value.ToString(format);
        public string ToString(IFormatProvider provider) => Value.ToString(provider);
        public string ToString(string format, IFormatProvider provider) => Value.ToString(format, provider);
    }
}

namespace CodeStage.AntiCheat.Detectors
{
    public static class SpeedHackDetector
    {
        public static void StartDetection(Action callback) { }
    }
}
