#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace ParadoxNotion.Design
{

    //reflection meta info
    public partial class EditorUtils
    {

        [InitializeOnLoadMethod]
        private static void Initialize_ScriptInfos()
        {
            TypePrefs.onPreferredTypesChanged -= FlushInfos;
            TypePrefs.onPreferredTypesChanged += FlushInfos;
        }

        private static void FlushInfos()
        {
            cachedInfos = null;
        }

        //For gathering script/type meta-information
        public struct ScriptInfo
        {
            public bool isValid { get; private set; }

            public Type originalType;
            public string originalName;
            public string originalCategory;

            public Type type;
            public string name;
            public string category;
            public int priority;

            public ScriptInfo(Type type, string name, string category, int priority)
            {
                isValid = true;
                originalType = type;
                originalName = name;
                originalCategory = category;

                this.type = type;
                this.name = name;
                this.category = category;
                this.priority = priority;
            }
        }

        ///Get a list of ScriptInfos of the baseType excluding: the base type, abstract classes, Obsolete classes and those with the DoNotList attribute, categorized as a list of ScriptInfo
        private static Dictionary<Type, List<ScriptInfo>> cachedInfos;
        public static List<ScriptInfo> GetScriptInfosOfType(Type baseType)
        {

            if (cachedInfos == null) { cachedInfos = new Dictionary<Type, List<ScriptInfo>>(); }

            List<ScriptInfo> infosResult;
            if (cachedInfos.TryGetValue(baseType, out infosResult))
            {
                return infosResult.ToList();
            }

            infosResult = new List<ScriptInfo>();

            Type[] subTypes = baseType.IsGenericTypeDefinition ? new Type[] { baseType } : ReflectionTools.GetImplementationsOf(baseType);
            foreach (Type subType in subTypes)
            {

                if (subType.IsAbstract || subType.RTIsDefined(typeof(DoNotListAttribute), true) || subType.RTIsDefined(typeof(ObsoleteAttribute), true))
                {
                    continue;
                }

                bool isGeneric = subType.IsGenericTypeDefinition && subType.RTGetGenericArguments().Length == 1;
                string scriptName = subType.FriendlyName().SplitCamelCase();
                string scriptCategory = string.Empty;
                int scriptPriority = 0;

                NameAttribute nameAttribute = subType.RTGetAttribute<NameAttribute>(true);
                if (nameAttribute != null)
                {
                    scriptPriority = nameAttribute.priority;
                    scriptName = nameAttribute.name;
                    if (isGeneric && !scriptName.EndsWith("<T>"))
                    {
                        scriptName += " (T)";
                    }
                }

                CategoryAttribute categoryAttribute = subType.RTGetAttribute<CategoryAttribute>(true);
                if (categoryAttribute != null)
                {
                    scriptCategory = categoryAttribute.category;
                }

                ScriptInfo info = new ScriptInfo(subType, scriptName, scriptCategory, scriptPriority);

                //add the generic types based on constrains and prefered types list
                if (isGeneric)
                {
                    bool exposeAsBaseDefinition = subType.RTIsDefined<ExposeAsDefinitionAttribute>(true);
                    if (!exposeAsBaseDefinition)
                    {
                        List<Type> typesToWrap = TypePrefs.GetPreferedTypesList(true);
                        foreach (Type t in typesToWrap)
                        {
                            infosResult.Add(info.MakeGenericInfo(t, string.Format("/{0}/{1}", info.name, t.NamespaceToPath())));
                            infosResult.Add(info.MakeGenericInfo(typeof(List<>).MakeGenericType(t), string.Format("/{0}/{1}{2}", info.name, TypePrefs.LIST_MENU_STRING, t.NamespaceToPath()), -1));
                            infosResult.Add(info.MakeGenericInfo(typeof(Dictionary<,>).MakeGenericType(typeof(string), t), string.Format("/{0}/{1}{2}", info.name, TypePrefs.DICT_MENU_STRING, t.NamespaceToPath()), -2));
                        }
                        continue;
                    }
                }

                infosResult.Add(info);
            }

            infosResult = infosResult
            .Where(s => s.isValid)
            .OrderBy(s => s.originalCategory)
            .ThenBy(s => s.priority * -1)
            .ThenBy(s => s.originalName)
            .ToList();
            cachedInfos[baseType] = infosResult;
            return infosResult;
        }

        ///Makes and returns a closed generic ScriptInfo for targetType out of an existing ScriptInfo
        public static ScriptInfo MakeGenericInfo(this ScriptInfo info, Type targetType, string subCategory = null, int priorityShift = 0)
        {
            if (!info.isValid || !info.originalType.IsGenericTypeDefinition)
            {
                return default(ScriptInfo);
            }

            if (info.originalType.CanBeMadeGenericWith(targetType))
            {
                Type genericType = info.originalType.MakeGenericType(targetType);
                string genericCategory = info.originalCategory + subCategory;
                string genericName = info.originalName.Replace("(T)", string.Format("({0})", targetType.FriendlyName()));
                ScriptInfo newInfo = new ScriptInfo(genericType, genericName, genericCategory, info.priority + priorityShift);
                newInfo.originalType = info.originalType;
                newInfo.originalName = info.originalName;
                newInfo.originalCategory = info.originalCategory;
                return newInfo;
            }
            return default(ScriptInfo);
        }

        //Not really. Only for purposes of menus usage.
        private static string NamespaceToPath(this Type type)
        {
            if (type == null) { return string.Empty; }
            return string.IsNullOrEmpty(type.Namespace) ? "No Namespace" : type.Namespace.Split('.').First();
        }
    }
}

#endif