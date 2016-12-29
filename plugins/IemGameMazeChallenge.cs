//Requires: IemGameBase
//Requires: IemStateManager
//Requires: IemObjectPlacement
//Requires: IemUI
//Requires: IemUtils
using System;
using System.Collections.Generic;
using System.Text;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;
 
namespace Oxide.Plugins
{
    [Info("Incursion Maze Challenge", "tolland", "0.1.0")]
    class IemGameMazeChallenge : RustPlugin
    {

        #region header 

        [PluginReference]
        IemUtils IemUtils;

        [PluginReference]
        IemUI IemUI;

        [PluginReference]
        Plugin Kits;

        [PluginReference]
        IemObjectPlacement IemObjectPlacement;

        [PluginReference]
        IemGameBase IemGameBase;

        static IemGameMazeChallenge me;

        static MCGameManager gm;
        static Dictionary<string, IemGameMazeChallengeGame> games = new Dictionary<string, IemGameMazeChallengeGame>();

        #endregion

        #region boiler plate

        void Init()
        {
            IemUtils.LogL("IemGameTargetPractice: Init started");
            me = this;
            //LoadConfigValues();
            IemUtils.LogL("IemGameTargetPractice: Init complete");
        }

        protected override void LoadDefaultConfig() => PrintWarning("New configuration file created.");

        void Loaded()
        {
            //Unsubscribe(nameof(OnEntityTakeDamage)); 
            IemUtils.LogL("IemGameTargetPractice: Loaded started");
            gm = new MCGameManager();
            IemGameBase.RegisterGameManager(gm);
            IemUtils.LogL("IemGameTargetPractice: Loaded complete");
        }

        void Unload()
        {
            //IemUtils.DDLog("IemGameTargetPractice: in unload");
            //IemUtils.LogL("IemGameTargetPractice: unloaded started");
            IemGameBase.UnregisterGameManager(gm);
            //IemUtils.LogL("IemGameTargetPractice: unloaded complete");
        }

        void OnServerInitialized()
        {
            //IemUtils.LogL("IemGameTargetPractice: OnServerInitialized started");
            //IemUtils.LogL("IemGameTargetPractice: OnServerInitialized complete");
        }

        public delegate void EnteredZone(string ZoneID, BasePlayer player);
        private static EnteredZone PlayerEnteredZone = delegate { };

        void OnEnterZone(string ZoneID, BasePlayer player)
        {                        //is this a team zone?
            PlayerEnteredZone(ZoneID, player);
        }

        #endregion

        #region game manager

        public class MCGameManager : IemGameBase.GameManager
        {
            public MCGameManager() : base()
            {
                Enabled = true;
                Mode = "Solo";
                Name = "Maze v1";
                TileImgUrl = "http://www.limepepper.co.uk/images/maze-44211617.png";
            }


            public override IemGameBase.IemGame CreateGame(BasePlayer player,
                string level = null)
            {
                me.Puts("in the tp game manager, creating new game");
                var newGame = new IemGameMazeChallengeGame(player);
                IemGameMazeChallenge.games.Add(newGame.GetGuid().ToString(), newGame);
                newGame.StartGame();
                return newGame;

            }
        }

        #endregion region

        #region IemGameMazeChallengeGame

        public class
        IemMazeChallengePlayer : IemGameBase.IemPlayer
        {
            public IemMazeChallengePlayer(BasePlayer player,
                IemGameBase.IemSoloGame game) : base(player)
            {

                me.IemUtils?.SaveInventory(player, game.GetGuid());
            }
        }

        public class IemGameMazeChallengeGame : IemGameBase.IemSoloGame
        {
            public MazeChallengeStateManager gsm;
            public float GameLobbyWait = 12;

            public List<IemGameBase.GameLevel> gamelevels;

            //pregame level is -1
            public int level = -1;

            public Timer walltimer;

            public List<IemObjectPlacement.CopyPastePlacement> mazes = new List<IemObjectPlacement.CopyPastePlacement>();
            public Vector3 startLoc;
            public Vector3 endLoc;


            public IemGameMazeChallengeGame(BasePlayer newPlayer) : base(newPlayer)
            {
                Name = "Maze Challenge";
                OnlyOneAtATime = true;
                Mode = "Solo";

                gamelevels = new List<IemGameBase.GameLevel>(
                        new IemGameBase.GameLevel[] {
                        new IemGameBase.GameLevel {Game=this},
                        new IemGameBase.GameLevel {Game=this},
                    });

                gsm = new MazeChallengeStateManager(
                    MazeChallengeStateManager.Created.Instance, this);

                gsm?.ChangeState(MazeChallengeStateManager.Setup.Instance);

                var newIemPlayer =
                    new IemMazeChallengePlayer(player, this);
                this.Players.Add(player.UserIDString, newIemPlayer);

                iemPlayer = newIemPlayer;
            }

            public override bool StartGame()
            {
                IemUtils.GLog("calling StartGame in IemGameMazeChallengeGame");
                if (gsm.IsAny(MazeChallengeStateManager.Setup.Instance))
                {
                    base.StartGame();
                    gsm.ChangeState(MazeChallengeStateManager.GameLobby.Instance);
                }
                else if (gsm.IsAny(MazeChallengeStateManager.GameLobby.Instance,
                    MazeChallengeStateManager.GameRunning.Instance
                    ))
                {
                    IemUtils.GLog("gsm already created");
                }
                return true;
            }

            public override bool CancelGame()
            {
                me.Puts("calling cancel game in maze");
                //if the game is over, but the player is on the results screen
                //just cleaup map
                if (gsm.IsAny(MazeChallengeStateManager.GameComplete.Instance))
                {
                    gsm?.ChangeState(MazeChallengeStateManager.CleanUp.Instance);
                }
                else
                {
                    gsm?.ChangeState(MazeChallengeStateManager.GameCancelled.Instance);
                    base.CancelGame();
                }
                return true;
            }

            public void PlayerImmortal(BaseCombatEntity entity, HitInfo hitInfo)
            {
                if (entity as BasePlayer == null || hitInfo == null) return;

                if (entity.ToPlayer() != null)
                {
                    IemUtils.GLog("scaling player damage to zero");
                    BasePlayer player = entity.ToPlayer();
                }
            }

            public void PlayerEnteredEndZone(string ZoneID, BasePlayer player)
            {
                if (ZoneID.StartsWith("zone_mc1_endpoint"))
                {
                    me.Puts("player entered end zone " + ZoneID);

                    // is this the final level?
                    if (level == gamelevels.Count - 1)
                    {
                        gsm?.ChangeState(MazeChallengeStateManager.GameComplete.Instance);
                    }
                    else
                    {
                        IemUI.CreateFadeoutBanner(player, "Level Complete");
                        gsm?.ChangeState(MazeChallengeStateManager.GameRunning.Instance);
                    }
                }
            }

            public void backToTheMap()
            {
                IemUI.CreateFadeoutBanner(player, "back to map");
                gsm?.ChangeState(MazeChallengeStateManager.CleanUp.Instance);
            }
        }

        #endregion

        #region MazeChallengeStateManager

        public class MazeChallengeStateManager : IemStateManager.StateManager
        {

            private IemGameMazeChallengeGame eg;

            Vector3 location = me.IemUtils.NextFreeLocation();

            public MazeChallengeStateManager(IemStateManager.IStateMachine initialState,
                IemGameMazeChallengeGame newEg) : base(initialState)
            {
                eg = newEg;
            }

            //private Timer walltimer;

            public class Created : IemStateManager.StateBase<Created>,
                    IemStateManager.IStateMachine
            {
                public new void Enter(IemStateManager.StateManager sm)
                { }
            }


            public class Setup : IemStateManager.StateBase<Setup>,
                IemStateManager.IStateMachine
            {
                public new void Enter(IemStateManager.StateManager sm)
                {
                    MazeChallengeStateManager gsm = (MazeChallengeStateManager)sm;


                }
            }

            /// <summary> 
            /// transitioning to this state is where players are moved to the field
            /// game has not yet started. A pre game message can be shown
            /// inventories can be reset
            /// however something must be done to prevent players attacking and building etc
            /// </summary>
            public class GameLobby : IemStateManager.StateBase<GameLobby>,
                IemStateManager.IStateMachine
            {
                public new void Enter(IemStateManager.StateManager sm)
                {
                    MazeChallengeStateManager gsm = (MazeChallengeStateManager)sm;

                    EntitiesTakingDamage += gsm.eg.PlayerImmortal;

                    gsm.eg.player.EndSleeping();

                    IemUtils.SetMetabolismValues(gsm.eg.player);
                    IemUtils.ClearInventory(gsm.eg.player);

                    me.Kits?.Call("GiveKit", gsm.eg.player, "maze_v1");
                    gsm.eg.player.inventory.SendSnapshot();

                    IemUI.CreateGameBanner(gsm.eg.player, "GAME LOBBY");
                    IemUI.ShowIntroOverlay(gsm.eg.player,
                        $"Prepare for the maze.\n find your way out \n",
                        "maze start_go " + gsm.eg.GetGuid()
                        );

                    IemUtils.PlaySound(gsm.eg.player);

                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    MazeChallengeStateManager gsm = (MazeChallengeStateManager)sm;
                    gsm.eg.walltimer?.Destroy();
                    EntitiesTakingDamage -= gsm.eg.PlayerImmortal;
                    CuiHelper.DestroyUi(gsm.eg.player, "ShowIntroOverlay");

                }
            }

            public class GameRunning : IemStateManager.StateBase<GameRunning>,
                IemStateManager.IStateMachine
            {
                public new void Enter(IemStateManager.StateManager sm)
                {
                    MazeChallengeStateManager gsm = (MazeChallengeStateManager)sm;
                    gsm.eg.level++;

                    // log the score
                    PlayerEnteredZone += gsm.eg.PlayerEnteredEndZone;

                    Vector3 location = me.IemUtils.NextFreeLocation();

                    if (location == null)
                    {
                        me.Puts("location is null");
                    }

                    gsm.eg.mazes.Add(
                         new IemObjectPlacement.CopyPastePlacement(
                       "maze_v1", location));

                    WaterCatcher obj = IemUtils.FindComponentNearestToLocation<WaterCatcher>(location, 50);

                    if (obj == null)
                    {
                        me.Puts("obj is null");
                    }
                    else
                    {
                        me.Puts("start is located at " + obj.transform.position);
                        gsm.eg.startLoc = obj.transform.position;

                        IemUtils.CreateSphere(gsm.eg.startLoc, fade: true);

                        obj.Kill(BaseNetworkable.DestroyMode.None);
                    }
                    LiquidContainer lc = IemUtils.FindComponentNearestToLocation<LiquidContainer>(
                        location, 50, "waterbarrel");

                    if (lc == null)
                    {
                        me.Puts("lc is null");
                    }
                    else
                    {
                        me.Puts("end is located at " + lc.transform.position);
                        gsm.eg.endLoc = lc.transform.position + new Vector3(0, 1.9f, 0);
                        lc.Kill(BaseNetworkable.DestroyMode.None);

                        gsm.eg.AddGameZone(
                            "mc1_endpoint_" + (gsm.eg.level + 1) + "_" + gsm.eg.GetGuid(),
                            gsm.eg.endLoc, 2);

                    }
                    IemUtils.MovePlayerToTeamLocation(gsm.eg.player, gsm.eg.startLoc);

                    IemUtils.PlaySound(gsm.eg.player);
                    IemUI.CreateGameBanner(gsm.eg.player, "Level " + (gsm.eg.level + 1));

                    ResearchTable researchtable = IemUtils.FindComponentNearestToLocation<ResearchTable>(
                            location, 50);
                    Vector3 centrepoint = researchtable.transform.position;
                    researchtable.Kill(BaseNetworkable.DestroyMode.None);

                    //me.timer.Once(3f, () =>
                    //{
                    gsm.eg.gamelevels[gsm.eg.level].returnZone =
                    new IemUtils.ReturnZone(location, gsm.eg.startLoc, gsm.eg.player);
                    //});

                    // it looks like there are some lag issues relating to repawning
                    // move this to a zone check, when the player leaves the start zone
                    gsm.eg.gamelevels[gsm.eg.level].Start();
                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    MazeChallengeStateManager gsm = (MazeChallengeStateManager)sm;

                    gsm.eg.gamelevels[gsm.eg.level].returnZone.Destroy();
                    //if the game has a confirm dialog for end of level, this will need
                    // to be moved to there, as otherwise it will count the dialog waiting
                    // for player to respond time
                    gsm.eg.gamelevels[gsm.eg.level].End();

                    IemUI.CreateGameBanner(gsm.eg.player, "");
                    EntitiesTakingDamage -= gsm.eg.PlayerImmortal;
                    PlayerEnteredZone -= gsm.eg.PlayerEnteredEndZone;

                    me.Puts("Game Level count is " + gsm.eg.gamelevels.Count);
                    me.Puts("Game Level is " + (gsm.eg.level + 1));

                    gsm.eg.RemoveGameZone(
                            "mc1_endpoint_" + (gsm.eg.level + 1) + "_" + gsm.eg.GetGuid());


                }
            }

            /// <summary>
            /// GameComplete is effectively a post game lobby for the players
            /// </summary>
            public class GameComplete : IemStateManager.StateBase<GameComplete>,
                IemStateManager.IStateMachine
            {

                public new void Enter(IemStateManager.StateManager sm)
                {
                    MazeChallengeStateManager gsm = (MazeChallengeStateManager)sm;
                    // resultsTimer = IemUI.ShowResultsUiForSolo(gsm.eg.Players.Select(d => d.Value).ToList(), gsm.eg, 8);
                    IemUI.CreateGameBanner(gsm.eg.player, "Game is complete! - score was "
                        + gsm.eg.totalTime);

                    gsm.eg.EndGame();

                    IemUI.Confirm(gsm.eg.player, "Game completed!\nreturn to the map",
                            "Back",
                        gsm.eg.backToTheMap);

                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    MazeChallengeStateManager gsm = (MazeChallengeStateManager)sm;

                    CuiHelper.DestroyUi(gsm.eg.player, "CreateFadeoutBanner");
                    foreach (IemGameBase.IemPlayer iemPlayer in gsm.eg.Players.Values)
                    {
                        BasePlayer player = IemUtils.FindPlayerByID(iemPlayer.PlayerId);
                        IemUI.CreateGameBanner(player, "");
                    }
                    gsm.eg.MarkComplete();
                }
            }

            public class GameCancelled : IemStateManager.StateBase<GameCancelled>,
                IemStateManager.IStateMachine
            {

                public new void Enter(IemStateManager.StateManager sm)
                {
                    MazeChallengeStateManager gsm = (MazeChallengeStateManager)sm;
                    gsm?.ChangeState(MazeChallengeStateManager.CleanUp.Instance);
                }
            }


            public class CleanUp : IemStateManager.StateBase<CleanUp>,
                IemStateManager.IStateMachine
            {
                public new void Enter(IemStateManager.StateManager sm)
                {
                    MazeChallengeStateManager gsm = (MazeChallengeStateManager)sm;

                    me.Puts("moving player to " + gsm.eg.iemPlayer.PreviousLocation);

                    IemUtils.MovePlayerToTeamLocation(gsm.eg.player,
                        gsm.eg.iemPlayer.PreviousLocation);

                    me.IemUtils.RestoreInventory(gsm.eg.player, gsm.eg.GetGuid());

                    foreach (var maze in gsm.eg.mazes)
                    {
                        maze.Remove();
                    }
                }
            }
        }

        #endregion

        #region static methods

        #endregion

        #region Oxide Hooks for game

        //public delegate object EntitiesCanBeWounded(BasePlayer player, HitInfo hitInfo);
        //private static EntitiesCanBeWounded EntitiesBeingWounded = NullFunc;

        //private object CanBeWounded(BasePlayer player, HitInfo hitInfo)
        //{
        //    if (player == null || hitInfo == null) return true;
        //    return EntitiesBeingWounded(player, hitInfo);
        //}

        public delegate void RunningPlayerMetabolism(PlayerMetabolism m, BaseCombatEntity entity);

        private static RunningPlayerMetabolism RunPlayerMetabolism = delegate { };

        void OnRunPlayerMetabolism(PlayerMetabolism m, BaseCombatEntity entity)
        {

        }

        public delegate void EntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo);

        private static EntityTakeDamage EntitiesTakingDamage = delegate { };

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            EntitiesTakingDamage(entity, hitInfo);
        }

        #endregion


        #region Commands

        [ConsoleCommand("maze")]
        private void ccmdZone(ConsoleSystem.Arg arg)
        {
            switch (arg.Args[0].ToLower())
            {
                case "start_go":

                    string guid = arg.Args[1].ToLower();
                    IemGameMazeChallenge.games[guid].gsm.
                    ChangeState(MazeChallengeStateManager.GameRunning.Instance);
                    break;
                case "look":
                    //LiquidContainer obj = IemUtils.FindComponentNearestToLocation<LiquidContainer>(location, 50);
                    LiquidContainer obj = IemUtils.FindComponentNearestToLocation<LiquidContainer>(
                        arg.Player().transform.position, 50, "waterbarrel");

                    if (obj == null)
                    {
                        me.Puts("obj is null");
                    }
                    else
                    {
                        IemUtils.DestroyAllSpheres();
                        me.Puts("start is located at " + obj.transform.position);
                        IemUtils.CreateSphere(obj.transform.position, 1.2f);
                        //   gsm.startLoc = obj.transform.position;
                        //   obj.Kill(BaseNetworkable.DestroyMode.None);
                    }

                    break;
                case "find":
                    var foundentities = IemUtils.FindComponentsNearToLocation<LiquidContainer>(
                        arg.Player().transform.position, 50, "waterbarrel");

                    foreach (var entity in foundentities)
                    {
                        me.Puts("type " + entity.GetType());
                        me.Puts("LookupShortPrefabNameWithoutExtension " + entity.LookupPrefab().name);
                    }

                    break;
            }

        }
        #endregion

        #region Configuration Data
        // Do not modify these values because this will not change anything, the values listed below are only used to create
        // the initial configuration file. If you wish changes to the configuration file you should edit 'GatherManager.json'
        // which is located in your server's config folder: <drive>:\...\server\<your_server_identity>\oxide\config\

        private bool configChanged;

        // Plugin options
        private static readonly Dictionary<string, IemGameBase.DifficultyMode> DefaultDifficultyModes
            = new Dictionary<string, IemGameBase.DifficultyMode>() {
                { "Easy", new IemGameBase.DifficultyMode() {
                    Name ="Easy",
                    Description ="Easy going",
                    GameLevelDefinitions = new List<IemGameBase.GameLevelDefinition>(
                        new IemGameBase.GameLevelDefinition[] {
                new IemGameBase.GameLevelDefinition {Timer=20},
                new IemGameBase.GameLevelDefinition {Timer=20},
                new IemGameBase.GameLevelDefinition {Timer=18},
                       }  )
            } },{ "Harder", new IemGameBase.DifficultyMode() {
                    Name ="Harder",
                    Description ="",
                    GameLevelDefinitions = new List<IemGameBase.GameLevelDefinition>(
                        new IemGameBase.GameLevelDefinition[] {
                new IemGameBase.GameLevelDefinition {Timer=20},
                new IemGameBase.GameLevelDefinition {Timer=20},
                new IemGameBase.GameLevelDefinition {Timer=18},
                       }  )
            } },{ "Impossible", new IemGameBase.DifficultyMode() {
                    Name ="Impossible",
                    Description ="",
                    GameLevelDefinitions = new List<IemGameBase.GameLevelDefinition>(
                        new IemGameBase.GameLevelDefinition[] {
                new IemGameBase.GameLevelDefinition {Timer=20},
                new IemGameBase.GameLevelDefinition {Timer=20},
                new IemGameBase.GameLevelDefinition {Timer=18},
                       }  )
            } } };

        public Dictionary<string, IemGameBase.DifficultyMode> DifficultyModes { get; private set; }


        #endregion

        #region config

        private void LoadConfigValues()
        {

            // Plugin options
            var difficultyModes = GetConfigValue("Options", "DifficultyModes",
                DefaultDifficultyModes);

            DifficultyModes = new Dictionary<string, IemGameBase.DifficultyMode>();
            foreach (var entry in difficultyModes)
            {
                DifficultyModes.Add(entry.Key, entry.Value);
            }


            if (!configChanged) return;
            PrintWarning("Configuration file updated.");
            SaveConfig();
        }

        private T GetConfigValue<T>(string category, string setting, T defaultValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[category] = data;
                configChanged = true;
            }
            if (data.TryGetValue(setting, out value)) return (T)Convert.ChangeType(value, typeof(T));
            value = defaultValue;
            data[setting] = value;
            configChanged = true;
            return (T)Convert.ChangeType(value, typeof(T));
        }

        private void SetConfigValue<T>(string category, string setting, T newValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data != null && data.TryGetValue(setting, out value))
            {
                value = newValue;
                data[setting] = value;
                configChanged = true;
            }
            SaveConfig();
        }

        #endregion

    }
}

