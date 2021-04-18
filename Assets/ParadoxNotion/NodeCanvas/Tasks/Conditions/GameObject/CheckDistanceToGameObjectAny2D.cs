﻿using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeCanvas.Tasks.Conditions
{

    [Name("Any Target Within Distance 2D")]
    [Category("GameObject")]
    public class CheckDistanceToGameObjectAny2D : ConditionTask<Transform>
    {

        public BBParameter<List<GameObject>> targetObjects;
        public CompareMethod checkType = CompareMethod.LessThan;
        public BBParameter<float> distance = 10;

        [SliderField(0, 0.1f)]
        public float floatingPoint = 0.05f;

        [BlackboardOnly]
        public BBParameter<List<GameObject>> allResults;
        [BlackboardOnly]
        public BBParameter<GameObject> closerResult;

        protected override string info
        {
            get { return "Distance Any" + OperationTools.GetCompareString(checkType) + distance + " in " + targetObjects; }
        }

        protected override bool OnCheck()
        {
            bool r = false;
            List<GameObject> temp = new List<GameObject>();
            foreach (GameObject o in targetObjects.value)
            {

                if (o == agent.gameObject) { continue; }

                if (OperationTools.Compare(Vector2.Distance(agent.position, o.transform.position), distance.value, checkType, floatingPoint))
                {
                    temp.Add(o);
                    r = true;
                }
            }

            if (!allResults.isNone || !closerResult.isNone)
            {
                IOrderedEnumerable<GameObject> ordered = temp.OrderBy(x => Vector2.Distance(agent.position, x.transform.position));
                if (!allResults.isNone) { allResults.value = ordered.ToList(); }
                if (!closerResult.isNone) { closerResult.value = ordered.FirstOrDefault(); }
            }

            return r;
        }

        public override void OnDrawGizmosSelected()
        {
            if (agent != null) { Gizmos.DrawWireSphere((Vector2)agent.position, distance.value); }
        }
    }
}