using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.Tasks.Actions
{

    [Category("GameObject")]
    public class LookAt : ActionTask<Transform>
    {

        [RequiredField]
        public BBParameter<GameObject> lookTarget;
        public bool repeat = false;

        protected override string info
        {
            get { return "LookAt " + lookTarget; }
        }

        protected override void OnExecute() { DoLook(); }
        protected override void OnUpdate() { DoLook(); }

        private void DoLook()
        {
            Vector3 lookPos = lookTarget.value.transform.position;
            lookPos.y = agent.position.y;
            agent.LookAt(lookPos);

            if (!repeat)
            {
                EndAction(true);
            }
        }
    }
}