using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion;
using ParadoxNotion.Design;
using ParadoxNotion.Serialization;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace NodeCanvas.Tasks.Actions
{

    [Name("Set Property", 7)]
    [Category("✫ Reflected")]
    [Description("Set a property on a script")]
    public class SetProperty_Multiplatform : ActionTask, IReflectedWrapper
    {

        [SerializeField]
        protected SerializedMethodInfo method;
        [SerializeField]
        protected BBObjectParameter parameter;

        private MethodInfo targetMethod => method;

        public override System.Type agentType
        {
            get
            {
                if (targetMethod == null) { return typeof(Transform); }
                return targetMethod.IsStatic ? null : targetMethod.RTReflectedOrDeclaredType();
            }
        }

        protected override string info
        {
            get
            {
                if (method == null) { return "No Property Selected"; }
                if (targetMethod == null) { return method.AsString().FormatError(); }
                string mInfo = targetMethod.IsStatic ? targetMethod.RTReflectedOrDeclaredType().FriendlyName() : agentInfo;
                return string.Format("{0}.{1} = {2}", mInfo, targetMethod.Name, parameter.ToString());
            }
        }

        ISerializedReflectedInfo IReflectedWrapper.GetSerializedInfo() { return method; }

        public override void OnValidate(ITaskSystem ownerSystem)
        {
            if (method != null && method.HasChanged()) { SetMethod(method); }
        }

        protected override string OnInit()
        {
            if (method == null) { return "No property selected"; }
            if (targetMethod == null) { return string.Format("Missing property '{0}'", method.AsString()); }
            return null;
        }

        protected override void OnExecute()
        {
            targetMethod.Invoke(targetMethod.IsStatic ? null : agent, ReflectionTools.SingleTempArgsArray(parameter.value));
            EndAction();
        }

        private void SetMethod(MethodInfo method)
        {
            if (method != null)
            {
                this.method = new SerializedMethodInfo(method);
                parameter.SetType(method.GetParameters()[0].ParameterType);
            }
        }


        ///----------------------------------------------------------------------------------------------
        ///---------------------------------------UNITY EDITOR-------------------------------------------
#if UNITY_EDITOR

        protected override void OnTaskInspectorGUI()
        {

            if (!Application.isPlaying && GUILayout.Button("Select Property"))
            {
                UnityEditor.GenericMenu menu = new UnityEditor.GenericMenu();
                if (agent != null)
                {
                    foreach (Component comp in agent.GetComponents(typeof(Component)).Where(c => c.hideFlags != HideFlags.HideInInspector))
                    {
                        menu = EditorUtils.GetInstanceMethodSelectionMenu(comp.GetType(), typeof(void), typeof(object), SetMethod, 1, true, false, menu);
                    }
                    menu.AddSeparator("/");
                }
                foreach (System.Type t in TypePrefs.GetPreferedTypesList(typeof(object)))
                {
                    menu = EditorUtils.GetStaticMethodSelectionMenu(t, typeof(void), typeof(object), SetMethod, 1, true, false, menu);
                    if (typeof(UnityEngine.Component).IsAssignableFrom(t))
                    {
                        menu = EditorUtils.GetInstanceMethodSelectionMenu(t, typeof(void), typeof(object), SetMethod, 1, true, false, menu);
                    }
                }
                menu.ShowAsBrowser("Select Property", GetType());
                Event.current.Use();
            }

            if (targetMethod != null)
            {
                GUILayout.BeginVertical("box");
                UnityEditor.EditorGUILayout.LabelField("Type", targetMethod.RTReflectedOrDeclaredType().FriendlyName());
                UnityEditor.EditorGUILayout.LabelField("Property", targetMethod.Name);
                UnityEditor.EditorGUILayout.LabelField("Set Type", parameter.varType.FriendlyName());
                UnityEditor.EditorGUILayout.HelpBox(DocsByReflection.GetMemberSummary(targetMethod), UnityEditor.MessageType.None);
                GUILayout.EndVertical();
                NodeCanvas.Editor.BBParameterEditor.ParameterField("Set Value", parameter);
            }
        }

#endif
    }
}