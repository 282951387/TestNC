using NodeCanvas.Framework;
using ParadoxNotion.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace NodeCanvas
{

    [AddComponentMenu("NodeCanvas/Standalone Action List (Bonus)")]
    public class ActionListPlayer : MonoBehaviour, ITaskSystem, ISerializationCallbackReceiver
    {

        public bool playOnAwake;

        [SerializeField]
        private string _serializedList;
        [SerializeField]
        private List<UnityEngine.Object> _objectReferences;
        [SerializeField]
        private Blackboard _blackboard;

        [System.NonSerialized]
        private ActionList _actionList;

        private float timeStarted;

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (ParadoxNotion.Services.Threader.applicationIsPlaying) { return; }
            _objectReferences = new List<UnityEngine.Object>();
            _serializedList = JSONSerializer.Serialize(typeof(ActionList), _actionList, _objectReferences);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _actionList = JSONSerializer.Deserialize<ActionList>(_serializedList, _objectReferences);
            if (_actionList == null)
            {
                _actionList = (ActionList)Task.Create(typeof(ActionList), this);
            }
        }

        ///----------------------------------------------------------------------------------------------

        public ActionList actionList => _actionList;
        public float elapsedTime => Time.time - timeStarted;
        public float deltaTime => Time.deltaTime;
        Object ITaskSystem.contextObject => this;
        Component ITaskSystem.agent => this;

        public IBlackboard blackboard
        {
            get { return _blackboard; }
            set
            {
                if (!ReferenceEquals(_blackboard, value))
                {
                    _blackboard = (Blackboard)value;
                    UpdateTasksOwner();
                }
            }
        }

        ///----------------------------------------------------------------------------------------------

        public static ActionListPlayer Create()
        {
            return new GameObject("ActionList").AddComponent<ActionListPlayer>();
        }

        protected void Awake()
        {
            UpdateTasksOwner();
            if (playOnAwake)
            {
                Play();
            }
        }

        public void UpdateTasksOwner()
        {
            actionList.SetOwnerSystem(this);
            foreach (ActionTask a in actionList.actions)
            {
                a.SetOwnerSystem(this);
                BBParameter.SetBBFields(a, blackboard);
            }
        }

        void ITaskSystem.SendEvent(string name, object value, object sender)
        {
            ParadoxNotion.Services.Logger.LogWarning("Sending events to action lists has no effect");
        }
        void ITaskSystem.SendEvent<T>(string name, T value, object sender)
        {
            ParadoxNotion.Services.Logger.LogWarning("Sending events to action lists has no effect");
        }

        [ContextMenu("Play")]
        public void Play() { Play(this, blackboard, null); }
        public void Play(System.Action<Status> OnFinish) { Play(this, blackboard, OnFinish); }
        public void Play(Component agent, IBlackboard blackboard, System.Action<Status> OnFinish)
        {
            if (Application.isPlaying)
            {
                timeStarted = Time.time;
#if UNITY_EDITOR
                actionList.ExecuteIndependent(agent, blackboard, OnFinish);
#endif
            }
        }

        public Status Execute() { return actionList.Execute(this, blackboard); }
        public Status Execute(Component agent) { return actionList.Execute(agent, blackboard); }


        ///----------------------------------------------------------------------------------------------
        ///---------------------------------------UNITY EDITOR-------------------------------------------
#if UNITY_EDITOR

        [UnityEditor.MenuItem("Tools/ParadoxNotion/NodeCanvas/Create/Standalone Action List")]
        private static void CreateActionListPlayer()
        {
            UnityEditor.Selection.activeObject = Create();
        }

        private void Reset()
        {
            Blackboard bb = GetComponent<Blackboard>();
            _blackboard = bb != null ? bb : gameObject.AddComponent<Blackboard>();
            _actionList = (ActionList)Task.Create(typeof(ActionList), this);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                UpdateTasksOwner();
            }
        }
#endif

    }
}