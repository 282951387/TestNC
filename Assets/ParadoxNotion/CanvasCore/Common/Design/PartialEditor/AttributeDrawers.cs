﻿#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ParadoxNotion.Design
{

    ///Will dim control for bool, int, float, string if its default value (or empty for string)
    public class DimIfDefaultDrawer : AttributeDrawer<DimIfDefaultAttribute>
    {
        public override object OnGUI(GUIContent content, object instance)
        {
            bool dim = false;

            if (fieldInfo.FieldType.IsClass)
            {
                dim = instance == null;
            }

            if (fieldInfo.FieldType == typeof(bool))
            {
                dim = (bool)instance == false;
            }

            if (fieldInfo.FieldType == typeof(int))
            {
                dim = (int)instance == 0;
            }

            if (fieldInfo.FieldType == typeof(float))
            {
                dim = (float)instance == 0;
            }

            if (fieldInfo.FieldType == typeof(string))
            {
                dim = string.IsNullOrEmpty((string)instance);
            }

            if (dim) { GUI.color = GUI.color.WithAlpha(0.5f); }
            instance = MoveNextDrawer();
            GUI.color = Color.white;
            return instance;
        }
    }

    ///Will show value only if another field is equal to target
	public class ShowIfDrawer : AttributeDrawer<ShowIfAttribute>
    {
        public override object OnGUI(GUIContent content, object instance)
        {
            System.Reflection.FieldInfo targetField = context.GetType().RTGetField(attribute.fieldName);
            if (targetField != null)
            {
                int fieldValue = (int)System.Convert.ChangeType(targetField.GetValue(context), typeof(int));
                if (fieldValue != attribute.checkValue)
                {
                    return instance; //return instance without any editor (thus hide it)
                }
            }
            return MoveNextDrawer();
        }
    }

    ///Will show in red if value is null or empty
	public class RequiredFieldDrawer : AttributeDrawer<RequiredFieldAttribute>
    {
        public override object OnGUI(GUIContent content, object instance)
        {
            bool isNull = instance == null || instance.Equals(null) || ((instance is string) && string.IsNullOrEmpty((string)instance));
            instance = MoveNextDrawer();
            if (isNull) { EditorUtils.MarkLastFieldError("An instance is required."); }
            return instance;
        }
    }

    ///Will show a button above field
    public class ShowButtonDrawer : AttributeDrawer<ShowButtonAttribute>
    {
        public override object OnGUI(GUIContent content, object instance)
        {
            if (!string.IsNullOrEmpty(attribute.methodName))
            {
                System.Reflection.MethodInfo method = info.wrapperInstanceContext.GetType().RTGetMethod(attribute.methodName);
                if (method != null && method.GetParameters().Length == 0)
                {
                    if (GUILayout.Button(attribute.buttonTitle))
                    {
                        method.Invoke(info.wrapperInstanceContext, null);
                    }
                }
                else
                {
                    GUILayout.Label(string.Format("Can't find ShowIf method '{0}'.", attribute.methodName));
                }
            }
            return MoveNextDrawer();
        }
    }

    ///Will invoke a callback method when value change
	public class CallbackDrawer : AttributeDrawer<CallbackAttribute>
    {
        public override object OnGUI(GUIContent content, object instance)
        {
            object newValue = MoveNextDrawer();
            if (!Equals(newValue, instance))
            {
                System.Reflection.MethodInfo method = info.wrapperInstanceContext.GetType().RTGetMethod(attribute.methodName);
                if (method != null && method.GetParameters().Length == 0)
                {
                    fieldInfo.SetValue(context, newValue); //manual set field before invoke
                    method.Invoke(info.wrapperInstanceContext, null);
                }
                else
                {
                    GUILayout.Label(string.Format("Can't find Callback method '{0}'.", attribute.methodName));
                }
            }
            return newValue;
        }
    }

    ///----------------------------------------------------------------------------------------------

    ///Will clamp float or int value to min
    public class MinValueDrawer : AttributeDrawer<MinValueAttribute>
    {
        public override object OnGUI(GUIContent content, object instance)
        {
            if (fieldInfo.FieldType == typeof(float))
            {
                return Mathf.Max((float)MoveNextDrawer(), attribute.min);
            }
            if (fieldInfo.FieldType == typeof(int))
            {
                return Mathf.Max((int)MoveNextDrawer(), (int)attribute.min);
            }
            return MoveNextDrawer();
        }
    }

    ///----------------------------------------------------------------------------------------------

    ///Will make float, int or string field show in a delayed control
    public class DelayedFieldDrawer : AttributeDrawer<DelayedFieldAttribute>
    {
        public override object OnGUI(GUIContent content, object instance)
        {
            if (fieldInfo.FieldType == typeof(float))
            {
                return EditorGUILayout.DelayedFloatField(content, (float)instance);
            }
            if (fieldInfo.FieldType == typeof(int))
            {
                return EditorGUILayout.DelayedIntField(content, (int)instance);
            }
            if (fieldInfo.FieldType == typeof(string))
            {
                return EditorGUILayout.DelayedTextField(content, (string)instance);
            }
            return MoveNextDrawer();
        }
    }

    ///Will force to use ObjectField editor, usefull for interfaces
	public class ForceObjectFieldDrawer : AttributeDrawer<ForceObjectFieldAttribute>
    {
        public override object OnGUI(GUIContent content, object instance)
        {
            if (typeof(UnityEngine.Object).IsAssignableFrom(fieldInfo.FieldType) || fieldInfo.FieldType.IsInterface)
            {
                return EditorGUILayout.ObjectField(content, instance as UnityEngine.Object, fieldInfo.FieldType, true);
            }
            return MoveNextDrawer();
        }
    }

    ///Will restrict selection on provided values
	public class PopupFieldDrawer : AttributeDrawer<PopupFieldAttribute>
    {
        public override object OnGUI(GUIContent content, object instance)
        {
            if (attribute.options != null && attribute.options.Length > 0)
            {
                System.Type optType = attribute.options[0].GetType();
                if (fieldInfo.FieldType.IsAssignableFrom(optType))
                {
                    return EditorUtils.Popup<object>(content, instance, attribute.options);
                }
            }
            return MoveNextDrawer();
        }
    }

    ///Will show a slider for int and float values
    public class SliderFieldDrawer : AttributeDrawer<SliderFieldAttribute>
    {
        public override object OnGUI(GUIContent content, object instance)
        {
            if (fieldInfo.FieldType == typeof(float))
            {
                return EditorGUILayout.Slider(content, (float)instance, attribute.min, attribute.max);
            }
            if (fieldInfo.FieldType == typeof(int))
            {
                return EditorGUILayout.IntSlider(content, (int)instance, (int)attribute.min, (int)attribute.max);
            }
            return MoveNextDrawer();
        }
    }

    ///Will show a layer selection for int values
	public class LayerFieldDrawer : AttributeDrawer<LayerFieldAttribute>
    {
        public override object OnGUI(GUIContent content, object instance)
        {
            if (fieldInfo.FieldType == typeof(int))
            {
                return EditorGUILayout.LayerField(content, (int)instance);
            }
            return MoveNextDrawer();
        }
    }

    ///Will show a Tag selection for string values
	public class TagFieldDrawer : AttributeDrawer<TagFieldAttribute>
    {
        public override object OnGUI(GUIContent content, object instance)
        {
            if (fieldInfo.FieldType == typeof(string))
            {
                return EditorGUILayout.TagField(content, (string)instance);
            }
            return MoveNextDrawer();
        }
    }

    ///Will show a text area for string values
	public class TextAreaDrawer : AttributeDrawer<TextAreaFieldAttribute>
    {
        private static GUIStyle areaStyle;
        static TextAreaDrawer()
        {
            areaStyle = new GUIStyle(GUI.skin.GetStyle("TextArea"));
            areaStyle.wordWrap = true;
        }
        public override object OnGUI(GUIContent content, object instance)
        {
            if (fieldInfo.FieldType == typeof(string))
            {
                GUILayout.Label(content);
                return EditorGUILayout.TextArea((string)instance, areaStyle, GUILayout.Height(attribute.numberOfLines * areaStyle.lineHeight));
            }
            return MoveNextDrawer();
        }
    }
}

#endif