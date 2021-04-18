#if UNITY_EDITOR

using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using System.Linq;
using UnityEngine;

namespace NodeCanvas.Editor
{

    ///A drawer for INodeReference which is useful to weak reference nodes from within one another
    public class NodeReferenceDrawer : ObjectDrawer<INodeReference>
    {
        public override INodeReference OnGUI(GUIContent content, INodeReference instance)
        {
            //we presume that INodeRefence is serialized in a Node context
            if (instance == null)
            {
                UnityEditor.EditorGUILayout.LabelField(content.text, "Null NodeReference Instance");
                return instance;
            }
            Node contextNode = context as Node;
            if (contextNode == null || contextNode.graph == null) { return instance; }
            Graph graph = contextNode.graph;

            System.Collections.Generic.IEnumerable<Node> targets = graph.allNodes.Where(x => instance.type.IsAssignableFrom(x.GetType()));
            Node current = instance.Get(graph);
            Node newTarget = EditorUtils.Popup<Node>(content, current, targets);
            if (newTarget != current)
            {
                UndoUtility.RecordObject(contextUnityObject, "Set Node Reference");
                instance.Set(newTarget);
                foreach (CallbackAttribute callbackAtt in attributes.OfType<CallbackAttribute>())
                {
                    System.Reflection.MethodInfo m = contextNode.GetType().RTGetMethod(callbackAtt.methodName);
                    if (m != null) { m.Invoke(contextNode, null); }
                }
                UndoUtility.SetDirty(contextUnityObject);
            }

            return instance;
        }
    }
}

#endif