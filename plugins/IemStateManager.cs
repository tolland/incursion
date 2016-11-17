using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{

    [Info("Incursion State Manager", "Tolland", "0.1.0")]
    public class IemStateManager : RustPlugin
    {
        static IemStateManager iemStateManager;

        private static bool Debug = false;

        void Init()
        {
            iemStateManager = this;
        }

        #region State Machine

        public class StateManager
        {
            IStateMachine currentState;
            IStateMachine previousState;

            public StateManager(IStateMachine initialState)
            {
                if ((initialState == null))
                    throw new Exception("initialState cannot be null");

                IemUtils.SLog(this.GetType().Name + ":CREATING");
                //set the initial state
                currentState = initialState;


                //@todo not sure if this is correct. should we enter the initial state?
                currentState.Enter(this);
                IemUtils.SLog(this.GetType().Name + ":initialstate:" +
                    currentState.ToString().Replace("Oxide.Plugins.", ""));
            }


            //in the state patten, this is used to execute the current state
            //not sure if this is relevant to this system???
            public void Update()
            {
                if ((currentState == null))
                    throw new Exception("currentState state is null");

                string buf = currentState.ToString();

                IemUtils.SLog(this.GetType().Name + ":Executing:" + buf);
                currentState.Execute(this);
                IemUtils.SLog(this.GetType().Name + ":Executed:" + buf);
            }

            public virtual void ChangeState(IStateMachine newState)
            {
                //@todo probably want to throw exception instead
                if ((currentState == null) || (newState == null))
                {
                    throw new Exception("can't change from or to invalid state");
                }

                //IemUtils.SLog("stateManager:"+ this.GetType().Name);
                IemUtils.SLog(this.GetType().Name + ":ChangeState:oldstate:" +
                    currentState.ToString().Replace("Oxide.Plugins.", ""));

                currentState.Exit(this);
                previousState = currentState;
                currentState = newState;
                currentState.Enter(this);
                IemUtils.SLog(this.GetType().Name + ":ChangeState:newstate:" +
                    newState.ToString().Replace("Oxide.Plugins.", ""));
            }

            /// <summary>
            /// enter a substate, where you want to revert to the previous state
            /// </summary>
            /// <param name="newState"></param>
            public virtual void SubState(IStateMachine newState)
            {
                if ((currentState == null))
                    throw new Exception("currentState state is null");

                if ((newState == null))
                    throw new Exception("newState is null");


                IemUtils.SLog(this.GetType().Name + ":ChangeState:PrevState:" + currentState);
                previousState = currentState;
                currentState = newState;
                currentState.Enter(this);
                IemUtils.SLog(this.GetType().Name + ":ChangeState:newstate:" + newState);
            }

            public virtual void SubStateReturn()
            {
                if ((previousState == null))
                    throw new Exception("previousState is null");
                if ((currentState == null))
                    throw new Exception("currentState state is null");

                IemUtils.SLog(this.GetType().Name + ":ChangeState:PrevState:" + currentState);
                IemUtils.SLog(this.GetType().Name + ":ChangeState:Reverting:" + previousState);
                currentState.Exit(this);
                currentState = previousState;
            }

            public IStateMachine GetState()
            {
                if ((currentState == null))
                    throw new Exception("currentState state is null");

                return currentState;
            }

            public bool IsAny(params IStateMachine[] states)
            {
                if ((currentState == null))
                    throw new Exception("currentState state is null");

                foreach (var state in states)
                {
                    if (state.Equals(currentState))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public interface IStateMachine
        {
            void Enter(StateManager esm);
            void Execute(StateManager esm);
            void Exit(StateManager esm);
        }

        public abstract class StateBase<T> where T : StateBase<T>,
            IemStateManager.IStateMachine, new()
        {

            //add something for thread safety.... locks etc
            private static T _instance = new T();
            private Type statemanagertype;

            public static T Instance
            {
                get { return _instance; }

            }

            //TODO how to force this to the correct runtime type
            public virtual void Enter(StateManager psm)
            {
                // IemUtils.DLog("Entering the " + typeof(T));
            }

            public void Execute(StateManager psm)
            {
                // IemUtils.DLog("Executing the " + typeof(T));
            }

            public void Exit(StateManager psm)
            {
                // IemUtils.DLog("Exiting the " + typeof(T));
            }


        }

        #endregion

    }

}