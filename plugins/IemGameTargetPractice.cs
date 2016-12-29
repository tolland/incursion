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
using System.Linq;
using Newtonsoft.Json;

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
        }

        void Loaded()
        {
            gm = new TPGameManager();
            IemGameBase.RegisterGameManager(gm);
        }

        void Unload()
        {
            IemGameBase.UnregisterGameManager(gm);
        }

        void OnServerInitialized()
        {
            knockdownHealth = typeof(ReactiveTarget).GetField("knockdownHealth", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        }

        void LoadDefaultConfig()
        {
            Config.Clear();
            Config["Enabled"] = false;
            Config.Save();
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
                Description = "This game will help you improve your pvp shooting skills\n" +
                    "\n" +
                    "Shoot down the targets to complete the level\n" +
                    "The number of remaining targets is displayed in the topbar centre\n" +
                    "The timer is on the top left\n" +
                    "If you don't complete a level in time, you can try it again\n\n" +
                    "Checkout your player stats on the left to see how you are improving\n" +
                    "The game stats show the best players this month\n\n" +
                    "<color=red>Tip: Accuracy is critical, try and hit the target centre</color>\n" +
                    "<color=green>Tip: Try double taps to improve your recoil control</color>\n" +
                    "<color=yellow>Tip: Once you can complete the level easily, " +
                    "try strafing left and right, as that is more game realistic</color>\n";

                // this block allows the specification of different levels to be offered
                // to the player in the UI
                difficultyModes = new Dictionary<string, IemGameBase.DifficultyMode>()
                 {
                { "Easy", new IemGameBase.DifficultyMode {
                    Name ="Easy",
                    Description ="Easy going",
                    GameLevelDefinitions = new List<IemGameBase.GameLevelDefinition>(   new TPGameLevelDefinition[] {
                new TPGameLevelDefinition {Targets=5, Timer=20, kitname="tpv1_ak"},
                new TPGameLevelDefinition {Targets=6, Timer=20, kitname="tpv1_p250"},
                new TPGameLevelDefinition {Targets=5, Timer=18, kitname="tpv1_revolver"},
                    }  )
            } },
                    { "Moderate",
                new IemGameBase.DifficultyMode {
                    Name ="Moderate",
                    Description ="Some practice required",
                    GameLevelDefinitions = new List<IemGameBase.GameLevelDefinition>(   new TPGameLevelDefinition[] {
                new TPGameLevelDefinition {Targets=6, Timer=18},
                new TPGameLevelDefinition {Targets=7, Timer=18},
                new TPGameLevelDefinition {Targets=7, Timer=17},
                new TPGameLevelDefinition {Targets=8, Timer=17},
                    }  )
            } },
                    { "Challenging",
                new IemGameBase.DifficultyMode {
                    Name ="Challenging",
                    Description ="Challenging to complete",
                    GameLevelDefinitions = new List<IemGameBase.GameLevelDefinition>(   new TPGameLevelDefinition[] {
                new TPGameLevelDefinition {Targets=6, Timer=18},
                new TPGameLevelDefinition {Targets=7, Timer=18},
                new TPGameLevelDefinition {Targets=7, Timer=17},
                new TPGameLevelDefinition {Targets=8, Timer=17},
                //new GameLevel {Targets=9, Timer=18, IemGame=this},
                //new GameLevel {Targets=11, Timer=18, IemGame=this},
                //new GameLevel {Targets=11, Timer=18, IemGame=this},
                //new GameLevel {Targets=12, Timer=17, IemGame=this}
                    }  )
            } },
                    { "Impossible",
                new IemGameBase.DifficultyMode {
                    Name ="Impossible",
                    Description ="Only the best of the best",
                    GameLevelDefinitions = new List<IemGameBase.GameLevelDefinition>(   new TPGameLevelDefinition[] {
                new TPGameLevelDefinition {Targets=6, Timer=18},
                new TPGameLevelDefinition {Targets=7, Timer=18},
                new TPGameLevelDefinition {Targets=7, Timer=17},
                new TPGameLevelDefinition {Targets=8, Timer=17},
                new TPGameLevelDefinition {Targets=9, Timer=18},
                new TPGameLevelDefinition {Targets=10, Timer=18},
                new TPGameLevelDefinition {Targets=10, Timer=17},
                new TPGameLevelDefinition {Targets=11, Timer=17}
                    }  )
            } }
                    };

                HasDifficultyModes = true;
                HasStats = true;
                HasGameStats = true;
            }

            public override IemGameBase.IemGame CreateGame(BasePlayer player, string level)
            {
                //me.Puts("in the tp game manager, creating new game");
                var newGame = new IemGameTargetPracticeGame(player);

                foreach (TPGameLevelDefinition origlevel in difficultyModes[level].GameLevelDefinitions
                    .Cast<TPGameLevelDefinition>().ToList())
                {
                    TPGameLevel newLevel = new TPGameLevel()
                    {
                        Game = newGame,
                        Targets = origlevel.Targets,
                        Timer = origlevel.Timer,
                        Player = player,
                        gameLevelDefinition = origlevel
                    };
                    newGame.gamelevels.Add(newLevel);
                }

                newGame.difficultyLevel = level;
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

        public class TPGameLevelDefinition : IemGameBase.GameLevelDefinition
        {
            // public int Targets { get; set; }
        }

        public class TPGameLevel : IemGameBase.GameLevel
        {
            public int Targets { get; set; }
        }

        public class IemGameTargetPracticeGame : IemGameBase.IemSoloGame
        {
            [JsonIgnore]
            public TargetPracticeStateManager gsm;

            [JsonIgnore]
            public IemUtils.ReturnZone returnZone;

            // find reactive targets within 50 units of the game location
            // TODO this presumes that the game is a skygame
            [JsonIgnore]
            public List<ReactiveTarget> targets = new List<ReactiveTarget>();

            public IemGameTargetPracticeGame(BasePlayer newPlayer) : base(newPlayer)
            {
                Name = "Target_Practice";
                OnlyOneAtATime = true;
                Mode = "Solo";
                // level 0 is pregame
                level = -1;

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
                me.Puts("cancelling game in TPgame");
                gsm?.ChangeState(TargetPracticeStateManager.GameCancelled.Instance);
                base.CancelGame();
                return true;
            }

            public bool CheckLevelComplete()
            {
                TPGameLevel gamelevel = (TPGameLevel)gamelevels[level];
                if (iemPlayer.Score >= gamelevel.Targets)
                    return true;

                return false;
            }

            public void GoNextLevel()
            {
                IemUI.CreateFadeoutBanner(player, "level complete");
                gamelevels[level].End();

                IemUI.CreateRightFadeout(player, "shots fired " + gamelevels[level].accuracy.ShotsFired
                    + "\nshots hit " + gamelevels[level].accuracy.ShotsHit + "\naccuracy "
                    + gamelevels[level].accuracy.GetAccuracyAsString());

                // level is indexed at 1 when game is in progress, 0 is pregame
                if (level == (gamelevels.Count - 1))
                {
                    //me.Puts("settig game complete");
                    gsm?.ChangeState(TargetPracticeStateManager.GameComplete.Instance);
                }
                else
                {
                    // me.Puts("going to next level");
                    //player.inventory.Strip();
                    me.timer.Once(1f, () =>

                        gsm?.ChangeState(TargetPracticeStateManager.GameRunning.Instance)
                         );
                }
            }

            public void wasCancelled()
            {
                IemUI.CreateFadeoutBanner(player, "cancelling");
                CancelGame();
            }

            public void wasConfirmed()
            {
                gsm?.ChangeState(TargetPracticeStateManager.GameRunning.Instance);
            }

            public override bool RestartLevel()
            {
                gsm?.ChangeState(TargetPracticeStateManager.GameRunning.Instance);


                return true;
            }

            public void playAgain()
            {
                IemUI.CreateFadeoutBanner(player, "playing again");
                gsm?.ChangeState(TargetPracticeStateManager.GameComplete.Instance);
            }

            public void backToTheMap()
            {
                IemUI.CreateFadeoutBanner(player, "back to map");
                gsm?.ChangeState(TargetPracticeStateManager.CleanUp.Instance);
            }

            internal void TPWeaponFired(BaseProjectile projectile,
                BasePlayer player,
                ItemModProjectile mod,
                ProtoBuf.ProjectileShoot projectiles)
            {
                //var attacker = (BasePlayer)hitinfo.Initiator;
                if (player.UserIDString != iemPlayer.AsBasePlayer().UserIDString)
                {
                    me.Puts("is not game player");
                    return;
                }
                else
                {
                    TPGameLevel gamelevel = (TPGameLevel)gamelevels[level];
                    gamelevel.accuracy.ShotsFired += 1;
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
                        if (attacker.UserIDString != iemPlayer.AsBasePlayer().UserIDString)
                        {
                            me.Puts("is not game player");
                            return;
                        }

                        if (entity == null || attacker == null)
                            return;

                        // TODO resolve problem that the ReactiveTarget is squashing the HitInfo
                        //me.Puts("hitinfo.HitBone " + hitinfo.HitBone);
                        //me.Puts("target_collider_bullseye " + StringPool.Get("target_collider_bullseye"));
                        if (hitinfo.HitBone == StringPool.Get("target_collider_bullseye"))
                        {
                            IemUtils.DrawChatMessage(attacker, target, "Bullseye!!!!");
                        }

                        // hits on reactive targets tigger OnEntityTakeDamage twice
                        // this selects for the one created by on shared hit in ReactiveTarget
                        if (hitinfo.damageTypes.Total() != 1f)
                            return;

                        //this is the amount of damage done to the target by the hit
                        var health = knockdownHealth.GetValue(target);

                        TPGameLevel gamelevel = (TPGameLevel)gamelevels[level];

                        if (!gamelevel.Started)
                        {
                            me.Puts("starting gamelevel " + (level));
                            gamelevel.Start();
                            IemUtils.GameTimer.Start(player);
                        }
                        else
                        {
                            me.Puts("gamelevel " + (level) + " is already started at " + gamelevel.StartTime);
                        }

                        gamelevel.accuracy.ShotsHit += 1;

                        if (target.IsKnockedDown())
                        {
                            iemPlayer.Score += 1;
                            IemUI.CreateGameBanner(attacker, "Level " + (level + 1) +
                                "/" + gamelevels.Count +
                                " - targets remaining " + (gamelevel.Targets - iemPlayer.Score));

                            if (CheckLevelComplete())
                            {
                                GoNextLevel();
                            }
                            else
                            {
                                target.CancelInvoke("ResetTarget");
                                target.health = target.MaxHealth();
                                target.SendNetworkUpdate();
                            }

                            targets[new System.Random().Next(0,
                                 targets.Count)].ResetTarget();


                        }
                    }
                }
                catch (Exception ex)
                {
                    me.Puts("exception " + ex);
                }
            }

            public void PlayerImmortal(BaseCombatEntity entity, HitInfo hitInfo)
            {
                //IemUtils.GLog("calling immortality check");
                if (entity as BasePlayer == null || hitInfo == null)
                {
                    // IemUtils.GLog("was null");
                    return;
                }

                if (entity.ToPlayer() != null)
                {
                    // IemUtils.GLog("scaling player damage to zero");
                    BasePlayer player = entity.ToPlayer();

                    if (GetIemPlayerById(player.UserIDString) != null)
                    {
                        // TODO probably better way to scale this damage
                        //hitInfo.damageTypes.Scale(DamageType.Bullet, 0f);
                        IemUtils.NullifyDamage(ref hitInfo);
                    }
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

            public class Created : IemStateManager.StateBase<Created>,
                IemStateManager.IStateMachine
            {
                public new void Enter(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;

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

                    gsm.partition = new IemObjectPlacement.CopyPastePlacement(
                        "tp_v1_" + gsm.eg.difficultyLevel, gsm.location);

                    ResearchTable researchtable = IemUtils.FindComponentNearestToLocation<ResearchTable>(
                        gsm.location, 50);
                    Vector3 playerstart = researchtable.transform.position;
                    researchtable.Kill(BaseNetworkable.DestroyMode.None);
                    RepairBench repairbench = IemUtils.FindComponentNearestToLocation<RepairBench>(
                        gsm.location, 50);
                    Vector3 playerlook = repairbench.transform.position;
                    repairbench.Kill(BaseNetworkable.DestroyMode.None);

                    me.Puts("player start location is " + playerstart);
                    me.Puts("player look location is " + playerlook);

                    me.Puts("swutcged " + IemUtils.SwitchTypesToTarget<BaseOven>(gsm.location));

                    gsm.eg.player.inventory.Strip();

                    EntitiesTakingDamage += gsm.eg.PlayerImmortal;

                    IemUtils.MovePlayerToTeamLocation(gsm.eg.player,
                        playerstart);

                    Vector3 relativePos = playerstart - playerlook;
                    Quaternion rotation = Quaternion.LookRotation(relativePos);
                    gsm.eg.player.transform.rotation = rotation;

                    IemUtils.SetMetabolismValues(gsm.eg.player);
                    IemUtils.ClearInventory(gsm.eg.player);

                    IemUI.CreateGameBanner(gsm.eg.player, "GAME LOBBY");

                    IemUtils.PlaySound(gsm.eg.player);

                    gsm.eg.returnZone = new IemUtils.ReturnZone(playerstart, gsm.eg.player);

                    IemUI.Confirm(gsm.eg.player, $"Weclome to target practice\n" +
                        $"Knock down the targets to proceed to the next level.\n" +
                        $"each level timer will start when you fire the first shot", "Start Shootin'",
                        gsm.proceedAction);

                    gsm.eg.targets = IemUtils.FindComponentsNearToLocation<ReactiveTarget>(gsm.location, 50);

                    me.Puts("targets count is " + gsm.eg.targets.Count());
                    var targetbuff = new List<ReactiveTarget>();
                    for (int i = 0; i < gsm.eg.targets.Count; i++)
                    {
                        if (gsm.eg.targets[i] != null && !gsm.eg.targets[i].IsDestroyed)
                        {
                            targetbuff.Add(gsm.eg.targets[i]);
                        }
                    }
                    gsm.eg.targets = targetbuff;


                    me.Puts("targets count is " + gsm.eg.targets.Count());
                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;
                    IemUI.CreateGameBanner(gsm.eg.player, "");
                    EntitiesTakingDamage -= gsm.eg.PlayerImmortal;
                    CuiHelper.DestroyUi(gsm.eg.player, "ShowIntroOverlay");
                }
            }

            public class GameRunning : IemStateManager.StateBase<GameRunning>,
                IemStateManager.IStateMachine
            {
                public new void Enter(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;

                    // this is a fresh game at pregame
                    if (gsm.eg.level == -1)
                    {
                        gsm.eg.level++;
                    }
                    else
                    {
                        // level has already been started, and this is a rerun of the level
                        // reset level vals
                        if (gsm.eg.gamelevels[gsm.eg.level].Started &&
                        !gsm.eg.gamelevels[gsm.eg.level].Ended)
                        {
                            gsm.eg.gamelevels[gsm.eg.level].Reset();
                        }
                        else if (!gsm.eg.gamelevels[gsm.eg.level].Started &&
                        !gsm.eg.gamelevels[gsm.eg.level].Ended)
                        {
                            //erm what to do if game is not started or ended???
                        }
                        else
                        {
                            // this is a new level
                            gsm.eg.level++;
                        }
                    }

                    // get the properties of this game level
                    TPGameLevel gamelevel = (TPGameLevel)gsm.eg.gamelevels[gsm.eg.level];

                    if (gamelevel.gameLevelDefinition.kitname != null)
                    {
                        gsm.eg.player.inventory.Strip();
                        me.Kits?.Call("GiveKit", gsm.eg.player, gamelevel.gameLevelDefinition.kitname);
                        gsm.eg.player.inventory.SendSnapshot();
                    }


                    EntitiesTakingDamage += gsm.eg.PlayerImmortal;

                    // reset the player score for this level
                    gsm.eg.iemPlayer.Score = 0;


                    IemUI.CreateGameBanner(gsm.eg.player, "Level " + (gsm.eg.level + 1) +
                                "/" + gsm.eg.gamelevels.Count +
                        " - targets remaining " + (gamelevel.Targets - gsm.eg.iemPlayer.Score));

                    IemUtils.GameTimer.CreateTimerPaused(gsm.eg.player, gamelevel.Timer, () =>
                    {
                        if (!gsm.eg.CheckLevelComplete())
                        {
                            IemUI.ConfirmCancel(gsm.eg.player,
                                "Level was not completed!\nYou can play this level again, or return to the map", "Play Again", "Quit",
                                gsm.eg.wasConfirmed,
                                gsm.eg.wasCancelled,
                                true);
                        }
                    });

                    // log the score
                    EntitiesTakingDamage += gsm.eg.ScorePlayerHit;

                    // refill magazines in weapons on belt container
                    IemUtils.RefillBeltMagazines(gsm.eg.player);

                    foreach (var target in gsm.eg.targets)
                    {
                        if (target != null)
                        {
                            //target.ResetTarget();
                            //var health = knockdownHealth.GetValue(target);
                            knockdownHealth.SetValue(target, 100f);
                            target.SetFlag(BaseEntity.Flags.On, false);
                        }
                    }

                    gsm.eg.targets[new System.Random().Next(0,
                        gsm.eg.targets.Count)].SetFlag(BaseEntity.Flags.On, true);

                    RunWeaponFired += gsm.eg.TPWeaponFired;
                    IemUtils.PlaySound(gsm.eg.player);
                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;

                    //stop the level timer
                    IemUtils.GameTimer.Destroy(gsm.eg.player);

                    //stop counting shots fired
                    RunWeaponFired -= gsm.eg.TPWeaponFired;
                    //stop protecting player from damage
                    EntitiesTakingDamage -= gsm.eg.PlayerImmortal;
                    //stop counting targets hit
                    EntitiesTakingDamage -= gsm.eg.ScorePlayerHit;

                    //reset  the game banners
                    IemUI.CreateGameBanner(gsm.eg.player, "");
                    IemUI.CreateGameBanner2(gsm.eg.player, "");
                    
                }
            }

            public class GameComplete : IemStateManager.StateBase<GameComplete>,
                IemStateManager.IStateMachine
            {

                public new void Enter(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;

                    CuiHelper.DestroyUi(gsm.eg.player, "ConfirmComplete");

                    List<ReactiveTarget> targets = IemUtils.FindComponentsNearToLocation<ReactiveTarget>(gsm.location, 50);
                    foreach (var target in targets)
                    {
                        target.SetFlag(BaseEntity.Flags.On, false);
                    }

                    gsm.eg.EndGame();

                    double totalTime = 0;
                    foreach (TPGameLevel level in gsm.eg.gamelevels)
                    {
                        totalTime += level.LevelTime();
                    }

                    IemUI.CreateGameBanner(gsm.eg.player, "Game is complete! - your time was "
                        + IemUtils.GetTimeFromSeconds(totalTime) + " seconds");


                    IemUI.Confirm(gsm.eg.player, "Game completed!\nreturn to the map",
                        "Back",
                    gsm.eg.backToTheMap);

                    //IemUI.ConfirmCancel(gsm.eg.player,
                    //    "Game completed!\nreturn to the map\nor play again", 
                    //    "Play Again", "Quit",
                    //    gsm.eg.wasConfirmed,
                    //    gsm.eg.backToTheMap);
                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;

                    //CuiHelper.DestroyUi(gsm.eg.player, "CreateFadeoutBanner");
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
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;
                    gsm?.ChangeState(TargetPracticeStateManager.CleanUp.Instance);
                }
            }

            public class CleanUp : IemStateManager.StateBase<CleanUp>,
                IemStateManager.IStateMachine
            {
                public new void Enter(IemStateManager.StateManager sm)
                {
                    TargetPracticeStateManager gsm = (TargetPracticeStateManager)sm;

                    gsm.eg.returnZone.Destroy();

                    me.Puts("moving player to previous location " + gsm.eg.iemPlayer.PreviousLocation);
                    IemUtils.MovePlayerToTeamLocation(gsm.eg.player,
                        gsm.eg.iemPlayer.PreviousLocation);

                    gsm.partition.Remove();
                    me.IemUtils.RestoreInventory(gsm.eg.player, gsm.eg.GetGuid());
                }
            }
        }

        #endregion

        #region Oxide Hooks for game


        public delegate void WeaponFired(BaseProjectile projectile, BasePlayer player,
                ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles);

        private static WeaponFired RunWeaponFired = delegate { };

        void OnWeaponFired(BaseProjectile projectile,
                        BasePlayer player,
                        ItemModProjectile mod,
                        ProtoBuf.ProjectileShoot projectiles)
        {
            RunWeaponFired(projectile, player, mod, projectiles);
        }

        public delegate void RunningPlayerMetabolism(PlayerMetabolism m, BaseCombatEntity entity);
        private static RunningPlayerMetabolism RunPlayerMetabolism = delegate { };
        void OnRunPlayerMetabolism(PlayerMetabolism m, BaseCombatEntity entity)
        {

        }

        // find entities taking damage, and create a delegate which can be attached
        public delegate void EntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo);
        private static EntityTakeDamage EntitiesTakingDamage = delegate { };

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            EntitiesTakingDamage(entity, hitInfo);
        }

        #endregion
    }
}

