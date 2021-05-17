using NodeCanvas.Framework.Internal;
using ParadoxNotion.Serialization;
using ParadoxNotion.Serialization.FullSerializer;
using ParadoxNotion.Services;
using System;
using System.Collections;
using UnityEngine;


namespace NodeCanvas.Framework
{

    ///Base class for actions. Extend this to create your own. T is the agentType required by the action.
    ///Generic version where T is the AgentType (Component or Interface) required by the Action.
    ///For GameObject, use 'Transform'
    public abstract class ActionTask<T> : ActionTask where T : class
    {
        public sealed override Type agentType { get { return typeof(T); } }
        public new T agent { get { return base.agent as T; } }
    }

    ///----------------------------------------------------------------------------------------------

#if UNITY_EDITOR //handles missing types
    [fsObject(Processor = typeof(fsRecoveryProcessor<ActionTask, MissingAction>))]
#endif

    ///Base class for all actions. Extend this to create your own.
    public abstract class ActionTask : Task
    {

        private Status status = Status.Resting;
        private float timeStarted;
        private bool latch;

        ///The time in seconds this action is running if at all
        public float elapsedTime => (isRunning ? ownerSystem.elapsedTime - timeStarted : 0);

        ///Is the action currently running?
        public bool isRunning => status == Status.Running;

        ///Is the action currently paused?
        public bool isPaused { get; private set; }

        ///----------------------------------------------------------------------------------------------

        ///Used to call an action for standalone execution providing a callback.
        ///Be careful! *This will make the action execute as a coroutine*
        
        //TODO!!!
        public void ExecuteIndependent(Component agent, IBlackboard blackboard, Action<Status> callback)
        {
#if UNITY_EDITOR
            if (!isRunning) { MonoManager.current.StartCoroutine(IndependentActionUpdater(agent, blackboard, callback)); }
#endif
        }

        //The internal updater for when an action has been called with a callback parameter and only then.
        //This is only used and usefull if user needs to execute an action task completely as standalone.
        private IEnumerator IndependentActionUpdater(Component agent, IBlackboard blackboard, Action<Status> callback)
        {
            while (Execute(agent, blackboard) == Status.Running) { yield return null; }
            if (callback != null) { callback(status); }
        }

        ///----------------------------------------------------------------------------------------------

        [System.Obsolete("Use 'Execute'")]
        public Status ExecuteAction(Component agent, IBlackboard blackboard) { return Execute(agent, blackboard); }

        ///Ticks the action for the provided agent and blackboard
        public Status Execute(Component agent, IBlackboard blackboard)
        {

            if (!isUserEnabled)
            {
                return Status.Optional;
            }

            if (isPaused)
            {
                OnResume();
            }

            isPaused = false;
            if (status == Status.Running)
            {
                OnUpdate();
                latch = false;
                return status;
            }

            //latch is used to be able to call EndAction anywhere
            if (latch)
            {
                latch = false;
                return status;
            }

            if (!Set(agent, blackboard))
            {
                latch = false;
                return Status.Failure;
            }

            timeStarted = ownerSystem.elapsedTime;
            status = Status.Running;
            OnExecute();
            if (status == Status.Running)
            {
                OnUpdate();
            }
            latch = false;
            return status;
        }

        ///Ends the action either in success or failure. Ending with null means that it's a cancel/interrupt.
        ///Null is used by the external system. You should use true or false when calling EndAction within it.
        public void EndAction() { EndAction(true); }
        public void EndAction(bool success) { EndAction((bool?)success); }
        public void EndAction(bool? success)
        {

            if (status != Status.Running)
            {
                return;
            }

            latch = success != null ? true : false;

            isPaused = false;
            status = success == null ? Status.Resting : (success == true ? Status.Success : Status.Failure);
            OnStop(success == null);
        }

        ///Pause the action from updating and calls OnPause
        public void Pause()
        {

            if (status != Status.Running)
            {
                return;
            }

            isPaused = true;
            OnPause();
        }

        ///----------------------------------------------------------------------------------------------

        ///Called once when the actions is executed.
        protected virtual void OnExecute() { }
        ///Called every frame, if and while the action is running and until it ends.
        protected virtual void OnUpdate() { }
        ///Called whenever the action ends due to any reason with the argument denoting whether the action was interrupted or finished properly.
        protected virtual void OnStop(bool interrupted) { OnStop(); }
        ///Called whenever the action ends due to any reason.
        protected virtual void OnStop() { }
        ///Called when the action gets paused
        protected virtual void OnPause() { }
        ///Called when the action resumes after being paused
        protected virtual void OnResume() { }
        ///----------------------------------------------------------------------------------------------
    }
}