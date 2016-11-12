//Requires: IemGameBase
//Requires: IemStateManager
//Requires: IemObjectPlacement
//Requires: IemUI
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
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
        IemObjectPlacement IemObjectPlacement;

        [PluginReference]
        IemGameBase IemGameBase;

        static IemGameTargetPractice me;
        private static FieldInfo knockdownHealth;

        static TPGameManager gm;
        static Dictionary<string, IemGameTargetPracticeGame> games = new Dictionary<string, IemGameTargetPracticeGame>();

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

            knockdownHealth = typeof(ReactiveTarget).GetField("knockdownHealth", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
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


            public override IemGameBase.IemGame CreateGame(BasePlayer player)
            {
                me.Puts("in the tp game manager, creating new game");
                var newGame = new IemGameTargetPracticeGame(player);
                newGame.StartGame();
                return newGame;

            }
        }

        #endregion region

        #region IemGameTargetPracticeGame

        public class
        IemTargetPracticePlayer : IemGameBase.IemPlayer
        {
            public IemTargetPracticePlayer(BasePlayer player,
                IemGameBase.IemSoloGame game) : base(player)
            {

                me.IemUtils?.SaveInventory(player, game.GetGuid());
            }
        }

        public class GameLevel
        {
            public int Targets { get; set; }
            public int Timer { get; set; }
            public bool Started = false;

        }

        public class IemGameTargetPracticeGame : IemGameBase.IemSoloGame
        {
            public TargetPracticeStateManager gsm;
            public float GameLobbyWait = 12;
            public int MainPhaseWait = 20;

            public List<GameLevel> gamelevels = new List<GameLevel>(new GameLevel[] {
                new GameLevel {Targets=5, Timer=20},
                new GameLevel {Targets=7, Timer=20},
                new GameLevel {Targets=7, Timer=18},
                new GameLevel {Targets=9, Timer=17},
                new GameLevel {Targets=9, Timer=18},
                new GameLevel {Targets=11, Timer=18},
                new GameLevel {Targets=11, Timer=18},
                new GameLevel {Targets=12, Timer=17}
            });

            public int level = 0;

            // find reactive targets within 50 units of the game location
            // TODO this presumes that the game is a skygame
           public  List<ReactiveTarget> targets = new List<ReactiveTarget>();


            public IemGameTargetPracticeGame(BasePlayer newPlayer) : base(newPlayer)
            {
                Name = "Target_Practice";
                OnlyOneAtATime = true;
                Mode = "Solo";
                gsm = new TargetPracticeStateManager(
                    TargetPracticeStateManager.Created.Instance, this);

                var newIemPlayer =
                    new IemTargetPracticePlayer(player, this);
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

            public override bool CancelGame()
            {
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
                    player.CancelInvoke("WoundingTick");
                }

                if (!IemUtils.CheckPointNearToLocation(player.transform.position, location, 2))
                    //IemUtils.MovePlayerTo(player, location);
                    IemUtils.TeleportPlayerPosition(player, location);

                return true;
            }

            private float GetPlayerDistance(Vector3 targetPos, Vector3 attackerPos)
            {
                var distance = Vector3.Distance(targetPos, attackerPos);
                var rounded = Mathf.Round(distance * 100f) / 100f;
                return rounded;
            }

            public bool CheckLevelComplete()
            {
                GameLevel gamelevel = gamelevels[level];
                if (iemPlayer.Score >= gamelevel.Targets)
                {
                    return true;
                }
                else
                {
                    //IemUI.CreateFadeoutBanner(player, "");
                }

                return false;

            }

            public void GoNextLevel()
            {
                leveltimer.Destroy();
                level++;
                IemUI.CreateFadeoutBanner(player, "level complete");

                if (level >= gamelevels.Count)
                {
                    gsm?.ChangeState(TargetPracticeStateManager.GameComplete.Instance);
                }
                else
                {
                    gsm?.ChangeState(TargetPracticeStateManager.GameRunning.Instance);
                }
            }

            private Timer leveltimer;

            void wasCancelled()
            {
                me.Puts("was cancelled");
                IemUI.CreateFadeoutBanner(player, "cancelling");
                gsm?.ChangeState(TargetPracticeStateManager.GameComplete.Instance);
            }

            void wasConfirmed()
            {
                IemUI.CreateFadeoutBanner(player, "playing level again");
                gsm?.ChangeState(TargetPracticeStateManager.GameRunning.Instance);
            }

            public void StartLevelTimer()
            {

                GameLevel gamelevel = gamelevels[level];

                if (!gamelevel.Started)
                {
                    gamelevel.Started = true;
                    IemUI.ShowGameTimer(player, gamelevel.Timer);

                    leveltimer = me.timer.Once(gamelevel.Timer, () =>
                    {
                        if (!CheckLevelComplete())
                        {

                            IemUI.ConfirmCancel(player, "Level was not completed!\nYou can play this level again, or return to the map", "Again?", "Quit",
                      wasConfirmed, wasCancelled);

                        }
                    });
                }
            }


            public void ScorePlayerHit(BaseCombatEntity entity, HitInfo hitinfo)
            {
                try
                {
                    if (entity is ReactiveTarget && hitinfo.Initiator is BasePlayer)
                    {

                        var target = (ReactiveTarget)entity;

                        if (!targets.Contains(target))
                        {
                            me.Puts("is not game target");
                            return;
                        }

                        var attacker = (BasePlayer)hitinfo.Initiator;
                        if (attacker.UserIDString != iemPlayer.AsBasePlayer().UserIDString) {
                            me.Puts("is not game player");
                            return;
                        }
                            

                        if (entity == null || attacker == null)
                            return;

                        // hits on reactive targets tigger OnEntityTakeDamage twice
                        // this selects for the one created by on shared hit in ReactiveTarget
                        if (hitinfo.damageTypes.Total() != 1f)
                            return;

                        if (hitinfo.HitBone == StringPool.Get("target_collider_bullseye"))
                        {
                            IemUtils.DrawChatMessage(attacker, target, "Bullseye!!!!");
                        }

                        var health = knockdownHealth.GetValue(target);

                        StartLevelTimer();

                        if (target.IsKnockedDown())
                        {
                            iemPlayer.Score += 1;
                            IemUI.CreateGameBanner2(attacker, "score=" + iemPlayer.Score);

                            if (CheckLevelComplete())
                            {
                                GoNextLevel();
                            }
                            else
                            {
                                target.CancelInvoke("ResetTarget");
                                target.health = target.MaxHealth();
                                target.SendNetworkUpdate();
                                // me.timer.Once(1, () => target.SetFlag(BaseEntity.Flags.On, true));
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


            Vector3 location = me.IemUtils.NextFreeLocation();

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

                    gsm.partition = new IemObjectPlacement.CopyPastePlacement(
                        "shootinggallery3", gsm.location);



                }
            }

            void proceedAction()
            {
                me.Kits?.Call("GiveKit", eg.player, "tp_v1");
                eg.player.inventory.SendSnapshot();
                ChangeState(TargetPracticeStateManager.GameRunning.Instance);

            }

            public class GameLobby : IemStateManager.StateBase<GameLobby>,
                IemStateManager.IStateMachine
            {
                public new void Enter(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;
                    
                    gsm.eg.targets = IemUtils.FindComponentsNearToLocation<ReactiveTarget>(gsm.location, 50);

                    gsm.eg.player.inventory.Strip();

                    EntitiesTakingDamage += gsm.eg.PlayerImmortal;

                    gsm.eg.MovePlayerToTeamLocation(gsm.eg.player,
                        gsm.location);

                    gsm.eg.player.EndSleeping();

                    IemUtils.SetMetabolismValues(gsm.eg.player);
                    IemUtils.ClearInventory(gsm.eg.player);

                    IemUI.CreateGameBanner(gsm.eg.player, "GAME LOBBY");
                    //IemUI.ShowIntroOverlay(gsm.eg.player,
                    //    $"Weclome to target practice\n" +
                    //    $"Shoot as many targets as possible.\n" +
                    //    $"each level timer will start when you fire the first shot");

                    IemUtils.PlaySound(gsm.eg.player);

                    IemUI.Confirm(gsm.eg.player, $"Weclome to target practice\n" +
                        $"Knock down the targets to proceed to the next level.\n" +
                        $"each level timer will start when you fire the first shot", "Start Shootin'",
                        gsm.proceedAction);

                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;
                    gsm.walltimer?.Destroy();
                    EntitiesTakingDamage -= gsm.eg.PlayerImmortal;
                    CuiHelper.DestroyUi(gsm.eg.player, "ShowIntroOverlay");


                }
            }

            private float GetPlayerDistance(Vector3 targetPos, Vector3 attackerPos)
            {
                var distance = Vector3.Distance(targetPos, attackerPos);
                var rounded = Mathf.Round(distance * 100f) / 100f;
                return rounded;
            }



            void DrawChatMessage(BasePlayer onlinePlayer, BaseEntity entity, string message)
            {
                float distanceBetween = Vector3.Distance(entity.transform.position, onlinePlayer.transform.position);



                if (distanceBetween <= 50)
                {

                    string lastMessage = message;
                    Color messageColor = new Color(1, 1, 1, 1);

                    onlinePlayer.SendConsoleCommand("ddraw.text", 2f, messageColor, entity.transform.position + new Vector3(0, 1.9f, 0), "<size=25>" + lastMessage + "</size>");

                }
            }
            public class GameRunning : IemStateManager.StateBase<GameRunning>,
                IemStateManager.IStateMachine
            {
                public new void Enter(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;

                    // reset the player score for this level
                    gsm.eg.iemPlayer.Score = 0;

                    // get the properties of this game level
                    GameLevel gamelevel = gsm.eg.gamelevels[gsm.eg.level];

                    // log the score
                    EntitiesTakingDamage += gsm.eg.ScorePlayerHit;

                    // refill magazines in weapons on belt container
                    IemUtils.RefillBeltMagazines(gsm.eg.player);

                    foreach (var target in gsm.eg.targets)
                    {
                        target.ResetTarget();
                        var health = knockdownHealth.GetValue(target);
                        knockdownHealth.SetValue(target, 100f);
                    }


                    IemUtils.PlaySound(gsm.eg.player);
                    IemUI.CreateGameBanner(gsm.eg.player, "Game is running! level " + (gsm.eg.level + 1));

                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;
                    gsm.walltimer?.Destroy();
                    IemUI.CreateGameBanner(gsm.eg.player, "");
                    EntitiesTakingDamage -= gsm.eg.PlayerImmortal;
                    EntitiesTakingDamage -= gsm.eg.ScorePlayerHit;

                    CuiHelper.DestroyUi(gsm.eg.player, "CreateFadeoutBanner");

                }
            }

            public class GameComplete : IemStateManager.StateBase<GameComplete>,
                IemStateManager.IStateMachine
            {

                public new void Enter(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;

                    List<ReactiveTarget> targets = IemUtils.FindComponentsNearToLocation<ReactiveTarget>(gsm.location, 50);
                    foreach (var target in targets)
                    {
                        //var pos = target.transform.position;
                        //var rot = target.transform.rotation;
                        target.SetFlag(BaseEntity.Flags.On, false);

                    }

                    // resultsTimer = IemUI.ShowResultsUiForSolo(gsm.eg.Players.Select(d => d.Value).ToList(), gsm.eg, 8);
                    IemUI.CreateGameBanner(gsm.eg.player, "Game is complete! - score was "
                        + gsm.eg.iemPlayer.Score);

                    gsm.eg.EndGame();



                    IemUI.ShowGameTimer(gsm.eg.player, 10f, "back to map in: ");

                    gsm.walltimer = me.timer.Once(10f, () =>
                    {
                        gsm?.ChangeState(TargetPracticeStateManager.CleanUp.Instance);
                    });
                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;

                    foreach (IemGameBase.IemPlayer iemPlayer in gsm.eg.Players.Values)
                    {
                        BasePlayer player = IemUtils.FindPlayerByID(iemPlayer.PlayerId);
                        IemUI.CreateGameBanner(player, "");
                    }

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



            public class CleanUp : IemStateManager.StateBase<CleanUp>,
                IemStateManager.IStateMachine
            {
                public new void Enter(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;

                    gsm.partition.Remove();
                    gsm.eg.MovePlayerToTeamLocation(gsm.eg.player,
                        gsm.eg.iemPlayer.PreviousLocation);
                    me.IemUtils.RestoreInventory(gsm.eg.player, gsm.eg.GetGuid());
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

