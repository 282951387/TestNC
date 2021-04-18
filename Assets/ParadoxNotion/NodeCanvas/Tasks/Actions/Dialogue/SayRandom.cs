using NodeCanvas.DialogueTrees;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using System.Collections.Generic;
using UnityEngine;

namespace NodeCanvas.Tasks.Actions
{

    [Category("Dialogue")]
    [Icon("Dialogue")]
    [Description("A random statement will be chosen each time for the actor to say")]
    public class SayRandom : ActionTask<IDialogueActor>
    {

        public List<Statement> statements = new List<Statement>();

        protected override void OnExecute()
        {
            int index = Random.Range(0, statements.Count);
            Statement statement = statements[index];
            IStatement tempStatement = statement.BlackboardReplace(blackboard);
            SubtitlesRequestInfo info = new SubtitlesRequestInfo(agent, tempStatement, EndAction);
            DialogueTree.RequestSubtitles(info);
        }


        ////////////////////////////////////////
        ///////////GUI AND EDITOR STUFF/////////
        ////////////////////////////////////////
#if UNITY_EDITOR

        protected override void OnTaskInspectorGUI()
        {
            EditorUtils.ReorderableListOptions options = new EditorUtils.ReorderableListOptions();
            options.allowAdd = true;
            options.allowRemove = true;
            options.unityObjectContext = ownerSystem.contextObject;
            EditorUtils.ReorderableList(statements, options, (i, picked) =>
            {
                if (statements[i] == null) { statements[i] = new Statement("..."); }
                Statement statement = statements[i];
                statement.text = UnityEditor.EditorGUILayout.TextArea(statement.text, "textField", GUILayout.Height(50));
                statement.audio = (AudioClip)UnityEditor.EditorGUILayout.ObjectField("Audio Clip", statement.audio, typeof(AudioClip), false);
                statement.meta = UnityEditor.EditorGUILayout.TextField("Meta", statement.meta);
                EditorUtils.Separator();
            });
        }
#endif

    }
}