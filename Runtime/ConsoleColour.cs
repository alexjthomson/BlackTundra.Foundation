using System;

using UnityEngine;

namespace BlackTundra.Foundation {

    public sealed class ConsoleColour {

        #region constant

        public static readonly ConsoleColour White = new ConsoleColour("#ffffff");
        public static readonly ConsoleColour Gray = new ConsoleColour("#cccccc");
        public static readonly ConsoleColour DarkGray = new ConsoleColour("#aaaaaa");
        public static readonly ConsoleColour Red = new ConsoleColour("#ff4333");
        public static readonly ConsoleColour Orange = new ConsoleColour("#ff9561");
        public static readonly ConsoleColour Gold = new ConsoleColour("#ffc524");
        public static readonly ConsoleColour Yellow = new ConsoleColour("#ffca4a");
        public static readonly ConsoleColour Green = new ConsoleColour("#a0ff33");
        public static readonly ConsoleColour Blue = new ConsoleColour("#86b6fe");
        public static readonly ConsoleColour DarkBlue = new ConsoleColour("#718bd1");
        public static readonly ConsoleColour Purple = new ConsoleColour("#a473ff");

        public static readonly ConsoleColour Trace = Orange;
        public static readonly ConsoleColour Debug = Purple;
        public static readonly ConsoleColour Info = Green;
        public static readonly ConsoleColour Warning = Gold;
        public static readonly ConsoleColour Error = Red;
        public static readonly ConsoleColour Fatal = new ConsoleColour("#ff0000");

        #endregion

        #region variable

        /// <summary>
        /// <see cref="Color"/> of the <see cref="ConsoleColor"/>
        /// </summary>
        public readonly Color colour;

        /// <summary>
        /// Hex colour in the format <c>RRGGBB</c> based off of the <see cref="colour"/>.
        /// </summary>
        public readonly string hex;

        #endregion

        #region constructor

        private ConsoleColour() => throw new NotSupportedException();

        private ConsoleColour(in string hex) {
            this.colour = ColorUtility.TryParseHtmlString(hex, out Color colour) ? colour : throw new ArgumentException(nameof(hex));
            this.hex = ColorUtility.ToHtmlStringRGB(this.colour);
        }

        #endregion

    }

}