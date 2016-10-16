//Requires: IemGameBase
//Requires: IemStateManager
//Requires: IemObjectPlacement
//Requires: IemUI
using System;
using System.Collections.Generic;
using System.Text;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Incursion Target Practice", "tolland", "0.1.0")]
    class IemGameTargetPractice : RustPlugin
    {

        #region header

        [PluginReference]
        IemUtils IemUtils;

        [PluginReference]
        IemUI IemUI;

        [PluginReference]
        Plugin Kits;

        [PluginReference]
        Plugin ScreenTimer;

        [PluginReference]
        IemObjectPlacement IemObjectPlacement;

        [PluginReference]
        IemGameBase IemGameBase;

        static IemGameTargetPractice me;

        static TPGameManager gm;
        static Dictionary<string, IemGameTargetPracticeGame> games = new Dictionary<string, IemGameTargetPracticeGame>();

        int multiplier = 100;
        int multicount = 1;

        #endregion

        #region boiler plate

        void Init()
        {
            me = this;
            //IemUtils.LogL("IemGameTargetPractice: Init complete");
        }

        void Loaded()
        {
            Unsubscribe(nameof(OnRunPlayerMetabolism));
            //Unsubscribe(nameof(OnEntityTakeDamage)); 
            gm = new TPGameManager();
            IemGameBase.RegisterGameManager(gm);
            //IemUtils.LogL("IemGameTargetPractice: Loaded complete");
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

        void LoadDefaultConfig()
        {
            Config.Clear();
            Config["Enabled"] = false;
            Config.Save();
        }

        void OnGameEnded(IemGameBase.IemGame endedGame)
        {
            //  IemUtils.GLog("Hook called to indicate game ended");
        }

        #endregion

        #region game manager

        public class TPGameManager : IemGameBase.GameManager
        {
            public TPGameManager() : base()
            {
                Enabled = true;
                Mode = "Solo";
                Name = "Target Practice";
                TileImgUrl = "http://www.limepepper.co.uk/images/PNG_Example.png";
            }

            public override IemGameBase.IemGame SendPlayerToGameManager(BasePlayer player)
            {
                foreach(var mygame in games)
                {
                    me.Puts("game is " + mygame.Value.Name);
                    me.Puts("- state is " + mygame.Value.CurrentState);
                }
                if (games.ContainsKey(player.UserIDString)) 
                {
                    me.Puts("in the tp game manager, found existing game for player");
                    if (games[player.UserIDString].CurrentState == IemUtils.State.Complete ||
                        games[player.UserIDString].CurrentState == IemUtils.State.Cancelled)
                    {
                        var newGame = new IemGameTargetPracticeGame(player);
                        games[player.UserIDString] = newGame;
                    }
                } 
                else
                {
                    me.Puts("in the tp game manager, creating new game");
                    var newGame = new IemGameTargetPracticeGame(player);
                    games[player.UserIDString] = newGame;
                    games[player.UserIDString].StartGame();
                }
                
                return games[player.UserIDString];
            }
        }

        #endregion region

        #region IemGameTargetPracticeGame

        public class
        IemTargetPracticePlayer : IemGameBase.IemPlayer
        {
            public IemTargetPracticePlayer(BasePlayer player) : base(player)
            {
            }
        }

        public class IemGameTargetPracticeGame : IemGameBase.IemSoloGame
        {
            public TargetPracticeStateManager gsm;
            public float GameLobbyWait = 12;
            public int MainPhaseWait = 20;
            public BasePlayer player;
            public IemGameBase.IemPlayer iemPlayer;

            public IemGameTargetPracticeGame(BasePlayer newPlayer)
            {
                player = newPlayer;
                Name = "Target_Practice";
                OnlyOneAtATime = true;
                Mode = "Team";
                gsm = new TargetPracticeStateManager(
                    TargetPracticeStateManager.Created.Instance, this);

                var newIemPlayer =
                    new IemTargetPracticePlayer(player);
                this.Players.Add(player.UserIDString, newIemPlayer);
                iemPlayer = newIemPlayer;
            }

            public override bool StartGame()
            {
                IemUtils.GLog("calling StartGame in IemGameTargetPracticeGame");
                if (gsm.IsAny(TargetPracticeStateManager.Created.Instance))
                {
                    base.StartGame();
                    gsm.ChangeState(TargetPracticeStateManager.GameLobby.Instance);
                }
                else if (gsm.IsAny(TargetPracticeStateManager.GameLobby.Instance,
                    TargetPracticeStateManager.GameRunning.Instance
                    ))
                {
                    IemUtils.GLog("gsm already created");
                }
                return true;
            }

            public override bool EndGame()
            {
                IemUtils.GLog("calling EndGame in IemGameTargetPracticeGame");

                base.EndGame();
                return true;
            }

            public override bool CancelGame()
            {

                IemUtils.DDLog("listing in CancelGame");
                //((IemGameTargetPracticeGame)game).gsm.partition?.List();

                IemUtils.GLog("calling CancelGame in IemGameTargetPracticeGame");
                gsm?.ChangeState(TargetPracticeStateManager.GameCancelled.Instance);

                base.CancelGame();
                return true;
            }

            public bool MovePlayerToTeamLocation(BasePlayer player, Vector3 location)
            {

                IemUtils.GLog("moving players to game location: " + location);

                if (player.IsSleeping())
                {
                    player.EndSleeping();
                }
                if (player.IsWounded())
                {
                    IemUtils.SetMetabolismValues(player);
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, false);
                    player.CancelInvoke("WoundingEnd");
                }

                if (!IemUtils.CheckPointNearToLocation(player.transform.position, location, 2))
                    //IemUtils.MovePlayerTo(player, location);
                    IemUtils.TeleportPlayerPosition(player, location); 

                return true;
            }

            public void ScorePlayerHit(BaseCombatEntity entity, HitInfo hitinfo)
            {
                try
                {
                    if (entity is ReactiveTarget && hitinfo.Initiator is BasePlayer)
                    {

                        var target = (ReactiveTarget)entity;
                        var attacker = (BasePlayer)hitinfo.Initiator;
                        if (entity != null && attacker != null)
                        {
                            if (hitinfo.HitBone == StringPool.Get("target_collider_bullseye"))
                            {
                            }
                            if (target.IsKnockedDown())
                            {
                                target.CancelInvoke("ResetTarget");
                                target.health = target.MaxHealth();
                                target.SendNetworkUpdate();
                                me.timer.Once(1, () => target.SetFlag(BaseEntity.Flags.On, true));

                                //eventPlayer.Score += 1;
                                IemUtils.GLog("scoring knockdown");
                                iemPlayer.Score += 1;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    me.Puts("exception");
                }
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
        }

        #endregion


        #region TargetPracticeStateManager

        public class TargetPracticeStateManager : IemStateManager.StateManager
        {

            public IemObjectPlacement.CopyPastePlacement partition;
            private IemGameTargetPracticeGame eg;

            public TargetPracticeStateManager(IemStateManager.IStateMachine initialState,
                IemGameTargetPracticeGame newEg) : base(initialState)
            {

                eg = newEg;
            }

            private Timer walltimer;

            public class Created : IemStateManager.StateBase<Created>,
                IemStateManager.IStateMachine
            {
                public new void Enter(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;

                    me.multicount += 1;
                    gsm.partition = new IemObjectPlacement.CopyPastePlacement(
                        "shootinggallery1", new Vector3(-120 + (me.multicount * me.multiplier),
                        136, 266 + (me.multicount * me.multiplier)));

                    IemUtils.GLog("partition2_1500_7777_3123 " + gsm.partition);
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
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;

                    IemUtils.DLog("in the game lobby");

                    EntitiesTakingDamage += gsm.eg.PlayerImmortal;

                    gsm.eg.MovePlayerToTeamLocation(gsm.eg.player,
                        new Vector3(-120 + (me.multicount * me.multiplier), 136, 266 + (me.multicount * me.multiplier)));


                    gsm.eg.player.EndSleeping();

                    IemUtils.SetMetabolismValues(gsm.eg.player);
                    IemUtils.ClearInventory(gsm.eg.player);

                    IemUI.CreateGameBanner(gsm.eg.player, "GAME LOBBY");
                    IemUI.ShowIntroOverlay(gsm.eg.player,
                        $"Weclome to target practice\n" +
                        $"Shoot as many targets as possible.\n");

                    IemUtils.PlaySound(gsm.eg.player);


                    IemUI.ShowGameTimer(gsm.eg.player, gsm.eg.GameLobbyWait - 3, "starting in: ");

                    gsm.walltimer = me.timer.Once(gsm.eg.GameLobbyWait - 3, () =>
                    {
                        CuiHelper.DestroyUi(gsm.eg.player, "ShowIntroOverlay");
                        gsm?.ChangeState(TargetPracticeStateManager.GameRunning.Instance);
                    });
                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;
                    gsm.walltimer?.Destroy();
                    EntitiesTakingDamage -= gsm.eg.PlayerImmortal;

                }
            }

            public class GameRunning : IemStateManager.StateBase<GameRunning>,
                IemStateManager.IStateMachine
            {
                public new void Enter(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;

                    // this only needs to be set once for the plugin
                    //me.Subscribe(nameof(OnEntityTakeDamage));

                    // log the score
                    EntitiesTakingDamage += gsm.eg.ScorePlayerHit;

                    me.Kits?.Call("GiveKit", gsm.eg.player, "autokit");
                    gsm.eg.player.inventory.SendSnapshot();
                    IemUtils.PlaySound(gsm.eg.player);
                    IemUI.CreateGameBanner(gsm.eg.player, "Game is running!");
                    IemUtils.DLog("game is running");

                    IemUI.ShowGameTimer(gsm.eg.player, gsm.eg.MainPhaseWait - 3);
                    gsm.walltimer = me.timer.Once(gsm.eg.MainPhaseWait - 3, () =>
                    {
                        //    CuiHelper.DestroyUi(gsm.eg.player, "ShowIntroOverlay");
                        gsm?.ChangeState(TargetPracticeStateManager.GameComplete.Instance);
                    });
                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;
                    gsm.walltimer?.Destroy();
                    EntitiesTakingDamage -= gsm.eg.PlayerImmortal;
                    EntitiesTakingDamage -= gsm.eg.ScorePlayerHit;
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
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;
                    // resultsTimer = IemUI.ShowResultsUiForSolo(gsm.eg.Players.Select(d => d.Value).ToList(), gsm.eg, 8);
                    IemUI.CreateGameBanner(gsm.eg.player, "Game is complete! - score was "
                        + gsm.eg.iemPlayer.Score);


                    IemUI.ShowGameTimer(gsm.eg.player, 10f, "back to map in: ");

                    gsm.walltimer = me.timer.Once(10f, () =>
                    {
                        IemUtils.GLog("calling game complete on the event manager");
                        gsm.eg.EndGame();
                        gsm.eg.MovePlayerToTeamLocation(gsm.eg.player,
                            gsm.eg.iemPlayer.previousLocation);
                        gsm.partition.Remove();
                        

                    });
                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;
                }
            }

            public class GameCancelled : IemStateManager.StateBase<GameCancelled>,
                IemStateManager.IStateMachine
            {

                public new void Enter(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;

                    me.Unsubscribe(nameof(OnRunPlayerMetabolism));
                }
            }

        }

        #endregion

        #region static methods

        #endregion

        #region Oxide Hooks for game

        void OnRunPlayerMetabolism(PlayerMetabolism m, BaseCombatEntity entity)
        {
            var player = entity.ToPlayer();
            if (player == null)
                return;
            IemUtils.SetMetabolismNoNutrition(player);
        }

        static bool NullFunc(BasePlayer entity, HitInfo hitInfo)
        {
            return true;
        }

        public delegate bool EntitiesCanBeWounded(BasePlayer player, HitInfo hitInfo);
        private static EntitiesCanBeWounded EntitiesBeingWounded = NullFunc;

        private bool CanBeWounded(BasePlayer player, HitInfo hitInfo)
        {
            if (player == null || hitInfo == null) return true;
            return EntitiesBeingWounded(player, hitInfo);
        }

        public delegate void EntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo);

        private static EntityTakeDamage EntitiesTakingDamage = delegate { };

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            EntitiesTakingDamage(entity, hitInfo);
        }

        #endregion
    }
}

