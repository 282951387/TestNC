#if UNITY_EDITOR

using NodeCanvas.Framework;
using ParadoxNotion.Design;
using ParadoxNotion.Serialization;
using ParadoxNotion.Serialization.FullSerializer;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NodeCanvas.Editor
{

    ///Graph Refactoring
    public class GraphRefactor : EditorWindow
    {

        //...
        public static void ShowWindow()
        {
            GetWindow<GraphRefactor>().Show();
        }

        private Dictionary<string, List<IMissingRecoverable>> recoverablesMap;
        private Dictionary<string, string> recoverableChangesMap;

        private Dictionary<string, List<ISerializedReflectedInfo>> reflectedMap;
        private Dictionary<string, fsData> reflectedChangesMap;

        ///----------------------------------------------------------------------------------------------

        private void Flush()
        {
            recoverablesMap = null;
            reflectedChangesMap = null;
            recoverablesMap = null;
            reflectedChangesMap = null;
        }

        //...
        private void Gather()
        {

            EditorGUIUtility.keyboardControl = 0;
            EditorGUIUtility.hotControl = 0;

            GatherRecoverables();
            GatherReflected();
        }

        //...
        private void GatherRecoverables()
        {
            recoverablesMap = new Dictionary<string, List<IMissingRecoverable>>();
            recoverableChangesMap = new Dictionary<string, string>();

            Graph graph = GraphEditor.currentGraph;
            ParadoxNotion.HierarchyTree.Element metaGraph = graph.GetFlatMetaGraph();
            IEnumerable<IMissingRecoverable> recoverables = metaGraph.GetAllChildrenReferencesOfType<IMissingRecoverable>();
            foreach (IMissingRecoverable recoverable in recoverables)
            {
                List<IMissingRecoverable> collection;
                if (!recoverablesMap.TryGetValue(recoverable.missingType, out collection))
                {
                    collection = new List<IMissingRecoverable>();
                    recoverablesMap[recoverable.missingType] = collection;
                    recoverableChangesMap[recoverable.missingType] = recoverable.missingType;
                }
                collection.Add(recoverable);
            }
        }

        //...
        private void GatherReflected()
        {
            reflectedMap = new Dictionary<string, List<ISerializedReflectedInfo>>();
            reflectedChangesMap = new Dictionary<string, fsData>();
            Graph graph = GraphEditor.currentGraph;
            JSONSerializer.SerializeAndExecuteNoCycles(typeof(NodeCanvas.Framework.Internal.GraphSource), graph.GetGraphSource(), DoCollect);
        }

        //...
        private void DoCollect(object o, fsData d)
        {
            if (o is ISerializedReflectedInfo)
            {
                ISerializedReflectedInfo reflect = (ISerializedReflectedInfo)o;
                if (reflect.AsMemberInfo() == null)
                {
                    List<ISerializedReflectedInfo> collection;
                    if (!reflectedMap.TryGetValue(reflect.AsString(), out collection))
                    {
                        collection = new List<ISerializedReflectedInfo>();
                        reflectedMap[reflect.AsString()] = collection;
                        reflectedChangesMap[reflect.AsString()] = d;
                    }
                    collection.Add(reflect);
                }
            }
        }

        ///----------------------------------------------------------------------------------------------

        //...
        private void Save()
        {

            if (recoverableChangesMap.Count > 0 || reflectedChangesMap.Count > 0)
            {

                if (recoverableChangesMap.Count > 0)
                {
                    SaveRecoverables();
                }
                if (reflectedChangesMap.Count > 0)
                {
                    SaveReflected();
                }

                GraphEditor.currentGraph.SelfSerialize();
                GraphEditor.currentGraph.SelfDeserialize();
                GraphEditor.currentGraph.Validate();
                Gather();
            }
        }

        //...
        private void SaveRecoverables()
        {
            foreach (KeyValuePair<string, List<IMissingRecoverable>> pair in recoverablesMap)
            {
                foreach (IMissingRecoverable recoverable in pair.Value)
                {
                    recoverable.missingType = recoverableChangesMap[pair.Key];
                }
            }
        }

        //...
        private void SaveReflected()
        {
            foreach (KeyValuePair<string, List<ISerializedReflectedInfo>> pair in reflectedMap)
            {
                foreach (ISerializedReflectedInfo reflect in pair.Value)
                {
                    fsData data = reflectedChangesMap[pair.Key];
                    JSONSerializer.TryDeserializeOverwrite(reflect, data.ToString(), null);
                }
            }
        }

        ///----------------------------------------------------------------------------------------------

        //...
        private void OnEnable()
        {
            titleContent = new GUIContent("Refactor", StyleSheet.canvasIcon);
            GraphEditor.onCurrentGraphChanged -= OnGraphChanged;
            GraphEditor.onCurrentGraphChanged += OnGraphChanged;
        }

        //...
        private void OnDisable()
        {
            GraphEditor.onCurrentGraphChanged -= OnGraphChanged;
        }

        private void OnGraphChanged(Graph graph) { Flush(); Repaint(); }

        //...
        private void OnGUI()
        {

            if (Application.isPlaying)
            {
                ShowNotification(new GUIContent("Refactor only works in editor mode. Please exit play mode."));
                return;
            }

            if (GraphEditor.current == null || GraphEditor.currentGraph == null)
            {
                ShowNotification(new GUIContent("No Graph is currently open in the Graph Editor."));
                return;
            }

            RemoveNotification();

            EditorGUILayout.HelpBox("Batch refactor missing nodes, tasks, types as well as missing reflection based methods, properties, fields and so on references. Note that changes made here are irreversible. Please proceed with caution.\n\n1) Hit Gather to fetch missing elements from the currently viewing graph in the editor.\n2) Rename elements serialization data to their new name (keep the same format).\n3) Hit Save to commit your changes.", MessageType.Info);

            if (GUILayout.Button("Gather", GUILayout.Height(30))) { Gather(); }
            EditorUtils.Separator();

            if (recoverablesMap == null || reflectedMap == null) { return; }

            EditorGUI.indentLevel = 1;
            DoRecoverables();

            EditorGUI.indentLevel = 1;
            DoReflected();

            if (recoverableChangesMap.Count > 0 || reflectedChangesMap.Count > 0)
            {
                if (GUILayout.Button("Save", GUILayout.Height(30))) { Save(); }
            }
            else
            {
                GUILayout.Label("It's all looking good :-)");
                EditorUtils.Separator();
            }
        }

        //...
        private void DoRecoverables()
        {

            if (recoverablesMap.Count == 0)
            {
                GUILayout.Label("No missing recoverable elements found.");
                return;
            }

            foreach (KeyValuePair<string, List<IMissingRecoverable>> pair in recoverablesMap)
            {
                string originalName = pair.Key;
                GUILayout.Label(string.Format("<b>{0} occurencies: Type '{1}'</b>", pair.Value.Count, originalName));
                GUILayout.Space(5);
                string typeName = recoverableChangesMap[originalName];
                typeName = EditorGUILayout.TextField("Type Name", typeName);
                recoverableChangesMap[originalName] = typeName;
                EditorUtils.Separator();
            }
        }

        //...
        private void DoReflected()
        {

            if (reflectedMap.Count == 0)
            {
                GUILayout.Label("No missing reflected references found.");
                return;
            }

            foreach (KeyValuePair<string, List<ISerializedReflectedInfo>> pair in reflectedMap)
            {
                string information = pair.Key;
                GUILayout.Label(string.Format("<b>{0} occurencies: '{1}'</b>", pair.Value.Count, information));
                GUILayout.Space(5);
                fsData data = reflectedChangesMap[information];
                Dictionary<string, fsData> dict = new Dictionary<string, fsData>(data.AsDictionary);
                foreach (KeyValuePair<string, fsData> dataPair in dict)
                {
                    string value = dataPair.Value.AsString;
                    string newValue = EditorGUILayout.TextField(dataPair.Key, value);
                    if (newValue != value)
                    {
                        data.AsDictionary[dataPair.Key] = new fsData(newValue);
                    }
                }
                reflectedChangesMap[information] = data;
                EditorUtils.Separator();
            }
        }

    }
}
#endif

