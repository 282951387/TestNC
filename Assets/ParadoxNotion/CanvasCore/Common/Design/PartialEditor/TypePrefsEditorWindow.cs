#if UNITY_EDITOR

using ParadoxNotion.Serialization;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace ParadoxNotion.Design
{

    ///An editor for preferred types
    public class TypePrefsEditorWindow : EditorWindow
    {

        private List<System.Type> typeList;
        private List<System.Type> alltypes;
        private Vector2 scrollPos;

        ///Open window
        public static void ShowWindow()
        {
            TypePrefsEditorWindow window = GetWindow<TypePrefsEditorWindow>();
            window.Show();
        }

        //...
        private void OnEnable()
        {
            titleContent = new GUIContent("Preferred Types");
            typeList = TypePrefs.GetPreferedTypesList();
            alltypes = ReflectionTools.GetAllTypes(true).Where(t => !t.IsGenericType && !t.IsGenericTypeDefinition).ToList();
        }

        //...
        private void OnGUI()
        {

            GUI.skin.label.richText = true;
            EditorGUILayout.HelpBox("Here you can specify frequently used types for your project and for easier access wherever you need to select a type, like for example when you create a new blackboard variable or using any refelection based actions. Furthermore, it is essential when working with AOT platforms like iOS or WebGL, that you generate an AOT Classes and link.xml files with the relevant button bellow. To add types in the list quicker, you can also Drag&Drop an object, or a Script file in this editor window.\n\nIf you save a preset in your 'Editor Default Resources/" + TypePrefs.SYNC_FILE_NAME + "' it will automatically sync with the list. Useful when working with others on source control.", MessageType.Info);

            if (GUILayout.Button("Add New Type", EditorStyles.miniButton))
            {
                GenericMenu.MenuFunction2 Selected = delegate (object o)
                {
                    if (o is System.Type)
                    {
                        AddType((System.Type)o);
                    }
                    if (o is string)
                    { //namespace
                        foreach (System.Type type in alltypes)
                        {
                            if (type.Namespace == (string)o)
                            {
                                AddType(type);
                            }
                        }
                    }
                };

                GenericMenu menu = new GenericMenu();
                List<string> namespaces = new List<string>();
                menu.AddItem(new GUIContent("Classes/System/Object"), false, Selected, typeof(object));
                foreach (System.Type t in alltypes)
                {
                    string a = (string.IsNullOrEmpty(t.Namespace) ? "No Namespace/" : t.Namespace.Replace(".", "/") + "/") + t.FriendlyName();
                    string b = string.IsNullOrEmpty(t.Namespace) ? string.Empty : " (" + t.Namespace + ")";
                    string friendlyName = a + b;
                    string category = "Classes/";
                    if (t.IsValueType)
                    {
                        category = "Structs/";
                    }

                    if (t.IsInterface)
                    {
                        category = "Interfaces/";
                    }

                    if (t.IsEnum)
                    {
                        category = "Enumerations/";
                    }

                    menu.AddItem(new GUIContent(category + friendlyName), typeList.Contains(t), Selected, t);
                    if (t.Namespace != null && !namespaces.Contains(t.Namespace))
                    {
                        namespaces.Add(t.Namespace);
                    }
                }

                menu.AddSeparator("/");
                foreach (string ns in namespaces)
                {
                    string path = "Whole Namespaces/" + ns.Replace(".", "/") + "/Add " + ns;
                    menu.AddItem(new GUIContent(path), false, Selected, ns);
                }

                menu.ShowAsBrowser("Add Preferred Type");
            }


            if (GUILayout.Button("Generate AOTClasses.cs and link.xml Files", EditorStyles.miniButton))
            {
                if (EditorUtility.DisplayDialog("Generate AOT Classes", "A script relevant to AOT compatibility for certain platforms will now be generated.", "OK"))
                {
                    string path = EditorUtility.SaveFilePanelInProject("AOT Classes File", "AOTClasses", "cs", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        AOTClassesGenerator.GenerateAOTClasses(path, TypePrefs.GetPreferedTypesList(true).ToArray());
                    }
                }

                if (EditorUtility.DisplayDialog("Generate link.xml File", "A file relevant to 'code stripping' for platforms that have code stripping enabled will now be generated.", "OK"))
                {
                    string path = EditorUtility.SaveFilePanelInProject("AOT link.xml", "link", "xml", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        AOTClassesGenerator.GenerateLinkXML(path, TypePrefs.GetPreferedTypesList().ToArray());
                    }
                }

                AssetDatabase.Refresh();
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset Defaults", EditorStyles.miniButtonLeft))
            {
                if (EditorUtility.DisplayDialog("Reset Preferred Types", "Are you sure?", "Yes", "NO!"))
                {
                    TypePrefs.ResetTypeConfiguration();
                    typeList = TypePrefs.GetPreferedTypesList();
                    Save();
                }
            }

            if (GUILayout.Button("Save Preset", EditorStyles.miniButtonMid))
            {
                string path = EditorUtility.SaveFilePanelInProject("Save Types Preset", "PreferredTypes", "typePrefs", "");
                if (!string.IsNullOrEmpty(path))
                {
                    System.IO.File.WriteAllText(path, JSONSerializer.Serialize(typeof(List<System.Type>), typeList, null, true));
                    AssetDatabase.Refresh();
                }
            }

            if (GUILayout.Button("Load Preset", EditorStyles.miniButtonRight))
            {
                string path = EditorUtility.OpenFilePanel("Load Types Preset", "Assets", "typePrefs");
                if (!string.IsNullOrEmpty(path))
                {
                    string json = System.IO.File.ReadAllText(path);
                    typeList = JSONSerializer.Deserialize<List<System.Type>>(json);
                    Save();
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            string syncPath = TypePrefs.SyncFilePath();
            EditorGUILayout.HelpBox(syncPath != null ? "List synced with file: " + syncPath.Replace(Application.dataPath, ".../Assets") : "No sync file found in '.../Assets/Editor Default Resources'. Types are currently saved in Unity EditorPrefs only.", MessageType.None);
            GUILayout.Space(5);

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            for (int i = 0; i < typeList.Count; i++)
            {
                if (EditorGUIUtility.isProSkin) { GUI.color = Color.black.WithAlpha(i % 2 == 0 ? 0.3f : 0); }
                if (!EditorGUIUtility.isProSkin) { GUI.color = Color.white.WithAlpha(i % 2 == 0 ? 0.3f : 0); }
                GUILayout.BeginHorizontal("box");
                GUI.color = Color.white;
                System.Type type = typeList[i];
                if (type == null)
                {
                    GUILayout.Label("MISSING TYPE", GUILayout.Width(300));
                    GUILayout.Label("---");
                }
                else
                {
                    string name = type.FriendlyName();
                    Texture icon = TypePrefs.GetTypeIcon(type);
                    GUILayout.Label(icon, GUILayout.Width(16), GUILayout.Height(16));
                    GUILayout.Label(name, GUILayout.Width(300));
                    GUILayout.Label(type.Namespace);
                }
                if (GUILayout.Button("X", GUILayout.Width(18)))
                {
                    RemoveType(type);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            AcceptDrops();
            Repaint();
        }


        //Handles Drag&Drop operations
        private void AcceptDrops()
        {
            Event e = Event.current;
            if (e.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            }

            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                foreach (Object o in DragAndDrop.objectReferences)
                {

                    if (o == null)
                    {
                        continue;
                    }

                    if (o is MonoScript)
                    {
                        System.Type type = (o as MonoScript).GetClass();
                        if (type != null)
                        {
                            AddType(type);
                        }
                        continue;
                    }

                    AddType(o.GetType());
                }
            }
        }

        ///Add a type
        private void AddType(System.Type t)
        {
            if (!typeList.Contains(t))
            {
                typeList.Add(t);
                Save();
                ShowNotification(new GUIContent(string.Format("Type '{0}' Added!", t.FriendlyName())));
                return;
            }

            ShowNotification(new GUIContent(string.Format("Type '{0}' is already in the list.", t.FriendlyName())));
        }

        ///Remove a type
        private void RemoveType(System.Type t)
        {
            typeList.Remove(t);
            Save();
            ShowNotification(new GUIContent(string.Format("Type '{0}' Removed.", t.FriendlyName())));
        }

        ///Save changes
        private void Save()
        {
            TypePrefs.SetPreferedTypesList(typeList);
        }
    }
}

#endif