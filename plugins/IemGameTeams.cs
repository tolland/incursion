//Requires: IncursionEvents
//Requires: IncursionUI
//Requires: IncursionStateManager
//Requires: IemUtils
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oxide.Plugins
{

    [Info("Incursion Team Game", "tolland", "0.1.0")]
    class IemGameTeams : RustPlugin
    {
        [PluginReference]
        IncursionEvents IncursionEvents;

        [PluginReference]
        IncursionStateManager IncursionStateManager;

        static IemGameTeams iemGameTeams = null;

        void Init()
        {
            iemGameTeams = this;
        }

        public IncursionEvents.EventStateManager esm;

        void Loaded()
        {
            Unsubscribe(nameof(OnRunPlayerMetabolism));

        }

        void OnRunPlayerMetabolism(PlayerMetabolism m, BaseCombatEntity entity)
        {
            var player = entity.ToPlayer();
            if (player == null) return;
            IemUtils.SetMetabolismNoNutrition(player);
        }


        void OnServerInitialized()
        {

            IemUtils.DLog(" >>>>>>>starting Game");

            esm = IncursionEvents.esm;

            //new instance of the Game State manager
            TeamGameStateManager teamGameStateManager
                = new TeamGameStateManager(GameCreated.Instance, "Example Team Game");

            //load the game details
            teamGameStateManager.eg = new TeamEventGame(teamGameStateManager);

            //tell the event manager about this game
            esm.RegisterGameStateManager(teamGameStateManager);

            //tell the event manager to open the event lobby with this game loaded
            esm.ChangeState(IncursionEvents.EventLobbyOpen.Instance);

        }

        class TeamEventGame : IncursionEventGame.EventGame
        {


            public TeamEventGame(IncursionEventGame.GameStateManager gamestatemanager)
                : base(gamestatemanager)
            {
                TeamGame = true;
                FixedNumberOfTeams = true;

                MinPlayers = 1;
                MinPlayersPerTeam = 0;

                TimedGame = true;
                TimeLimit = 1500;

            }


            public override bool StartGame()
            {
                IemUtils.DLog("calling startgame in team event game");
                iemGameTeams.rust.RunServerCommand("env.time", "12");
                gsm.ChangeState(GameLobby.Instance);
                return true;
            }
        }

        public class TeamGameStateManager : IncursionEventGame.GameStateManager
        {

            public IncursionEventGame.EventPlayer eventPlayer;

            public TeamGameStateManager(IncursionStateManager.IStateMachine initialState,
                string Gamename) : base(initialState, Gamename)
            {
                IemUtils.DLog("creating a game state manager");
                Name = Gamename;
            }

            public override void ReinitializeGame()
            {
                eg = new TeamEventGame(this);
                ChangeState(GameCreated.Instance);
            }
        }

        public class GameCreated : IncursionStateManager.StateBase<GameCreated>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager gsm)
            {
                IemUtils.DLog("entry in GameCreated");
                IncursionUI.CreateAdminBanner3("state:" + gsm.GetState().ToString());
            }
        }



        public class GameLobby : IncursionStateManager.StateBase<GameLobby>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager sm)
            {
                IncursionEventGame.GameStateManager gsm = (IncursionEventGame.GameStateManager)sm;
                IemUtils.DLog("entry in GameLobby");
                IncursionUI.CreateAdminBanner3("state:" + gsm.GetState().ToString());
                iemGameTeams.Subscribe(nameof(OnRunPlayerMetabolism));
                ((IncursionEventGame.GameStateManager)gsm).eg.MovePlayersToGame();

                foreach (IncursionEventGame.EventPlayer eventPlayer in gsm.eg.gamePlayers.Values)
                {


                    IncursionUI.ShowGameBanner(eventPlayer.player,
                        ((IncursionEventGame.GameStateManager)gsm).eg.GameIntroBanner);
                }

                IncursionUI.CreateBanner("GAME CAN START");
                Timer warningTimer = iemGameTeams.timer.Once(2f, () =>
                {
                    gsm.ChangeState(GameStarted.Instance);
                });
            }

            public new void Exit(IncursionStateManager.StateManager esm)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    IncursionEventGame.EventPlayer eventPlayer
                        = IncursionEventGame.GetEventPlayer(player);
                    //eventPlayer.psm.eg = ((IncursionEvents.EventStateManager)esm).eg;
                    IncursionUI.HideGameBanner(player);
                }
                iemGameTeams.Unsubscribe(nameof(OnRunPlayerMetabolism));

            }
        }

        public class GameStarted : IncursionStateManager.StateBase<GameStarted>,
            IncursionStateManager.IStateMachine
        {
            private Timer warningTimer;
            private Timer finalWarningTimer;
            private Timer gameTimer;

            private DateTime startTime = DateTime.UtcNow;
            TimeSpan breakDuration = TimeSpan.FromSeconds(15);

            public new void Enter(IncursionStateManager.StateManager sm)
            {
                TeamGameStateManager gsm = (TeamGameStateManager)sm;
                IemUtils.DLog("entry in Game Started And Open");
                IncursionUI.CreateAdminBanner3("state:" + gsm.GetState().ToString());
                iemGameTeams.Subscribe(nameof(OnRunPlayerMetabolism));

                iemGameTeams.rust.BroadcastChat("Game Started And Open");
                if (gsm.eg.TimedGame)
                {
                    warningTimer = iemGameTeams.timer.Once(gsm.eg.TimeLimit - 10, () =>
                    {
                        //incursionEvents.EndGameWarning();
                        IncursionUI.CreateBanner("Game ending in 10 seconds - warning");
                    });
                    finalWarningTimer = iemGameTeams.timer.Once(gsm.eg.TimeLimit - 5, () =>
                    {
                        //incursionEvents.EndGameFinalWarning();
                        IncursionUI.CreateBanner("Game ending in 5 seconds - final warning");
                    });
                    gameTimer = iemGameTeams.timer.Once(gsm.eg.TimeLimit, () =>
                    {
                        gsm.ChangeState(GameComplete.Instance);
                        //incursionEvents.EndGame();
                    });
                }

            }

            public new void Exit(IncursionStateManager.StateManager esm)
            {

                iemGameTeams.Unsubscribe(nameof(OnRunPlayerMetabolism));

            }
        }

        public class GamePaused : IncursionStateManager.StateBase<GamePaused>,
                IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager sm)
            {
                iemGameTeams.Subscribe(nameof(OnRunPlayerMetabolism));
            }

            public new void Exit(IncursionStateManager.StateManager esm)
            {

                iemGameTeams.Unsubscribe(nameof(OnRunPlayerMetabolism));

            }


        }

        public class GameComplete : IncursionStateManager.StateBase<GameComplete>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager gsm)
            {
                iemGameTeams.Subscribe(nameof(OnRunPlayerMetabolism));
                IncursionUI.CreateAdminBanner3("state:" + gsm.GetState().ToString());
                IemUtils.DLog("entering in GameComplete");
                IncursionUI.CreateBanner("Game ended");
                ((IncursionEventGame.GameStateManager)gsm).eg.ShowGameResultUI();
                Timer warningTimer = iemGameTeams.timer.Once(10f, () =>
                {
                    IemUtils.DLog("calling game complete on the event manager");
                    iemGameTeams.esm.GameComplete();
                });

            }

            public new void Exit(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("exiting in GameComplete");
                ((IncursionEventGame.GameStateManager)esm).eg.RemoveGameResultUI();
                iemGameTeams.Unsubscribe(nameof(OnRunPlayerMetabolism));
            }

        }

        public class GameUnloaded : IncursionStateManager.StateBase<GameUnloaded>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager gsm)
            {
                IncursionUI.CreateAdminBanner3("state:" + gsm.GetState().ToString());
                IemUtils.DLog("entering Game unloaded");
                //need to clean up game playing field here
            }

            public new void Exit(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("exiting in GameUnloaded");

            }
        }



    }
}
