using UnityEngine;

namespace NodeCanvas.Framework
{

    ///Used to parametrize root graph local blackboard parameters from GraphOwner, without affecting the graph variables serialization.
    ///So each GraphOwner can parametrize the assigned graph individually, while the graph remains the same serialization-wise.
    ///Relevant when either using Prefab GraphOwners with Bound Graphs, or re-using Asset Graphs on GraphOwners.
    [ParadoxNotion.Design.SpoofAOT]
    public abstract class ExposedParameter
    {
        public abstract string targetVariableID { get; }
        public abstract System.Type type { get; }
        public abstract object valueBoxed { get; set; }
        public abstract void Bind(IBlackboard blackboard);
        public abstract void UnBind(IBlackboard blackboard);
        public abstract Variable varRefBoxed { get; }

        public static ExposedParameter CreateInstance(Variable target)
        {
            return (ExposedParameter)System.Activator.CreateInstance(typeof(ExposedParameter<>).MakeGenericType(target.varType), ParadoxNotion.ReflectionTools.SingleTempArgsArray(target));
        }
    }

    ///See ExposedParameter
    public sealed class ExposedParameter<T> : ExposedParameter
    {
        [SerializeField] private readonly string _targetVariableID;
        [SerializeField] private T _value;

        public Variable<T> varRef { get; private set; }

        public ExposedParameter() { }
        public ExposedParameter(Variable target)
        {
            //Debug.Assert(target is Variable<T>, "Target Variable is not typeof T");
            _targetVariableID = target.ID;
            _value = (T)target.value;
        }

        public override string targetVariableID => _targetVariableID;
        public override System.Type type => typeof(T);
        public override object valueBoxed { get { return value; } set { this.value = (T)value; } }
        public override Variable varRefBoxed => varRef;

        ///Value of the parameter
        public T value
        {
            get 
            { 
                return varRef != null && Application.isPlaying ? varRef.value : _value; 
            }
            set
            {
                if (varRef != null && Application.isPlaying)
                {
                    varRef.value = value;
                }
                _value = value;
            }
        }

        ///Initialize Variables binding from target blackboard
        public override void Bind(IBlackboard blackboard)
        {
            varRef = (Variable<T>)blackboard.GetVariableByID(targetVariableID);
            if (varRef != null) { varRef.BindGetSet(GetRawValue, SetRawValue); }
        }

        public override void UnBind(IBlackboard blackboard)
        {
            varRef = (Variable<T>)blackboard.GetVariableByID(targetVariableID);
            if (varRef != null) { varRef.UnBind(); }
        }

        private T GetRawValue() { return _value; }

        private void SetRawValue(T value) { _value = value; }
    }
}