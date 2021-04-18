#if UNITY_EDITOR
using UnityEditor;
using NodeCanvas.Editor;
#endif

using System.Collections.Generic;
using ParadoxNotion;
using ParadoxNotion.Design;
using ParadoxNotion.Serialization;
using UnityEngine;

namespace NodeCanvas.Framework
{

    /// ConditionList is a ConditionTask itself that holds many ConditionTasks. It can be set to either require all true or any true.
    [DoNotList]
    public class ConditionList : ConditionTask
    {

        public enum ConditionsCheckMode
        {
            AllTrueRequired,
            AnyTrueSuffice
        }

        public ConditionsCheckMode checkMode;
        public List<ConditionTask> conditions = new List<ConditionTask>();

        private bool allTrueRequired { get { return checkMode == ConditionsCheckMode.AllTrueRequired; } }


        protected override string info
        {
            get
            {
                if (conditions.Count == 0)
                {
                    return "No Conditions";
                }

                string finalText = conditions.Count > 1 ? ("<b>(" + (allTrueRequired ? "ALL True" : "ANY True") + ")</b>\n") : string.Empty;
                for (int i = 0; i < conditions.Count; i++)
                {

                    if (conditions[i] == null)
                    {
                        continue;
                    }

                    if (conditions[i].isUserEnabled)
                    {
                        string prefix = "▪";
                        finalText += prefix + conditions[i].summaryInfo + (i == conditions.Count - 1 ? "" : "\n");
                    }
                }
                return finalText;
            }
        }

        ///ConditionList overrides to duplicate listed conditions correctly
        public override Task Duplicate(ITaskSystem newOwnerSystem)
        {
            ConditionList newList = (ConditionList)base.Duplicate(newOwnerSystem);
            newList.conditions.Clear();
            foreach (ConditionTask condition in conditions)
            {
                newList.AddCondition((ConditionTask)condition.Duplicate(newOwnerSystem));
            }

            return newList;
        }

        //Forward Enable call
        protected override void OnEnable()
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                conditions[i].Enable(agent, blackboard);
            }
        }

        //Forward Disable call
        protected override void OnDisable()
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                conditions[i].Disable();
            }
        }

        protected override bool OnCheck()
        {
            int succeedChecks = 0;
            for (int i = 0; i < conditions.Count; i++)
            {

                if (!conditions[i].isUserEnabled)
                {
                    succeedChecks++;
                    continue;
                }

                if (conditions[i].Check(agent, blackboard))
                {
                    if (!allTrueRequired)
                    {
                        return true;
                    }
                    succeedChecks++;

                }
                else
                {

                    if (allTrueRequired)
                    {
                        return false;
                    }
                }
            }

            return succeedChecks == conditions.Count;
        }

        public override void OnDrawGizmosSelected()
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                if (conditions[i].isUserEnabled)
                {
                    conditions[i].OnDrawGizmosSelected();
                }
            }
        }

        public void AddCondition(ConditionTask condition)
        {

            if (condition is ConditionList)
            {
                foreach (ConditionTask subCondition in (condition as ConditionList).conditions)
                {
                    AddCondition(subCondition);
                }
                return;
            }

#if UNITY_EDITOR
            UndoUtility.RecordObject(ownerSystem.contextObject, "List Add Task");
            currentViewCondition = condition;
#endif

            conditions.Add(condition);
            condition.SetOwnerSystem(ownerSystem);
        }

        internal override string GetWarningOrError()
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                string result = conditions[i].GetWarningOrError();
                if (result != null) { return result; }
            }
            return null;
        }


        ///----------------------------------------------------------------------------------------------
        ///---------------------------------------UNITY EDITOR-------------------------------------------
#if UNITY_EDITOR

        private ConditionTask currentViewCondition;

        //...
        protected override void OnTaskInspectorGUI()
        {
            ShowListGUI();
            ShowNestedConditionsGUI();
        }

        ///Show the sub-tasks list
        public void ShowListGUI()
        {

            TaskEditor.ShowCreateTaskSelectionButton<ConditionTask>(ownerSystem, AddCondition);

            ValidateList();

            if (conditions.Count == 0)
            {
                EditorGUILayout.HelpBox("No Conditions", MessageType.None);
                return;
            }

            if (conditions.Count == 1) { return; }

            EditorUtils.ReorderableList(conditions, (i, picked) =>
            {
                ConditionTask condition = conditions[i];
                GUI.color = Color.white.WithAlpha(condition == currentViewCondition ? 0.75f : 0.25f);
                GUILayout.BeginHorizontal("box");

                GUI.color = Color.white.WithAlpha(condition.isUserEnabled ? 0.8f : 0.25f);
                GUI.enabled = !Application.isPlaying;
                condition.isUserEnabled = EditorGUILayout.Toggle(condition.isUserEnabled, GUILayout.Width(18));
                GUI.enabled = true;

                GUILayout.Label(condition.summaryInfo, GUILayout.MinWidth(0), GUILayout.ExpandWidth(true));

                if (!Application.isPlaying && GUILayout.Button("X", GUILayout.MaxWidth(20)))
                {
                    UndoUtility.RecordObject(ownerSystem.contextObject, "List Remove Task");
                    conditions.RemoveAt(i);
                    if (conditions.Count == 1) { conditions[0].isUserEnabled = true; }
                }

                GUILayout.EndHorizontal();

                Rect lastRect = GUILayoutUtility.GetLastRect();
                EditorGUIUtility.AddCursorRect(lastRect, MouseCursor.Link);
                if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
                {
                    currentViewCondition = condition == currentViewCondition ? null : condition;
                    Event.current.Use();
                }

                GUI.color = Color.white;
            });

            checkMode = (ConditionsCheckMode)EditorGUILayout.EnumPopup(checkMode);
        }

        ///Show currently selected task inspector
        public void ShowNestedConditionsGUI()
        {

            if (conditions.Count == 1)
            {
                currentViewCondition = conditions[0];
            }

            if (currentViewCondition != null)
            {
                EditorUtils.Separator();
                TaskEditor.TaskFieldSingle(currentViewCondition, (c) =>
                {
                    if (c == null)
                    {
                        int i = conditions.IndexOf(currentViewCondition);
                        conditions.RemoveAt(i);
                    }
                    currentViewCondition = (ConditionTask)c;
                });
            }
        }

        //Validate possible null tasks
        private void ValidateList()
        {
            for (int i = conditions.Count; i-- > 0;)
            {
                if (conditions[i] == null)
                {
                    conditions.RemoveAt(i);
                }
            }
        }

        [ContextMenu("Save List Preset")]
        private void DoSavePreset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Preset", "", "conditionList", "");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, JSONSerializer.Serialize(typeof(ConditionList), this, null, true)); //true for pretyJson
                AssetDatabase.Refresh();
            }
        }

        [ContextMenu("Load List Preset")]
        private void DoLoadPreset()
        {
            string path = EditorUtility.OpenFilePanel("Load Preset", "Assets", "conditionList");
            if (!string.IsNullOrEmpty(path))
            {
                string json = System.IO.File.ReadAllText(path);
                ConditionList list = JSONSerializer.TryDeserializeOverwrite<ConditionList>(this, json);
                conditions = list.conditions;
                checkMode = list.checkMode;
                currentViewCondition = null;
                foreach (ConditionTask a in conditions)
                {
                    a.SetOwnerSystem(ownerSystem);
                }
            }
        }


#endif
    }
}