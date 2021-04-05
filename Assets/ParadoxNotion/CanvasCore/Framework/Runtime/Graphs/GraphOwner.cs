using NodeCanvas.Framework.Internal;
using ParadoxNotion.Serialization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace NodeCanvas.Framework
{

    /// A component that is used to control a Graph in respects to the gameobject attached to
	public abstract class GraphOwner : MonoBehaviour, ISerializationCallbackReceiver
    {

        ///----------------------------------------------------------------------------------------------

        public enum EnableAction
        {
            EnableBehaviour,
            DoNothing
        }

        public enum DisableAction
        {
            DisableBehaviour,
            PauseBehaviour,
            DoNothing
        }

        public enum FirstActivation
        {
            OnEnable,
            OnStart,
            Async
        }

        ///----------------------------------------------------------------------------------------------
        [SerializeField] private SerializationPair[] _serializedExposedParameters;
        internal List<ExposedParameter> exposedParameters { get; set; }

        //serialize exposed parameters
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (exposedParameters == null || exposedParameters.Count == 0)
            {
                _serializedExposedParameters = null;
                return;
            }
            _serializedExposedParameters = new SerializationPair[exposedParameters.Count];
            for (int i = 0; i < _serializedExposedParameters.Length; i++)
            {
                SerializationPair serializedParam = new SerializationPair();
                serializedParam._json = JSONSerializer.Serialize(typeof(ExposedParameter), exposedParameters[i], serializedParam._references);
                _serializedExposedParameters[i] = serializedParam;
            }
        }

        //deserialize exposed parameters
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (_serializedExposedParameters != null)
            {
                if (exposedParameters == null) { exposedParameters = new List<ExposedParameter>(); } else { exposedParameters.Clear(); }
                for (int i = 0; i < _serializedExposedParameters.Length; i++)
                {
                    ExposedParameter exposedParam = JSONSerializer.Deserialize<ExposedParameter>(_serializedExposedParameters[i]._json, _serializedExposedParameters[i]._references);
                    exposedParameters.Add(exposedParam);
                }
            }
        }
        ///----------------------------------------------------------------------------------------------


        ///Raised when the assigned behaviour state is changed (start/pause/stop)
        public static event System.Action<GraphOwner> onOwnerBehaviourStateChange;
        ///Raised only once when "Start" is called, then is set to null
        public event System.Action onMonoBehaviourStart;

        [SerializeField, FormerlySerializedAs("boundGraphSerialization")]
        private string _boundGraphSerialization;
        [SerializeField, FormerlySerializedAs("boundGraphObjectReferences")]
        private List<UnityEngine.Object> _boundGraphObjectReferences;
        [SerializeField]
        private GraphSource _boundGraphSource = new GraphSource();

        [SerializeField, FormerlySerializedAs("firstActivation")]
        [Tooltip("When the graph will first activate. Async mode will load the graph on a separate thread (thus no spikes), but the graph will activate a few frames later.")]
        private FirstActivation _firstActivation = FirstActivation.OnEnable;
        [SerializeField, FormerlySerializedAs("enableAction")]
        [Tooltip("What will happen when the GraphOwner is enabled")]
        private EnableAction _enableAction = EnableAction.EnableBehaviour;
        [SerializeField, FormerlySerializedAs("disableAction")]
        [Tooltip("What will happen when the GraphOwner is disabled")]
        private DisableAction _disableAction = DisableAction.DisableBehaviour;
        [SerializeField, Tooltip("If enabled, bound graph prefab overrides in instances will not be possible")]
        private bool _lockBoundGraphPrefabOverrides = true;
        [SerializeField, Tooltip("If enabled, all subgraphs will be pre-initialized in Awake along with the root graph, but this may have a loading performance cost")]
        private bool _preInitializeSubGraphs;
        [SerializeField, Tooltip("Specify when (if) the behaviour is updated. Changes to this only work when the behaviour starts, or re-starts")]
        private Graph.UpdateMode _updateMode = Graph.UpdateMode.NormalUpdate;

        private Dictionary<Graph, Graph> instances = new Dictionary<Graph, Graph>();

        ///----------------------------------------------------------------------------------------------

        ///The graph assigned
        public abstract Graph graph { get; set; }
        ///The blackboard assigned
        public abstract IBlackboard blackboard { get; set; }
        ///The type of graph that can be assigned
        public abstract System.Type graphType { get; }

        public bool initialized { get; private set; }
        public bool enableCalled { get; private set; }
        public bool startCalled { get; private set; }

        ///The bound graph source data
        public GraphSource boundGraphSource
        {
            get { return _boundGraphSource; }
            private set { _boundGraphSource = value; }
        }

        ///The bound graph serialization if any
        public string boundGraphSerialization
        {
            get { return _boundGraphSerialization; }
            private set { _boundGraphSerialization = value; }
        }

        ///The bound graph object references if any (this is a reference list. Dont touch if you are not sure how :) )
        public List<UnityEngine.Object> boundGraphObjectReferences
        {
            get { return _boundGraphObjectReferences; }
            private set { _boundGraphObjectReferences = value; }
        }

        ///Is the bound graph locked to changes from prefab instances?
        public bool lockBoundGraphPrefabOverrides
        {
            get { return _lockBoundGraphPrefabOverrides && graphIsBound; }
            set { _lockBoundGraphPrefabOverrides = value; }
        }

        ///Will subgraphs be preinitialized along with the root graph?
        public bool preInitializeSubGraphs
        {
            get { return _preInitializeSubGraphs; }
            set { _preInitializeSubGraphs = value; }
        }

        ///When will the first activation be (if EnableBehaviour at all)
        public FirstActivation firstActivation
        {
            get { return _firstActivation; }
            set { _firstActivation = value; }
        }

        ///What will happen OnEnable
        public EnableAction enableAction
        {
            get { return _enableAction; }
            set { _enableAction = value; }
        }

        ///What will happen OnDisable
        public DisableAction disableAction
        {
            get { return _disableAction; }
            set { _disableAction = value; }
        }

        ///When is the behaviour updated? Changes to this only work when the behaviour starts (or re-starts)
        public Graph.UpdateMode updateMode
        {
            get { return _updateMode; }
            set { _updateMode = value; }
        }

        ///Do we have a bound graph serialization?
        public bool graphIsBound => !string.IsNullOrEmpty(boundGraphSerialization);

        ///Is the assigned graph currently running?
        public bool isRunning => graph != null ? graph.isRunning : false;

        ///Is the assigned graph currently paused?
        public bool isPaused => graph != null ? graph.isPaused : false;

        ///The time is seconds the graph is running
        public float elapsedTime => graph != null ? graph.elapsedTime : 0;

        ///----------------------------------------------------------------------------------------------

        //Gets the instance graph for this owner from the provided graph
        protected Graph GetInstance(Graph originalGraph)
        {

            if (originalGraph == null)
            {
                return null;
            }

            //in editor the instance is always the original!
            if (!Application.isPlaying)
            {
                return originalGraph;
            }

            //if its already a stored instance, return the instance
            if (instances.ContainsValue(originalGraph))
            {
                return originalGraph;
            }

            Graph instance = null;

            //if it's not a strored instance create, store and return a new instance.
            if (!instances.TryGetValue(originalGraph, out instance))
            {
                instance = Graph.Clone<Graph>(originalGraph, null);
                instances[originalGraph] = instance;
            }

            return instance;
        }

        ///Start the graph assigned. It will be auto updated.
        public void StartBehaviour() { StartBehaviour(updateMode, null); }
        ///Start the graph assigned providing a callback for when it's finished if at all.
        public void StartBehaviour(System.Action<bool> callback) { StartBehaviour(updateMode, callback); }
        ///Start the graph assigned, optionally autoUpdated or not, and providing a callback for when it's finished if at all.
        public void StartBehaviour(Graph.UpdateMode updateMode, System.Action<bool> callback = null)
        {
            graph = GetInstance(graph);
            if (graph != null)
            {
                graph.StartGraph(this, blackboard, updateMode, callback);
                if (onOwnerBehaviourStateChange != null)
                {
                    onOwnerBehaviourStateChange(this);
                }
            }
        }

        ///Pause the current running graph
        public void PauseBehaviour()
        {
            if (graph != null)
            {
                graph.Pause();
                if (onOwnerBehaviourStateChange != null)
                {
                    onOwnerBehaviourStateChange(this);
                }
            }
        }

        ///Stop the current running graph
        public void StopBehaviour(bool success = true)
        {
            if (graph != null)
            {
                graph.Stop(success);
                if (onOwnerBehaviourStateChange != null)
                {
                    onOwnerBehaviourStateChange(this);
                }
            }
        }

        ///Manually update the assigned graph
        public void UpdateBehaviour()
        {
            if (graph != null)
            {
                graph.UpdateGraph();
            }
        }

        ///The same as calling Stop, Start Behaviour
        public void RestartBehaviour()
        {
            StopBehaviour();
            StartBehaviour();
        }

        ///----------------------------------------------------------------------------------------------

        ///Send an event to the graph. Note that this overload has no sender argument thus sender will be null.
        public void SendEvent(string eventName) { if (graph != null) { graph.SendEvent(eventName, null, null); } }
        ///Send an event to the graph
        public void SendEvent(string eventName, object value, object sender) { if (graph != null) { graph.SendEvent(eventName, value, sender); } }
        ///Send an event to the graph
        public void SendEvent<T>(string eventName, T eventValue, object sender) { if (graph != null) { graph.SendEvent(eventName, eventValue, sender); } }

        ///----------------------------------------------------------------------------------------------

        ///Return an exposed parameter value
        public T GetExposedParameterValue<T>(string name)
        {
            ExposedParameter param = exposedParameters.Find(x => x.varRefBoxed != null && x.varRefBoxed.name == name);
            return param != null ? (param as ExposedParameter<T>).value : default(T);
        }

        ///Set an exposed parameter value
        public void SetExposedParameterValue<T>(string name, T value)
        {
            ExposedParameter param = exposedParameters.Find(x => x.varRefBoxed != null && x.varRefBoxed.name == name);
            if (param != null) { (param as ExposedParameter<T>).value = value; }
        }

        ///----------------------------------------------------------------------------------------------

        //Initialize the bound or asset graph
        protected void Awake()
        {
            Initialize();
        }

        ///Initialize the bound or asset graph. This is called in Awake automatically,
        ///but it's public so that you can call this manually to pre-initialize when gameobject is deactive, if required.
        public void Initialize()
        {

            Debug.Assert(Application.isPlaying, "GraphOwner Initialize should have been called in runtime only");

            if (initialized)
            {
                return;
            }

            if (graph == null && !graphIsBound)
            {
                return;
            }

            GraphSource finalSource;
            string finalJson;
            List<UnityEngine.Object> finalReferences;

            Graph newGraph = (Graph)ScriptableObject.CreateInstance(graphType);

            if (graphIsBound)
            {
                //Bound
                newGraph.name = graphType.Name;
                finalSource = boundGraphSource;
                finalJson = boundGraphSerialization;
                finalReferences = boundGraphObjectReferences;
                instances[newGraph] = newGraph;
            }
            else
            {
                //Asset reference
                newGraph.name = graph.name;
                finalSource = graph.GetGraphSource();
                finalJson = graph.GetSerializedJsonData();
                finalReferences = graph.GetSerializedReferencesData();
                instances[graph] = newGraph;
            }

            graph = newGraph;

            GraphLoadData loadData = new GraphLoadData();
            loadData.source = finalSource;
            loadData.json = finalJson;
            loadData.references = finalReferences;
            loadData.agent = this;
            loadData.parentBlackboard = blackboard;
            loadData.preInitializeSubGraphs = preInitializeSubGraphs;

            if (firstActivation == FirstActivation.Async)
            {
                graph.LoadOverwriteAsync(loadData, () =>
                {
                    BindExposedParameters();
                    //remark: activeInHierarchy is checked in case user instantiate and disable gameobject instantly for pooling reasons
                    if (!isRunning && enableAction == EnableAction.EnableBehaviour && gameObject.activeInHierarchy)
                    {
                        StartBehaviour();
                        InvokeStartEvent();
                    }
                });
            }
            else
            {
                graph.LoadOverwrite(loadData);
                BindExposedParameters();
            }

            initialized = true;
        }

        ///Bind exposed parameters to local graph blackboard variables
        public void BindExposedParameters()
        {
            if (exposedParameters != null && graph != null)
            {
                for (int i = 0; i < exposedParameters.Count; i++)
                {
                    exposedParameters[i].Bind(graph.blackboard);
                }
            }
        }

        //handle enable behaviour setting
        protected void OnEnable()
        {
            if (firstActivation == FirstActivation.OnEnable || enableCalled)
            {
                if ((!isRunning || isPaused) && enableAction == EnableAction.EnableBehaviour)
                {
                    StartBehaviour();
                }
            }

            enableCalled = true;
        }

        //...
        protected void Start()
        {
            if (firstActivation == FirstActivation.OnStart)
            {
                if (!isRunning && enableAction == EnableAction.EnableBehaviour)
                {
                    StartBehaviour();
                }
            }

            InvokeStartEvent();
            startCalled = true;
        }

        //This can actually be invoked in Start but if loading async it also needs to be called.
        //In either case, it's called only once.
        private void InvokeStartEvent()
        {
            //since "Start" is called once anyway we clear the event
            if (onMonoBehaviourStart != null)
            {
                onMonoBehaviourStart();
                onMonoBehaviourStart = null;
            }
        }

        //handle disable behaviour setting
        protected void OnDisable()
        {

            if (disableAction == DisableAction.DisableBehaviour)
            {
                StopBehaviour();
            }

            if (disableAction == DisableAction.PauseBehaviour)
            {
                PauseBehaviour();
            }
        }

        //Destroy instanced graphs as well
        protected void OnDestroy()
        {

            StopBehaviour();

            foreach (Graph instanceGraph in instances.Values)
            {
                foreach (Graph subGraph in instanceGraph.GetAllInstancedNestedGraphs())
                {
                    Destroy(subGraph);
                }
                Destroy(instanceGraph);
            }
        }


        ///----------------------------------------------------------------------------------------------
        ///---------------------------------------UNITY EDITOR-------------------------------------------
        ///----------------------------------------------------------------------------------------------
#if UNITY_EDITOR

        protected Graph boundGraphInstance;

        ///Editor. Called after assigned graph is serialized.
        internal void OnAfterGraphSerialized(Graph serializedGraph)
        {
            ///If the graph is bound, we store the serialization data here.
            if (graphIsBound && boundGraphInstance == serializedGraph)
            {

                //---
                if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this))
                {
                    UnityEditor.SerializedProperty boundProp = new UnityEditor.SerializedObject(this).FindProperty(nameof(_boundGraphSerialization));
                    if (!boundProp.prefabOverride && boundGraphSerialization != serializedGraph.GetSerializedJsonData())
                    {
                        if (lockBoundGraphPrefabOverrides)
                        {
                            ParadoxNotion.Services.Logger.LogWarning("The Bound Graph is Prefab Locked!\nChanges you make are not saved!\nUnlock the Prefab, or Edit the Prefab Asset.", LogTag.EDITOR, this);
                            return;
                        }
                        else
                        {
                            ParadoxNotion.Services.Logger.LogWarning("Prefab Bound Graph just got overridden!", LogTag.EDITOR, this);
                        }
                    }
                }
                //---

                ParadoxNotion.Design.UndoUtility.RecordObject(this, ParadoxNotion.Design.UndoUtility.GetLastOperationNameOr("Bound Graph Change"));
                boundGraphSource = serializedGraph.GetGraphSource();
                boundGraphSerialization = serializedGraph.GetSerializedJsonData();
                boundGraphObjectReferences = serializedGraph.GetSerializedReferencesData();
                ParadoxNotion.Design.UndoUtility.SetDirty(this);
            }
        }

        ///Editor. Validate.
        protected void OnValidate() { Validate(); }
        ///Editor. Validate.
        internal void Validate()
        {

            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                //everything here is relevant to bound graphs only.
                //we only do this for when the object is an instance or is edited in the prefab editor.
                if (!UnityEditor.EditorUtility.IsPersistent(this) && graphIsBound)
                {

                    if (boundGraphInstance == null)
                    {
                        boundGraphInstance = (Graph)ScriptableObject.CreateInstance(graphType);
                    }

                    boundGraphInstance.name = graphType.Name;
                    boundGraphInstance.SetGraphSourceMetaData(boundGraphSource);
                    boundGraphInstance.Deserialize(boundGraphSerialization, boundGraphObjectReferences, false);
                    boundGraphInstance.UpdateReferencesFromOwner(this);
                    boundGraphInstance.Validate();
                }
                else if (graph != null)
                {
                    graph.UpdateReferencesFromOwner(this);
                    graph.Validate();
                }

                //done in editor as well only for convenience purposes.
                // DISABLE: was creating confusion when editing multiple graphowner instances using asset graphs and having different variable overrides
                // BindExposedParameters();
            }
        }

        ///Editor. Binds the target graph (null to delete current bound).
        internal void SetBoundGraphReference(Graph target)
        {

            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("SetBoundGraphReference method is an Editor only method!");
                return;
            }

            //cleanup
            graph = null;
            boundGraphInstance = null;
            if (target == null)
            {
                boundGraphSource = null;
                boundGraphSerialization = null;
                boundGraphObjectReferences = null;
                return;
            }

            //serialize target and store boundGraphSerialization data
            target.SelfSerialize();
            _boundGraphSerialization = target.GetSerializedJsonData();
            _boundGraphObjectReferences = target.GetSerializedReferencesData();
            _boundGraphSource = target.GetGraphSourceMetaDataCopy();
            Validate(); //validate to handle bound graph instance
        }

        ///Reset unity callback
        protected void Reset()
        {
            blackboard = gameObject.GetComponent<IBlackboard>();
            if (blackboard == null)
            {
                blackboard = gameObject.AddComponent<Blackboard>();
            }
        }

        //...
        protected void OnDrawGizmos()
        {
            Gizmos.DrawIcon(transform.position, "GraphOwnerGizmo.png", true);
        }

        ///Forward Gizmos callback
        protected void OnDrawGizmosSelected()
        {
            if (Editor.GraphEditorUtility.activeElement != null)
            {
                ParadoxNotion.HierarchyTree.Element rootElement = Editor.GraphEditorUtility.activeElement.graph.GetFlatMetaGraph().FindReferenceElement(Editor.GraphEditorUtility.activeElement);
                if (rootElement != null)
                {
                    foreach (Task task in rootElement.GetAllChildrenReferencesOfType<Task>())
                    {
                        task.OnDrawGizmosSelected();
                    }
                }
            }
        }
#endif

    }


    ///----------------------------------------------------------------------------------------------

    ///The class where GraphOwners derive from
    public abstract class GraphOwner<T> : GraphOwner where T : Graph
    {

        [SerializeField] private T _graph;
        [SerializeField] private Object _blackboard;

        ///The current behaviour Graph assigned
        public sealed override Graph graph
        {
            get
            {
#if UNITY_EDITOR
                //In Editor only and if graph is bound, return the bound graph instance
                if (graphIsBound && !ParadoxNotion.Services.Threader.applicationIsPlaying)
                {
                    return boundGraphInstance;
                }
#endif
                //In runtime an instance of either boundGraphSerialization json or Asset Graph is created in awake
                return _graph;
            }
            set { _graph = (T)value; }
        }

        ///The current behaviour Graph assigned (same as .graph but of type T)
        public T behaviour
        {
            get { return (T)graph; }
            set { graph = value; }
        }

        ///The blackboard that the assigned behaviour will be Started with or currently using
        public sealed override IBlackboard blackboard
        {
            //check != null to handle unity object when component is removed from inspector
            get { return _blackboard != null ? _blackboard as IBlackboard : null; }
            set
            {
                if (!ReferenceEquals(_blackboard, value))
                {
                    _blackboard = (Object)value;
                    if (graph != null)
                    {
                        graph.UpdateReferences(this, value);
                    }
                }
            }
        }

        ///The Graph type this Owner can be assigned
        public sealed override System.Type graphType => typeof(T);

        ///Start a new behaviour on this owner
        public void StartBehaviour(T newGraph) { StartBehaviour(newGraph, updateMode, null); }
        ///Start a new behaviour on this owner and get a callback for when it's finished if at all
        public void StartBehaviour(T newGraph, System.Action<bool> callback) { StartBehaviour(newGraph, updateMode, callback); }
        ///Start a new behaviour on this owner and optionally autoUpdated or not and optionally get a callback for when it's finished if at all
        public void StartBehaviour(T newGraph, Graph.UpdateMode updateMode, System.Action<bool> callback = null)
        {
            SwitchBehaviour(newGraph, updateMode, callback);
        }

        ///Use to switch the behaviour dynamicaly at runtime
        public void SwitchBehaviour(T newGraph) { SwitchBehaviour(newGraph, updateMode, null); }
        ///Use to switch or set graphs at runtime and optionaly get a callback when it's finished if at all
        public void SwitchBehaviour(T newGraph, System.Action<bool> callback) { SwitchBehaviour(newGraph, updateMode, callback); }
        ///Use to switch or set graphs at runtime and optionaly get a callback when it's finished if at all
        public void SwitchBehaviour(T newGraph, Graph.UpdateMode updateMode, System.Action<bool> callback = null)
        {
            StopBehaviour();
            graph = newGraph;
            StartBehaviour(updateMode, callback);
        }
    }
}