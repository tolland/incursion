//Requires: IncursionEvents
//Requires: IncursionUI
//Requires: IncursionStateManager
//Requires: IemUtils
//Requires: IemScheduler
//Requires: IemGameTeams
//Requires: IemObjectPlacement

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Core.Configuration;
using UnityEngine;
using Random = System.Random;

namespace Oxide.Plugins
{
    [Info("Incursion Game - Embankment", "tolland", "0.1.0")]
    class IemEmbankment : RustPlugin
    {
        [PluginReference]
        IncursionEvents IncursionEvents;

        [PluginReference]
        IemUtils IemUtils;

        [PluginReference]
        IncursionStateManager IncursionStateManager;

        [PluginReference]
        IemGameTeams IemGameTeams;

        static IemEmbankment iemEmbankment = null;

        DynamicConfigFile incursionEventsConfig;
        private bool AutoStart = false;

        public IncursionEvents.EventStateManager esm;

        #region boiler plate

        void Init()
        {
            iemEmbankment = this;
            IemUtils.LogL("Embankment: Init complete");
        }


        void Loaded()
        {
            IemUtils.LogL("Embankment: Loading started");



            esm = IncursionEvents.esm;

            if (esm == null)
                throw new Exception("esm is null");

            //new instance of the Game State manager for this game
            teamGameStateManager = new EmbankStateManager(
                EmbankStateManager.GameStateCreated.Instance, "Embankment");

            //tell the event manager about this game
            esm.RegisterGameStateManager(teamGameStateManager);

            //load the game to be managed by the state manager
            teamGameStateManager.eg = new EmbankmentTeamEventGame(teamGameStateManager);
            teamGameStateManager.ChangeState(EmbankStateManager.GameEventLoaded.Instance);

            //tell the Esm to check whether to update the lobby 
            //IemUtils.DDLog ("esm state is " + esm.GetState ());
            esm.Update();

            IemUtils.LogL("Embankment: Loaded complete");
        }

        #endregion 

        #region game hooks

        void OnRunPlayerMetabolism(PlayerMetabolism m, BaseCombatEntity entity)
        {
            var player = entity.ToPlayer();
            if (player == null)
                return;
            IemUtils.SetMetabolismNoNutrition(player);
        }
        void OnPlayerRespawn(BasePlayer player)
        {
            Puts(player.transform.position.ToString());
            player.transform.position = FindSpawnPoint(player);
            //return false;
        }

        Vector3 FindSpawnPoint(BasePlayer player)
        {
            IncursionEventGame.EventPlayer eventPlayer
                = IncursionEventGame.EventPlayer.GetEventPlayer(player);
            Vector3 loc = eventPlayer.eventTeam.Location;
            Vector3 circpoint = IemUtils.GetRandomPointOnCircle(loc, 4f);

            return IemUtils.GetGroundY(circpoint);

        }

        #endregion

        #region eventgame


        public class EmbankmentTeamEventGame : IncursionEventGame.EventGame
        {
            public int PartitionedPeriodLength = 10;
            public int MainPhasePeriodLength = 10;
            public int SuddenDeathPeriodLength = 10;

            public EmbankmentTeamEventGame(IncursionEventGame.GameStateManager gamestatemanager)
                : base(gamestatemanager)
            {
                TeamGame = true;
                FixedNumberOfTeams = true;

                MinPlayers = 0;
                MinPlayersPerTeam = 0;

                TimedGame = true;
                TimeLimit = 1500;

                GameLobbyWait = 10;

                eventTeams.Add("team_1", new IncursionEventGame.EventTeam("team_1", "Blue Team",
new Vector3(96, 24, 124), "blue"));
                eventTeams.Add("team_2", new IncursionEventGame.EventTeam("team_2", "Red Team",
                    new Vector3(114, 22, 100), "red"));

                //support rulesGUI format??
                GameIntroBanner = new List<string> {
                    "<color=white>The game is embankment!</color>",
                    "<color=blue>Play will start in 10 seconds and last for 60 minutes max.</color>",
                    "<color=red>Your team has 20 minutes to build defenses and craft weapons</color>",
                    "<color=red>Then the wall will come down, and attack starts</color>",
                    "<color=yellow>After 30 minutes, game will turn to sudden death</color>",
                    "<color=yellow>All hits will be fatal for 10 minutes</color>",
                    "<color=green>If players remain after game is over, the team with the most health points of their living players wins</color>"
                };

            }

            public override bool StartGame()
            {
                //IemUtils.DLog ("calling startgame in team event game");
                iemEmbankment.rust.RunServerCommand("env.time", "12");
                gsm.ChangeState(EmbankStateManager.GameLobby.Instance);
                return true;
            }
        }


        #endregion 



        #region game state manager

        private EmbankStateManager teamGameStateManager;

        public class EmbankStateManager : IncursionEventGame.GameStateManager
        {
            public EmbankStateManager(IncursionStateManager.IStateMachine initialState,
                             string Gamename) : base(initialState, Gamename)
            {
                Name = Gamename;
            }

            EmbankmentTeamEventGame GetEventGame()
            {
                return (EmbankmentTeamEventGame)eg;

            }

            public override void ReinitializeGame()
            {
                //IemUtils.DLog("reinit game in IemGameTeams");
                eg = new EmbankmentTeamEventGame(this);
                ChangeState(GameEventLoaded.Instance);

            }

            public override void CancelGame()
            {
                //IemUtils.DLog("cancelled game in IemGameTeams");
                ChangeState(IemGameTeams.GameCancelled.Instance);

            }

            public class GameStateCreated : IncursionStateManager.StateBase<GameStateCreated>,
    IncursionStateManager.IStateMachine
            {
                public new void Enter(IncursionStateManager.StateManager gsm)
                {
                    iemEmbankment.Unsubscribe(nameof(OnRunPlayerMetabolism));
                    iemEmbankment.Unsubscribe(nameof(OnPlayerRespawn));
                }
            }

            /// <summary>
            /// so a Game event is loaded and teams and players are available
            /// </summary>
            public class GameEventLoaded : IncursionStateManager.StateBase<GameEventLoaded>,
                IncursionStateManager.IStateMachine
            {
                public new void Enter(IncursionStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    foreach (var VARIABLE in gsm.eg.eventTeams.Keys)
                    {
                        IemUtils.SLog("team Id:" + VARIABLE);
                    }


                }

                public new void
                    Execute(IncursionStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    if (gsm.eg.CanGameStart())
                        gsm.ChangeState(GameEventCanStart.Instance);
                    else
                        gsm.ChangeState(GameEventCannotStart.Instance);
                }
            }

            /// <summary>
            /// so a Game event is loaded and teams and players are available
            /// </summary>
            public class GameEventCanStart : IncursionStateManager.StateBase<GameEventCanStart>,
                IncursionStateManager.IStateMachine
            {
                private Timer warningTimer;
                //private static readonly Object obj = new Object();

                public new void Enter(IncursionStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    gsm.CreateGameBanner("game can start");
                }

                public new void Execute(IncursionStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    IemUtils.DLog("GameEventCanStart: Execute");

                    if (!gsm.eg.CanGameStart())
                    {
                        gsm.ChangeState(GameEventCannotStart.Instance);
                    }
                    else
                    {
                        gsm.CreateGameBanner("game can start");
                        // IemUtils.SLog("setting up timer");

                        if (warningTimer == null)
                        {
                            // IemUtils.SLog("is null");
                        }
                        else
                        {

                            if (warningTimer.Destroyed)
                            {
                                //   IemUtils.SLog("warningTimer.Destroyed");
                            }
                        }

                        if (warningTimer == null || warningTimer.Destroyed)
                        {
                            //IemUtils.SLog("in conditional");
                            warningTimer = iemEmbankment.timer.Once(5f, () =>
                            {
                                gsm.ChangeState(GameLobby.Instance);
                            });
                        }

                    }
                }

                public new void Exit(IncursionStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    IemUtils.SLog("before cancel timer");
                    warningTimer.Destroy();
                    IemUtils.SLog("after cancel timer");
                    gsm.CreateGameBanner("EXITING");
                }
            }

            /// <summary>
            /// so a Game event is loaded and teams and players are available
            /// </summary>
            public class GameEventCannotStart : IncursionStateManager.StateBase<GameEventCannotStart>,
                IncursionStateManager.IStateMachine
            {
                public new void Enter(IncursionStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    gsm.CreateGameBanner(gsm.eg.GetGameStartCriteria());
                }

                public new void Execute(IncursionStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;

                    if (gsm.eg.CanGameStart())
                    {
                        gsm.ChangeState(GameEventCanStart.Instance);
                    }
                    else
                    {
                        gsm.CreateGameBanner(gsm.eg.GetGameStartCriteria());
                    }
                }
            }



            private Timer walltimer;
            IemObjectPlacement.CopyPastePlacement placement;

            /// <summary>
            /// transitioning to this state is where players are moved to the field
            /// game has not yet started. A pre game message can be shown
            /// inventories can be reset
            /// however something must be done to prevent players attacking and building etc
            /// </summary>
            public class GameLobby : IncursionStateManager.StateBase<GameLobby>,
                IncursionStateManager.IStateMachine
            {
                private Timer warningTimer;


                public new void Enter(IncursionStateManager.StateManager sm)
                {

                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    gsm.placement = new IemObjectPlacement.CopyPastePlacement(
                        "partition1_1500_7777_3123");



                    iemEmbankment.esm.ChangeState(IncursionEvents.EventRunning.Instance);
                    iemEmbankment.Subscribe(nameof(OnRunPlayerMetabolism));

                    // don't handle spawn location until required
                    iemEmbankment.Subscribe(nameof(OnPlayerRespawn));

                    gsm.eg.MovePlayersToGame();

                    foreach (IncursionEventGame.EventPlayer eventPlayer in gsm.eg.gamePlayers.Values)
                    {
                        eventPlayer.psm.ChangeState(IncursionEventGame.PlayerInGame.Instance);

                        IncursionUI.ShowGameBanner(eventPlayer.player,
                            gsm.eg.GameIntroBanner);
                    }

                    gsm.CreateGameBanner("GAME LOBBY");
                    warningTimer = iemEmbankment.timer.Once(gsm.eg.GameLobbyWait, () =>
                    {
                        gsm.ChangeState(PartitionedPeriod.Instance);
                    });
                }

                public new void Exit(IncursionStateManager.StateManager esm)
                {
                    warningTimer.Destroy();

                    foreach (BasePlayer player in BasePlayer.activePlayerList)
                    {
                        IncursionEventGame.EventPlayer eventPlayer
                            = IncursionEventGame.EventPlayer.GetEventPlayer(player);
                        //eventPlayer.psm.eg = ((IncursionEvents.EventStateManager)esm).eg;
                        IncursionUI.HideGameBanner(player);
                    }
                    iemEmbankment.Unsubscribe(nameof(OnRunPlayerMetabolism));

                }
            }

            public class PartitionedPeriod : IncursionStateManager.StateBase<PartitionedPeriod>,
                IncursionStateManager.IStateMachine
            {

                private Timer gameTimer;

                private DateTime startTime = DateTime.UtcNow;
                TimeSpan breakDuration = TimeSpan.FromSeconds(15);

                public new void Enter(IncursionStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    iemEmbankment.Subscribe(nameof(OnRunPlayerMetabolism));
                    gsm.CreateGameBanner("You have " + gsm.GetEventGame().PartitionedPeriodLength +
                        " minutes to build/craft weapons");
                    
                        gameTimer = iemEmbankment.timer.Once(
                            gsm.GetEventGame().PartitionedPeriodLength, () =>
                        {
                            gsm.ChangeState(MainPhase.Instance);
                        });
                    
                }

                public new void Exit(IncursionStateManager.StateManager esm)
                {
                    gameTimer?.Destroy();

                    iemEmbankment.Unsubscribe(nameof(OnRunPlayerMetabolism));

                }
            }

            public class MainPhase : IncursionStateManager.StateBase<MainPhase>,
                IncursionStateManager.IStateMachine
            {

                private Timer warningTimer;
                private Timer finalWarningTimer;
                private Timer gameTimer;

                private DateTime startTime = DateTime.UtcNow;
                TimeSpan breakDuration = TimeSpan.FromSeconds(15);

                public new void Enter(IncursionStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;

                    gsm.placement.Remove();


                    iemEmbankment.Subscribe(nameof(OnRunPlayerMetabolism));
                    gsm.CreateGameBanner("WALL HAS COME DOWN - main phase");
                    if (gsm.eg.TimedGame)
                    {
                        gameTimer = iemEmbankment.timer.Once(gsm.GetEventGame().MainPhasePeriodLength, () =>
                        {
                            gsm.ChangeState(ExtraTime.Instance);
                        });
                    }
                }

                public new void Exit(IncursionStateManager.StateManager esm)
                {
                    warningTimer?.Destroy();
                    finalWarningTimer?.Destroy();
                    gameTimer?.Destroy();

                    iemEmbankment.Unsubscribe(nameof(OnRunPlayerMetabolism));

                }
            }


            public class ExtraTime : IncursionStateManager.StateBase<ExtraTime>,
                IncursionStateManager.IStateMachine
            {

                private Timer warningTimer;
                private Timer finalWarningTimer;
                private Timer gameTimer;

                private DateTime startTime = DateTime.UtcNow;
                TimeSpan breakDuration = TimeSpan.FromSeconds(15);

                public new void Enter(IncursionStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    iemEmbankment.Subscribe(nameof(OnRunPlayerMetabolism));
                    gsm.CreateGameBanner("SUDDEN DEATH");
                    if (gsm.eg.TimedGame)
                    {
                        warningTimer = iemEmbankment.timer.Once(gsm.eg.TimeLimit - 10, () =>
                        {
                            gsm.CreateGameBanner("Game ending in 10 seconds - warning");
                        });
                        finalWarningTimer = iemEmbankment.timer.Once(gsm.eg.TimeLimit - 5, () =>
                        {
                            gsm.CreateGameBanner("Game ending in 5 seconds - final warning");
                        });
                        
                        gameTimer = iemEmbankment.timer.Once(
                            gsm.GetEventGame().SuddenDeathPeriodLength, () =>
                        {
                            gsm.ChangeState(GameComplete.Instance);
                        });
                    }
                }

                public new void Exit(IncursionStateManager.StateManager esm)
                {
                    warningTimer?.Destroy();
                    finalWarningTimer?.Destroy();
                    gameTimer?.Destroy();

                    iemEmbankment.Unsubscribe(nameof(OnRunPlayerMetabolism));

                }
            }

            public class GameComplete : IncursionStateManager.StateBase<GameComplete>,
                IncursionStateManager.IStateMachine
            {
                private Timer warningTimer;

                public new void Enter(IncursionStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;

                    //tell the esm that the game is complete
                    //esm.ChangeState(Plugins.IncursionEvents.EventComplete.Instance);

                    iemEmbankment.Subscribe(nameof(OnRunPlayerMetabolism));
                    foreach (IncursionEventGame.EventPlayer eventPlayer in gsm.eg.gamePlayers.Values)
                    {
                        eventPlayer.psm.ChangeState(IncursionEventGame.PlayerInPostGame.Instance);

                    }

                    gsm.CreateGameBanner("Game ended");

                    ((IncursionEventGame.GameStateManager)gsm).eg.ShowGameResultUI();

                    warningTimer = iemEmbankment.timer.Once(10f, () =>
                    {
                        IemUtils.DLog("calling game complete on the event manager");
                        //iemGameTeams.esm.GameComplete();
                        gsm.ChangeState(GameUnloaded.Instance);
                        iemEmbankment.esm.ChangeState(IncursionEvents.EventComplete.Instance);
                    });

                }

                public new void Exit(IncursionStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    warningTimer?.Destroy();
                    gsm.CreateGameBanner("");
                    gsm.eg.RemoveGameResultUI();
                    iemEmbankment.Unsubscribe(nameof(OnRunPlayerMetabolism));
                }

            }

            public class GameUnloaded : IncursionStateManager.StateBase<GameUnloaded>,
    IncursionStateManager.IStateMachine
            {
                public new void Enter(IncursionStateManager.StateManager gsm)
                {
                    //@todo need to clean up game playing field here
                    IncursionEvents.esm.UnregisterGameStateManager(
                        iemEmbankment.teamGameStateManager);
                }
            }
        }

        #endregion

    }
}
