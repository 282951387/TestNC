using NodeCanvas.Framework;
using ParadoxNotion.Design;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = ParadoxNotion.Services.Logger;

namespace NodeCanvas.StateMachines
{

    /// Use FSMs to create state like behaviours
    [GraphInfo(
        packageName = "NodeCanvas",
        docsURL = "http://nodecanvas.paradoxnotion.com/documentation/",
        resourcesURL = "http://nodecanvas.paradoxnotion.com/downloads/",
        forumsURL = "http://nodecanvas.paradoxnotion.com/forums-page/"
        )]
    [CreateAssetMenu(menuName = "ParadoxNotion/NodeCanvas/FSM Asset")]
    public class FSM : Graph
    {
        ///Transition Calling Mode (see "EnterState")
        public enum TransitionCallMode
        {
            Normal = 0,
            Stacked = 1,
            Clean = 2,
        }

        private List<IUpdatable> updatableNodes;
        private IStateCallbackReceiver[] callbackReceivers;
        private Stack<FSMState> stateStack;

        public event System.Action<IState> onStateEnter;
        public event System.Action<IState> onStateUpdate;
        public event System.Action<IState> onStateExit;
        public event System.Action<IState> onStateTransition;

        ///The current FSM state
        public FSMState currentState { get; private set; }
        ///The previous FSM state
        public FSMState previousState { get; private set; }

        ///The current state name. Null if none
        public string currentStateName => currentState != null ? currentState.name : null;

        ///The previous state name. Null if none
        public string previousStateName => previousState != null ? previousState.name : null;

        public override System.Type baseNodeType => typeof(FSMNode);
        public override bool requiresAgent => true;
        public override bool requiresPrimeNode => true;
        public override bool isTree => false;
        public override bool allowBlackboardOverrides => true;
        public sealed override bool canAcceptVariableDrops => false;

        ///----------------------------------------------------------------------------------------------

        protected override void OnGraphInitialize()
        {
            //we may be loading in async
            ThreadSafeInitCall(GatherCallbackReceivers);
            updatableNodes = new List<IUpdatable>();
            for (int i = 0; i < allNodes.Count; i++)
            {
                if (allNodes[i] is IUpdatable)
                {
                    updatableNodes.Add((IUpdatable)allNodes[i]);
                }
            }
        }

        protected override void OnGraphStarted()
        {
            stateStack = new Stack<FSMState>();
            EnterState((FSMState)primeNode, TransitionCallMode.Normal);
        }

        protected override void OnGraphUpdate()
        {

            if (currentState != null)
            {

                //Update defer updatables (basically AnyStates and ConcurentStates)
                for (int i = 0; i < updatableNodes.Count; i++)
                {
                    updatableNodes[i].Update();
                }

                //this can only happen if FSM stoped just now (from the above update)
                if (currentState == null) { Stop(false); return; }

                //Update current state
                currentState.Update();
                if (onStateUpdate != null && currentState.status == Status.Running)
                {
                    onStateUpdate(currentState);
                }

                //this can only happen if FSM stoped just now (from the above update)
                if (currentState == null) { Stop(false); return; }

                //state has nowhere to go..
                if (currentState.status != Status.Running && currentState.outConnections.Count == 0)
                {
                    //...but we have a stacked state -> pop return to it
                    if (stateStack.Count > 0)
                    {
                        FSMState popState = stateStack.Pop();
                        EnterState(popState, TransitionCallMode.Normal);
                        return;
                    }

                    //...and no updatables -> stop
                    if (!updatableNodes.Any(n => n.status == Status.Running))
                    {
                        Stop(true);
                        return;
                    }
                }
            }

            //if null state, stop.
            if (currentState == null)
            {
                Stop(false);
                return;
            }
        }

        protected override void OnGraphStoped()
        {
            if (currentState != null)
            {
                if (onStateExit != null)
                {
                    onStateExit(currentState);
                }
            }

            previousState = null;
            currentState = null;
            stateStack = null;
        }

        ///Enter a state providing the state itself
        public bool EnterState(FSMState newState, TransitionCallMode callMode)
        {

            if (!isRunning)
            {
                Logger.LogWarning("Tried to EnterState on an FSM that was not running", LogTag.EXECUTION, this);
                return false;
            }

            if (newState == null)
            {
                Logger.LogWarning("Tried to Enter Null State", LogTag.EXECUTION, this);
                return false;
            }

            if (currentState != null)
            {
                if (onStateExit != null) { onStateExit(currentState); }
                currentState.Reset(false);
                if (callMode == TransitionCallMode.Stacked)
                {
                    stateStack.Push(currentState);
                    if (stateStack.Count > 5)
                    {
                        Logger.LogWarning("State stack exceeds 5. Ensure that you are not cycling stack calls", LogTag.EXECUTION, this);
                    }
                }
            }

            if (callMode == TransitionCallMode.Clean)
            {
                stateStack.Clear();
            }

            previousState = currentState;
            currentState = newState;

            if (onStateTransition != null) { onStateTransition(currentState); }
            if (onStateEnter != null) { onStateEnter(currentState); }
            currentState.Execute(agent, blackboard);
            return true;
        }

        ///Trigger a state to enter by it's name. Returns the state found and entered if any
        public FSMState TriggerState(string stateName, TransitionCallMode callMode)
        {

            FSMState state = GetStateWithName(stateName);
            if (state != null)
            {
                EnterState(state, callMode);
                return state;
            }

            Logger.LogWarning("No State with name '" + stateName + "' found on FSM '" + name + "'", LogTag.EXECUTION, this);
            return null;
        }

        ///Get all State Names
        public string[] GetStateNames()
        {
            return allNodes.Where(n => n is FSMState).Select(n => n.name).ToArray();
        }

        ///Get a state by it's name
        public FSMState GetStateWithName(string name)
        {
            return (FSMState)allNodes.Find(n => n is FSMState && n.name == name);
        }

        //Gather IStateCallbackReceivers and subscribe them to state events
        private void GatherCallbackReceivers()
        {

            if (agent == null) { return; }

            callbackReceivers = agent.gameObject.GetComponents<IStateCallbackReceiver>();
            if (callbackReceivers.Length > 0)
            {
                onStateEnter += (x) => { foreach (IStateCallbackReceiver m in callbackReceivers) { m.OnStateEnter(x); } };
                onStateUpdate += (x) => { foreach (IStateCallbackReceiver m in callbackReceivers) { m.OnStateUpdate(x); } };
                onStateExit += (x) => { foreach (IStateCallbackReceiver m in callbackReceivers) { m.OnStateExit(x); } };
            }
        }

        public FSMState PeekStack()
        {
            return stateStack != null && stateStack.Count > 0 ? stateStack.Peek() : null;
        }


        ///----------------------------------------------------------------------------------------------
        ///---------------------------------------UNITY EDITOR-------------------------------------------
#if UNITY_EDITOR

        [UnityEditor.MenuItem("Tools/ParadoxNotion/NodeCanvas/Create/State Machine Asset", false, 0)]
        private static void Editor_CreateGraph()
        {
            FSM newGraph = EditorUtils.CreateAsset<FSM>();
            UnityEditor.Selection.activeObject = newGraph;
        }
#endif

    }
}