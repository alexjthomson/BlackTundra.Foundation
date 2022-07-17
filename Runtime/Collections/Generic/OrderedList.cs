using System;
using System.Collections.Generic;

namespace BlackTundra.Foundation.Collections.Generic {

    /// <summary>
    /// An extension of the <see cref="List{T}"/> collection that allows elements to be sorted from lowest value to highest value.
    /// </summary>
    public sealed class OrderedList<TKey, TValue> where TKey : IComparable {

        #region nested

        /// <summary>
        /// Associates an order (non-unique key) to a value.
        /// </summary>
        public struct SortedListEntry {

            #region variable

            /// <summary>
            /// Key that the entry is ordered by.
            /// </summary>
            public readonly TKey order;

            /// <summary>
            /// Value of the entry.
            /// </summary>
            public readonly TValue value;

            #endregion

            #region constructor

            internal SortedListEntry(in TKey order, in TValue value) {
                this.order = order;
                this.value = value;
            }

            #endregion

            #region logic

            public static implicit operator TValue(SortedListEntry entry) => entry.value;

            #endregion

        }

        #endregion

        #region variable

        /// <summary>
        /// List of each entry;
        /// </summary>
        internal readonly List<SortedListEntry> values;

        #endregion

        #region property

        /// <summary>
        /// Number of elements in the <see cref="OrderedList{TKey, TValue}"/>.
        /// </summary>
        /// <seealso cref="IsEmpty"/>
        public int Count => values.Count;

        /// <summary>
        /// <c>true</c> if the <see cref="OrderedList{TKey, TValue}"/> is empty.
        /// </summary>
        /// <seealso cref="Count"/>
        public bool IsEmpty => values.Count == 0;

        /// <summary>
        /// Lowest key (value to order by) in the <see cref="OrderedList{TKey, TValue}"/>.
        /// </summary>
        public TKey LowestKey => values[0].order;

        /// <summary>
        /// Highest key (value to order by) in the <see cref="OrderedList{TKey, TValue}"/>.
        /// </summary>
        public TKey HighestKey => values[^1].order;

        /// <summary>
        /// Value with the lowest key (value to order by) in the <see cref="OrderedList{TKey, TValue}"/>.
        /// </summary>
        public SortedListEntry LowestEntry => values[0];

        /// <summary>
        /// Value with the highest key (value to order by) in the <see cref="OrderedList{TKey, TValue}"/>.
        /// </summary>
        public SortedListEntry HighestEntry => values[^1];

        public SortedListEntry this[in int index] => values[index];

        #endregion

        #region constructor

        public OrderedList() {
            values = new List<SortedListEntry>();
        }

        public OrderedList(in List<TValue> list, in TKey value) {
            if (list == null) throw new ArgumentNullException(nameof(list));
            int count = list.Count;
            values = new List<SortedListEntry>(count);
            for (int i = count - 1; i >= 0; i--) values.Add(new SortedListEntry(value, list[i]));
        }

        internal OrderedList(in List<SortedListEntry> values) {
            if (values == null) throw new ArgumentNullException(nameof(values));
            this.values = values;
        }

        #endregion

        #region logic

        #region Add

        /// <summary>
        /// Adds an element into the <see cref="OrderedList{TKey, TValue}"/>.
        /// </summary>
        /// <param name="order">Value used to sort/order the <paramref name="value"/> in the <see cref="OrderedList{TKey, TValue}"/></param>
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

        #region AddRange

        public void AddRange(in OrderedList<TKey, TValue> list) {
            if (list == null) throw new ArgumentNullException(nameof(list));
            List<SortedListEntry> newValues = list.values;
            int newValuesCount = newValues.Count;
            SortedListEntry entry;
            TKey order;
            int count = values.Count;
            for (int i = newValuesCount - 1; i >= 0; i--) {
                entry = newValues[i];
                order = entry.order;
                for (int j = count - 1; j >= 0; j--) {
                    if (order.CompareTo(values[j].order) >= 0) {
                        values.Insert(j + 1, entry);
                        count++;
                        break;
                    }
                }
            }
        }

        #endregion

        #region RemoveAt

        public SortedListEntry RemoveAt(in int index) {
            if (index < 0 || index >= values.Count) throw new ArgumentOutOfRangeException(nameof(index));
            SortedListEntry entry = values[index];
            values.RemoveAt(index);
            return entry;
        }

        #endregion

        #region RemoveFirst

        /// <summary>
        /// Removes the first element from the <see cref="OrderedList{TKey, TValue}"/>. The first element is the least significant (lowest ordered) element.
        /// </summary>
        public SortedListEntry RemoveFirst() => RemoveAt(0);

        #endregion

        #region RemoveLast

        /// <summary>
        /// Removes the last element from the <see cref="OrderedList{TKey, TValue}"/>. The last element is the most significant (highest ordered) element.
        /// </summary>
        public SortedListEntry RemoveLast() => RemoveAt(values.Count - 1);

        #endregion

        #region Pop

        /// <summary>
        /// Synonymous with <see cref="RemoveLast"/>.
        /// </summary>
        /// <seealso cref="RemoveLast"/>
        public SortedListEntry Pop() => RemoveLast();

        #endregion

        #region GetWeightAtIndex

        public TKey GetWeightAtIndex(in int index) {
            if (index < 0 || index >= values.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return values[index].order;
        }

        #endregion

        #region operators

        public static explicit operator OrderedList<TKey, TValue>(OrderedList<TKey, object> list) {
            if (list == null) throw new ArgumentNullException(nameof(list));
            List<OrderedList<TKey, object>.SortedListEntry> values = list.values;
            int count = values.Count;
            List<SortedListEntry> newValues = new List<SortedListEntry>(count);
            OrderedList<TKey, object>.SortedListEntry entry;
            for (int i = 0; i < count; i++) {
                entry = values[i];
                newValues.Add(new SortedListEntry(entry.order, (TValue)entry.value));
            }
            return new OrderedList<TKey, TValue>(newValues);
        }

        #endregion

        #endregion

    }

    public static class OrderedListUtility {

        public static OrderedList<K, V> ToOrderedList<K, V>(this List<V> list, in K value) where K : IComparable {
            if (list == null) throw new ArgumentNullException(nameof(list));
            return new OrderedList<K, V>(list, value);
        }

        public static OrderedList<K, V1> ConvertValue<K, V0, V1>(this OrderedList<K, V0> list) where K : IComparable where V0 : V1 {
            if (list == null) throw new ArgumentNullException(nameof(list));
            List<OrderedList<K, V0>.SortedListEntry> values = list.values;
            int count = values.Count;
            List<OrderedList<K, V1>.SortedListEntry> newValues = new List<OrderedList<K, V1>.SortedListEntry>(count);
            OrderedList<K, V0>.SortedListEntry entry;
            for (int i = 0; i < count; i++) {
                entry = values[i];
                newValues.Add(new OrderedList<K, V1>.SortedListEntry(entry.order, entry.value));
            }
            return new OrderedList<K, V1>(newValues);
        }

        /// <summary>
        /// Normalizes the orders of every list element.
        /// </summary>
        public static OrderedList<float, V> Normalize<V>(this OrderedList<float, V> list) {
            if (list == null) throw new ArgumentNullException(nameof(list));
            int count = list.Count;
            if (count > 1) {
                List<OrderedList<float, V>.SortedListEntry> values = list.values;
                OrderedList<float, V>.SortedListEntry entry;
                entry = values[count - 1];
                float coefficient = 1.0f / entry.order;
                for (int i = count - 1; i >= 0; i--) {
                    entry = values[i];
                    values[i] = new OrderedList<float, V>.SortedListEntry(
                        entry.order * coefficient,
                        entry.value
                    );
                }
            }
            return list;
        }

        /// <summary>
        /// Normalizes the orders of every list element.
        /// </summary>
        public static OrderedList<float, V> Normalized<V>(this OrderedList<float, V> list) {
            if (list == null) throw new ArgumentNullException(nameof(list));
            int count = list.Count;
            if (count == 0) return new OrderedList<float, V>();
            List<OrderedList<float, V>.SortedListEntry> values = list.values;
            List<OrderedList<float, V>.SortedListEntry> newValues = new List<OrderedList<float, V>.SortedListEntry>(count);
            OrderedList<float, V>.SortedListEntry entry;
            if (count > 1) {
                entry = values[count - 1];
                float coefficient = 1.0f / entry.order;
                for (int i = count - 1; i >= 0; i--) {
                    entry = values[i];
                    newValues.Add(
                        new OrderedList<float, V>.SortedListEntry(
                            entry.order * coefficient,
                            entry.value
                        )
                    );
                }
            } else {
                entry = values[0];
                newValues.Add(new OrderedList<float, V>.SortedListEntry(entry.order, entry.value));
            }
            return new OrderedList<float, V>(newValues);
        }

    }

}