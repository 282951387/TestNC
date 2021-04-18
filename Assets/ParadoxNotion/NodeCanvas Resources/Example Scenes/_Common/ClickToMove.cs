using UnityEngine;
using System.Collections;

#if UNITY_5_5_OR_NEWER
using NavMeshAgent = UnityEngine.AI.NavMeshAgent;
#else
using NavMeshAgent = UnityEngine.NavMeshAgent;
#endif

[RequireComponent(typeof(NavMeshAgent))]
public class ClickToMove : MonoBehaviour
{

    private NavMeshAgent navAgent;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity))
            {
                navAgent.SetDestination(hit.point);
            }
        }
    }
}
