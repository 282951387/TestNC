using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion.Design;
using System.Collections.Generic;
using UnityEngine;


namespace NodeCanvas.StateMachines
{
    [Category("SubGraphs")]
    [Color("ffe4e1")]
    public abstract class FSMStateNested<T> : FSMState, IGraphAssignable<T> where T : Graph
    {
        [SerializeField] private List<BBMappingParameter> _variablesMap;

        public abstract T subGraph { get; set; }
        public abstract BBParameter subGraphParameter { get; }

        public T currentInstance { get; set; }
        public Dictionary<Graph, Graph> instances { get; set; }
        public List<BBMappingParameter> variablesMap { get { return _variablesMap; } set { _variablesMap = value; } }

        Graph IGraphAssignable.subGraph { get { return subGraph; } set { subGraph = (T)value; } }
        Graph IGraphAssignable.currentInstance { get { return currentInstance; } set { currentInstance = (T)value; } }
    }
}
