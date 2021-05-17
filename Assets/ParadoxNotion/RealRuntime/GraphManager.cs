using NodeCanvas.Framework.Internal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NodeCanvas.Framework
{

    public class GraphManager
    {
        public static void StartGraph(Graph g, GameObject owner)
        {
            g.StartGraph(owner.transform, new BlackboardSource(), Graph.UpdateMode.NormalUpdate, null);
        }

        public static Graph LoadGraphFromFile(string fileName, GameObject owner = null)
        {
            TextAsset text = Resources.Load<TextAsset>(fileName);
            if (owner == null)
            {
                owner = GameObject.FindGameObjectWithTag("TestGraphOwner");
            }

            return LoadGraph(text.text, owner);
        }

        public static Graph LoadGraph(string json, GameObject owner)
        {
            return Initialize(json, typeof(NodeCanvas.BehaviourTrees.BehaviourTree), owner.GetComponent<Transform>(), "Cat");
        }

        public static Graph Initialize(string json, System.Type graphType, Component agent, string graphName)
        {

            //Debug.Assert(Application.isPlaying, "GraphOwner Initialize should have been called in runtime only");

            //if (initialized)
            //{
            //    return;
            //}

            //if (graph == null && !graphIsBound)
            //{
            //    return;
            //}

            GraphSource finalSource;
            string finalJson;
            //List<UnityEngine.Object> finalReferences;

            Graph newGraph = (Graph)UnityEngine.ScriptableObject.CreateInstance(graphType);

            //if (graphIsBound)
            //{
            //    //Bound
            //    newGraph.name = graphType.Name;
            //    finalSource = boundGraphSource;
            //    finalJson = boundGraphSerialization;
            //    finalReferences = boundGraphObjectReferences;
            //    instances[newGraph] = newGraph;
            //}
            //else
            {
                //Asset reference
                newGraph.name = graphName;//graph.name;
                finalSource = null;// graph.GetGraphSource();
                finalJson = json;//graph.GetSerializedJsonData();
                //finalReferences = graph.GetSerializedReferencesData();
                //instances[graph] = newGraph;
            }

            Graph graph = newGraph;

            GraphLoadData loadData = new GraphLoadData();
            loadData.source = finalSource;
            loadData.json = finalJson;
            //loadData.references = finalReferences;
            loadData.agent = agent;
            loadData.parentBlackboard = null;//blackboard;
            loadData.preInitializeSubGraphs = false;//preInitializeSubGraphs; TODO

            //if (firstActivation == FirstActivation.Async)
            //{
            //    graph.LoadOverwriteAsync(loadData, () =>
            //    {
            //        BindExposedParameters();
            //        //remark: activeInHierarchy is checked in case user instantiate and disable gameobject instantly for pooling reasons
            //        if (!isRunning && enableAction == EnableAction.EnableBehaviour && gameObject.activeInHierarchy)
            //        {
            //            StartBehaviour();
            //            InvokeStartEvent();
            //        }
            //    });
            //}
            //else
            {
                graph.LoadOverwrite(loadData);
                //BindExposedParameters();
            }

            //initialized = true;
            return newGraph;
        }

    }
}
