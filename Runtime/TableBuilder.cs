using System.Collections.Generic;

namespace BlackTundra.Foundation {

    /// <summary>
    /// Builds a generic table.
    /// </summary>
    /// <remarks>
    /// This is useful for displaying tables to the <see cref="ConsoleWindow"/>.
    /// </remarks>
    public sealed class TableBuilder<T> {

        #region variable

        private readonly List<T[]> table;

        private int maxColumnCount;

        #endregion

        #region property

        public int width => maxColumnCount;
        public int height => table.Count;

        #endregion

        #region constructor

        public TableBuilder() {
            table = new List<T[]>();
            maxColumnCount = 0;
        }

        #endregion

        #region logic

        public void AddRow(params T[] elements) {
            if (elements == null) elements = new T[0];
            int elementCount = elements.Length;
            table.Add(elements);
            if (elementCount > maxColumnCount) {
                maxColumnCount = elementCount;
            }
        }

        /// <returns>
        /// Table [x, y] where [0, 0] is the top left of the table.
        /// </returns>
        public T[,] ToTable() {
            int width = maxColumnCount;
            int height = this.table.Count;
            T[,] table = new T[width, height];
            T[] row;
            for (int y = 0; y < height; y++) {
                row = this.table[y];
                int rowSize = row.Length;
                for (int x = 0; x < rowSize; x++) {
                    table[x, y] = row[x];
                }
                if (rowSize < width) {
                    for (int x = rowSize; x < width; x++) {
                        table[x, y] = default(T);
                    }
                }
            }
            return table;
        }

        #endregion

    }

}