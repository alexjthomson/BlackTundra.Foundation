using System;
using System.Text;

namespace BlackTundra.Foundation {

    /// <summary>
    /// Formats console messages automatically.
    /// </summary>
    public sealed class ConsoleFormatter {

        #region variable

        private string prefix;

        #endregion

        #region property

        public string name {
            get => _name;
            set {
                if (value == null) throw new ArgumentNullException();
                if (_name == value) return;
                _name = value;
                Bake();
            }
        }
        private string _name;

        #endregion

        #region constructor

        public ConsoleFormatter(in string name) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            _name = name;
            Bake();
        }

        #endregion

        #region logic

        private void Bake() {
            prefix = new StringBuilder()
                .Append('[')
                .Append(_name)
                .Append("] ")
                .ToString();
        }

        public void Debug(in string message) => Console.Debug(Format(message));
        public void Trace(in string message) => Console.Trace(Format(message));
        public void Info(in string message) => Console.Info(Format(message));
        public void Warning(in string message) => Console.Warning(Format(message));
        public void Error(in string message) => Console.Error(Format(message));
        public void Error(in string message, in Exception exception) => Console.Error(Format(message), exception);
        public void Fatal(in string message) => Console.Fatal(Format(message));
        public void Fatal(in string message, in Exception exception) => Console.Fatal(Format(message), exception);
        public string Format(in string message) => string.Concat(prefix, message);

        #endregion

    }

}