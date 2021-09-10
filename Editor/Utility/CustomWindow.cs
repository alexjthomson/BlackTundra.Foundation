using UnityEditor;

namespace BlackTundra.Foundation.Editor.Utility {

    public abstract class CustomWindow : EditorWindow {

        protected static T GetWindow<T>(in string title) where T : CustomWindow { return (T)GetWindow(typeof(T), false, title, true); }

        protected abstract void OnGUI();

    }

}