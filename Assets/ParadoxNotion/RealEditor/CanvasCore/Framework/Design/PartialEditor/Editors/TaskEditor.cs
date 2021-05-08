﻿#if UNITY_EDITOR

using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion;
using ParadoxNotion.Design;
using System;
using UnityEditor;
using UnityEngine;

namespace NodeCanvas.Editor
{

    public class TaskEditor : EditorObjectWrapper<Task>
    {

        private bool isUnfolded = true;
        private EditorPropertyWrapper<TaskAgentParameter> agentParameterProp;
        private EditorMethodWrapper onTaskInspectorGUI;

        private Task task { get { return target; } }

        protected override void OnEnable()
        {
            agentParameterProp = CreatePropertyWrapper<TaskAgentParameter>("_agentParameter");
            onTaskInspectorGUI = CreateMethodWrapper("OnTaskInspectorGUI");
        }

        ///----------------------------------------------------------------------------------------------

        ///Show a Task's field without ability to add if null or add multiple tasks to form a list.
        public static void TaskFieldSingle(Task task, Action<Task> callback, bool showTitlebar = true)
        {
            if (task != null) { ShowTaskInspectorGUI(task, callback, showTitlebar); }
        }

        ///Show a Task's field. If task null allow add task. Multiple tasks can be added to form a list.
        public static void TaskFieldMulti<T>(T task, ITaskSystem ownerSystem, Action<T> callback) where T : Task
        {
            TaskFieldMulti(task, ownerSystem, typeof(T), (Task t) => { callback((T)t); });
        }

        ///Show a Task's field. If task null allow add task. Multiple tasks can be added to form a list.
        public static void TaskFieldMulti(Task task, ITaskSystem ownerSystem, Type baseType, Action<Task> callback)
        {
            //if null simply show an assignment button
            if (task == null)
            {
                ShowCreateTaskSelectionButton(ownerSystem, baseType, callback);
                return;
            }

            //Handle Action/ActionLists so that in GUI level a list is used only when needed
            if (baseType == typeof(ActionTask))
            {
                if (!(task is ActionList))
                {
                    ShowCreateTaskSelectionButton(ownerSystem, baseType, (t) =>
                        {
                            ActionList newList = Task.Create<ActionList>(ownerSystem);
                            UndoUtility.RecordObject(ownerSystem.contextObject, "New Action Task");
                            newList.AddAction((ActionTask)task);
                            newList.AddAction((ActionTask)t);
                            callback(newList);
                        });
                }

                ShowTaskInspectorGUI(task, callback);

                if (task is ActionList)
                {
                    ActionList list = (ActionList)task;
                    if (list.actions.Count == 1)
                    {
                        list.actions[0].isUserEnabled = true;
                        callback(list.actions[0]);
                    }
                }
                return;
            }

            //Handle Condition/ConditionLists so that in GUI level a list is used only when needed
            if (baseType == typeof(ConditionTask))
            {
                if (!(task is ConditionList))
                {
                    ShowCreateTaskSelectionButton(ownerSystem, baseType, (t) =>
                        {
                            ConditionList newList = Task.Create<ConditionList>(ownerSystem);
                            UndoUtility.RecordObject(ownerSystem.contextObject, "New Condition Task");
                            newList.AddCondition((ConditionTask)task);
                            newList.AddCondition((ConditionTask)t);
                            callback(newList);
                        });
                }

                ShowTaskInspectorGUI(task, callback);

                if (task is ConditionList)
                {
                    ConditionList list = (ConditionList)task;
                    if (list.conditions.Count == 1)
                    {
                        list.conditions[0].isUserEnabled = true;
                        callback(list.conditions[0]);
                    }
                }
                return;
            }

            //in all other cases where the base type is not a base ActionTask or ConditionTask,
            //(thus lists can't be used unless the base type IS a list), simple show the inspector.
            ShowTaskInspectorGUI(task, callback);
        }

        ///Show the editor inspector of target task
        private static void ShowTaskInspectorGUI(Task task, Action<Task> callback, bool showTitlebar = true)
        {
            EditorWrapperFactory.GetEditor<TaskEditor>(task).ShowInspector(callback, showTitlebar);
        }

        //Shows a button that when clicked, pops a context menu with a list of tasks deriving the base type specified. When something is selected the callback is called
        public static void ShowCreateTaskSelectionButton<T>(ITaskSystem ownerSystem, Action<T> callback) where T : Task
        {
            ShowCreateTaskSelectionButton(ownerSystem, typeof(T), (Task t) => { callback((T)t); });
        }

        //Shows a button that when clicked, pops a context menu with a list of tasks deriving the base type specified. When something is selected the callback is called
        //On top of that it also shows a search field for Tasks
        public static void ShowCreateTaskSelectionButton(ITaskSystem ownerSystem, Type baseType, Action<Task> callback)
        {

            GUI.backgroundColor = Colors.lightBlue;
            string label = "Assign " + baseType.Name.SplitCamelCase();
            if (GUILayout.Button(label))
            {

                Action<Type> TaskTypeSelected = (t) =>
                {
                    Task newTask = Task.Create(t, ownerSystem);
                    UndoUtility.RecordObject(ownerSystem.contextObject, "New Task");
                    callback(newTask);
                };

                GenericMenu menu = EditorUtils.GetTypeSelectionMenu(baseType, TaskTypeSelected);
                if (CopyBuffer.TryGetCache<Task>(out Task copy) && baseType.IsAssignableFrom(copy.GetType()))
                {
                    menu.AddSeparator("/");
                    menu.AddItem(new GUIContent(string.Format("Paste ({0})", copy.name)), false, () => { callback(copy.Duplicate(ownerSystem)); });
                }
                menu.ShowAsBrowser(label, typeof(Task));
            }

            GUILayout.Space(2);
            GUI.backgroundColor = Color.white;
        }


        ///----------------------------------------------------------------------------------------------


        //Draw the task inspector GUI
        private void ShowInspector(Action<Task> callback, bool showTitlebar = true)
        {
            if (task.ownerSystem == null)
            {
                GUILayout.Label("<b>Owner System is null! This should really not happen but it did!\nPlease report a bug. Thank you :)</b>");
                return;
            }

            //make sure TaskAgent is not null in case task defines an AgentType
            if (task.agentIsOverride && agentParameterProp.value == null)
            {
                agentParameterProp.value = new TaskAgentParameter();
            }

            if (task.obsolete != string.Empty)
            {
                EditorGUILayout.HelpBox(string.Format("This is an obsolete Task:\n\"{0}\"", task.obsolete), MessageType.Warning);
            }

            if (!showTitlebar || ShowTitlebar(callback) == true)
            {

                if (Prefs.showNodeInfo && !string.IsNullOrEmpty(task.description))
                {
                    EditorGUILayout.HelpBox(task.description, MessageType.None);
                }

                UndoUtility.CheckUndo(task.ownerSystem.contextObject, "Task Inspector");

                SpecialCaseInspector();
                ShowAgentField();
                onTaskInspectorGUI.Invoke();

                UndoUtility.CheckDirty(task.ownerSystem.contextObject);
            }
        }

        //Some special cases for Action & Condition. A bit weird but better than creating a virtual method in this case
        private void SpecialCaseInspector()
        {

            if (task is ActionTask)
            {
                if (Application.isPlaying)
                {
                    if ((task as ActionTask).elapsedTime > 0)
                    {
                        GUI.color = Color.yellow;
                    }

                    EditorGUILayout.LabelField("Elapsed Time", (task as ActionTask).elapsedTime.ToString());
                    GUI.color = Color.white;
                }
            }

            if (task is ConditionTask)
            {
                GUI.color = (task as ConditionTask).invert ? Color.white : new Color(1f, 1f, 1f, 0.5f);
                (task as ConditionTask).invert = EditorGUILayout.ToggleLeft("Invert Condition", (task as ConditionTask).invert);
                GUI.color = Color.white;
            }
        }

        //a Custom titlebar for tasks
        private bool ShowTitlebar(Action<Task> callback)
        {

            GUI.backgroundColor = EditorGUIUtility.isProSkin ? Color.black.WithAlpha(0.3f) : Color.white.WithAlpha(0.5f);
            GUILayout.BeginHorizontal(GUI.skin.box);
            {
                GUI.backgroundColor = Color.white;
                GUILayout.Label("<b>" + (isUnfolded ? "▼ " : "► ") + task.name + "</b>" + (isUnfolded ? "" : "\n<i><size=10>(" + task.summaryInfo + ")</size></i>"), Styles.leftLabel);
                if (GUILayout.Button(Icons.csIcon, GUI.skin.label, GUILayout.Width(20), GUILayout.Height(20)))
                {
                    EditorUtils.OpenScriptOfType(task.GetType());
                }

                GUI.color = EditorGUIUtility.isProSkin ? Color.white : Color.grey;
                if (GUILayout.Button(Icons.gearPopupIcon, Styles.centerLabel, GUILayout.Width(20), GUILayout.Height(20)))
                {
                    GetMenu(callback).ShowAsContext();
                }
                GUI.color = Color.white;
            }
            GUILayout.EndHorizontal();

            Rect titleRect = GUILayoutUtility.GetLastRect();
            EditorGUIUtility.AddCursorRect(titleRect, MouseCursor.Link);

            Event e = Event.current;
            if (e.type == EventType.ContextClick && titleRect.Contains(e.mousePosition))
            {
                GetMenu(callback).ShowAsContext();
                e.Use();
            }

            if (e.button == 0 && e.type == EventType.MouseUp && titleRect.Contains(e.mousePosition))
            {
                isUnfolded = !isUnfolded;
                e.Use();
            }

            return isUnfolded;
        }

        ///Generate and return task menu
        private GenericMenu GetMenu(Action<Task> callback)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Open Script"), false, () => { EditorUtils.OpenScriptOfType(task.GetType()); });
            menu.AddItem(new GUIContent("Copy"), false, () => { CopyBuffer.SetCache<Task>(task); });

            foreach (System.Reflection.MethodInfo _m in task.GetType().RTGetMethods())
            {
                System.Reflection.MethodInfo m = _m;
                ContextMenu att = m.RTGetAttribute<ContextMenu>(true);
                if (att != null)
                {
                    menu.AddItem(new GUIContent(att.menuItem), false, () => { m.Invoke(task, null); });
                }
            }

            menu.AddSeparator("/");

            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                if (callback != null)
                {
                    UndoUtility.RecordObject(task.ownerSystem.contextObject, "Delete Task");
                    callback(null);
                }
            });

            return menu;
        }

        //Shows the agent field in case an agent type is specified through the use of the generic versions of Action or Condition Task
        private void ShowAgentField()
        {

            if (task.agentType == null)
            {
                return;
            }

            TaskAgentParameter agentParam = agentParameterProp.value;

            if (Application.isPlaying && task.agentIsOverride && agentParam.value == null)
            {
                GUILayout.Label("<b>Missing Agent Reference</b>".FormatError());
                return;
            }

            GUI.color = Color.white.WithAlpha(task.agentIsOverride ? 0.65f : 0.5f);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();

            if (task.agentIsOverride)
            {

                BBParameterEditor.ParameterField(null, agentParam, task.ownerSystem.contextObject);

            }
            else
            {

                string compInfo = task.agent == null ? task.agentType.FriendlyName().FormatError() : task.agentType.FriendlyName();
                Texture icon = TypePrefs.GetTypeIcon(task.agentType);
                string label = string.Format("Use Self ({0})", compInfo);
                GUIContent content = EditorUtils.GetTempContent(label, icon);
                GUILayout.Label(content, GUILayout.Height(18), GUILayout.Width(0), GUILayout.ExpandWidth(true));
            }

            GUI.color = Color.white;

            if (!Application.isPlaying)
            {
                bool newOverride = EditorGUILayout.Toggle(task.agentIsOverride, GUILayout.Width(18));
                if (newOverride != task.agentIsOverride)
                {
                    UndoUtility.RecordObject(task.ownerSystem.contextObject, "Override Agent");
                    task.agentIsOverride = newOverride;
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

    }
}

#endif