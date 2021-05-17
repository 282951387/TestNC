using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.BehaviourTrees
{

    /// Base class for BehaviourTree Composite nodes.
    public abstract class BTComposite : BTNode
    {

        public override string name { get { return base.name.ToUpper(); } }

        public sealed override int maxOutConnections { get { return -1; } }
#if UNITY_EDITOR
        public sealed override Alignment2x2 commentsAlignment { get { return Alignment2x2.Right; } }
#endif
        ///----------------------------------------------------------------------------------------------
        ///---------------------------------------UNITY EDITOR-------------------------------------------
#if UNITY_EDITOR

        protected override UnityEditor.GenericMenu OnContextMenu(UnityEditor.GenericMenu menu)
        {
            menu = base.OnContextMenu(menu);
            menu = EditorUtils.GetTypeSelectionMenu(typeof(BTComposite), (t) => { this.ReplaceWith(t); }, menu, "Replace");
            menu.AddItem(new GUIContent("Convert to SubTree"), false, () => { this.ConvertToSubTree(); });
            if (outConnections.Count > 0)
            {
                menu.AddItem(new GUIContent("Duplicate Branch"), false, () => { this.DuplicateBranch(graph); });
                menu.AddItem(new GUIContent("Delete Branch"), false, () => { this.DeleteBranch(); });
            }
            return menu;
        }

#endif

    }
}