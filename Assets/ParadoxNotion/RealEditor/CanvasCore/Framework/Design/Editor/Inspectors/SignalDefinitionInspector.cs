#if UNITY_EDITOR

using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEditor;
using UnityEngine;

namespace NodeCanvas.Editor
{

    [CustomEditor(typeof(SignalDefinition))]
    public class SignalDefinitionInspector : UnityEditor.Editor
    {

        public override void OnInspectorGUI()
        {

            base.OnInspectorGUI();

            SignalDefinition def = (SignalDefinition)target;

            if (GUILayout.Button("Add Parameter"))
            {
                EditorUtils.ShowPreferedTypesSelectionMenu(typeof(object), (t) =>
                {
                    UndoUtility.RecordObjectComplete(def, "Add Parameter");
                    def.AddParameter(t.FriendlyName(), t);
                    UndoUtility.SetDirty(def);
                });
            }

            UndoUtility.CheckUndo(def, "Definition");
            EditorUtils.ReorderableListOptions options = new EditorUtils.ReorderableListOptions();
            options.allowRemove = true;
            options.unityObjectContext = def;
            EditorUtils.ReorderableList(def.parameters, options, (i, picked) =>
            {
                DynamicParameterDefinition parameter = def.parameters[i];
                GUILayout.BeginHorizontal();
                parameter.name = UnityEditor.EditorGUILayout.DelayedTextField(parameter.name, GUILayout.Width(150), GUILayout.ExpandWidth(true));
                EditorUtils.ButtonTypePopup("", parameter.type, (t) => { parameter.type = t; });
                GUILayout.EndHorizontal();
            });
            UndoUtility.CheckDirty(def);

            EditorUtils.EndOfInspector();
            if (Event.current.isMouse) { Repaint(); }
        }
    }
}

#endif