using NodeCanvas.Framework;
using ParadoxNotion.Design;
using System.Collections.Generic;
//using UnityEngine;

#if false
namespace NodeCanvas.Tasks.Actions
{

    [Category("✫ Blackboard/Lists")]
    [Description("Get the closer game object to the agent from within a list of game objects and save it in the blackboard.")]
    public class GetCloserGameObjectInList : ActionTask<Transform>
    {

        [RequiredField]
        public BBParameter<List<GameObject>> list;

        [BlackboardOnly]
        public BBParameter<GameObject> saveAs;

        protected override string info
        {
            get { return "Get Closer from '" + list + "' as " + saveAs; }
        }

        protected override void OnExecute()
        {

            if (list.value.Count == 0)
            {
                EndAction(false);
                return;
            }

            float closerDistance = Mathf.Infinity;
            GameObject closerGO = null;
            foreach (GameObject go in list.value)
            {
                float dist = Vector3.Distance(agent.position, go.transform.position);
                if (dist < closerDistance)
                {
                    closerDistance = dist;
                    closerGO = go;
                }
            }

            saveAs.value = closerGO;
            EndAction(true);
        }
    }
}
#endif