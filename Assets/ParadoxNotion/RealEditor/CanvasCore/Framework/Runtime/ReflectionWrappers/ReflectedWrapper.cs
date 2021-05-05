using ParadoxNotion;
using ParadoxNotion.Serialization;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace NodeCanvas.Framework.Internal
{

    ///Wraps a MethodInfo with the relevant BBParameters to be called within a Reflection based Task
    public abstract class ReflectedWrapper : IReflectedWrapper
    {

        //required
        public ReflectedWrapper() { }

        [SerializeField]
        protected SerializedMethodInfo _targetMethod;

        public static ReflectedWrapper Create(MethodInfo method, IBlackboard bb)
        {
            if (method == null)
            {
                return null;
            }

            if (method.ReturnType == typeof(void))
            {
                return ReflectedActionWrapper.Create(method, bb);
            }
            return ReflectedFunctionWrapper.Create(method, bb);
        }

        ISerializedReflectedInfo IReflectedWrapper.GetSerializedInfo() { return _targetMethod; }

        public void SetVariablesBB(IBlackboard bb)
        {
            foreach (BBParameter bbVar in GetVariables())
            {
                bbVar.bb = bb;
            }
        }
        public SerializedMethodInfo GetSerializedMethod() { return _targetMethod; }
        public MethodInfo GetMethod() { return _targetMethod; }
        public bool HasChanged() { return _targetMethod != null ? _targetMethod.HasChanged() : false; }
        public string AsString() { return _targetMethod != null ? _targetMethod.AsString() : null; }
        public override string ToString() { return AsString(); }

        public abstract BBParameter[] GetVariables();
        public abstract void Init(object instance);
    }



    ///Wraps a MethodInfo Action with the relevant BBVariables to be commonly called within a Reflection based Task
    public abstract class ReflectedActionWrapper : ReflectedWrapper
    {

        public static new ReflectedActionWrapper Create(MethodInfo method, IBlackboard bb)
        {
            if (method == null)
            {
                return null;
            }

            Type type = null;
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                type = typeof(ReflectedAction);
            }

            if (parameters.Length == 1)
            {
                type = typeof(ReflectedAction<>);
            }

            if (parameters.Length == 2)
            {
                type = typeof(ReflectedAction<,>);
            }

            if (parameters.Length == 3)
            {
                type = typeof(ReflectedAction<,,>);
            }

            if (parameters.Length == 4)
            {
                type = typeof(ReflectedAction<,,,>);
            }

            if (parameters.Length == 5)
            {
                type = typeof(ReflectedAction<,,,,>);
            }

            if (parameters.Length == 6)
            {
                type = typeof(ReflectedAction<,,,,,>);
            }

            Type[] argTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                Type pType = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType() : parameter.ParameterType;
                argTypes[i] = pType;
            }

            ReflectedActionWrapper o = (ReflectedActionWrapper)Activator.CreateInstance(argTypes.Length > 0 ? type.RTMakeGenericType(argTypes) : type);
            o._targetMethod = new SerializedMethodInfo(method);

            BBParameter.SetBBFields(o, bb);

            BBParameter[] bbParams = o.GetVariables();
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo p = parameters[i];
                if (p.IsOptional)
                {
                    bbParams[i].value = p.DefaultValue;
                }
            }

            return o;
        }

        public abstract void Call();
    }

    ///Wraps a MethodInfo Function with the relevant BBVariables to be commonly called within a Reflection based Task
    public abstract class ReflectedFunctionWrapper : ReflectedWrapper
    {

        public static new ReflectedFunctionWrapper Create(MethodInfo method, IBlackboard bb)
        {
            if (method == null)
            {
                return null;
            }

            Type type = null;
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                type = typeof(ReflectedFunction<>);
            }

            if (parameters.Length == 1)
            {
                type = typeof(ReflectedFunction<,>);
            }

            if (parameters.Length == 2)
            {
                type = typeof(ReflectedFunction<,,>);
            }

            if (parameters.Length == 3)
            {
                type = typeof(ReflectedFunction<,,,>);
            }

            if (parameters.Length == 4)
            {
                type = typeof(ReflectedFunction<,,,,>);
            }

            if (parameters.Length == 5)
            {
                type = typeof(ReflectedFunction<,,,,,>);
            }

            if (parameters.Length == 6)
            {
                type = typeof(ReflectedFunction<,,,,,,>);
            }

            Type[] argTypes = new Type[parameters.Length + 1];
            argTypes[0] = method.ReturnType;
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                Type pType = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType() : parameter.ParameterType;
                argTypes[i + 1] = pType;
            }

            ReflectedFunctionWrapper o = (ReflectedFunctionWrapper)Activator.CreateInstance(type.RTMakeGenericType(argTypes.ToArray()));
            o._targetMethod = new SerializedMethodInfo(method);

            BBParameter.SetBBFields(o, bb);

            BBParameter[] bbParams = o.GetVariables();
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo p = parameters[i];
                if (p.IsOptional)
                {
                    bbParams[i + 1].value = p.DefaultValue; //index 0 is return value
                }
            }

            return o;
        }

        public abstract object Call();
    }
}