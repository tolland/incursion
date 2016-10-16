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
        Plugin ScreenTimer;

        [PluginReference]
        IemObjectPlacement IemObjectPlacement;

        [PluginReference]
        IncursionHoldingArea IncursionHoldingArea;

        [PluginReference]
        IemGameBase IemGameBase;

        static IemGameEmbankment me;
        static EMGameManager gm;
        public static IemUtils.IIemGame game;

        //static Dictionary<string, IemGameEmbankmentGame> games
        //    = new Dictionary<string, IemGameEmbankmentGame>();
        static List<IemGameEmbankmentGame> games
            = new List<IemGameEmbankmentGame>();

        #endregion

        #region boiler plate

        void Init()
        {
            me = this;
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
            Config["Enabled"] = false;
            Config.Save();
        }

        void OnGameEnded(IemGameBase.IemGame endedGame)
        {
            IemUtils.GLog("Hook called to indicate game ended");
        }

        void InitGame(IemGameBase.IemTeamGame teamgame)
        {

            IemGameBase.IemTeam team1 = teamgame.AddTeam(
                new IemGameBase.IemTeam("team_1", "blue", 1, 20, "Blue Bandits"));

            team1.Location = new Vector3(90, 23, 129);

            IemGameBase.IemTeam team2 = teamgame.AddTeam(
                new IemGameBase.IemTeam("team_2", "red", 1, 20, "Red Devils"));
            team2.Location = new Vector3(120, 24, 96);
        }  
         
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

        #region IemGameEmbankmentGame


        public class EMGameManager : IemGameBase.GameManager
        {
            public EMGameManager() : base()
            {

                Enabled = true;
                Mode = "Team";
                Name = "Embankment";
                //  TileImgUrl = "http://www.limepepper.co.uk/images/PNG_Example.png";
            }

            public override IemGameBase.IemGame SendPlayerToGameManager(BasePlayer player)
            {
                bool foundactive = false;
                IemGameEmbankmentGame newGame = null;
                foreach (var mygame in games)
                {
                    if(mygame.CurrentState== IemUtils.State.Before||
                        mygame.CurrentState == IemUtils.State.Paused||
                        mygame.CurrentState == IemUtils.State.Running)
                    {
                        foundactive = true;
                        newGame = mygame;
                    }
                }
                if (foundactive)
                {
                    me.Puts("in the tp game manager, found existing game for player");
                    me.Puts("game: "+newGame.CurrentState);
                }
                else
                {
                    me.Puts("in the tp game manager, creating new game");
                    newGame = new IemGameEmbankmentGame();
                    games.Add(newGame);
                    
                }
                newGame.Players[player.UserIDString] = new IemEmbankmentPlayer(player, newGame.teamLobby);
                IemUI.ShowTeamUiForPlayer(player, newGame);
                return newGame;
            }
        }

        public class IemEmbankmentPlayer : IemGameBase.IemPlayer
        {
            public IemEmbankmentPlayer(BasePlayer player) : base(player)
            {
            }

            public IemEmbankmentPlayer(BasePlayer player, IncursionHoldingArea.TeamSelectLobby teamLobby) : base(player)
            {
                IemUtils.TeleportPlayerPosition(player, teamLobby.location);
            } 
        }

        public class IemGameEmbankmentGame : IemGameBase.IemTeamGame
        {
            public EmbankStateManager gsm;
            public float GameLobbyWait = 12;
            public int PartitionWait = 20;
            public int MainPhaseWait = 20;
            public int SuddenDeathPhaseWait = 20;
            public IncursionHoldingArea.TeamSelectLobby teamLobby;

            public IemGameEmbankmentGame() 
            {
                IemUtils.GLog("calling parameterless ctor in IemGameEmbankmentGame");
                Name = "Embankment";
                IemUtils.GLog("setting gamename is " + this.Name);
                OnlyOneAtATime = true;
                Mode = "Team";
                MinPlayersPerTeam = 0;

                //adds some teams to the game, sets the team locations
                me.InitGame(this); 

                //create the team select lobby
                teamLobby = new IncursionHoldingArea.TeamSelectLobby("teamlobby_v1", this);


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
                else
                {
                    IemUtils.GLog("gsm already created");
                }
                return true;
            }

            public override bool EndGame()
            {
                IemUtils.GLog("calling EndGame in IemGameEmbankmentGame");
                teamLobby.Destroy();
                base.EndGame();
                return true;
            }

            public override bool CancelGame()
            {

                IemUtils.DDLog("listing in CancelGame");
                //((IemGameEmbankmentGame)game).gsm.partition?.List();

                IemUtils.GLog("calling CancelGame in IemGameEmbankmentGame");
                gsm?.ChangeState(EmbankStateManager.GameCancelled.Instance);

                base.CancelGame();
                return true;
            }

            public override bool CleanUp()
            {
                IemUtils.GLog("calling cleanup in IemGameEmbankmentGame");
                gsm.ChangeState(EmbankStateManager.CleanUp.Instance);

                base.CleanUp();
                return true;
            }

            public bool MovePlayerToTeamLocation(BasePlayer player, Vector3 location)
            {

                IemUtils.GLog("moving players to game");

                if (!IemUtils.CheckPointNearToLocation(player.transform.position, location, 2))
                    IemUtils.MovePlayerTo(player, location);
                //IemUtils.TeleportPlayerPosition(player, location);

                return true;
            }

            public override IemUtils.IIemTeam Winner()
            {
                IemUtils.IIemTeam team = null;
                IemUtils.GLog("here");
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
                    //foreach (IemGameBase.IemPlayer iemPlayer in iemTeam.Players.Values)
                    //{
                    //    BasePlayer player = IemUtils.FindPlayerByID(iemPlayer.PlayerId);
                    //    IemUI.CreateGameStatusBanner(player, "time is ");
                    //}
                }
                foreach (IemGameBase.IemTeam iemTeam in Teams.Values)
                {

                    foreach (IemGameBase.IemPlayer iemPlayer in iemTeam.Players.Values)
                    {
                        BasePlayer player = IemUtils.FindPlayerByID(iemPlayer.PlayerId);
                        IemUI.CreateGameStatusBanner(player, status);
                    }
                }
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

                    //       PlayerDying(player, info);
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

            public bool SuddenDeathWounded(BaseCombatEntity entity, HitInfo hitInfo)
            {
                return false;
            }


            public void SuddenDeath(BaseCombatEntity entity, HitInfo hitInfo)
            {
                if (entity as BasePlayer == null || hitInfo == null) return;
                //IemUtils.GLog("calling entities any damage");

                if (entity.ToPlayer() != null)
                {
                    IemUtils.GLog("player died");
                    BasePlayer player = entity.ToPlayer();

                    //       PlayerDying(player, info);
                    //  GetIemPlayerById(entity.UserIDString).PlayerState =
                    //    IemGameBase.PlayerState.Dead;
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

                    //       PlayerDying(player, info);
                    //  GetIemPlayerById(entity.UserIDString).PlayerState =
                    //    IemGameBase.PlayerState.Dead;
                    if (GetIemPlayerById(player.UserIDString) != null)
                    {
                        // TODO probably better way to scale this damage
                        IemUtils.GLog("scaling damage");
                        //hitInfo.damageTypes.Scale(DamageType.Bullet, 0f);
                        IemUtils.NullifyDamage(ref hitInfo);
                    }
                    else
                    {
                        IemUtils.GLog("not found player");
                    }
                }
            }

        }

        #endregion

        #region EmbankStateManager

        public class EmbankStateManager : IemStateManager.StateManager
        {

            public IemObjectPlacement.CopyPastePlacement partition;
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

                    gsm.partition = new IemObjectPlacement.CopyPastePlacement(
                        "partition2_1500_7777_3123");

                    IemUtils.GLog("partition2_1500_7777_3123 " + gsm.partition);


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
                            //IemOnHooks.OneLife anyDamage
                            //= player.GetComponent<IemOnHooks.OneLife>();

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

                //    iemEmbankment.Subscribe(nameof(OnRunPlayerMetabolism));
                //    // don't handle spawn location until required
                //    iemEmbankment.Subscribe(nameof(OnPlayerRespawn));

                //    gsm.eg.MovePlayersToGame();

                //    foreach (IncursionEventGame.EventPlayer eventPlayer in gsm.eg.gamePlayers.Values)
                //    {
                //        eventPlayer.psm.ChangeState(IncursionEventGame.PlayerInGame.Instance);

                //        IncursionUI.ShowGameBanner(eventPlayer.player,
                //            gsm.eg.GameIntroBanner);
                //    }

                //    gsm.CreateGameBanner("GAME LOBBY");
                //    warningTimer = iemEmbankment.timer.Once(gsm.eg.GameLobbyWait, () =>
                //    {
                //        gsm.ChangeState(PartitionedPeriod.Instance);
                //    });
                //}

                public new void Exit(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;

                    gameLobbyWaitTimer.Destroy();
                    gameLobbyBannerTimer.Destroy();


                    EntitiesTakingDamage -= gsm.eg.PlayerImmortal;

                    //    foreach (BasePlayer player in BasePlayer.activePlayerList)
                    //    {
                    //        IncursionEventGame.EventPlayer eventPlayer
                    //            = IncursionEventGame.EventPlayer.GetEventPlayer(player);
                    //        //eventPlayer.psm.eg = ((IncursionEvents.EventStateManager)esm).eg;
                    //        IncursionUI.HideGameBanner(player);
                    //    }
                    //    iemEmbankment.Unsubscribe(nameof(OnRunPlayerMetabolism));

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

                    //    gsm.CreateGameBanner("You have " + gsm.GetEventGame().PartitionedPeriodLength +
                    //        " minutes to build/craft weapons");

                    // nullify any damage
                    EntitiesTakingDamage += gsm.eg.PlayerImmortal;

                    me.ScreenTimer?.Call("CreateTimer", gsm.eg.PartitionWait);

                    foreach (IemGameBase.IemTeam iemTeam in gsm.eg.Teams.Values)
                    {
                        foreach (IemGameBase.IemPlayer iemPlayer in iemTeam.Players.Values)
                        {
                            BasePlayer player = IemUtils.FindPlayerByID(iemPlayer.PlayerId);
                            //player.EndSleeping(); 

                            me.Kits?.Call("GiveKit", player, "autokit");
                            player.inventory.SendSnapshot();
                            IemUtils.PlaySound(player);
                            IemUI.CreateGameBanner(player, "Zone is Partitioned!");

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

                    me.ScreenTimer?.Call("DestroyUI");

                    // remove hook on damage for immortality
                    EntitiesTakingDamage -= gsm.eg.PlayerImmortal;
                }

                public new void Execute(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    gsm.eg.ShowGameStatus();
                    gsm.eg.ShowGameTimer(countdown--);

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
                    gsm.partition.Remove();
                    foreach (IemGameBase.IemTeam iemTeam in gsm.eg.Teams.Values)
                    {
                        foreach (IemGameBase.IemPlayer iemPlayer in iemTeam.Players.Values)
                        {
                            BasePlayer player = IemUtils.FindPlayerByID(iemPlayer.PlayerId);
                            //gsm.eg.MovePlayerToTeamLocation(player, iemTeam.Location);
                            //IemOnHooks.OneLife anyDamage
                            //= player.GetComponent<IemOnHooks.OneLife>();

                            IemUI.CreateGameBanner(player, "Main Phase, partition is removed!");
                        }
                    }

                    me.ScreenTimer?.Call("CreateTimer", gsm.eg.MainPhaseWait);

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

                    me.ScreenTimer?.Call("DestroyUI");

                }

                public new void Execute(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;

                    gsm.eg.CheckGameState();

                    gsm.eg.ShowGameStatus();
                    gsm.eg.ShowGameTimer(countdown--);
                }
            }


            public class SuddenDeath : IemStateManager.StateBase<SuddenDeath>,
                IemStateManager.IStateMachine
            {
                private int countdown;

                //private Timer warningTimer;
                //private Timer finalWarningTimer;
                private Timer suddenDeathTimer;
                private Timer updatesTimer;

                //private DateTime startTime = DateTime.UtcNow;
                //TimeSpan breakDuration = TimeSpan.FromSeconds(15);

                public new void Enter(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;

                    me.ScreenTimer?.Call("CreateTimer", gsm.eg.SuddenDeathPhaseWait);

                    // change the player status
                    PlayerDying += gsm.eg.PlayerDied;
                    //log the score
                    PlayerDying += gsm.eg.ScorePlayerKill;

                    // any damage is fatal
                    EntitiesTakingDamage += gsm.eg.SuddenDeath;
                    // player can't be wounded in sudden death
                    EntitiesBeingWounded += gsm.eg.SuddenDeathWounded;


                    //    iemEmbankment.Subscribe(nameof(OnRunPlayerMetabolism));

                    suddenDeathTimer = me.timer.Once(
                        gsm.eg.SuddenDeathPhaseWait, () =>
                        {
                            gsm.ChangeState(EmbankStateManager.GameComplete.Instance);
                        });
                    //}

                    foreach (IemGameBase.IemTeam iemTeam in gsm.eg.Teams.Values)
                    {
                        foreach (IemGameBase.IemPlayer iemPlayer in iemTeam.Players.Values)
                        {
                            BasePlayer player = IemUtils.FindPlayerByID(iemPlayer.PlayerId);
                            //gsm.eg.MovePlayerToTeamLocation(player, iemTeam.Location);
                            //IemOnHooks.OneLife anyDamage
                            //= player.GetComponent<IemOnHooks.OneLife>();
                            //IemUtils.SetMetabolismValues(player);

                            IemUI.CreateGameBanner(player, "Sudden Death - all hits are fatal!!!");
                        }
                    }

                    countdown = gsm.eg.SuddenDeathPhaseWait;
                    updatesTimer = me.timer.Every(1f, () =>
                    {
                        gsm.Update();

                    });
                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;

                    // set the player status
                    PlayerDying -= gsm.eg.PlayerDied;
                    // log the score
                    PlayerDying -= gsm.eg.ScorePlayerKill;

                    // any damage is fatal
                    EntitiesTakingDamage -= gsm.eg.SuddenDeath;
                    // player can't be wounded in sudden death
                    EntitiesBeingWounded -= gsm.eg.SuddenDeathWounded;

                    me.ScreenTimer?.Call("DestroyUI");

                    suddenDeathTimer?.Destroy();
                    updatesTimer?.Destroy();
                }

                public new void Execute(IemStateManager.StateManager sm)
                {

                    EmbankStateManager gsm = (EmbankStateManager)sm;

                    gsm.eg.CheckGameState();
                    gsm.eg.ShowGameStatus();
                    gsm.eg.ShowGameTimer(countdown--);
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

                    completeTimer = me.timer.Once(10f, () =>
                    {
                        IemUtils.GLog("calling game complete on the event manager");
                        gsm.eg.EndGame();

                    });

                }

                public new void Exit(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    completeTimer?.Destroy();
                    // gsm.CreateGameBanner("");
                    //gsm.eg.RemoveGameResultUI();
                    //iemEmbankment.Unsubscribe(nameof(OnRunPlayerMetabolism));
                }

            }

            public class GameCancelled : IemStateManager.StateBase<GameCancelled>,
                IemStateManager.IStateMachine
            {

                public new void Enter(IemStateManager.StateManager sm)
                {
                    EmbankStateManager gsm = (EmbankStateManager)sm;
                    IemUtils.DDLog("list the wall in cancel game");


                    //gsm.partition.List();

                    IemUtils.DDLog("removing the wall in cancel game");

                    gsm.partition.Remove();

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

                    IemUtils.DDLog("removing the wall in cleanup game");
                    gsm.partition?.Remove();

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

        #region console

        [ConsoleCommand("embank")]
        void ccmdEvent222(ConsoleSystem.Arg arg)
        {
            if (!IemUtils.hasAccess(arg))
                return;
            switch (arg.Args[0].ToLower())
            {
                case "create_start":
               //     InitGame(arg.Player());
                    break;
                case "next":
                    NextPhase();
                    break;
                case "autostart":
                    Config["Enabled"] = true;
               //     InitGame(arg.Player());
                    break;
            }
        }
        #endregion
    }
}

