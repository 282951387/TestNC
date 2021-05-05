using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion;
using ParadoxNotion.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace NodeCanvas.Tasks.Actions
{

    [System.Obsolete("Execute Function now supports static functions as well")]
    public class ExecuteStaticFunction_Multiplatform : ActionTask
    {

        [SerializeField]
        protected SerializedMethodInfo method;
        [SerializeField]
        protected List<BBObjectParameter> parameters = new List<BBObjectParameter>();
        [SerializeField]
        [BlackboardOnly]
        protected BBObjectParameter returnValue;

        private MethodInfo targetMethod => method;

        protected override string info
        {
            get
            {
                if (method == null) { return "No Method Selected"; }
                if (targetMethod == null) { return method.AsString().FormatError(); }

                string returnInfo = targetMethod.ReturnType == typeof(void) ? "" : returnValue.ToString() + " = ";
                string paramInfo = "";
                for (int i = 0; i < parameters.Count; i++)
                {
                    paramInfo += (i != 0 ? ", " : "") + parameters[i].ToString();
                }
                return string.Format("{0}{1}.{2} ({3})", returnInfo, targetMethod.DeclaringType.FriendlyName(), targetMethod.Name, paramInfo);
            }
        }

        public override void OnValidate(ITaskSystem ownerSystem)
        {
            if (method != null && method.HasChanged()) { SetMethod(method); }
        }

        //store the method info on init
        protected override string OnInit()
        {
            if (method == null) { return "No methMethodd selected"; }
            if (targetMethod == null) { return string.Format("Missing Method '{0}'", method.AsString()); }
            return null;
        }

        //do it by calling delegate or invoking method
        protected override void OnExecute()
        {
            object[] args = parameters.Select(p => p.value).ToArray();
            returnValue.value = targetMethod.Invoke(agent, args);
            EndAction();
        }

        private void SetMethod(MethodInfo method)
        {

            if (method == null)
            {
                return;
            }

            this.method = new SerializedMethodInfo(method);
            parameters.Clear();
            foreach (ParameterInfo p in method.GetParameters())
            {
                BBObjectParameter newParam = new BBObjectParameter(p.ParameterType) { bb = blackboard };
                if (p.IsOptional)
                {
                    newParam.value = p.DefaultValue;
                }
                parameters.Add(newParam);
            }

            if (method.ReturnType != typeof(void))
            {
                returnValue = new BBObjectParameter(method.ReturnType) { bb = blackboard };
            }
            else
            {
                returnValue = null;
            }
        }
    }
}