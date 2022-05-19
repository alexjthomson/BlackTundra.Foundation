/*
 * TODO:
 * - If .NET version is updated: replace int weight with INumber and use generic maths. This will allow for custom weight value types to be used.
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace BlackTundra.Foundation.Collections.Generic {

    /// <summary>
    /// A generic list associating values of type <typeparamref name="T"/> with an integer weight.
    /// </summary>
    [Serializable]
    public sealed class WeightedList<T> : IEnumerable<T> {

        #region constant

        /// <summary>
        /// <see cref="Random"/> number generator.
        /// </summary>
        private static readonly Random Random = new Random();

        #endregion

        #region nested

        /// <summary>
        /// Associated a <see cref="_weight"/> with a <see cref="_value"/>.
        /// </summary>
        [Serializable]
        public struct WeightValuePair {

            #region variable

            /// <summary>
            /// Weight associated with the <see cref="_value"/>.
            /// </summary>
            internal int _weight;

            /// <summary>
            /// Value.
            /// </summary>
            internal T _value;

            #endregion

            #region property

            public int weight => _weight;

            public T value => _value;

            #endregion

            #region constructor

            public WeightValuePair(in int weight, in T value) {
                if (weight < 0) throw new ArgumentOutOfRangeException(nameof(weight));
                _weight = weight;
                _value = value;
            }

            #endregion

        }

        [Serializable]
        internal struct CumulativeWeightValuePair {

            #region variable

            internal int cumulativeWeight;

            internal WeightValuePair wvp;

            #endregion

            #region constructor

            internal CumulativeWeightValuePair(in int cumulativeWeight, in WeightValuePair wvp) {
                this.cumulativeWeight = cumulativeWeight;
                this.wvp = wvp;
            }

            #endregion

        }

        #endregion

        #region variable

        /// <summary>
        /// <see cref="List{T}"/> of <see cref="CumulativeWeightValuePair"/> instances.
        /// </summary>
        internal List<CumulativeWeightValuePair> list;

        /// <summary>
        /// Total of all weights in the <see cref="list"/>.
        /// </summary>
        private int totalWeight;

        #endregion

        #region property

        /// <summary>
        /// <c>true</c> if the <see cref="WeightedList{T}"/> is empty.
        /// </summary>
        public bool IsEmpty => list.Count == 0;

        public WeightValuePair this[in int index] {
            get {
                if (index < 0 || index >= list.Count) throw new ArgumentOutOfRangeException(nameof(index));
                return list[index].wvp;
            }
        }

        /// <summary>
        /// Number of elements in the <see cref="WeightedList{T}"/>.
        /// </summary>
        public int Count => list.Count;

        /// <summary>
        /// Total weight of every element inside the <see cref="WeightedList{T}"/>.
        /// </summary>
        public int TotalWeight => totalWeight;

        #endregion

        #region constructor

        public WeightedList() {
            list = new List<CumulativeWeightValuePair>();
            totalWeight = 0;
        }

        #endregion

        #region logic

        #region Add

        public void Add(in int weight, in T value) {
            if (weight < 0) throw new ArgumentOutOfRangeException(nameof(weight));
            WeightValuePair wvp = new WeightValuePair(weight, value);
            CumulativeWeightValuePair cwvp = new CumulativeWeightValuePair(totalWeight, wvp);
            list.Add(cwvp);
            totalWeight += weight;
        }

        #endregion

        #region RemoveAt

        /// <summary>
        /// Removes a <see cref="WeightValuePair"/> at a specified <paramref name="index"/>.
        /// </summary>
        /// <returns>
        /// Returns the removed <see cref="WeightValuePair"/>.
        /// </returns>
        public WeightValuePair RemoveAt(in int index) {
            int elementCount = list.Count;
            if (index < 0 || index >= elementCount) throw new ArgumentOutOfRangeException(nameof(index));
            // get the wvp:
            CumulativeWeightValuePair cwvp = list[index];
            WeightValuePair wvp = cwvp.wvp;
            int weight = cwvp.wvp._weight;
            // remove from list:
            list.RemoveAt(index);
            // correct weights:
            totalWeight -= weight;
            for (int i = elementCount - 2; i >= index; i--) {
                cwvp = list[i];
                cwvp.cumulativeWeight -= weight;
                list[i] = cwvp;
            }
            return wvp;
        }

        #endregion

        #region GetRandom

        /// <returns>
        /// Gets a random <see cref="WeightValuePair"/> without taking into account weights.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown if the <see cref="WeightedList{T}"/> is empty.
        /// </exception>
        public WeightValuePair GetRandom() {
            int elementCount = list.Count;
            if (elementCount == 0) throw new NotSupportedException($"{nameof(WeightedList<T>)} is empty.");
            int index = Random.Next(0, elementCount);
            return list[index].wvp;
        }

        #endregion

        #region GetRandomWeighted

        /// <returns>
        /// Gets a random <see cref="WeightValuePair"/> based on the weights of the values in the <see cref="WeightedList{T}"/>.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown if the <see cref="WeightedList{T}"/> is empty.
        /// </exception>
        public WeightValuePair GetRandomWeighted() {
            int elementCount = list.Count;
            if (elementCount == 0) throw new NotSupportedException($"{nameof(WeightedList<T>)} is empty.");
            int weight = Random.Next(0, totalWeight + 1);
            CumulativeWeightValuePair cwvp;
            for (int i = elementCount - 1; i >= 0; i--) {
                cwvp = list[i];
                if (weight >= cwvp.cumulativeWeight) {
                    return cwvp.wvp;
                }
            }
            return list[0].wvp;
        }

        #endregion

        #region Swap

        /// <summary>
        /// Swaps two elements by index.
        /// </summary>
        public void Swap(in int i0, in int i1) {
            int count = list.Count;
            if (i0 < 0 || i0 >= count) throw new ArgumentOutOfRangeException("i0");
            if (i1 < 0 || i1 >= count) throw new ArgumentOutOfRangeException("i1");
            if (i0 == i1) return;
            CumulativeWeightValuePair temp = list[i0];
            list[i0] = list[i1];
            list[i1] = temp;
        }

        #endregion

        #region RecalculateWeights

        /// <summary>
        /// Recalculates the weights of the <see cref="WeightedList{T}"/>.
        /// </summary>
        /// <returns>
        /// Returns the total weight of every item in the <see cref="WeightedList{T}"/>.
        /// </returns>
        public int RecalculateWeights() {
            totalWeight = 0;
            int elementCount = list.Count;
            if (elementCount == 0) return 0;
            CumulativeWeightValuePair cwvp;
            WeightValuePair wvp;
            for (int i = 0; i < elementCount; i++) {
                cwvp = list[i];
                wvp = cwvp.wvp;
                cwvp.cumulativeWeight = totalWeight;
                totalWeight += wvp.weight;
                list[i] = cwvp;
            }
            return totalWeight;
        }

        #endregion

        #region GetEnumerator

        public IEnumerator<T> GetEnumerator() {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }

        #endregion

        #endregion

    }

}