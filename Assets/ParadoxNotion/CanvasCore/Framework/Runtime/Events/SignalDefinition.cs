﻿using ParadoxNotion;
using System.Collections.Generic;
using UnityEngine;

namespace NodeCanvas.Framework
{
    ///A Signal definition that things can listen to.
    ///Can also be invoked in code by calling 'Invoke' but args have to be same type and same length as the parameters defined.
    [CreateAssetMenu(menuName = "ParadoxNotion/CanvasCore/Signal Definition")]
    public class SignalDefinition : ScriptableObject
    {

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void Editor_Init()
        {
            ParadoxNotion.Design.AssetTracker.BeginTrackingAssetsOfType(typeof(SignalDefinition));
        }
#endif

        public delegate void InvokeArguments(Transform sender, Transform receiver, bool isGlobal, params object[] args);
        public event InvokeArguments onInvoke;

        [SerializeField, HideInInspector]
        private List<DynamicParameterDefinition> _parameters = new List<DynamicParameterDefinition>();

        ///The Signal parameters
        public List<DynamicParameterDefinition> parameters
        {
            get { return _parameters; }
            private set { _parameters = value; }
        }

        ///Invoke the Signal
        public void Invoke(Transform sender, Transform receiver, bool isGlobal, params object[] args)
        {
            if (onInvoke != null)
            {
                onInvoke(sender, receiver, isGlobal, args);
            }
        }

        //...
        public void AddParameter(string name, System.Type type)
        {
            DynamicParameterDefinition param = new DynamicParameterDefinition(name, type);
            _parameters.Add(param);
        }

        //...
        public void RemoveParameter(string name)
        {
            DynamicParameterDefinition param = _parameters.Find(p => p.name == name);
            if (param != null)
            {
                _parameters.Remove(param);
            }
        }
    }
}