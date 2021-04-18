#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Logger = ParadoxNotion.Services.Logger;
using UnityObject = UnityEngine.Object;

namespace ParadoxNotion.Design
{

    /// Specific Editor GUIs
    public partial class EditorUtils
    {

        ///Stores fold states
		private static readonly Dictionary<Type, bool> registeredEditorFoldouts = new Dictionary<Type, bool>();


        ///A cool label :-P (for headers)
        public static void CoolLabel(string text)
        {
            GUI.skin.label.richText = true;
            GUI.color = Colors.lightOrange;
            GUILayout.Label("<b><size=14>" + text + "</size></b>", Styles.topLeftLabel);
            GUI.color = Color.white;
            GUILayout.Space(2);
        }

        ///Combines the rest functions for a header style label
        public static void TitledSeparator(string title)
        {
            GUILayout.Space(1);
            BoldSeparator();
            CoolLabel(title + " ▼");
            Separator();
        }

        ///A thin separator
        public static void Separator()
        {
            Rect lastRect = GUILayoutUtility.GetLastRect();
            GUILayout.Space(7);
            GUI.color = new Color(0, 0, 0, 0.3f);
            GUI.DrawTexture(Rect.MinMaxRect(lastRect.xMin, lastRect.yMax + 4, lastRect.xMax, lastRect.yMax + 6), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        ///A thick separator similar to ngui. Thanks
        public static void BoldSeparator()
        {
            Rect lastRect = GUILayoutUtility.GetLastRect();
            GUILayout.Space(14);
            GUI.color = new Color(0, 0, 0, 0.3f);
            GUI.DrawTexture(new Rect(0, lastRect.yMax + 6, Screen.width, 4), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, lastRect.yMax + 6, Screen.width, 1), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, lastRect.yMax + 9, Screen.width, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        ///Just a fancy ending for inspectors
        public static void EndOfInspector()
        {
            Rect lastRect = GUILayoutUtility.GetLastRect();
            GUILayout.Space(8);
            GUI.color = new Color(0, 0, 0, 0.4f);
            GUI.DrawTexture(new Rect(0, lastRect.yMax + 6, Screen.width, 4), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, lastRect.yMax + 4, Screen.width, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        ///A Search Field
        public static string SearchField(string search)
        {
            GUILayout.BeginHorizontal();
            search = EditorGUILayout.TextField(search, Styles.toolbarSearchTextField);
            if (GUILayout.Button(string.Empty, Styles.toolbarSearchCancelButton))
            {
                search = string.Empty;
                GUIUtility.keyboardControl = 0;
            }
            GUILayout.EndHorizontal();
            return search;
        }

        ///Used just after a textfield with no prefix to show an italic transparent text inside when empty
        public static void CommentLastTextField(string check, string comment = "Comments...")
        {
            if (string.IsNullOrEmpty(check))
            {
                Rect lastRect = GUILayoutUtility.GetLastRect();
                GUI.Label(lastRect, " <i>" + comment + "</i>", Styles.topLeftLabel);
            }
        }

        ///Used just after a field to highlight it
        public static void HighlightLastField()
        {
            Rect lastRect = GUILayoutUtility.GetLastRect();
            lastRect.xMin += 2;
            lastRect.xMax -= 2;
            lastRect.yMax -= 4;
            Styles.Draw(lastRect, Styles.highlightBox);
        }

        ///Used just after a field to mark it as a prefab override (similar to native unity's one)
        public static void MarkLastFieldOverride()
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            rect.x -= 3; rect.width = 2;
            GUI.color = Colors.prefabOverrideColor;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        ///Used just after a field to mark warning icon to it
        public static void MarkLastFieldWarning(string tooltip)
        {
            Internal_MarkLastField(ParadoxNotion.Design.Icons.warningIcon, tooltip);
        }

        ///Used just after a field to mark warning icon to it
        public static void MarkLastFieldError(string tooltip)
        {
            Internal_MarkLastField(ParadoxNotion.Design.Icons.errorIcon, tooltip);
        }

        //...
        private static void Internal_MarkLastField(Texture2D icon, string tooltip)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            rect.x += UnityEditor.EditorGUIUtility.labelWidth;
            rect.x -= 16;
            rect.y += 1;
            rect.width = 16;
            rect.height = 16;
            GUI.Box(rect, EditorUtils.GetTempContent(null, icon, tooltip), GUIStyle.none);
        }

        // public static Rect BeginHighlightArea() {
        //     var rect = GUILayoutUtility.GetLastRect();
        //     GUILayout.BeginVertical();
        //     return rect;
        // }

        // public static void EndHighlightArea(Rect beginRect) {
        //     GUILayout.EndVertical();
        //     var last = GUILayoutUtility.GetLastRect();
        //     var rect = Rect.MinMaxRect(beginRect.xMin, beginRect.yMin, last.xMax, last.yMax);
        //     Styles.Draw(rect, Styles.highlightBox);
        // }

        ///Editor for LayerMask
		public static LayerMask LayerMaskField(string prefix, LayerMask layerMask, params GUILayoutOption[] layoutOptions)
        {
            return LayerMaskField(string.IsNullOrEmpty(prefix) ? GUIContent.none : new GUIContent(prefix), layerMask, layoutOptions);
        }

        ///Editor for LayerMask
        public static LayerMask LayerMaskField(GUIContent content, LayerMask layerMask, params GUILayoutOption[] layoutOptions)
        {
            LayerMask tempMask = EditorGUILayout.MaskField(content, UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(layerMask), UnityEditorInternal.InternalEditorUtility.layers, layoutOptions);
            layerMask = UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
            return layerMask;
        }

        ///Do a cached editor Foldout based on provided key object
        public static bool CachedFoldout(Type key, GUIContent content)
        {
            bool foldout = false;
            registeredEditorFoldouts.TryGetValue(key, out foldout);
            foldout = EditorGUILayout.Foldout(foldout, content);
            return registeredEditorFoldouts[key] = foldout;
        }

        ///An IList editor (List<T> and Arrays)
        public static IList ListEditor(GUIContent content, IList list, Type listType, InspectedFieldInfo info)
        {

            ListInspectorOptionAttribute optionsAtt = info.attributes?.FirstOrDefault(x => x is ListInspectorOptionAttribute) as ListInspectorOptionAttribute;

            Type argType = listType.GetEnumerableElementType();
            if (argType == null)
            {
                return list;
            }

            if (object.Equals(list, null))
            {
                GUILayout.Label("Null List");
                return list;
            }

            if (optionsAtt == null || optionsAtt.showFoldout)
            {
                if (!CachedFoldout(listType, content))
                {
                    return list;
                }
            }
            else
            {
                GUILayout.Label(content.text);
            }

            GUILayout.BeginVertical();
            EditorGUI.indentLevel++;

            ReorderableListOptions options = new ReorderableListOptions();
            options.allowAdd = optionsAtt == null || optionsAtt.allowAdd;
            options.allowRemove = optionsAtt == null || optionsAtt.allowRemove;
            options.unityObjectContext = info.unityObjectContext;
            list = EditorUtils.ReorderableList(list, options, (i, r) =>
            {
                list[i] = ReflectedFieldInspector("Element " + i, list[i], argType, info);
            });

            EditorGUI.indentLevel--;
            Separator();
            GUILayout.EndVertical();
            return list;
        }

        ///A IDictionary editor
        public static IDictionary DictionaryEditor(GUIContent content, IDictionary dict, Type dictType, InspectedFieldInfo info)
        {

            Type keyType = dictType.RTGetGenericArguments()[0];
            Type valueType = dictType.RTGetGenericArguments()[1];

            if (object.Equals(dict, null))
            {
                GUILayout.Label("Null Dictionary");
                return dict;
            }

            if (!CachedFoldout(dictType, content))
            {
                return dict;
            }

            GUILayout.BeginVertical();

            List<object> keys = dict.Keys.Cast<object>().ToList();
            List<object> values = dict.Values.Cast<object>().ToList();

            if (GUILayout.Button("Add Element"))
            {
                if (!typeof(UnityObject).IsAssignableFrom(keyType))
                {
                    object newKey = null;
                    if (keyType == typeof(string))
                    {
                        newKey = string.Empty;
                    }
                    else
                    {
                        newKey = Activator.CreateInstance(keyType);
                    }

                    if (dict.Contains(newKey))
                    {
                        Logger.LogWarning(string.Format("Key '{0}' already exists in Dictionary", newKey.ToString()), "Editor");
                        return dict;
                    }

                    keys.Add(newKey);

                }
                else
                {
                    Logger.LogWarning("Can't add a 'null' Dictionary Key", "Editor");
                    return dict;
                }

                values.Add(valueType.IsValueType ? Activator.CreateInstance(valueType) : null);
            }

            for (int i = 0; i < keys.Count; i++)
            {
                GUILayout.BeginHorizontal("box");
                GUILayout.Box("", GUILayout.Width(6), GUILayout.Height(35));

                GUILayout.BeginVertical();
                keys[i] = ReflectedFieldInspector("K:", keys[i], keyType, info);
                values[i] = ReflectedFieldInspector("V:", values[i], valueType, info);
                GUILayout.EndVertical();

                if (GUILayout.Button("X", GUILayout.Width(18), GUILayout.Height(34)))
                {
                    keys.RemoveAt(i);
                    values.RemoveAt(i);
                }

                GUILayout.EndHorizontal();
            }

            //clear and reconstruct on separate pass after GUI controls
            dict.Clear();
            for (int i = 0; i < keys.Count; i++)
            {
                try { dict.Add(keys[i], values[i]); }
                catch { Logger.Log("Dictionary Key removed due to duplicate found", "Editor"); }
            }

            Separator();

            GUILayout.EndVertical();
            return dict;
        }


        ///An editor field where if the component is null simply shows an object field, but if its not, shows a dropdown popup to select the specific component
        ///from within the gameobject
        public static Component ComponentField(GUIContent content, Component comp, Type type, params GUILayoutOption[] GUIOptions)
        {

            if (comp == null)
            {
                return EditorGUILayout.ObjectField(content, comp, type, true, GUIOptions) as Component;
            }

            List<Component> components = comp.GetComponents(type).ToList();
            List<string> componentNames = components.Where(c => c != null).Select(c => c.GetType().FriendlyName() + " (" + c.gameObject.name + ")").ToList();
            componentNames.Insert(0, "[NONE]");

            int index = components.IndexOf(comp);
            index = EditorGUILayout.Popup(content, index, componentNames.Select(n => new GUIContent(n)).ToArray(), GUIOptions);
            return index == 0 ? null : components[index];
        }


        ///A popup that is based on the string rather than the index
        public static string StringPopup(GUIContent content, string selected, IEnumerable<string> options, params GUILayoutOption[] GUIOptions)
        {
            EditorGUILayout.BeginVertical();
            int index = 0;
            List<string> copy = new List<string>(options);
            copy.Insert(0, "[NONE]");
            index = copy.Contains(selected) ? copy.IndexOf(selected) : 0;
            index = EditorGUILayout.Popup(content, index, copy.Select(n => new GUIContent(n)).ToArray(), GUIOptions);
            EditorGUILayout.EndVertical();
            return index == 0 ? string.Empty : copy[index];
        }


        ///Generic Popup for selection of any element within a list
        public static T Popup<T>(T selected, IEnumerable<T> options, params GUILayoutOption[] GUIOptions)
        {
            return Popup<T>(GUIContent.none, selected, options, GUIOptions);
        }

        ///Generic Popup for selection of any element within a list
        public static T Popup<T>(string prefix, T selected, IEnumerable<T> options, params GUILayoutOption[] GUIOptions)
        {
            return Popup<T>(string.IsNullOrEmpty(prefix) ? GUIContent.none : new GUIContent(prefix), selected, options, GUIOptions);
        }

        ///Generic Popup for selection of any element within a list
        public static T Popup<T>(GUIContent content, T selected, IEnumerable<T> options, params GUILayoutOption[] GUIOptions)
        {
            List<T> listOptions = new List<T>(options);
            listOptions.Insert(0, default(T));
            List<string> stringedOptions = new List<string>(listOptions.Select(o => o != null ? o.ToString() : "[NONE]"));
            stringedOptions[0] = listOptions.Count == 1 ? "[NONE AVAILABLE]" : "[NONE]";

            int index = 0;
            if (listOptions.Contains(selected))
            {
                index = listOptions.IndexOf(selected);
            }

            bool wasEnable = GUI.enabled;
            GUI.enabled = wasEnable && stringedOptions.Count > 1;
            index = EditorGUILayout.Popup(content, index, stringedOptions.Select(s => new GUIContent(s)).ToArray(), GUIOptions);
            GUI.enabled = wasEnable;
            return index == 0 ? default(T) : listOptions[index];
        }


        ///Generic Button Popup for selection of any element within a list
        public static void ButtonPopup<T>(string prefix, T selected, List<T> options, Action<T> Callback)
        {
            ButtonPopup<T>(string.IsNullOrEmpty(prefix) ? GUIContent.none : new GUIContent(prefix), selected, options, Callback);
        }

        ///Generic Button Popup for selection of any element within a list
        public static void ButtonPopup<T>(GUIContent content, T selected, List<T> options, Action<T> Callback)
        {
            string buttonText = selected != null ? selected.ToString() : "[NONE]";
            GUILayout.BeginHorizontal();
            if (content != null && content != GUIContent.none)
            {
                GUILayout.Label(content, GUILayout.Width(0), GUILayout.ExpandWidth(true));
            }
            if (GUILayout.Button(buttonText, "MiniPopup", GUILayout.Width(0), GUILayout.ExpandWidth(true)))
            {
                GenericMenu menu = new GenericMenu();
                foreach (T _option in options)
                {
                    T option = _option;
                    menu.AddItem(new GUIContent(option != null ? option.ToString() : "[NONE]"), object.Equals(selected, option), () => { Callback(option); });
                }
                menu.ShowAsBrowser("Select Option");
            }
            GUILayout.EndHorizontal();
        }

        ///Specialized Type button popup
        public static void ButtonTypePopup(string prefix, Type selected, Action<Type> Callback)
        {
            ButtonTypePopup(string.IsNullOrEmpty(prefix) ? GUIContent.none : new GUIContent(prefix), selected, Callback);
        }

        ///Specialized Type button popup
        public static void ButtonTypePopup(GUIContent content, Type selected, Action<Type> Callback)
        {
            string buttonText = selected != null ? selected.FriendlyName() : "[NONE]";
            GUILayout.BeginHorizontal();
            if (content != null && content != GUIContent.none)
            {
                GUILayout.Label(content, GUILayout.Width(0), GUILayout.ExpandWidth(true));
            }
            if (GUILayout.Button(buttonText, "MiniPopup", GUILayout.Width(0), GUILayout.ExpandWidth(true)))
            {
                GenericMenu menu = EditorUtils.GetPreferedTypesSelectionMenu(typeof(object), Callback);
                menu.ShowAsBrowser("Select Type");
            }
            GUILayout.EndHorizontal();
        }
    }
}

#endif