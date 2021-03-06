using NodeCanvas.Framework;
using ParadoxNotion;


namespace NodeCanvas.BehaviourTrees
{

    /// Super Base class for BehaviourTree nodes that can live within a BehaviourTree Graph.
    public abstract class BTNode : Node
    {

        public sealed override System.Type outConnectionType { get { return typeof(BTConnection); } }
        public sealed override bool allowAsPrime { get { return true; } }
        public sealed override bool canSelfConnect { get { return false; } }
#if UNITY_EDITOR
        public override Alignment2x2 commentsAlignment { get { return Alignment2x2.Bottom; } }
        public override Alignment2x2 iconAlignment { get { return Alignment2x2.Default; } }
#endif
        public override int maxInConnections { get { return 1; } }
        public override int maxOutConnections { get { return 0; } }
#if UNITY_EDITOR
        ///Add a child node to this node connected to the specified child index
        public T AddChild<T>(int childIndex) where T : BTNode
        {
            if (outConnections.Count >= maxOutConnections && maxOutConnections != -1)
            {
                return null;
            }
            T child = graph.AddNode<T>();
            graph.ConnectNodes(this, child, childIndex);
            return child;
        }

        ///Add a child node to this node connected last
        public T AddChild<T>() where T : BTNode
        {
            if (outConnections.Count >= maxOutConnections && maxOutConnections != -1)
            {
                return null;
            }
            T child = graph.AddNode<T>();
            graph.ConnectNodes(this, child);
            return child;
        }
#endif

        ///----------------------------------------------------------------------------------------------
        ///---------------------------------------UNITY EDITOR-------------------------------------------
#if UNITY_EDITOR

        protected override UnityEditor.GenericMenu OnContextMenu(UnityEditor.GenericMenu menu)
        {
            menu.AddItem(new UnityEngine.GUIContent("Breakpoint"), isBreakpoint, () => { isBreakpoint = !isBreakpoint; });
            return ParadoxNotion.Design.EditorUtils.GetTypeSelectionMenu(typeof(BTDecorator), (t) => { this.DecorateWith(t); }, menu, "Decorate");
        }

#endif
    }
}