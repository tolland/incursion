//Requires: IemGameBase
//Requires: IemStateManager
//Requires: IemObjectPlacement
//Requires: IncursionHoldingArea
//Requires: IemUI
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Incursion Embankment", "tolland", "0.1.0")]
    class IemGameEmbankment : RustPlugin
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
        IncursionHoldingArea IncursionHoldingArea;

        [PluginReference]
        IemGameBase IemGameBase;

        static IemGameEmbankment me;
        static EMGameManager gm;
        public static IemUtils.IIemGame game;

        #endregion

        #region boiler plate

        void Init()
        {
            me = this;
            //LoadConfigValues();
            IemUtils.LogL("IemGameEmbankment: Init complete");
        }

        void Loaded()
        {
            Unsubscribe(nameof(OnRunPlayerMetabolism));

            gm = new EMGameManager();
            IemGameBase.RegisterGameManager(gm);
            IemUtils.LogL("IemGameEmbankment: Loaded complete");
            IemUtils.LogL(" ");
        }

        void Unload()
        {
            IemUtils.DDLog("IemGameEmbankment: in unload");
            IemUtils.LogL("IemGameEmbankment: unloaded started");
            IemGameBase.UnregisterGameManager(gm);
            IemUtils.LogL("IemGameEmbankment: unloaded complete");
        }


        void OnServerInitialized()
        {
            // IemUtils.LogL("IemGameEmbankment: OnServerInitialized started");
            // IemUtils.LogL("IemGameEmbankment: OnServerInitialized complete");
        }

        void LoadDefaultConfig()
        {
            Config.Clear();
            Config["GameLevels"] = false;
            Config.Save();
        }

        #endregion

        #region IemGameEmbankmentGame

        public class EMGameManager : IemGameBase.GameManager
        {
            public EMGameManager() : base()
            {

                Enabled = true;
                Mode = "Team";
                Name = "Embankment";
                TileImgUrl = "http://www.limepepper.co.uk/images/high_walls_08.jpg";
            }


            public override IemGameBase.IemGame CreateGame(BasePlayer player,
                string level = null)
            {
                var newGame = new IemGameEmbankmentGame();
                newGame.Players[player.UserIDString] = new IemEmbankmentPlayer(player, newGame);
                IemUI.ShowTeamUiForPlayer(player, newGame);
                if (!newGame.CanStart())
                    IemUI.CreateGameBanner(player, newGame.CanStartCriteria());
                return newGame;
            }
        }
         
        public class IemEmbankmentPlayer : IemGameBase.IemPlayer
        {
            public IemEmbankmentPlayer(BasePlayer player, IemGameEmbankmentGame game) : base(player)
            {
                IemUtils.TeleportPlayerPosition(player, game.teamLobby.location);
                me.IemUtils?.SaveInventory(player, game.GetGuid());
                game.teamLobby.OpenDoors();
            }
        }   

        public class IemGameEmbankmentGame : IemGameBase.IemTeamGame
        {
            public EmbankStateManager gsm;
            public float GameLobbyWait = 10;
            public int PartitionWait = 60;
            public int MainPhaseWait = 60;
            public int SuddenDeathPhaseWait = 15;
            public IncursionHoldingArea.TeamSelectLobby teamLobby;

            public IemGameEmbankmentGame()
            {
                Name = "Embankment";
                OnlyOneAtATime = true;
                Mode = "Team";
                MinPlayersPerTeam = 0;

                //adds some teams to the game, sets the team locations
                InitGame();

                //create the team select lobby
                teamLobby = new IncursionHoldingArea.TeamSelectLobby("teamlobby_v1", this);
            }

            //TODO utility function, this needs to be moved to default config
            void InitGame()
            {
                IemGameBase.IemTeam team1 = AddTeam(
                    new IemGameBase.IemTeam("team_1", "blue", 1, 20, "Blue Bandits"));

                team1.Location = new Vector3(27, 34, 36);

                IemGameBase.IemTeam team2 = AddTeam(
                    new IemGameBase.IemTeam("team_2", "red", 1, 20, "Red Devils"));
                team2.Location = new Vector3(21, 41, -14);
            }

            public override IemUtils.IIemTeamPlayer AddPlayer(BasePlayer player)
            {
                if (!Players.ContainsKey(player.UserIDString))
                    Players[player.UserIDString] = new IemEmbankmentPlayer(player, this);
                IemUI.ShowTeamUiForPlayer(player, this);
                IemUtils.TeleportPlayerPosition(player, teamLobby.location);

                return (IemUtils.IIemTeamPlayer)Players[player.UserIDString];
            }

            public override bool StartGame()
            {
                IemUtils.GLog("calling StartGame in IemGameEmbankmentGame");
                if (gsm == null)
                {
                    base.StartGame();
                    gsm = new EmbankStateManager(EmbankStateManager.Created.Instance, this);
                    gsm.ChangeState(EmbankStateManager.GameLobby.Instance);
                }
                return true;
            } 

            public override bool CleanUp()
            {
                base.CleanUp();

                IemUtils.DDLog("cleaning up the game");
                teamLobby?.Destroy();

                foreach (var partition in gsm.partitions)
                    partition?.Remove();

                return true;
            }

            public override bool CancelGame()
            {
                gsm?.ChangeState(EmbankStateManager.GameCancelled.Instance);
                base.CancelGame();
                return true;
            }

            public bool MovePlayerToTeamLocation(BasePlayer player, Vector3 location)
            {

                IemUtils.GLog("moving players to game");

                if (!IemUtils.CheckPointNearToLocation(
                    player.transform.position, location, 2))
                    IemUtils.TeleportPlayerPosition(player, location);

                return true;
            }

            public override IemUtils.IIemTeam Winner()
            {
                IemUtils.IIemTeam team = null;
                int bestscore = 0;
                foreach (var iemTeam in Teams.Values)
                {
                    if (iemTeam.Score > bestscore)
                    {
                        bestscore = iemTeam.Score;
                        team = iemTeam;
                    }
                    IemUtils.GLog("best score is " + bestscore);
                }
                return team;
            }

            public int GetCountOfLivingPlayers(IemGameBase.IemTeam iemTeam)
            {
                int count = 0;
                foreach (IemGameBase.IemPlayer iemPlayer in iemTeam.Players.Values)
                {
                    BasePlayer player = IemUtils.FindPlayerByID(iemPlayer.PlayerId);
                    if (iemPlayer.PlayerState == IemUtils.PlayerState.Alive)
                        count++;
                }
                return count;
            }

            public bool CheckGameState()
            {
                bool someoneWon = false;
                foreach (IemGameBase.IemTeam iemTeam in Teams.Values)
                {
                    if (iemTeam.State != IemUtils.TeamState.Empty)
                    {
                        int count = 0;
                        foreach (IemGameBase.IemPlayer iemPlayer in iemTeam.Players.Values)
                        {
                            BasePlayer player = IemUtils.FindPlayerByID(iemPlayer.PlayerId);
                            if (iemPlayer.PlayerState == IemUtils.PlayerState.Alive)
                                count++;
                        }
                        if (count == 0)
                        {
                            iemTeam.State = IemUtils.TeamState.Lost;
                        }
                    }
                }
                int teamsNotLost = 0;
                IemGameBase.IemTeam winTeam = null;
                foreach (IemGameBase.IemTeam iemTeam in Teams.Values)
                {
                    if (iemTeam.State != IemUtils.TeamState.Empty &&
                        iemTeam.State != IemUtils.TeamState.Lost)
                    {
                        teamsNotLost++;
                        winTeam = iemTeam;
                    }
                }
                if (teamsNotLost == 1)
                {
                    someoneWon = true;
                    winTeam.State = IemUtils.TeamState.Won;
                }
                if (someoneWon)
                {
                    gsm.ChangeState(EmbankStateManager.GameComplete.Instance);
                }
                return someoneWon;
            }

            public void ShowGameStatus()
            {
                string status = "";
                foreach (IemGameBase.IemTeam iemTeam in Teams.Values)
                {
                    status += iemTeam.Name + " (" + GetCountOfLivingPlayers(iemTeam) + ") ";
                }
                IemUI.UpdateGameStatusBanner(status, this);
            } 

            public void ShowGameTimer(int countdown)
            {
                string status = "";

                foreach (IemGameBase.IemTeam iemTeam in Teams.Values)
                {
                    foreach (IemGameBase.IemPlayer iemPlayer in iemTeam.Players.Values)
                    {
                        BasePlayer player = IemUtils.FindPlayerByID(iemPlayer.PlayerId);
                        IemUI.CreateGameTimer(player, countdown);
                    }
                }
            }

            public void PlayerDied(BasePlayer victim, HitInfo info)
            {
                IemUtils.GLog("player died in one life phase");


                if (victim.ToPlayer() != null)
                {
                    IemUtils.GLog("player died - setting player dead");
                    BasePlayer player = victim.ToPlayer();

                    GetIemPlayerById(victim.UserIDString).PlayerState =
                        IemUtils.PlayerState.Dead;

                }
            }

            public void ScorePlayerKill(BasePlayer victim, HitInfo hitInfo)
            {
                IemUtils.GLog("player died in game phase, calculate new score");

                if (victim.ToPlayer() != null)
                {
                    IemUtils.GLog("player died - hook called");
                    BasePlayer player = victim.ToPlayer();

                    if (hitInfo?.Initiator != null &&
                        hitInfo?.Initiator?.ToPlayer() != null &&
                        victim.ToPlayer() != null)
                    {
                        IemUtils.GLog("found player hit, found player scoring kill");
                        //       PlayerDying(player, info);
                        IemUtils.IIemTeamPlayer iemTeamPlayer = GetIemPlayerById(victim.UserIDString);
                        if (iemTeamPlayer != null)
                        {
                            IemUtils.GLog("incrementing score");
                            GetIemPlayerById(hitInfo?.Initiator?.ToPlayer().UserIDString).Score += 1;
                            GetIemPlayerById(hitInfo?.Initiator?.ToPlayer().UserIDString).Team.Score += 1;
                        }
                    }
                }
            }

            public object SuddenDeathWounded(BaseCombatEntity entity, HitInfo hitInfo)
            {
                return false;
            }


            public void SuddenDeath(BaseCombatEntity entity, HitInfo hitInfo)
            {
                if (entity as BasePlayer == null || hitInfo == null) return;
                
                if (entity.ToPlayer() != null)
                {
                    IemUtils.DLog("player died");
                    BasePlayer player = entity.ToPlayer();
                    
                    if (GetIemPlayerById(player.UserIDString) != null)
                    {

                        IemUtils.GLog("scaling damage");
                        hitInfo.damageTypes.Scale(DamageType.Bullet, 1000f);
                        hitInfo.damageTypes.Scale(DamageType.Arrow, 1000f);
                        hitInfo.damageTypes.Scale(DamageType.Blunt, 1000f);
                        hitInfo.damageTypes.Scale(DamageType.Explosion, 1000f);
                        hitInfo.damageTypes.Scale(DamageType.Stab, 1000f);
                        //hitInfo.damageTypes.Scale(DamageType., 1000f);
                    }
                    else
                    {
                        IemUtils.GLog("not found player");
                    }
                }
            }   

            public void PlayerImmortal(BaseCombatEntity entity, HitInfo hitInfo)
            {
                IemUtils.GLog("calling immortality check");
                if (entity as BasePlayer == null || hitInfo == null)
                {
                    IemUtils.GLog("was null");
                    return;
                }

                if (entity.ToPlayer() != null)
                {
                    IemUtils.GLog("scaling player damage to zero");
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

        #region EmbankStateManager

        public class EmbankStateManager : IemStateManager.StateManager
        {

            public List<IemObjectPlacement.CopyPastePlacement> partitions
                    = new List<IemObjectPlacement.CopyPastePlacement>();
            int wallheight = 7;
            int startheight = -30;
            int blockshigh = 15;

            private IemGameEmbankmentGame eg;

            public EmbankStateManager(IemStateManager.IStateMachine initialState,
                IemGameEmbankmentGame newEg)
                : base(initialState)
            {

                IemUtils.GLog("eg is " + newEg);
                eg = newEg;
            }

            private Timer walltimer;

            public class Created : IemStateManager.StateBase<Created>,
                IemStateManager.IStateMachine
            {
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
                private Timer gameLobbyWaitTimer;
                private Timer gameLobbyBannerTimer;


                public new void Enter(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;


                    EntitiesTakingDamage += gsm.eg.PlayerImmortal;

                    me.rust.RunServerCommand("env.time", "12");
                    for (int i = 0; i <= gsm.blockshigh; i++)
                    {
                        gsm.partitions.Add(new IemObjectPlacement.CopyPastePlacement(
                            "base_partition_wall", new Vector3(-500, gsm.startheight + (gsm.wallheight * i), 10)));
                    }

                    // gsm.CreateGameBanner("GAME LOBBY");
                    gameLobbyWaitTimer = me.timer.Once(gsm.eg.GameLobbyWait, () =>
                    {
                        gsm.ChangeState(PartitionedPeriod.Instance);
                    });
                    // gsm.CreateGameBanner("GAME LOBBY");
                    gameLobbyBannerTimer = me.timer.Once(gsm.eg.GameLobbyWait - 3, () =>
                    {
                        foreach (IemGameBase.IemTeam iemTeam in gsm.eg.Teams.Values)
                        {
                            foreach (IemGameBase.IemPlayer iemPlayer in iemTeam.Players.Values)
                            {
                                BasePlayer player = IemUtils.FindPlayerByID(iemPlayer.PlayerId);
                                CuiHelper.DestroyUi(player, "ShowIntroOverlay");

                            }
                        }
                    });

                    foreach (IemGameBase.IemTeam iemTeam in gsm.eg.Teams.Values)
                    {
                        foreach (IemGameBase.IemPlayer iemPlayer in iemTeam.Players.Values)
                        {
                            BasePlayer player = IemUtils.FindPlayerByID(iemPlayer.PlayerId);
                            IemUI.ShowGameTimer(player, gsm.eg.GameLobbyWait - 3, "starting in: ");

                            if (player.IsConnected())
                            {
                                if (player.IsDead())
                                {

                                    IemUtils.GLog("player is dead");
                                    player.Respawn();
                                }

                            }

                            player.EndSleeping();
                            gsm.eg.MovePlayerToTeamLocation(player, iemTeam.Location);

                            IemUtils.SetMetabolismValues(player);
                            IemUtils.ClearInventory(player);

                            IemUI.CreateGameBanner(player, "GAME LOBBY");
                            IemUI.ShowIntroOverlay(player,
                                $"The Game is Embankment\n" +
                                $"it is in 3 phases.\n" +
                                $"1) <color=green>The paritioned period</color> ({gsm.eg.PartitionWait}) a time for crafting and building\n" +
                                $"a wall separates the teams. Players are invulnerable\n" +
                                $"Once the first phase is finished, the wall comes down\n" +
                                $"2)  <color=blue>The main phase.</color> Players have 1 life. Attack and defend.\n" +
                                $"after {gsm.eg.MainPhaseWait} the games enters....\n" +
                                $"3) <color=red>Sudden Death</color>. All melle, weapon and explosion damage\n" +
                                $"is fatal. This period lasts for {gsm.eg.SuddenDeathPhaseWait}\n" +
                                $"if players remain after sudden death. The Winning team is the one\n" +
                                $"with the most total health points remaining.\n");

                            IemUtils.PlaySound(player);


                        }
                    }
                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;

                    gameLobbyWaitTimer.Destroy();
                    gameLobbyBannerTimer.Destroy();

                    EntitiesTakingDamage -= gsm.eg.PlayerImmortal;

                }
            }

            public class PartitionedPeriod : IemStateManager.StateBase<PartitionedPeriod>,
                IemStateManager.IStateMachine
            {

                private Timer partitionTimer;
                private Timer updatesTimer;
                private int countdown;

                //private DateTime startTime = DateTime.UtcNow;
                //TimeSpan breakDuration = TimeSpan.FromSeconds(15);

                public new void Enter(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    me.Subscribe(nameof(OnRunPlayerMetabolism));

                    // nullify any damage
                    EntitiesTakingDamage += gsm.eg.PlayerImmortal;

                    foreach (IemGameBase.IemTeam iemTeam in gsm.eg.Teams.Values)
                    {
                        foreach (IemGameBase.IemPlayer iemPlayer in iemTeam.Players.Values)
                        {
                            BasePlayer player = IemUtils.FindPlayerByID(iemPlayer.PlayerId);
                            //player.EndSleeping(); 

                            me.Kits?.Call("GiveKit", player, "embank_v1");
                            player.inventory.SendSnapshot();
                            IemUtils.PlaySound(player);
                            IemUI.CreateGameBanner(player, "Zone is Partitioned!");

                            IemUI.ShowGameTimer(player, gsm.eg.PartitionWait, "partion removed in: ");
                        }
                    }


                    updatesTimer = me.timer.Every(1f, () =>
                    {
                        gsm.Update();
                    });

                    countdown = gsm.eg.PartitionWait;

                    partitionTimer = me.timer.Once(
                        gsm.eg.PartitionWait, () =>
                        {
                            gsm.ChangeState(MainPhase.Instance);
                        });

                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;

                    partitionTimer?.Destroy();
                    updatesTimer?.Destroy();

                    // remove hook on damage for immortality
                    EntitiesTakingDamage -= gsm.eg.PlayerImmortal;
                }

                public new void Execute(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    gsm.eg.ShowGameStatus();

                }
            }

            public class MainPhase : IemStateManager.StateBase<MainPhase>,
                IemStateManager.IStateMachine
            {

                private Timer gameTimer;
                private Timer updatesTimer;
                private int countdown;

                public new void Enter(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    foreach (var partition in gsm.partitions)
                        partition?.Remove();
                    foreach (IemGameBase.IemTeam iemTeam in gsm.eg.Teams.Values)
                    {
                        foreach (IemGameBase.IemPlayer iemPlayer in iemTeam.Players.Values)
                        {
                            BasePlayer player = IemUtils.FindPlayerByID(iemPlayer.PlayerId);

                            IemUI.CreateGameBanner(player, "Main Phase, partition is removed!");
                            IemUI.ShowGameTimer(player, gsm.eg.PartitionWait, "Main Phase: ");
                        }
                    } 
                
                    // change the player status
                    PlayerDying += gsm.eg.PlayerDied;

                    // log the score
                    PlayerDying += gsm.eg.ScorePlayerKill;

                    gameTimer = me.timer.Once(gsm.eg.MainPhaseWait, () =>
                    {
                        gsm.ChangeState(SuddenDeath.Instance);
                    });

                    countdown = gsm.eg.MainPhaseWait;
                    updatesTimer = me.timer.Every(1f, () =>
                    {
                        gsm.Update();

                    });
                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;

                    updatesTimer?.Destroy();
                    gameTimer?.Destroy();

                    // don't set the player status
                    PlayerDying -= gsm.eg.PlayerDied;
                    // don't log the score
                    PlayerDying -= gsm.eg.ScorePlayerKill;
                    

                }

                public new void Execute(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;

                    gsm.eg.CheckGameState();
                    gsm.eg.ShowGameStatus();
                }
            }


            public class SuddenDeath : IemStateManager.StateBase<SuddenDeath>,
                IemStateManager.IStateMachine
            {
                private int countdown;

                //TODO this only works because its a team game
                private Timer suddenDeathTimer;
                private Timer updatesTimer;

                //private DateTime startTime = DateTime.UtcNow;
                //TimeSpan breakDuration = TimeSpan.FromSeconds(15);

                public new void Enter(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    
                    // change the player status
                    PlayerDying += gsm.eg.PlayerDied;
                    //log the score
                    PlayerDying += gsm.eg.ScorePlayerKill;

                    // any damage is fatal
                    EntitiesTakingDamage += gsm.eg.SuddenDeath;
                    // player can't be wounded in sudden death
                    EntitiesBeingWounded += gsm.eg.SuddenDeathWounded;

                    foreach (IemGameBase.IemTeam iemTeam in gsm.eg.Teams.Values)
                    {
                        foreach (IemGameBase.IemPlayer iemPlayer in iemTeam.Players.Values)
                        {
                            BasePlayer player = IemUtils.FindPlayerByID(iemPlayer.PlayerId);

                            IemUI.ShowGameTimer(player, gsm.eg.PartitionWait, "Sudden Death: ");
                        }
                    }


                    suddenDeathTimer = me.timer.Once(
                        gsm.eg.SuddenDeathPhaseWait, () =>
                        {
                            gsm.ChangeState(EmbankStateManager.GameComplete.Instance);
                        });
                    //}

                    IemUI.UpdateGameBanner("Sudden Death - all hits are fatal!!!", gsm.eg);


                    countdown = gsm.eg.SuddenDeathPhaseWait;
                    updatesTimer = me.timer.Every(1f, () =>
                    {
                        gsm.Update();

                    });
                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    IemUI.UpdateGameStatusBanner("", gsm.eg);
                    IemUI.UpdateGameBanner("", gsm.eg);

                    // set the player status
                    PlayerDying -= gsm.eg.PlayerDied;
                    // log the score
                    PlayerDying -= gsm.eg.ScorePlayerKill;

                    // any damage is fatal
                    EntitiesTakingDamage -= gsm.eg.SuddenDeath;
                    // player can't be wounded in sudden death
                    EntitiesBeingWounded -= gsm.eg.SuddenDeathWounded;
                    
                    suddenDeathTimer?.Destroy();
                    updatesTimer?.Destroy();
                }

                public new void Execute(IemStateManager.StateManager sm)
                {

                    EmbankStateManager gsm = (EmbankStateManager)sm;

                    gsm.eg.CheckGameState();
                    gsm.eg.ShowGameStatus();
                }

            }

            /// <summary>
            /// GameComplete is effectively a post game lobby for the players
            /// </summary>
            public class GameComplete : IemStateManager.StateBase<GameComplete>,
                IemStateManager.IStateMachine
            {
                private Timer completeTimer;
                private Timer resultsTimer;

                public new void Enter(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;

                    resultsTimer = IemUI.ShowResultsUiFor(
                        gsm.eg.Players.Select(d => d.Value).ToList(), gsm.eg, 8);

                    gsm.eg.EndGame();

                    completeTimer = me.timer.Once(10f, () =>
                    {
                        gsm.ChangeState(EmbankStateManager.CleanUp.Instance);
                    });
                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;

                    IemUI.UpdateGameStatusBanner("", gsm.eg);
                    completeTimer?.Destroy();
                }

            }

            public class GameCancelled : IemStateManager.StateBase<GameCancelled>,
                IemStateManager.IStateMachine
            {

                public new void Enter(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    
                    foreach (var partition in gsm.partitions)
                        partition?.Remove();

                    me.Unsubscribe(nameof(OnRunPlayerMetabolism));
                }
            }

            /// <summary>
            /// CleanUp is where the field is reset, should be the exit point of
            /// the GSM
            /// </summary>
            public class CleanUp : IemStateManager.StateBase<CleanUp>,
                IemStateManager.IStateMachine
            {
                private Timer completeTimer;

                public new void Enter(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;

                    gsm.eg.teamLobby.Destroy();

                    foreach (IemGameBase.IemTeam iemTeam in gsm.eg.Teams.Values)
                    {
                        foreach (IemGameBase.IemPlayer iemPlayer in iemTeam.Players.Values)
                        {
                            BasePlayer player = IemUtils.FindPlayerByID(iemPlayer.PlayerId);
                            me.IemUtils?.RestoreInventory(player, gsm.eg.GetGuid());


                            IemUtils.MovePlayerToTeamLocation(player,
                                iemPlayer.PreviousLocation);

                        }
                    }

                    foreach (var partition in gsm.partitions)
                        partition?.Remove();

                    me.Unsubscribe(nameof(OnRunPlayerMetabolism));
                }
            }
        }

        #endregion

        #region Oxide Hooks for game

        void OnRunPlayerMetabolism(PlayerMetabolism m, BaseCombatEntity entity)
        {
            var player = entity.ToPlayer();
            if (player == null)
                return;
            IemUtils.SetMetabolismNoNutrition(player);
        }

        static object NullFunc(BasePlayer entity, HitInfo hitInfo)
        {
            return null;
        }

        public delegate object EntitiesCanBeWounded(BasePlayer player, HitInfo hitInfo);
        private static EntitiesCanBeWounded EntitiesBeingWounded = NullFunc;

        private object CanBeWounded(BasePlayer player, HitInfo hitInfo)
        {

            //if (player == null || hitInfo == null) return true;

            return EntitiesBeingWounded(player, hitInfo);
        }

        public delegate void PlayerDeath(BasePlayer player, HitInfo hitInfo);

        private static PlayerDeath PlayerDying = OnEntityDeathXXX; // delegate { };

        // TODO how to shorthand a null delegate with a specific number of parameters
        static void OnEntityDeathXXX(BaseCombatEntity victim, HitInfo info)
        {
            IemUtils.GLog("player diedgjruioghui!!!");
        }

        void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
        {
            if (victim == null)
                return;

            if (victim.ToPlayer() != null)
            {
                IemUtils.GLog("player died - calling hooks");
                BasePlayer player = victim.ToPlayer();

                PlayerDying(player, info);

            }
        }

        public delegate void EntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo);

        private static EntityTakeDamage EntitiesTakingDamage = delegate { };

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity as BasePlayer == null || hitInfo == null) return;
            //IemUtils.GLog("player took damage - calling hooks");
            EntitiesTakingDamage(entity, hitInfo);
        }

        #endregion


        string ListGames()
        {
            string buff = "listing games in embank\n";
            foreach (var mygame in gm.games)
            {
                buff += "Game: " + mygame.Name;

            }
            return buff;
        }

        #region console

        [ConsoleCommand("embank")]
        void ccmdEvent222(ConsoleSystem.Arg arg)
        {
            if (!IemUtils.hasAccess(arg))
                return;
            switch (arg.Args[0].ToLower())
            {
                case "next":
                    NextPhase();
                    break;
                case "list_games":

                    SendReply(arg, me.ListGames());
                    break;
            }
        }

        // debuggin function to allow moving to next phase quickly
        void NextPhase()
        {
            if (game != null)
            {
                if (game.CurrentState == IemUtils.State.Running)
                {
                    IemGameEmbankmentGame teamgame = (IemGameEmbankmentGame)game;
                    if (teamgame.gsm.IsAny(EmbankStateManager.PartitionedPeriod.Instance))
                    {
                        teamgame.gsm.ChangeState(EmbankStateManager.MainPhase.Instance);
                    }
                    else if (teamgame.gsm.IsAny(EmbankStateManager.MainPhase.Instance))
                    {
                        teamgame.gsm.ChangeState(EmbankStateManager.SuddenDeath.Instance);
                    }
                    else if (teamgame.gsm.IsAny(EmbankStateManager.SuddenDeath.Instance))
                    {
                        teamgame.gsm.ChangeState(EmbankStateManager.GameComplete.Instance);
                    }
                }
            }
        }

        #endregion

        #region Configuration Data
        // Do not modify these values because this will not change anything, the values listed below are only used to create
        // the initial configuration file. If you wish changes to the configuration file you should edit 'GatherManager.json'
        // which is located in your server's config folder: <drive>:\...\server\<your_server_identity>\oxide\config\

        private bool configChanged;
        private static readonly Dictionary<string, object> DefaultDifficultyLevels 
            = new Dictionary<string, object>();

        public Dictionary<string, float> DifficultyLevels { get; private set; }

        private void LoadConfigValues()
        {

            // Plugin options
            var difficultyLevels = GetConfigValue("Options", "DifficultyLevels",
                DefaultDifficultyLevels);

            Puts("here");

            DifficultyLevels = new Dictionary<string, float>();
            foreach (var entry in difficultyLevels)
            {

                Puts("here2");
                float rate;
                if (!float.TryParse(entry.Value.ToString(), out rate)) continue;
                DifficultyLevels.Add(entry.Key, rate);
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

