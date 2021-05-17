﻿using ParadoxNotion;
using ParadoxNotion.Design;
using ParadoxNotion.Serialization;
using UnityEngine;

namespace NodeCanvas.Framework.Internal
{

    ///Missing node types are deserialized into this on deserialization and can load back if type is found
    [DoNotList]
    [Description("Please resolve the MissingNode issue by either replacing the node, importing the missing node type, or refactoring the type in GraphRefactor.")]
    public sealed class MissingNode : Node, IMissingRecoverable
    {

        [SerializeField]
        private string _missingType;
        [SerializeField]
        private string _recoveryState;

        string IMissingRecoverable.missingType
        {
            get { return _missingType; }
            set { _missingType = value; }
        }

        string IMissingRecoverable.recoveryState
        {
            get { return _recoveryState; }
            set { _recoveryState = value; }
        }

        public override string name
        {
            get { return "Missing Node".FormatError(); }
        }

        public override System.Type outConnectionType { get { return null; } }
        public override int maxInConnections { get { return 0; } }
        public override int maxOutConnections { get { return 0; } }
        public override bool allowAsPrime { get { return false; } }
        public override bool canSelfConnect { get { return false; } }
#if UNITY_EDITOR
        public override Alignment2x2 commentsAlignment { get { return Alignment2x2.Right; } }
        public override Alignment2x2 iconAlignment { get { return Alignment2x2.Default; } }
#endif


        ////////////////////////////////////////
        ///////////GUI AND EDITOR STUFF/////////
        ////////////////////////////////////////
#if UNITY_EDITOR

        protected override void DrawNodeConnections(Rect drawCanvas, bool fullDrawPass, Vector2 canvasMousePos, float zoomFactor)
        {
            foreach (Connection c in outConnections)
            {
                UnityEditor.Handles.DrawBezier(c.sourceNode.rect.center, c.targetNode.rect.center, c.sourceNode.rect.center, c.targetNode.rect.center, Color.red, Editor.StyleSheet.bezierTexture, 3);
            }
            foreach (Connection c in inConnections)
            {
                UnityEditor.Handles.DrawBezier(c.sourceNode.rect.center, c.targetNode.rect.center, c.sourceNode.rect.center, c.targetNode.rect.center, Color.red, Editor.StyleSheet.bezierTexture, 3);
            }
        }

        protected override void OnNodeGUI()
        {
            GUILayout.Label(ReflectionTools.FriendlyTypeName(_missingType).FormatError());
        }

        protected override void OnNodeInspectorGUI()
        {
            GUILayout.Label(_missingType.FormatError());
            GUILayout.Label(_recoveryState);
        }
#endif

    }
}