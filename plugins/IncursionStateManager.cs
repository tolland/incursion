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
            IStateMachine currentState;

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

            public void ChangeState(IStateMachine newState)
            {
                //probably want to throw exception instead
                if ((currentState == null) || (newState == null))
                    return;
                currentState.Exit(this);
                currentState = newState;
                currentState.Enter(this);
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

            public void Enter(StateManager psm)
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