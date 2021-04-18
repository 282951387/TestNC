using NodeCanvas.Framework;
using ParadoxNotion.Design;
using System.Collections.Generic;
using UnityEngine;


namespace NodeCanvas.Tasks.Actions
{

    [Category("GameObject")]
    [Description("Note that this is slow.\nAction will end in Failure if no objects are found")]
    public class FindAllWithName : ActionTask
    {

        [RequiredField]
        public BBParameter<string> searchName = "GameObject";
        [BlackboardOnly]
        public BBParameter<List<GameObject>> saveAs;

        protected override string info
        {
            get { return "GetObjects '" + searchName + "' as " + saveAs; }
        }

        protected override void OnExecute()
        {

            List<GameObject> gos = new List<GameObject>();
            foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
            {
                if (go.name == searchName.value)
                {
                    gos.Add(go);
                }
            }

            saveAs.value = gos;
            EndAction(gos.Count != 0);
        }
    }
}