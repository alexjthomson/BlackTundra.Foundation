using System;

using UnityEngine;

namespace BlackTundra.Foundation.Utility {

    /// <summary>
    /// Stores a floating point number that has a minimum and maximum value.
    /// </summary>
    [Serializable]
    public sealed class ClampedFloat : IEquatable<ClampedFloat>, IEquatable<float> {

        #region variable

        [SerializeField] private float _value; // clamped value
        [SerializeField] private float _min; // min clamp
        [SerializeField] private float _max; // max clamp

        #endregion

        #region property

        /// <summary>
        /// Clamped value of the ClampedFloat.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float value {
#pragma warning restore IDE1006 // naming styles

            get => _value;
            set => _value = value < _min ? _min : (value > _max ? _max : value);

        }

        /// <summary>
        /// Minimum value that the ClampedFloat can have.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float min {
#pragma warning restore IDE1006 // naming styles

            get => _min;
            set {

                if (value > _max) throw new ArgumentException("min cannot be more than max");
                _min = value; // set new min
                if (_value < value) _value = value; // apply lower clamp again

            }

        }

        /// <summary>
        /// Maximum value that the ClampedFloat can have.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float max {
#pragma warning restore IDE1006 // naming styles

            get => _max;
            set {

                if (value < _min) throw new ArgumentException("max cannot be less then min");
                _max = value; // set new max
                if (_value > value) _value = value; // apply upper clamp again

            }

        }

        #endregion

        #region constructor

        /// <summary>
        /// Constructs a clamped float.
        /// </summary>
        /// <param name="value">Value of the clamped float.</param>
        /// <param name="min">Minimum value. If this is more than the maximum value, the minimum and maximum values will be swapped to correct for the mistake.</param>
        /// <param name="max">Maximum value.</param>
        public ClampedFloat(in float value = 0.0f, in float min = 0.0f, in float max = 1.0f) {

            if (min < max) { // min max correct way around
                _min = min;
                _max = max;
            } else { // swap min and max to correct for mistake rather than throwing an exception
                _min = max;
                _max = min;
            }
            _value = value < _min ? _min : (value > _max ? _max : value);

        }

        #endregion

        #region logic

        #region Equals

        public bool Equals(float value) => _value == value;

        public bool Equals(ClampedFloat value) => value != null && _value == value._value;

        #endregion

        #endregion

    }

}