using System;
using System.Collections.Generic;

namespace BlackTundra.Foundation.Collections.Generic {

    /// <summary>
    /// An extension of the <see cref="List{T}"/> collection that allows elements to be sorted from lowest value to highest value.
    /// </summary>
    public sealed class OrderedList<TKey, TValue> where TKey : IComparable {

        #region nested

        /// <summary>
        /// Associates an order to a value.
        /// </summary>
        public struct SortedListEntry {
            public readonly TKey order;
            public readonly TValue value;
            internal SortedListEntry(in TKey order, in TValue value) {
                this.order = order;
                this.value = value;
            }
        }

        #endregion

        #region variable

        /// <summary>
        /// List of each entry;
        /// </summary>
        private readonly List<SortedListEntry> values;

        #endregion

        #region property

        public int Count => values.Count;

        public TValue this[in int index] => values[index].value;

        #endregion

        #region constructor

        public OrderedList() {
            values = new List<SortedListEntry>();
        }

        #endregion

        #region logic

        #region Add

        /// <summary>
        /// Adds an element into the <see cref="SortedList{T}"/>.
        /// </summary>
        /// <param name="order">Value used to sort/order the <paramref name="value"/> in the <see cref="SortedList{T}"/></param>
        public void Add(in TKey order, in TValue value) {
            for (int i = values.Count - 1; i >= 0; i--) {
                if (order.CompareTo(values[i].order) >= 0) {
                    values.Insert(i + 1, new SortedListEntry(order, value));
                    return;
                }
            }
            values.Insert(0, new SortedListEntry(order, value));
        }

        #endregion

        #region GetWeightAtIndex

        public TKey GetWeightAtIndex(in int index) {
            if (index < 0 || index >= values.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return values[index].order;
        }

        #endregion

        #endregion

    }

}