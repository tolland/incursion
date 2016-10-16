//Requires: IemUtils

using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using UnityEngine;

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

        static List<IemUtils.IIemGame> games = new List<IemUtils.IIemGame>();
        static Dictionary<string, GameManager> gameManagers = new Dictionary<string, GameManager>();


        static GameManager gm;

        #endregion

        #region boiler plate

        void Init()
        {
            iemGameBase = this;
            me = this;
            IemUtils.LogL("IemGame: Init complete");
        }

        void Loaded()
        {
            gm = new GameManager();
            IemGameBase.RegisterGameManager(gm);
            IemUtils.LogL("IemGame: Loaded complete");
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
            //IemUtils.DLog("unregistering game type" + typeof(IemGame));
            if (gameManagers.ContainsKey(gm.GetType().Name))
                gameManagers.Remove(gm.GetType().Name);
        }



        public static Dictionary<string, GameManager> GetGameManagers()
        {
            IemUtils.DLog("listing game managers registered");

            return gameManagers;
        }

        static bool RemoveFromActive<IemGame>()
        {
            foreach (var game in games)
            {
                //IemUtils.DLog("removing from active");
                if (game.GetType() == typeof(IemGame))
                {
                    game.CancelGame();
                    game.CleanUp();
                    games.Remove(game);
                    return true;
                }
            }
            return false;
        }

        static bool RemoveFromActive(string gametype)
        {
            foreach (var game in games)
            {
                if (game.GetType() == typeof(IemGame))
                {
                    games.Remove(game);
                    return true;
                }
            }
            return false;
        }

        public static void StartFromMenu(BasePlayer player, string game)
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


            var newgame = gameManagers[game].SendPlayerToGameManager(player);
            //newgame.StartGame();
        }

        #endregion

        #region game classes

        public class GameManager
        {
            public string Mode { get; set; }
            public string Name { get; set; }
            public string TileImgUrl { get; set; }
            public bool Enabled { get; set; }

            public GameManager()
            {
                Mode = "Team";
                Name = "Default Game Manager";
                TileImgUrl = "http://www.limepepper.co.uk/images/games-icon.png";
                Enabled = false;
            }

            // player who is starting the game, if its a solo, individual game
            public virtual IemGame SendPlayerToGameManager(BasePlayer player)
            {
                IemGame game = SendPlayerToGameManager(player);
                RegisterGame(game);
                return game;
            }

            public virtual void RegisterGame(IemGame game)
            {
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
            public Dictionary<string, IemUtils.IIemPlayer> Players { get; set; }
            public int MaxPlayers { get; set; }
            public int MinPlayers { get; set; }

            Guid _guid;

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

            public virtual bool StartGame() 
            {

                IemUtils.DLog("calling StartGame in IemGameBase");
                if (!CanStart())
                    return false;
                CurrentState = IemUtils.State.Running;
                StartedTime = DateTime.Now;
                return true;
            }

            public virtual bool StartGame(BasePlayer player)
            {
                return StartGame();
            }

            public virtual bool EndGame()
            {
                CurrentState = IemUtils.State.Complete;
                EndedTime = DateTime.Now;
                Interface.Oxide.CallHook("OnGameEnded", this);
                return true;
            }

            public virtual bool CancelGame()
            {
                //IemUtils.DLog("calling CancelGame in IemGame");
                CurrentState = IemUtils.State.Cancelled;
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
                //CurrentState = State.Paused;
                return true;
            }
        }

        public class IemSoloGame : IemGame
        {

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

            }

            public void RemovePlayer(IemUtils.IIemTeamPlayer player)
            {
                if (Players.ContainsKey(player.PlayerId))
                    Players.Remove(player.PlayerId);
                player.Team = null;
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
            public Vector3 previousLocation;
            public Vector3 previousRotation;

            public IemPlayer(BasePlayer player)
            {
                _guid = Guid.NewGuid();
                PlayerId = player.UserIDString;
                Score = 0;
                Name = player.displayName;
                PlayerState = IemUtils.PlayerState.Alive;
                previousLocation = player.transform.position;
                previousRotation = player.GetNetworkRotation();
            }

            //TODO this is stupid
            public BasePlayer AsBasePlayer()
            {
                return BasePlayer.FindByID(ulong.Parse(PlayerId));
            }
        }

        #endregion

        #region game object access methods

        void GetActiveGames()
        {

            foreach (var game in games)
            {
                //SendConsoleMessage(arg, " - game state is " +
                //                        game.CurrentState + game.StartedTime);
            }
        }


        void ListActiveGames(ConsoleSystem.Arg arg)
        {
            SendConsoleMessage(arg, "game count is " + games.Count);
            foreach (var game in games)
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
                messages.Add("");

                SendConsoleMessage(arg, messages);
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
            foreach (var game in games)
            {
                IemUtils.DLog("game name is " + game.Name);
                IemUtils.DLog("substring is " + substring);
                if (game.Name.ToLower().StartsWith(
                    (substring.ToLower())))
                    tempGames.Add(game);
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