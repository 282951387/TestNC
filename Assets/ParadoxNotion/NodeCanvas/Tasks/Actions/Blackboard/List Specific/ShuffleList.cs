using NodeCanvas.Framework;
using ParadoxNotion.Design;
using System.Collections;
using UnityEngine;


namespace NodeCanvas.Tasks.Actions
{

    [Category("✫ Blackboard/Lists")]
    public class ShuffleList : ActionTask
    {

        [RequiredField]
        [BlackboardOnly]
        public BBParameter<IList> targetList;

        protected override void OnExecute()
        {

            IList list = targetList.value;

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = (int)Mathf.Floor(Random.value * (i + 1));
                object temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }

            EndAction();
        }
    }
}