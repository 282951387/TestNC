using NodeCanvas.Framework;
using ParadoxNotion;
using System.Linq;

namespace NodeCanvas.BehaviourTrees
{

    /// Base class for BehaviourTree Decorator nodes.
    public abstract class BTDecorator : BTNode
    {

        public sealed override int maxOutConnections { get { return 1; } }
#if UNITY_EDITOR
        public sealed override Alignment2x2 commentsAlignment { get { return Alignment2x2.Right; } }
#endif
        ///The decorated connection element
        protected Connection decoratedConnection
        {
            get { return outConnections.Count > 0 ? outConnections[0] : null; }
        }

        ///The decorated node element
        protected Node decoratedNode
        {
            get
            {
                Connection c = decoratedConnection;
                return c != null ? c.targetNode : null;
            }
        }


        ///----------------------------------------------------------------------------------------------
        ///---------------------------------------UNITY EDITOR-------------------------------------------
#if UNITY_EDITOR

        protected override UnityEditor.GenericMenu OnContextMenu(UnityEditor.GenericMenu menu)
        {
            menu = base.OnContextMenu(menu);
            menu = ParadoxNotion.Design.EditorUtils.GetTypeSelectionMenu(typeof(BTDecorator), (t) => { this.ReplaceWith(t); }, menu, "Replace");
            return menu;
        }

#endif

    }
}