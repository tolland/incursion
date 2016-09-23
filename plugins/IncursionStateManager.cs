using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{

    [Info("Incursion State Manager", "Tolland", "0.1.0")]
    public class IncursionStateManager : RustPlugin
    {
        static IncursionStateManager incursionStateManager = null;

        private static bool Debug = false;

        void Init()
        {
            incursionStateManager = this;
        }

        #region State Machine

        public class StateManager
        {
            IStateMachine currentState = null;
            IStateMachine previousState = null;

            public StateManager(IStateMachine initialState)
            {
                if (initialState == null)
                    return;

                //set the initial state
                currentState = initialState;
                //@todo not sure if this is correct. should we enter the initial state?
                currentState.Enter(this);

            }


            //in the state patten, this is used to execute the current state
            //not sure if this is relevant to this system???
            public void Update()
            {
                incursionStateManager.Puts(this.ToString());
                currentState.Execute(this);
            }

            public virtual void ChangeState(IStateMachine newState)
            {
                //@todo probably want to throw exception instead
                if ((currentState == null) || (newState == null))
                    return;
                IemUtils.SLog("stateManager:"+ this.GetType().Name);
                IemUtils.SLog("StateManager:ChangeState:oldstate:" + currentState);
                IemUtils.SLog("StateManager:ChangeState:newstate:" + newState);

                currentState.Exit(this);
                previousState = currentState;
                currentState = newState;
                currentState.Enter(this);
            }

            /// <summary>
            /// enter a substate, where you want to revert to the previous state
            /// </summary>
            /// <param name="newState"></param>
            public virtual void SubState(IStateMachine newState)
            {
                if ((previousState == null) || (newState == null))
                    return;
                IemUtils.SLog("stateManager:" + this.GetType().Name);
                IemUtils.SLog("StateManager:ChangeState:PrevState:" + currentState);
                IemUtils.SLog("StateManager:ChangeState:newstate:" + newState);
                previousState = currentState;
                currentState = newState;
                currentState.Enter(this);
            }

            public virtual void SubStateReturn()
            {
                IemUtils.SLog("stateManager:" + this.GetType().Name);
                IemUtils.SLog("StateManager:ChangeState:PrevState:" + currentState);
                IemUtils.SLog("StateManager:ChangeState:Reverting:" + previousState);
                currentState.Exit(this);
                currentState = previousState;
            }

            public IStateMachine GetState()
            {
                return currentState;
            }

        }

        public interface IStateMachine
        {
            void Enter(StateManager esm);
            void Execute(StateManager esm);
            void Exit(StateManager esm);
        }

        public abstract class StateBase<T> where T : StateBase<T>,
            IncursionStateManager.IStateMachine, new()
        {

            //add something for thread safety.... locks etc
            private static T _instance = new T();

            public static T Instance
            {
                get { return _instance; }
            }

            public virtual void Enter(StateManager psm)
            {
                DLog("Entering the " + typeof(T));
            }

            public void Execute(StateManager psm)
            {
                DLog("Executing the " + typeof(T));
            }

            public void Exit(StateManager psm)
            {
                DLog("Exiting the " + typeof(T));
            }


        }

        #endregion

        static void DLog(string message)
        {
            ConVar.Server.Log("oxide/logs/ESMlog.txt", message);
            incursionStateManager.Puts(message);
        }

        static void BroadcastChat(string message)
        {
            incursionStateManager.rust.BroadcastChat(message);
        }
    }

}