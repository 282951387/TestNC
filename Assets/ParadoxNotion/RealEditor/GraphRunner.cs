using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphRunner : MonoBehaviour
{
    NodeCanvas.Framework.Graph g;
    // Start is called before the first frame update
    private void Start()
    {
        g = NodeCanvas.Framework.GraphManager.LoadGraphFromFile("Cat2", gameObject);
        NodeCanvas.Framework.GraphManager.StartGraph(g, gameObject);
        //NodeCanvas.Editor.GraphEditor.OpenWindow(g);
    }

    // Update is called once per frame
    private void Update()
    {
        g.UpdateGraph();
    }
}
