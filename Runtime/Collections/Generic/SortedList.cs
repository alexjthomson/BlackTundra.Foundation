using System.Collections.Generic;

namespace BlackTundra.Foundation.Collections.Generic {

    /// <summary>
    /// An extension of the <see cref="List{T}"/> collection that allows elements to be sorted by an <see cref="int"/>.
    /// </summary>
    public sealed class SortedList<T> {

        #region nested

        /// <summary>
        /// Associates an order to a value.
        /// </summary>
        public struct SortedListEntry {
            public readonly int order;
            public readonly T value;
            internal SortedListEntry(in int order, in T value) {
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

        public T this[in int index] => values[index].value;

        #endregion

        #region constructor

        public SortedList() {
            values = new List<SortedListEntry>();
        }

        #endregion

        #region Add

        /// <summary>
        /// Adds an element into the <see cref="SortedList{T}"/>.
        /// </summary>
        /// <param name="order">Value used to sort/order the <paramref name="value"/> in the <see cref="SortedList{T}"/></param>
        public void Add(in int order, in T value) {
            for (int i = values.Count - 1; i >= 0; i--) {
                if (order < values[i].order) {
                    values.Insert(i, new SortedListEntry(order, value));
                    return;
                }
            }
            values.Add(new SortedListEntry(order, value));
        }

        #endregion

    }

}