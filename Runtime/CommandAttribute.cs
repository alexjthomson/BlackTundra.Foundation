using System;

namespace BlackTundra.Foundation {

    /// <summary>
    /// Marks a static method as a console command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class CommandAttribute : Attribute {

        #region variable

        /// <summary>
        /// Name of the command.
        /// </summary>
        public readonly string name;

        /// <summary>
        /// Description of the command.
        /// </summary>
        public readonly string description;

        /// <summary>
        /// Usage of the command.
        /// </summary>
        public readonly string usage;

        /// <summary>
        /// When <c>true</c>, this command is by default hidden.
        /// </summary>
        public readonly bool hidden;

        #endregion

        #region constructor

        public CommandAttribute(string name, string description = null, string usage = null, bool hidden = false) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            this.name = name.ToLower();
            this.description = description;
            this.usage = usage;
            this.hidden = hidden;
        }

        #endregion

    }

}