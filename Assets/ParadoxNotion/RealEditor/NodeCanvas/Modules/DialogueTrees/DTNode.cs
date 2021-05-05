using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.DialogueTrees
{

    /// Base class for DialogueTree nodes that can live within a DialogueTree Graph.
    public abstract class DTNode : Node
    {

        [SerializeField] private string _actorName = DialogueTree.INSTIGATOR_NAME;
        [SerializeField] private string _actorParameterID;

        public override string name
        {
            get
            {
                if (requireActorSelection)
                {
                    if (DLGTree.definedActorParameterNames.Contains(actorName))
                    {
                        return string.Format("{0}", actorName);
                    }
                    return string.Format("<color=#d63e3e>* {0} *</color>", _actorName);
                }
                return base.name;
            }
        }

        public virtual bool requireActorSelection { get { return true; } }
        public override int maxInConnections { get { return -1; } }
        public override int maxOutConnections { get { return 1; } }
        public sealed override System.Type outConnectionType { get { return typeof(DTConnection); } }
        public sealed override bool allowAsPrime { get { return true; } }
        public sealed override bool canSelfConnect { get { return false; } }
        public sealed override Alignment2x2 commentsAlignment { get { return Alignment2x2.Right; } }
        public sealed override Alignment2x2 iconAlignment { get { return Alignment2x2.Bottom; } }

        protected DialogueTree DLGTree
        {
            get { return (DialogueTree)graph; }
        }

        ///The key name actor parameter to be used for this node
        public string actorName
        {
            get
            {
                DialogueTree.ActorParameter result = DLGTree.GetParameterByID(_actorParameterID);
                return result != null ? result.name : _actorName;
            }
            private set
            {
                if (_actorName != value && !string.IsNullOrEmpty(value))
                {
                    _actorName = value;
                    DialogueTree.ActorParameter param = DLGTree.GetParameterByName(value);
                    _actorParameterID = param != null ? param.ID : null;
                }
            }
        }

        ///The DialogueActor that will execute the node
        public IDialogueActor finalActor
        {
            get
            {
                IDialogueActor result = DLGTree.GetActorReferenceByID(_actorParameterID);
                return result != null ? result : DLGTree.GetActorReferenceByName(_actorName);
            }
        }


        ////////////////////////////////////////
        ///////////GUI AND EDITOR STUFF/////////
        ////////////////////////////////////////
#if UNITY_EDITOR

        protected override void OnNodeInspectorGUI()
        {
            if (requireActorSelection)
            {
                GUI.backgroundColor = Colors.lightBlue;
                actorName = EditorUtils.Popup<string>(actorName, DLGTree.definedActorParameterNames);
                GUI.backgroundColor = Color.white;
            }
            base.OnNodeInspectorGUI();
        }

        protected override UnityEditor.GenericMenu OnContextMenu(UnityEditor.GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Breakpoint"), isBreakpoint, () => { isBreakpoint = !isBreakpoint; });
            return menu;
        }

#endif
    }
}