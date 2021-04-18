using NodeCanvas.Framework;
using ParadoxNotion.Design;
using System.Collections.Generic;
using UnityEngine;

namespace NodeCanvas.Tasks.Actions
{

    [Category("✫ Blackboard/Lists")]
    public class InsertElementToList<T> : ActionTask
    {
        [RequiredField]
        [BlackboardOnly]
        public BBParameter<List<T>> targetList;
        public BBParameter<T> targetElement;
        public BBParameter<int> targetIndex;

        protected override string info
        {
            get { return string.Format("Insert {0} in {1} at {2}", targetElement, targetList, targetIndex); }
        }

        protected override void OnExecute()
        {
            int index = targetIndex.value;
            List<T> list = targetList.value;
            if (index < 0 || index >= list.Count)
            {
                EndAction(false);
                return;
            }

            list.Insert(index, targetElement.value);
            EndAction(true);
        }
    }
}