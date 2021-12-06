using BlackTundra.Foundation.Serialization;

using System;
using System.Runtime.InteropServices;

using UnityEngine;

namespace BlackTundra.Foundation.Utility {

    public interface IMinMaxable<T> where T : struct {

        #region property

#pragma warning disable IDE1006 // naming styles
        T min { get; }
        T max { get; }
        T range { get; }
#pragma warning restore IDE1006 // naming styles

        #endregion

        #region logic

        void Evaluate(in T value);

        #endregion

    }

    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 8, Pack = 1)]
    public struct MinMaxInteger : IMinMaxable<int> {

        #region varaible

        [SerializeField]
        [FieldOffset(0)]
        private int _min;

        [SerializeField]
        [FieldOffset(4)]
        private int _max;

        #endregion

        #region property

        public int min => _min;
        public int max => _max;
        public int range => _max - _min;

        #endregion

        #region constructor

        public MinMaxInteger(in int min = int.MaxValue, in int max = int.MinValue) {
            _min = min;
            _max = max;
        }

        #endregion

        #region logic

        public void Evaluate(in int value) {
            if (value < _min) _min = value;
            if (value > _max) _max = value;
        }

        #endregion

    }

    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 8, Pack = 1)]
    public struct MinMaxFloat : IMinMaxable<float> {

        #region varaible

        [SerializeField]
        [FieldOffset(0)]
        private float _min;

        [SerializeField]
        [FieldOffset(4)]
        private float _max;

        #endregion

        #region property

        public float min => _min;
        public float max => _max;
        public float range => _max - _min;

        #endregion

        #region constructor

        public MinMaxFloat(in float min = float.MaxValue, in float max = float.MinValue) {
            _min = min;
            _max = max;
        }

        #endregion

        #region logic

        public void Evaluate(in float value) {
            if (value < _min) _min = value;
            if (value > _max) _max = value;
        }

        #endregion

    }

    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 16, Pack = 1)]
    public sealed class MinMaxVector2 : IMinMaxable<Vector2> {

        #region varaible

        [SerializeField]
        [FieldOffset(0)]
        private SerializableVector2 _min;

        [SerializeField]
        [FieldOffset(8)]
        private SerializableVector2 _max;

        #endregion

        #region property

        public Vector2 min => (Vector2)_min;
        public Vector2 max => (Vector2)_max;
        public Vector2 range => (Vector2)(_max - _min);

        #endregion

        #region constructor

        public MinMaxVector2() {
            _min = new SerializableVector2(float.MaxValue, float.MaxValue);
            _max = new SerializableVector2(float.MinValue, float.MinValue);
        }

        public MinMaxVector2(in Vector2 min, in Vector2 max) {
            _min = new SerializableVector2(min);
            _max = new SerializableVector2(max);
        }

        #endregion

        #region logic

        public void Evaluate(in Vector2 value) {
            _min = new SerializableVector2(Mathf.Min(value.x, _min.x), Mathf.Min(value.y, _min.y));
            _max = new SerializableVector2(Mathf.Max(value.x, _max.x), Mathf.Max(value.y, _max.y));
        }

        #endregion

    }

    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 24, Pack = 1)]
    public sealed class MinMaxVector3 : IMinMaxable<Vector3> {

        #region varaible

        [SerializeField]
        [FieldOffset(0)]
        private SerializableVector3 _min;

        [SerializeField]
        [FieldOffset(12)]
        private SerializableVector3 _max;

        #endregion

        #region property

        public Vector3 min => (Vector3)_min;
        public Vector3 max => (Vector3)_max;
        public Vector3 range => (Vector3)(_max - _min);

        #endregion

        #region constructor

        public MinMaxVector3() {
            _min = new SerializableVector3(float.MaxValue, float.MaxValue, float.MaxValue);
            _max = new SerializableVector3(float.MinValue, float.MinValue, float.MinValue);
        }

        public MinMaxVector3(in Vector3 min, in Vector3 max) {
            _min = new SerializableVector3(min);
            _max = new SerializableVector3(max);
        }

        #endregion

        #region logic

        public void Evaluate(in Vector3 value) {
            _min = new SerializableVector3(Mathf.Min(value.x, _min.x), Mathf.Min(value.y, _min.y), Mathf.Min(value.z, _min.z));
            _max = new SerializableVector3(Mathf.Max(value.x, _max.x), Mathf.Max(value.y, _max.y), Mathf.Max(value.z, _max.z));
        }

        public Bounds ToBounds() => new Bounds((Vector3)((_min + _max) * 0.5f), (Vector3)(_max - _min));

        #endregion

    }

}