#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace ParadoxNotion.Design
{

    /// ContextMenus, mostly reflection ones
    public partial class EditorUtils
    {

        ///A generic purpose menu to pick an item
        public static GenericMenu GetMenu<T>(List<T> options, T current, Action<T> callback)
        {
            GenericMenu menu = new GenericMenu();
            foreach (T _option in options)
            {
                T option = _option;
                string label = option != null ? option.ToString() : "null";
                menu.AddItem(new GUIContent(label), object.Equals(current, option), () => { callback(option); });
            }
            return menu;
        }

        ///Get a selection menu of types deriving base type
        public static GenericMenu GetTypeSelectionMenu(Type baseType, Action<Type> callback, GenericMenu menu = null, string subCategory = null)
        {

            if (menu == null)
            {
                menu = new GenericMenu();
            }

            if (subCategory != null)
            {
                subCategory = subCategory + "/";
            }

            GenericMenu.MenuFunction2 Selected = delegate (object selectedType)
            {
                callback((Type)selectedType);
            };

            List<ScriptInfo> scriptInfos = GetScriptInfosOfType(baseType);

            foreach (ScriptInfo info in scriptInfos.Where(info => string.IsNullOrEmpty(info.category)))
            {
                menu.AddItem(new GUIContent(subCategory + info.name), false, info.type != null ? Selected : null, info.type);
            }

            foreach (ScriptInfo info in scriptInfos.Where(info => !string.IsNullOrEmpty(info.category)))
            {
                menu.AddItem(new GUIContent(subCategory + info.category + "/" + info.name), false, info.type != null ? Selected : null, info.type);
            }

            return menu;
        }


        /// !* Providing an open GenericTypeDefinition for 'baseType', wraps the Preferred Types wihin the 1st Generic Argument of that Definition *!
        public static GenericMenu GetPreferedTypesSelectionMenu(Type baseType, Action<Type> callback, GenericMenu menu = null, string subCategory = null, bool showAddTypeOption = false)
        {

            if (menu == null)
            {
                menu = new GenericMenu();
            }

            if (subCategory != null)
            {
                subCategory = subCategory + "/";
            }

            Type constrainType = baseType;
            bool isGenericDefinition = baseType.IsGenericTypeDefinition && baseType.RTGetGenericArguments().Length == 1;
            Type genericDefinitionType = isGenericDefinition ? baseType : null;
            if (isGenericDefinition)
            {
                constrainType = genericDefinitionType.GetFirstGenericParameterConstraintType();
            }

            GenericMenu.MenuFunction2 Selected = (object t) => { callback((Type)t); };

            Dictionary<Type, string> listTypes = new Dictionary<Type, string>();
            Dictionary<Type, string> dictTypes = new Dictionary<Type, string>();

            foreach (Type t in TypePrefs.GetPreferedTypesList(constrainType, true))
            {
                string nsString = t.NamespaceToPath() + "/";

                Type finalType = isGenericDefinition && genericDefinitionType.CanBeMadeGenericWith(t) ? genericDefinitionType.MakeGenericType(t) : t;
                string finalString = nsString + finalType.FriendlyName();
                menu.AddItem(new GUIContent(subCategory + finalString), false, Selected, finalType);

                Type listType = typeof(List<>).MakeGenericType(t);
                Type finalListType = isGenericDefinition && genericDefinitionType.CanBeMadeGenericWith(listType) ? genericDefinitionType.MakeGenericType(listType) : listType;
                if (constrainType.IsAssignableFrom(finalListType))
                {
                    listTypes[finalListType] = nsString;
                }

                Type dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), t);
                Type finalDictType = isGenericDefinition && genericDefinitionType.CanBeMadeGenericWith(dictType) ? genericDefinitionType.MakeGenericType(dictType) : dictType;
                if (constrainType.IsAssignableFrom(finalDictType))
                {
                    dictTypes[finalDictType] = nsString;
                }
            }

            foreach (KeyValuePair<Type, string> pair in listTypes)
            {
                menu.AddItem(new GUIContent(subCategory + TypePrefs.LIST_MENU_STRING + pair.Value + pair.Key.FriendlyName()), false, Selected, pair.Key);
            }

            foreach (KeyValuePair<Type, string> pair in dictTypes)
            {
                menu.AddItem(new GUIContent(subCategory + TypePrefs.DICT_MENU_STRING + pair.Value + pair.Key.FriendlyName()), false, Selected, pair.Key);
            }

            if (showAddTypeOption)
            {
                menu.AddItem(new GUIContent(subCategory + "Add Type..."), false, () => { TypePrefsEditorWindow.ShowWindow(); });
            }

            if (menu.GetItemCount() == 0)
            {
                menu.AddDisabledItem(new GUIContent(string.Format("No {0} derived types found in Preferred Types List", baseType.Name)));
            }

            return menu;
        }

        //...
        public static void ShowPreferedTypesSelectionMenu(Type type, Action<Type> callback)
        {
            GetPreferedTypesSelectionMenu(type, callback).ShowAsBrowser("Select Type");
        }

        ///----------------------------------------------------------------------------------------------

        public static GenericMenu GetInstanceFieldSelectionMenu(Type type, Type fieldType, Action<FieldInfo> callback, GenericMenu menu = null, string subMenu = null)
        {
            return Internal_GetFieldSelectionMenu(BindingFlags.Public | BindingFlags.Instance, type, fieldType, callback, menu, subMenu);
        }

        public static GenericMenu GetStaticFieldSelectionMenu(Type type, Type fieldType, Action<FieldInfo> callback, GenericMenu menu = null, string subMenu = null)
        {
            return Internal_GetFieldSelectionMenu(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy, type, fieldType, callback, menu, subMenu);
        }

        ///Get a GenericMenu for field selection in a type
        private static GenericMenu Internal_GetFieldSelectionMenu(BindingFlags flags, Type type, Type fieldType, Action<FieldInfo> callback, GenericMenu menu = null, string subMenu = null)
        {

            if (menu == null)
            {
                menu = new GenericMenu();
            }

            if (subMenu != null)
            {
                subMenu = subMenu + "/";
            }

            GenericMenu.MenuFunction2 Selected = delegate (object selectedField)
            {
                callback((FieldInfo)selectedField);
            };

            foreach (FieldInfo field in type.GetFields(flags).Where(field => fieldType.IsAssignableFrom(field.FieldType)))
            {
                bool inherited = field.DeclaringType != type;
                string category = inherited ? subMenu + type.FriendlyName() + "/Inherited" : subMenu + type.FriendlyName();
                menu.AddItem(new GUIContent(string.Format("{0}/{1} : {2}", category, field.Name, field.FieldType.FriendlyName())), false, Selected, field);
            }

            return menu;
        }


        ///----------------------------------------------------------------------------------------------

        public static GenericMenu GetInstancePropertySelectionMenu(Type type, Type propType, Action<PropertyInfo> callback, bool mustRead = true, bool mustWrite = true, GenericMenu menu = null, string subMenu = null)
        {
            return Internal_GetPropertySelectionMenu(BindingFlags.Public | BindingFlags.Instance, type, propType, callback, mustRead, mustWrite, menu, subMenu);
        }

        public static GenericMenu GetStaticPropertySelectionMenu(Type type, Type propType, Action<PropertyInfo> callback, bool mustRead = true, bool mustWrite = true, GenericMenu menu = null, string subMenu = null)
        {
            return Internal_GetPropertySelectionMenu(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy, type, propType, callback, mustRead, mustWrite, menu, subMenu);
        }

        ///Get a GenericMenu for properties of a type optionaly specifying mustRead & mustWrite
        private static GenericMenu Internal_GetPropertySelectionMenu(BindingFlags flags, Type type, Type propType, Action<PropertyInfo> callback, bool mustRead = true, bool mustWrite = true, GenericMenu menu = null, string subMenu = null)
        {

            if (menu == null)
            {
                menu = new GenericMenu();
            }

            if (subMenu != null)
            {
                subMenu = subMenu + "/";
            }

            GenericMenu.MenuFunction2 Selected = delegate (object selectedProperty)
            {
                callback((PropertyInfo)selectedProperty);
            };

            foreach (PropertyInfo prop in type.GetProperties(flags))
            {

                if (!prop.CanRead && mustRead)
                {
                    continue;
                }

                if (!prop.CanWrite && mustWrite)
                {
                    continue;
                }

                if (!propType.IsAssignableFrom(prop.PropertyType))
                {
                    continue;
                }

                if (prop.GetCustomAttributes(typeof(System.ObsoleteAttribute), true).FirstOrDefault() != null)
                {
                    continue;
                }

                bool inherited = prop.DeclaringType != type;
                string category = inherited ? subMenu + type.FriendlyName() + "/Inherited" : subMenu + type.FriendlyName();
                menu.AddItem(new GUIContent(string.Format("{0}/{1} : {2}", category, prop.Name, prop.PropertyType.FriendlyName())), false, Selected, prop);
            }

            return menu;
        }

        ///----------------------------------------------------------------------------------------------

        ///Get a menu for instance methods
        public static GenericMenu GetInstanceMethodSelectionMenu(Type type, Type returnType, Type acceptedParamsType, System.Action<MethodInfo> callback, int maxParameters, bool propertiesOnly, bool excludeVoid = false, GenericMenu menu = null, string subMenu = null)
        {
            return Internal_GetMethodSelectionMenu(BindingFlags.Public | BindingFlags.Instance, type, returnType, acceptedParamsType, callback, maxParameters, propertiesOnly, excludeVoid, menu, subMenu);
        }

        ///Get a menu for static methods
        public static GenericMenu GetStaticMethodSelectionMenu(Type type, Type returnType, Type acceptedParamsType, System.Action<MethodInfo> callback, int maxParameters, bool propertiesOnly, bool excludeVoid = false, GenericMenu menu = null, string subMenu = null)
        {
            return Internal_GetMethodSelectionMenu(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy, type, returnType, acceptedParamsType, callback, maxParameters, propertiesOnly, excludeVoid, menu, subMenu);
        }

        ///Get a GenericMenu for method or property get/set methods selection in a type
        private static GenericMenu Internal_GetMethodSelectionMenu(BindingFlags flags, Type type, Type returnType, Type acceptedParamsType, System.Action<MethodInfo> callback, int maxParameters, bool propertiesOnly, bool excludeVoid = false, GenericMenu menu = null, string subMenu = null)
        {

            if (menu == null)
            {
                menu = new GenericMenu();
            }

            if (subMenu != null)
            {
                subMenu = subMenu + "/";
            }

            GenericMenu.MenuFunction2 Selected = delegate (object selectedMethod)
            {
                callback((MethodInfo)selectedMethod);
            };

            foreach (MethodInfo method in type.GetMethods(flags))
            {

                if (propertiesOnly != method.IsPropertyAccessor())
                {
                    continue;
                }

                if (method.IsGenericMethod)
                {
                    continue;
                }

                if (!returnType.IsAssignableFrom(method.ReturnType))
                {
                    continue;
                }

                if (method.ReturnType == typeof(void) && excludeVoid)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length > maxParameters && maxParameters != -1)
                {
                    continue;
                }

                if (parameters.Length > 0)
                {
                    if (acceptedParamsType != typeof(object) && parameters.Any(param => !acceptedParamsType.IsAssignableFrom(param.ParameterType)))
                    {
                        continue;
                    }
                }

                MemberInfo member = method;
                //get the actual property to check for ObsoleteAttribute
                if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                {
                    member = method.DeclaringType.GetProperty(method.Name.Replace("get_", "").Replace("set_", ""));
                }
                if (member == null || member.RTIsDefined(typeof(System.ObsoleteAttribute), true))
                {
                    continue;
                }

                bool inherited = method.DeclaringType != type;
                string category = inherited ? subMenu + type.FriendlyName() + "/Inherited" : subMenu + type.FriendlyName();
                menu.AddItem(new GUIContent(category + "/" + method.SignatureName()), false, Selected, method);
            }

            return menu;
        }

        ///----------------------------------------------------------------------------------------------

        ///Get a GenericMenu for Instance Events of the type and only event handler type of System.Action
        public static GenericMenu GetInstanceEventSelectionMenu(Type type, Type argType, Action<EventInfo> callback, GenericMenu menu = null, string subMenu = null)
        {
            return Internal_GetEventSelectionMenu(BindingFlags.Public | BindingFlags.Instance, type, argType, callback, menu, subMenu);
        }

        ///Get a GenericMenu for Static Events of the type and only event handler type of System.Action
        public static GenericMenu GetStaticEventSelectionMenu(Type type, Type argType, Action<EventInfo> callback, GenericMenu menu = null, string subMenu = null)
        {
            return Internal_GetEventSelectionMenu(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy, type, argType, callback, menu, subMenu);
        }

        ///Get a GenericMenu for Events of the type and only event handler type of System.Action
        private static GenericMenu Internal_GetEventSelectionMenu(BindingFlags flags, Type type, Type argType, Action<EventInfo> callback, GenericMenu menu = null, string subMenu = null)
        {

            if (menu == null)
            {
                menu = new GenericMenu();
            }

            if (subMenu != null)
            {
                subMenu = subMenu + "/";
            }

            GenericMenu.MenuFunction2 Selected = delegate (object selectedEvent)
            {
                callback((EventInfo)selectedEvent);
            };

            Type eventType = argType == null ? typeof(System.Action) : typeof(System.Action<>).MakeGenericType(new Type[] { argType });
            foreach (EventInfo e in type.GetEvents(flags))
            {
                if (e.EventHandlerType == eventType)
                {
                    string eventInfoString = string.Format("{0}({1})", e.Name, argType != null ? argType.FriendlyName() : "");
                    menu.AddItem(new GUIContent(subMenu + type.FriendlyName() + "/" + eventInfoString), false, Selected, e);
                }
            }

            return menu;
        }

        ///----------------------------------------------------------------------------------------------


        ///MenuItemInfo exposition
        public struct MenuItemInfo
        {
            public bool isValid { get; private set; }
            public GUIContent content;
            public bool separator;
            public bool selected;
            public GenericMenu.MenuFunction func;
            public GenericMenu.MenuFunction2 func2;
            public object userData;
            public MenuItemInfo(GUIContent c, bool sep, bool slc, GenericMenu.MenuFunction f1, GenericMenu.MenuFunction2 f2, object o)
            {
                isValid = true;
                content = c;
                separator = sep;
                selected = slc;
                func = f1;
                func2 = f2;
                userData = o;
            }
        }

        ///Gets an array of MenuItemInfo out of the GenericMenu provided
        public static MenuItemInfo[] GetMenuItems(GenericMenu menu)
        {

            FieldInfo itemField = typeof(GenericMenu).GetField("menuItems", BindingFlags.Instance | BindingFlags.NonPublic);
            ArrayList items = itemField.GetValue(menu) as ArrayList;
            if (items.Count == 0)
            {
                return new MenuItemInfo[0];
            }

            Type itemType = items[0].GetType();
            Func<object, GUIContent> contentGetter = ReflectionTools.GetFieldGetter<object, GUIContent>(itemType.GetField("content"));
            Func<object, bool> sepGetter = ReflectionTools.GetFieldGetter<object, bool>(itemType.GetField("separator"));
            Func<object, bool> selectedGetter = ReflectionTools.GetFieldGetter<object, bool>(itemType.GetField("on"));
            Func<object, GenericMenu.MenuFunction> func1Getter = ReflectionTools.GetFieldGetter<object, GenericMenu.MenuFunction>(itemType.GetField("func"));
            Func<object, GenericMenu.MenuFunction2> func2Getter = ReflectionTools.GetFieldGetter<object, GenericMenu.MenuFunction2>(itemType.GetField("func2"));
            Func<object, object> dataGetter = ReflectionTools.GetFieldGetter<object, object>(itemType.GetField("userData"));

            List<MenuItemInfo> result = new List<MenuItemInfo>();
            foreach (object item in items)
            {
                GUIContent content = contentGetter(item);
                bool separator = sepGetter(item);
                bool selected = selectedGetter(item);
                GenericMenu.MenuFunction func1 = func1Getter(item);
                GenericMenu.MenuFunction2 func2 = func2Getter(item);
                object userData = dataGetter(item);
                result.Add(new MenuItemInfo(content, separator, selected, func1, func2, userData));
            }

            return result.ToArray();
        }

        ///Shows the Generic Menu as a browser with CompleteContextMenu.
        public static void ShowAsBrowser(this GenericMenu menu, Vector2 pos, string title, System.Type keyType = null)
        {
            if (menu != null) { GenericMenuBrowser.Show(menu, pos, title, keyType); }
        }

        ///Shows the Generic Menu as a browser with CompleteContextMenu.
        public static void ShowAsBrowser(this GenericMenu menu, string title, System.Type keyType = null)
        {
            if (menu != null) { GenericMenuBrowser.Show(menu, Event.current.mousePosition, title, keyType); }
        }

        ///Shortcut
        public static void Show(this GenericMenu menu, bool asBrowser, string title, System.Type keyType = null)
        {
            if (asBrowser) { menu.ShowAsBrowser(title, keyType); } else { menu.ShowAsContext(); Event.current.Use(); }
        }
    }
}

#endif