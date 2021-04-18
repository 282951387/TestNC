#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace ParadoxNotion.Design
{

    ///A generic popup editor
    public class GenericInspectorWindow : EditorWindow
    {

        private static GenericInspectorWindow current;

        private string friendlyTitle;
        private System.Type targetType;
        private Object unityObjectContext;
        private System.Func<object> read;
        private System.Action<object> write;
        private Vector2 scrollPos;
        private bool willRepaint;

        // ...
        private void OnEnable()
        {
            titleContent = new GUIContent("Object Editor");
            current = this;

#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged -= PlayModeChange;
            EditorApplication.playModeStateChanged += PlayModeChange;
#else
        	EditorApplication.playmodeStateChanged -= PlayModeChange;
            EditorApplication.playmodeStateChanged += PlayModeChange;
#endif
        }

        //...
        private void OnDisable()
        {
#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged -= PlayModeChange;
#else
        	EditorApplication.playmodeStateChanged -= PlayModeChange;
#endif
        }

#if UNITY_2017_2_OR_NEWER
        private void PlayModeChange(PlayModeStateChange state) { Close(); }
#else
        void PlayModeChange(){ Close(); }
#endif

        ///Open utility window to inspect target object of type in context using read/write delegates.
        public static void Show(string title, System.Type targetType, Object unityObjectContext, System.Func<object> read, System.Action<object> write)
        {
            GenericInspectorWindow window = current != null ? current : CreateInstance<GenericInspectorWindow>();
            window.friendlyTitle = title;
            window.targetType = targetType;
            window.unityObjectContext = unityObjectContext;
            window.write = write;
            window.read = read;
            window.ShowUtility();
        }

        //...
        private void Update()
        {
            if (willRepaint)
            {
                willRepaint = false;
                Repaint();
            }
        }

        //...
        private void OnGUI()
        {

            if (targetType == null)
            {
                return;
            }

            Event e = Event.current;
            if (e.type == EventType.ValidateCommand && e.commandName == "UndoRedoPerformed")
            {
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;
                e.Use();
                return;
            }

            GUILayout.Space(10);
            GUILayout.Label(string.Format("<size=14><b>{0}</b></size>", targetType.FriendlyName()), Styles.centerLabel);
            EditorUtils.Separator();
            GUILayout.Space(10);
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            InspectedFieldInfo serializationInfo = new InspectedFieldInfo(unityObjectContext, null, null, null);
            object oldValue = read();
            object newValue = EditorUtils.ReflectedFieldInspector(friendlyTitle, oldValue, targetType, serializationInfo);
            if (!Equals(oldValue, newValue) || GUI.changed)
            {
                write(newValue);
            }
            GUILayout.EndScrollView();

            willRepaint = true;
        }
    }
}

#endif