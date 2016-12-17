//Requires: IemUtils
using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("Incursion Game Class", "tolland", "0.1.0")]
    public class IemGameBase : RustPlugin
    {
        #region header

        [PluginReference]
        IemUtils IemUtils;

        static IemGameBase iemGameBase;
        static IemGameBase me;

        public static Dictionary<string, GameManager> gameManagers = new Dictionary<string, GameManager>();

        static GameManager gm;

        #endregion

        #region boiler plate

        void Init()
        {
            iemGameBase = this;
            me = this;
            IemUtils.LogL("IemGame: Init complete");
        }

        Timer todTimer = null;

        void Loaded()
        {
            gm = new GameManager();
            IemGameBase.RegisterGameManager(gm);
            IemUtils.LogL("IemGame: Loaded complete");
            me.rust.RunServerCommand("env.time", "12");
            timer.Every(300, () =>
            {
                me.rust.RunServerCommand("env.time", "12");
            });

        }

        void Unload()
        {
            IemGameBase.UnregisterGameManager(gm);
            IemUtils.LogL("IemGame: unloaded complete");
        }

        void OnServerInitialized()
        {
            // IemGame.CreateGame("Base Game");

        }

        #endregion

        #region static methods

        public static void RegisterGameManager(GameManager gm)
        {
            //IemUtils.DLog("registering game type" + typeof(IemGame));
            if (!gameManagers.ContainsKey(gm.GetType().Name))
                gameManagers.Add(gm.GetType().Name, gm);
        }
        public static void UnregisterGameManager(GameManager gm)
        {
            //TODO remove from active
            foreach (var game in gm.games)
            {
                if (game.CurrentState == IemUtils.State.Before
                    || game.CurrentState == IemUtils.State.Running
                    || game.CurrentState == IemUtils.State.Ended
                    || game.CurrentState == IemUtils.State.Paused)
                {
                    game.CancelGame();
                }
            }
            //IemUtils.DLog("unregistering game type" + typeof(IemGame));
            if (gameManagers.ContainsKey(gm.GetType().Name))
                gameManagers.Remove(gm.GetType().Name);
        }
        
        public static Dictionary<string, GameManager> GetGameManagers()
        {
            IemUtils.DLog("listing game managers registered");

            return gameManagers;
        }

        public static void StartFromMenu(BasePlayer player, string game)
        {
            var newgame = gameManagers[game].SendPlayerToGameManager(player, "Easy");
        }

        public static void StartFromMenu(BasePlayer player, string game, string levelname)
        {
            //foreach (var currentgame in games)
            //{
            //    if (currentgame.Players.ContainsKey(player.UserIDString))
            //    {
            //        if (currentgame.CurrentState == IemUtils.State.Running)
            //        {
            //            if (!IemUI.Confirm(player, "You are currently in a game, you will " +
            //                "leave that game to join this game. Proceeed?"))
            //                return;
            //        }
            //    }
            //}
            var newgame = gameManagers[game].SendPlayerToGameManager(player, levelname);
            //newgame.StartGame();
        }

        #endregion

        #region game classes

        public class DifficultyMode
        {
            public string Description { get; set; }
            public string Name { get; set; }
            public List<GameLevelDefinition> GameLevelDefinitions { get; set; }
        }

        public class GameManager
        {
            public string Mode { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string TileImgUrl { get; set; }
            public bool Enabled { get; set; }
            public bool HasDifficultyModes { get; set; }
            public Dictionary<string, IemGameBase.DifficultyMode> difficultyModes { get; set; }
            public bool HasStats { get; set; }
            public bool HasGameStats { get; set; }

            public List<IemGame> games
                = new List<IemGame>();

            public GameManager()
            {
                Mode = "Team";
                Name = "Default Game Manager";
                TileImgUrl = "http://www.limepepper.co.uk/images/games-icon.png";
                Enabled = false;
                Description = "Here is where some information described the game\n" +
                    "should go.\n" +
                    "and you can write some stuff, and put it here.\n" +
                    "more text more text <color=red>text in red</color>";
                difficultyModes = new Dictionary<string, IemGameBase.DifficultyMode>();
                HasDifficultyModes = false;
                HasGameStats = false;
            }

            // player who is requesting the game, if its a solo, individual game
            public virtual IemGame SendPlayerToGameManager(BasePlayer player, string level = null)
            {
                IemGame currentGame = null;
                foreach (IemGameBase.IemGame game in games)
                {
                    if (game.CurrentState == IemUtils.State.Before)
                    {
                        me.Puts("existing game in before state");
                        if (game is IemSoloGame)
                        {
                            if (((IemSoloGame)game).UserIDString == player.UserIDString)
                            {
                                currentGame = game;
                            }
                        }
                        else if (game is IemTeamGame)
                        {
                            currentGame = game;
                            ((IemTeamGame)currentGame).SendPlayerToGame(player);
                        }
                    }
                    else if (game.CurrentState == IemUtils.State.Running
                     || game.CurrentState == IemUtils.State.Paused)
                    {
                        // TODO this exists for the purpose of handling 
                        // sending a player back to game they are already in
                        me.Puts("existing game running or paused");
                        if (game is IemSoloGame)
                        {
                            if (((IemSoloGame)game).UserIDString == player.UserIDString)
                            {
                                currentGame = game;
                            }
                        }
                        else if (game is IemTeamGame)
                        {
                            currentGame = game;
                        }
                    }
                    else if (game.CurrentState == IemUtils.State.Complete
                        || game.CurrentState == IemUtils.State.Cancelled)
                    {

                    }
                }

                if (currentGame == null)
                {
                    //get a new game from the GM implementation
                    currentGame = CreateGame(player, level);
                    RegisterGame(currentGame);
                }
                return currentGame;
            }

            /// <summary>
            /// override this in the implementation to return the concrete game instance
            /// </summary>
            /// <param name="player"></param>
            /// <returns></returns>
            //        public virtual IemGame CreateGame(BasePlayer player)
            //        {
            //            throw new Exception("not call this method in base CreateGame(" +
            //"BasePlayer player)");
            //            return null;
            //        }

            public virtual IemGame CreateGame(BasePlayer player,
                string level = null)
            {
                throw new Exception("not call this method in base CreateGame(" +
                    "BasePlayer player,                 string level)");
                return null;
            }

            public virtual void RegisterGame(IemGame game)
            {
                if (!games.Contains(game))
                    games.Add(game);
            }
        }

        public class IemGame : IemUtils.IIemGame
        {
            public string Mode = "Solo";
            public string Name { get; set; }
            public IemUtils.State CurrentState { get; set; }
            public static bool OnlyOneAtATime = false;
            public DateTime StartedTime { get; set; }
            public DateTime EndedTime { get; set; }
            [JsonIgnore]
            public Dictionary<string, IemUtils.IIemPlayer> Players { get; set; }
            public int MaxPlayers { get; set; }
            public int MinPlayers { get; set; }
            public bool HasDifficultyLevels { get; set; }
            public string difficultyLevel = "Easy";
            public double totalTime = 0;


            public bool HasGameLevels { get; set; }
            public List<GameLevel> gamelevels = new List<GameLevel>();
            public int level = -1;

            //track gamezones
            [JsonIgnore]
            public Dictionary<string, IemUtils.GameZone> gamezones = new Dictionary<string, IemUtils.GameZone>();

            public void AddGameZone(string name, Vector3 location, int radius)
            {
                gamezones.Add(
                           name,
                           new IemUtils.GameZone(name,
                           location, 2));
            }

            public void RemoveGameZone(string name)
            {
                if (gamezones.ContainsKey(name))
                {
                    gamezones[name].Remove();
                    gamezones.Remove(name);
                }

            }

            public Guid _guid;

            public Guid GetGuid()
            {
                return _guid;
            }

            public IemGame()
            {
                //IemUtils.DLog("calling parameterless ctor in IemGame");

                _guid = Guid.NewGuid();
                Players = new Dictionary<string, IemUtils.IIemPlayer>();
                Name = "Base Game";
                CurrentState = IemUtils.State.Before;
                MinPlayers = 1;
                MaxPlayers = 20;
            }


            public virtual bool CanStart()
            {
                if (CurrentState != IemUtils.State.Before)
                    return false;

                return true;
            }

            public virtual string CanStartCriteria()
            {
                return "";
            }


            public virtual bool StartGame()
            {

                IemUtils.DLog("calling StartGame in IemGameBase");
                if (!CanStart())
                    return false;
                CurrentState = IemUtils.State.Running;
                StartedTime = DateTime.Now;
                Interface.Oxide.CallHook("OnGameStarted", this);
                return true;
            }

            public virtual bool StartGame(BasePlayer player)
            {
                return StartGame();
            }

            /// <summary>
            /// This is probably misleadingly named
            /// basically, EndGame is used to mark the game complete in terms
            /// of stats and tracking for the Interfafce
            /// </summary>
            /// <returns></returns>
            public virtual bool EndGame()
            {
                CurrentState = IemUtils.State.Ended;
                EndedTime = DateTime.Now;
                CleanUp();

                foreach (var player in Players.Values)
                {
                    Interface.Oxide.CallHook("OnGameEnded", BasePlayer.Find(player.PlayerId), this);
                }

                return true;
            }

            //This means that the gsm has not more interest in the game
            //with respect to cleaning up
            public virtual bool MarkComplete()
            {
                CurrentState = IemUtils.State.Complete;
                return true;
            }

            /// <summary>
            /// this method is used to send a signal to the implementation
            /// to wrap things up.
            /// </summary>
            /// <returns></returns>
            public virtual bool CancelGame()
            {
                IemUtils.DLog("calling CancelGame in IemGame");
                CurrentState = IemUtils.State.Cancelled;
                CleanUp();
                Interface.Oxide.CallHook("OnGameCancelled", this);
                return true;
            }

            public virtual bool PauseGame()
            {
                CurrentState = IemUtils.State.Paused;
                return true;
            }

            public virtual bool CleanUp()
            {
                //CurrentState = IemUtils.State.Cancelled;
                foreach (var gamezone in gamezones.Values)
                {
                    gamezone.Remove();
                }
                foreach (var iemplayer in Players.Values)
                {
                    CuiHelper.DestroyUi(BasePlayer.Find(iemplayer.PlayerId), "ConfirmCancel");
                    if (IemUI.confirms.ContainsKey(iemplayer.PlayerId))
                    {
                        IemUI.confirms.Remove(iemplayer.PlayerId);
                    }
                }

                return true;
            }

            public virtual bool RestartLevel()
            {
                return true;
            }

            public virtual void RestoreBasePlayers()
            {
                foreach (var player in Players.Values)
                {
                    IemUtils.TeleportPlayerPosition(BasePlayer.Find(player.PlayerId),
                        player.PreviousLocation);
                    me.IemUtils.RestoreInventory(BasePlayer.Find(player.PlayerId), GetGuid());
                }
            }


            public IemUtils.IIemPlayer GetIemPlayerById(string id)
            {
                if (Players.ContainsKey(id))
                    return (IemUtils.IIemPlayer)Players[id];

                return null;
            }
        }

        public class IemSoloGame : IemGame
        {
            [JsonIgnore]
            public BasePlayer player;

            public string displayname;
            public string UserIDString;

            [JsonIgnore]
            public IemPlayer iemPlayer;

            public IemSoloGame(BasePlayer newPlayer)
            {
                player = newPlayer;
                UserIDString = player.UserIDString;
                displayname = player.displayName;
            }


            public virtual bool StartGame(IemGameBase.IemPlayer player)
            {

                //IemUtils.DLog("calling StartGame in IemGame");
                if (!CanStart())
                    return false;
                CurrentState = IemUtils.State.Running;
                StartedTime = DateTime.Now;
                return true;
            }
        }

        public class IemIndividualGame : IemGame
        {
            public virtual IemUtils.IIemTeam Winner()
            {
                return null;
            }
        }

        public class IemTeamGame : IemGame, IemUtils.IIemTeamGame
        {

            //public Dictionary<string, IemUtils.IIemPlayer> Players { get; set; }
            public Dictionary<string, IemUtils.IIemTeam> Teams { get; set; }
            public int MinTeams { get; set; }
            public int MaxTeams { get; set; }
            public int MinPlayersPerTeam { get; set; }
            public int MaxPlayersPerTeam { get; set; }

            public virtual IemUtils.IIemTeam Winner()
            {
                return null;
            }

            public IemTeamGame()
            {
                //IemUtils.DLog("calling parameterless in iemTeamGame");
                Teams = new Dictionary<string, IemUtils.IIemTeam>();
                MinTeams = 2;
                MaxTeams = 2;
                MaxPlayersPerTeam = 10;
                MinPlayersPerTeam = 1;
            }

            //TODO is this the same as AddPlayer to game??
            public IemUtils.IIemTeamPlayer SendPlayerToGame(BasePlayer player)
            {
                return AddPlayer(player);
            }

            public virtual IemUtils.IIemTeamPlayer AddPlayer(BasePlayer player)
            {
                return null;
            }


            public override bool CanStart()
            {
                //if (ForceStart)
                //    return true;

                int totalPlayers = 0;
                foreach (KeyValuePair<string, IemUtils.IIemTeam> team in Teams)
                {
                    if (team.Value.Players.Count < MinPlayersPerTeam)
                    {
                        IemUtils.DLog("too few players on team for Game MinPlayersPerTeam " + team.Value.Name);
                        return false;
                    }

                    if (team.Value.Players.Count > MaxPlayersPerTeam)
                    {
                        IemUtils.DLog("too many players on team  Game MaxPlayersPerTeam" + team.Value.Name);
                        return false;
                    }

                    if (team.Value.Players.Count < team.Value.MinPlayers)
                    {
                        IemUtils.DLog("too few players on team for Team MinPlayers " + team.Value.Name);
                        return false;
                    }

                    if (team.Value.Players.Count > team.Value.MaxPlayers)
                    {
                        IemUtils.DLog("too many players on team Team MaxPlayers " + team.Value.Name);
                        return false;
                    }

                    totalPlayers = totalPlayers + team.Value.Players.Count;
                }

                if (totalPlayers < MinPlayers)
                {
                    IemUtils.DLog("below min players");
                    return false;
                }

                if (totalPlayers > MaxPlayers)
                {
                    IemUtils.DLog("above max players");
                    return false;
                }


                return base.CanStart();

            }



            public string CanStartCriteria()
            {

                int totalPlayers = 0;
                string buff = "";

                foreach (KeyValuePair<string, IemUtils.IIemTeam> team in Teams)
                {
                    if (team.Value.Players.Count < team.Value.MinPlayers
                        || team.Value.Players.Count < MinPlayersPerTeam)
                    {
                        buff += "<color=" + team.Value.Color + ">" +
                            team.Value.Name + "</color>" +
                            "<color=red>(" + team.Value.Players.Count + "/" +
                              Math.Max(team.Value.MinPlayers, MinPlayersPerTeam) + ")</color> ";

                    }
                    else
                    {
                        buff += "<color=" + team.Value.Color + ">" +
                            team.Value.Name + "</color>" +
                            "<color=blue>(" + team.Value.Players.Count + "/" +
                              Math.Max(team.Value.MinPlayers, MinPlayersPerTeam) + ")</color> ";
                    }

                    if (team.Value.Players.Count > team.Value.MaxPlayers
                        || team.Value.Players.Count > MaxPlayersPerTeam)
                    {

                        buff += "<color=" + team.Value.Color + ">" +
                            team.Value.Name + "</color>" +
                        team.Value.Name + "<color=red>(" + team.Value.Players.Count + "/" +
                              Math.Min(MaxPlayersPerTeam, team.Value.MaxPlayers) + ")</color> ";
                    }
                    totalPlayers = totalPlayers + team.Value.Players.Count;
                }

                if (totalPlayers < MinPlayers)
                {
                    buff += "below min players for game: " + MinPlayers;
                }

                if (totalPlayers > MaxPlayers)
                {
                    buff += "above max players for game: " + MaxPlayers;
                }


                return buff;
            }


            public override bool StartGame()
            {
                base.StartGame();
                foreach (IemTeam iemTeam in Teams.Values)
                {
                    if (iemTeam.Players.Count > 0)
                    {
                        iemTeam.State = IemUtils.TeamState.Playing;
                    }
                    else
                    {
                        iemTeam.State = IemUtils.TeamState.Empty;
                    }
                }
                return true;
            }

            public IemTeam AddTeam(IemTeam team)
            {
                if (Teams.Count >= MaxTeams)
                {
                    IemUtils.DLog("returning null");
                    return null;
                }
                if (!Teams.ContainsKey(team.GetGuid().ToString()))
                    Teams.Add(team.GetGuid().ToString(), team);
                team.TeamGame = this;
                return team;
            }

            public bool RemoveTeam(IemTeam team)
            {
                if (!Teams.ContainsKey(team.GetGuid().ToString()))
                    return false;
                Teams.Remove(team.GetGuid().ToString());
                team.TeamGame = null;
                return true;
            }

            public IemUtils.IIemTeam GetTeamWithLeastPlayers()
            {
                IemUtils.IIemTeam team = null;
                int count = 999;
                foreach (var iemTeam in Teams.Values)
                {
                    if (iemTeam.Players.Count < count)
                    {
                        team = iemTeam;
                        count = iemTeam.Players.Count;
                    }
                }
                return team;
            }

            public IemUtils.IIemTeamPlayer GetIemPlayerById(string id)
            {
                if (Players.ContainsKey(id))
                    return (IemUtils.IIemTeamPlayer)Players[id];

                return null;
            }

        }



        public class IemTeam : IemUtils.IIemTeam
        {
            public string TeamId { get; set; }
            public string Name { get; set; }
            public string Color { get; set; }
            public Vector3 Location { get; set; }
            //TODO asymmetrical teams??
            public int MaxPlayers { get; set; }
            public int MinPlayers { get; set; }
            public Dictionary<string, IemUtils.IIemTeamPlayer> Players { get; set; }
            public IemUtils.IIemTeamGame TeamGame;
            public int Score { get; set; }
            public IemUtils.TeamState State { get; set; }
            Guid _guid;

            public Guid GetGuid()
            {
                return _guid;
            }


            public IemTeam()
            {
                Players = new Dictionary<string, IemUtils.IIemTeamPlayer>();
                Score = 0;
                State = IemUtils.TeamState.Empty;
                _guid = Guid.NewGuid();
            }

            public IemTeam(string teamid,
                string color,
                int minPlayers,
                int maxPlayers,
                string newName) : this()
            {
                TeamId = teamid;
                Color = color;
                MinPlayers = minPlayers;
                MaxPlayers = maxPlayers;
                Name = newName;
            }



            public void AddPlayer(IemUtils.IIemTeamPlayer player)
            {
                if (player.Team != null && player.Team != this)
                {
                    me.Puts("removing palyer from team " + player.Team.Name);
                    player.Team.RemovePlayer(player);
                }

                if (!Players.ContainsKey(player.PlayerId))
                    Players.Add(player.PlayerId, player);

                player.Team = this;
                player.TeamGame = TeamGame;

                if (!TeamGame.Players.ContainsKey(player.PlayerId))
                    TeamGame.Players.Add(player.PlayerId, player);

                if (Players.Count > 0)
                    State = IemUtils.TeamState.Before;
            }

            public void RemovePlayer(IemUtils.IIemTeamPlayer player)
            {
                if (Players.ContainsKey(player.PlayerId))
                    Players.Remove(player.PlayerId);
                player.Team = null;

                if (Players.Count == 0)
                    State = IemUtils.TeamState.Empty;

            }
        }




        public class IemPlayer : MonoBehaviour, IemUtils.IIemTeamPlayer
        {
            public string PlayerId { get; set; }
            public string Name { get; set; }
            public IemUtils.IIemTeam Team { get; set; }
            public IemUtils.IIemTeamGame TeamGame { get; set; }
            public int Score { get; set; }
            public IemUtils.PlayerState PlayerState { get; set; }
            Guid _guid;

            public Guid GetGuid()
            {
                return _guid;
            }
            public Vector3 PreviousLocation { get; set; }
            public Vector3 PreviousRotation { get; set; }

            public IemPlayer(BasePlayer player)
            {
                _guid = Guid.NewGuid();
                PlayerId = player.UserIDString;
                Score = 0;
                Name = player.displayName;
                PlayerState = IemUtils.PlayerState.Alive;
                PreviousLocation = player.transform.position;
                PreviousRotation = player.GetNetworkRotation();
            }

            //TODO this is stupid
            public BasePlayer AsBasePlayer()
            {
                return BasePlayer.FindByID(ulong.Parse(PlayerId));
            }
        }

        #endregion

        #region Game Level Classes


        public class GameLevelAccuracy
        {
            public IemGame Game { get; set; }
            public int ShotsFired { get; set; }
            public int ShotsHit { get; set; }
            public int BullsEyes { get; set; }
            private GameLevel gameLevel;

            public GameLevelAccuracy(GameLevel gameLevel)
            {
                this.gameLevel = gameLevel;
            }

            public double GetAccuracy()
            {
                return ((float)ShotsHit / (float)ShotsFired);
            }

            public double GetShotRate()
            {
                return (float)ShotsFired / gameLevel.LevelTime();
            }

            public double GetHitRate()
            {
                return (float)ShotsHit / gameLevel.LevelTime();
            }

            public string GetAccuracyAsString()
            {
                me.Puts("fired is " + ShotsFired);
                me.Puts("hit is " + ShotsHit);
                me.Puts("ratio is " + ((float)ShotsHit / (float)ShotsFired));
                string percentile = (100 * (float)ShotsHit / (float)ShotsFired).ToString("0.00") + " %\n";
                if (gameLevel.Ended)
                {
                    percentile += "shots/sec " +
                        ((float)ShotsFired / gameLevel.LevelTime()).ToString("0.00") + "\n";
                    percentile += "hits/sec "
                        + ((float)ShotsHit / gameLevel.LevelTime()).ToString("0.00")
                        + "\n";
                    percentile += "level time "
                        + (gameLevel.LevelTime()).ToString("0.00")
                        + " \n";
                }

                return percentile;
            }
        }


        public class GameLevelDefinition
        {
            // number of targets required to complete this level
            public int Targets { get; set; }
            // level timer starting amount
            public int Timer { get; set; }
            // if min accuracy is required to complete level
            public float Accuracy { get; set; }
            // kit that the player will be initialized with
            public string kitname { get; set; }
            // if this is true, any weapons on the belt will be filled with ammo to max
            public bool fillmagazines { get; set; }

        }

        public class GameLevel
        {
            [JsonIgnore]
            public IemGame Game { get; set; }

            [JsonIgnore]
            public BasePlayer Player { get; set; }

            [JsonIgnore]
            public IemUtils.ReturnZone returnZone { get; set; }

            // the definition that this level was made from
            internal GameLevelDefinition gameLevelDefinition { get; set; }
            public GameLevelAccuracy accuracy { get; set; }
            public int Timer { get; set; }
            private bool started = false;
            private bool ended = false;
            private DateTime startTime;
            public DateTime StartTime { get { return startTime; } }
            private DateTime endTime;
            public DateTime EndTime { get { return endTime; } }
            public bool Started { get { return started; } }
            public bool Ended { get { return ended; } }

            Guid _guid;


            public Guid GetGuid()
            {
                return _guid;
            }


            public GameLevel()
            {
                _guid = Guid.NewGuid();
                accuracy = new GameLevelAccuracy(this);
            }

            public bool Start()
            {
                if (started || ended)
                {
                    return false;
                }
                else
                {
                    started = true;
                    startTime = DateTime.Now;
                    //       me.Puts("in GameLevel Start(), time is " + startTime);
                    return true;
                }
            }

            public bool End()
            {
                if (started && !ended)
                {
                    endTime = DateTime.Now;
                    ended = true;
                    //      me.Puts("in GameLevel End(), endtime is " + endTime);
                    //     me.Puts("in GameLevel End(), startTime is " + startTime);
                    Game.totalTime += LevelTime();
                    //     me.Puts("LevelTime(), time is " + LevelTime());
                    Interface.Oxide.CallHook("OnGameLevelEnded", this);
                    return ended;
                }
                else
                {
                    return false;
                }
            }

            public void Reset()
            {
                accuracy = new GameLevelAccuracy(this);
                // endTime = null;
                started = false;
                ended = false;
                me.Puts("in GameLevel Reset(), resetting started and ended");

            }

            public double LevelTime()
            {
                return (EndTime - StartTime).TotalSeconds;
            }
        }

        #endregion

        #region game object access methods

        void GetActiveGames()
        {

            //   foreach (var game in games)
            //     {
            //SendConsoleMessage(arg, " - game state is " +
            //                        game.CurrentState + game.StartedTime);
            //    }
        }

        public static IemGame FindActiveGameForPlayer(BasePlayer player)
        {
            foreach (var game_manager in gameManagers.Values)
            {
                foreach (var game in game_manager.games)
                {
                    if (game.CurrentState == IemUtils.State.Before
                        || game.CurrentState == IemUtils.State.Paused
                        || game.CurrentState == IemUtils.State.Running)
                    {
                        foreach (IemPlayer iemPlayer in game.Players.Values)
                        {
                            if (iemPlayer.AsBasePlayer() == player)
                            {
                                //maybe want to do something different if the player is dead
                                // and is out of the running game
                                if (iemPlayer.PlayerState == IemUtils.PlayerState.Dead)
                                {
                                }
                                return game;
                            }

                        }
                    }
                }
            }
            return null;
        }

        void ListActiveGames(ConsoleSystem.Arg arg)
        {
            foreach (var game_manager in gameManagers.Values)
            {
                SendConsoleMessage(arg, ">>>> game manager for " + game_manager.Name + "<<<<<");
                SendConsoleMessage(arg, "game count is " + game_manager.games.Count);
                foreach (var game in game_manager.games)
                {
                    var messages = new List<string>();
                    messages.Add("active game: " + game.Name);
                    messages.Add(" - status: " +
                                  game.CurrentState);
                    messages.Add(" - type: " +
                                  game.GetType());

                    if (game.CurrentState != IemUtils.State.Before)
                        messages.Add(" - started: " +
                                  game.StartedTime);

                    if (game.CurrentState == IemUtils.State.Complete ||
                        game.CurrentState == IemUtils.State.Cancelled)
                        messages.Add(" - ended: " +
                                  game.EndedTime);

                    if (game is IemTeamGame)
                    {
                        IemTeamGame teamgame = (IemTeamGame)game;
                        messages.Add(" - teams are:");
                        foreach (IemTeam iemTeam in teamgame.Teams.Values)
                        {
                            messages.Add("---* " + iemTeam.Name + " (" + iemTeam.State + ")" + ":");

                            foreach (IemPlayer iemPlayer in iemTeam.Players.Values)
                            {
                                messages.Add("---*--# " + iemPlayer.PlayerId +
                                    ":" + iemPlayer.Name + " (" + iemPlayer.PlayerState + ")");
                            }
                        }
                    }
                    string buff = "(";
                    foreach (IemPlayer iemPlayer in game.Players.Values)
                    {
                        buff += "" +
                            ":" + iemPlayer.Name + " [" + iemPlayer.PlayerState + "],";
                    }
                    messages.Add(buff + ")");
                    messages.Add("");

                    SendConsoleMessage(arg, messages);
                }
            }
        }

        void ListAvailableGames(ConsoleSystem.Arg arg)
        {
            foreach (var type in gameManagers)
            {
                SendConsoleMessage(arg, " - game type is " +
                               type.Key + ":" + type.Value);
            }
        }

        List<IemUtils.IIemGame> FindActiveGameFromSubString(string substring)
        {
            IemUtils.DLog("find games");
            List<IemUtils.IIemGame> tempGames = new List<IemUtils.IIemGame>();

            foreach (var game_manager in gameManagers.Values)
            {
                foreach (var game in game_manager.games)
                {
                    IemUtils.DLog("game name is " + game.Name);
                    IemUtils.DLog("substring is " + substring);
                    if (game.Name.ToLower().StartsWith(
                        (substring.ToLower())))
                        tempGames.Add(game);
                }
            }
            return tempGames;
        }

        #endregion

        #region console

        void SendConsoleMessage(ConsoleSystem.Arg arg, string message)
        {

            SendReply(arg, message);
        }

        void SendConsoleMessage(ConsoleSystem.Arg arg, List<string> messages)
        {
            foreach (var message in messages)
            {
                SendConsoleMessage(arg, message);
            }
        }

        [ConsoleCommand("iem.game")]
        void ccmdEvent222(ConsoleSystem.Arg arg)
        {
            if (!IemUtils.hasAccess(arg))
                return;
            switch (arg.Args[0].ToLower())
            {
                case "list_active":
                    ListActiveGames(arg);
                    break;
                case "list_available":
                    ListAvailableGames(arg);
                    break;
                case "cancel":
                    foreach (IemGame iemGame in FindActiveGameFromSubString((string)arg.Args[1]))
                    {
                        iemGame.CancelGame();
                    }
                    break;
            }
        }

        #endregion
    }
}