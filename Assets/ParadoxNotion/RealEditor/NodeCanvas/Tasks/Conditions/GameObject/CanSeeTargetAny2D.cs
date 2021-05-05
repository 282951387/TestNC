using NodeCanvas.Framework;
using ParadoxNotion.Design;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace NodeCanvas.Tasks.Conditions
{

    [Category("GameObject")]
    [Description("A combination of line of sight and view angle check")]
    public class CanSeeTargetAny2D : ConditionTask<Transform>
    {

        public BBParameter<List<GameObject>> targetObjects;
        public BBParameter<float> maxDistance = 50;
        public BBParameter<float> awarnessDistance = 0f;
        [SliderField(1, 180)]
        public BBParameter<float> viewAngle = 70f;
        public Vector2 offset;

        [BlackboardOnly]
        public BBParameter<List<GameObject>> allResults;
        [BlackboardOnly]
        public BBParameter<GameObject> closerResult;

        protected override string info { get { return "Can See Any " + targetObjects; } }

        protected override bool OnCheck()
        {

            bool r = false;
            bool store = !allResults.isNone || !closerResult.isNone;
            List<GameObject> temp = store ? new List<GameObject>() : null;

            foreach (GameObject o in targetObjects.value)
            {

                if (o == agent.gameObject) { continue; }

                Transform t = o.transform;
                if (Vector2.Distance(agent.position, t.position) > maxDistance.value)
                {
                    continue;
                }

                RaycastHit2D hit = Physics2D.Linecast((Vector2)agent.position + offset, (Vector2)t.position + offset);
                if (hit.collider != t.GetComponent<Collider2D>()) { continue; }


                if (Vector2.Angle((Vector2)t.position - (Vector2)agent.position, agent.right) < viewAngle.value)
                {
                    if (store) { temp.Add(o); }
                    r = true;
                }

                if (Vector2.Distance(agent.position, t.position) < awarnessDistance.value)
                {
                    if (store) { temp.Add(o); }
                    r = true;
                }

            }

            if (store)
            {
                IOrderedEnumerable<GameObject> ordered = temp.OrderBy(x => Vector3.Distance(agent.position, x.transform.position));
                if (!allResults.isNone) { allResults.value = ordered.ToList(); }
                if (!closerResult.isNone) { closerResult.value = ordered.FirstOrDefault(); }
            }

            return r;
        }

        public override void OnDrawGizmosSelected()
        {
            if (agent != null)
            {
                Gizmos.DrawLine((Vector2)agent.position, (Vector2)agent.position + offset);
                Gizmos.DrawLine((Vector2)agent.position + offset, (Vector2)agent.position + offset + ((Vector2)agent.right * maxDistance.value));
                Gizmos.DrawWireSphere((Vector2)agent.position + offset + ((Vector2)agent.right * maxDistance.value), 0.1f);
                Gizmos.DrawWireSphere((Vector2)agent.position, awarnessDistance.value);
                Gizmos.matrix = Matrix4x4.TRS((Vector2)agent.position + offset, agent.rotation, Vector3.one);
                Gizmos.DrawFrustum(Vector3.zero, viewAngle.value, 5, 0, 1f);
            }
        }
    }
}