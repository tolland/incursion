//Requires: IncursionUI
//Requires: IncursionStateManager
//Requiers: IemUtils
using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Incursion Event Game", "tolland", "0.1.0")]

    public class IncursionEventGame : RustPlugin
    {

        [PluginReference]
        IncursionUI IncursionUI;

        [PluginReference] IemUtils IemUtils;

        [PluginReference]
        IncursionStateManager IncursionStateManager;

        static IncursionEventGame incursionEventGame = null;

        void Init()
        {
            incursionEventGame = this;
        }

        /// <summary>
        /// This implements a minimal viable game
        /// and is designed to be used as a base class in a game implementation
        /// </summary>
        public class EventGame
        {
            //this game is managed by this object
            public GameStateManager gsm;

            //team or solo game
            public bool TeamGame { get; set; }

            //ignored if not team game
            public bool FixedNumberOfTeams { get; set; }
            public int MinTeams { get; set; }
            public int MaxTeams { get; set; }
            public int MinPlayersPerTeam { get; set; }
            public int MaxPlayersPerTeam { get; set; }


            //private readonly Dictionary<BasePlayer, ZoneFlags> playerTags 
            //= new Dictionary<BasePlayer, ZoneFlags> ();
            public Dictionary<string, EventTeam> eventTeams
                = new Dictionary<string, EventTeam>();

            public Dictionary<string, EventPlayer> gamePlayers
                = new Dictionary<string, EventPlayer>();

            //total of players on all teams, or all solo players
            public int MinPlayers { get; set; }
            public int MaxPlayers { get; set; }

            //can players spectate when dead, or not in game
            public Boolean CanSpectate { get; set; }

            //if true, event manager will call end game
            public Boolean TimedGame { get; set; }

            //time limit in seconds
            public int TimeLimit { get; set; }

            public Boolean autoStart { get; set; }

            public Vector3 Location { get; set; }
            public List<string> GameIntroBanner { get; set; }

            /// <summary>
            /// mysql persistance fields
            /// </summary>

            public int GameStartedDateTime { get; set; }
            public int GameEndedDateTime { get; set; }

            public EventGame(GameStateManager gamestatemanager) : this()
            {
                gsm = gamestatemanager;
            }

            public EventGame()
            {
                TeamGame = true;
                FixedNumberOfTeams = true;
                MinTeams = 2;
                MaxTeams = 2;

                MinPlayers = 1;
                MaxPlayers = 20;
                MinPlayersPerTeam = 0;
                MaxPlayersPerTeam = 12;

                eventTeams.Add("team_1", new EventTeam("team_1", "Blue Team", 
                    new Vector3(-394, 3, -25), "blue"));
                eventTeams.Add("team_2", new EventTeam("team_2", "Red Team", 
                    new Vector3(-376, 3, 3), "red"));
                //eventTeams.Add("team_3", new EventTeam("team_3", "Green Team", "green"));
                //eventTeams.Add("team_4", new EventTeam("team_4", "Yellow Team", "yellow"));

                //support rulesGUI format??
                GameIntroBanner = new List<string>
                {
                    "<color=blue>Play will start in 10 seconds.</color>",
                    "<color=red>The team that shoots the most targets will win.</color>",
                    "<color=red>FYI: If the opposing team players are dead, they can't shoot targets!</color>",
                    "<color=red>one possible strategy is to have half your team shooting targets, and the other half attacking/defending the other team</color>",
                    "<color=yellow>However you don't get points for killing your opponents</color>"
                };


                CanSpectate = true;
                TimedGame = true;
                //in seconds
                TimeLimit = 120;

                Location = new Vector3(-396, 3, -25);
            }

            public bool IsOpen()
            {
                return true;
            }

            public virtual bool StartGame()
            {
                IemUtils.DLog("calling startgame in event game");
                incursionEventGame.rust.RunServerCommand("env.time", "12");
                return true;
            }

            public bool PauseGame()
            {
                return true;
            }

            public Boolean EndGame()
            {
                return true;
            }

            int TimeRemaining()
            {
                return 5;
            }

            public Boolean InitializePlayingField()
            {

                return true;
            }

            Boolean ClearPlayingField()
            {

                return true;
            }

            public bool CanGameStart()
            {

                int totalPlayers = 0;
                foreach (KeyValuePair<string, EventTeam> team in eventTeams)
                {
                    if (team.Value.teamPlayers.Count < MinPlayersPerTeam)
                    {
                        IemUtils.DLog("too few players on team " + team.Value.TeamName);
                        return false;
                    }

                    if (team.Value.teamPlayers.Count > MaxPlayersPerTeam)
                    {
                        IemUtils.DLog("too many players on team " + team.Value.TeamName);
                        return false;
                    }

                    totalPlayers = totalPlayers + team.Value.teamPlayers.Count;
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

                IemUtils.DLog("game can start");
                return true;
            }

            public EventTeam GetTeamById(string teamId)
            {
                if(eventTeams==null)
                    Plugins.IemUtils.DLog("eventTeams is null");

                return eventTeams[teamId];

            }

            public List<IncursionUI.UiTeam> ConvertTeamsToDict()
            {

                List<IncursionUI.UiTeam> teamData = new List<IncursionUI.UiTeam>();
                foreach (KeyValuePair<string, IncursionEventGame.EventTeam> teamPair in eventTeams)
                {
                    EventTeam team = teamPair.Value;
                    
                    List<string> players = new List<string>() { };
                    foreach (KeyValuePair<string, EventPlayer> player in team.teamPlayers)
                    {
                        players.Add(player.Value.player.displayName);
                    }
                    IncursionUI.UiTeam uiTeam = new IncursionUI.UiTeam(team.TeamName,players,team.Colour);
                    teamData.Add(uiTeam);

                }
                return teamData;
            }

            


            public bool AddPlayerToTeam(BasePlayer player, EventTeam team)
            {
                IemUtils.DLog("calling add player to team");
                //If the BasePlayer is a new connection
                EventPlayer eventPlayer
                    = GetEventPlayer(player);

                if (!gamePlayers.ContainsKey(eventPlayer.PlayerId))
                    gamePlayers.Add(eventPlayer.PlayerId, eventPlayer);

                if (eventPlayer.eventTeam != null)
                {
                    if (eventPlayer.eventTeam.Equals(team))
                    {
                        IemUtils.DLog("player is already in team");
                        return true;
                    }

                    if (eventPlayer.eventTeam.teamPlayers.ContainsKey(eventPlayer.PlayerId))
                        eventPlayer.eventTeam.teamPlayers.Remove(eventPlayer.PlayerId);
                }



                if (!team.teamPlayers.ContainsKey(eventPlayer.PlayerId))
                    team.teamPlayers.Add(eventPlayer.PlayerId, eventPlayer);

                eventPlayer.eventTeam = team;
                eventPlayer.psm.ChangeState(PlayerInEventLobbyTeamed.Instance);

                return true;
            }

            public bool MovePlayersToGame()
            {
                foreach (KeyValuePair<string, EventPlayer> eventPlayer
                    in gamePlayers)
                {
                    BasePlayer player = eventPlayer.Value.player;
                    Plugins.IemUtils.DLog("moving players to game");

                    IemUtils.MovePlayerTo(player, eventPlayer.Value.eventTeam.Location);
                }

                return true;
            }

            public Dictionary<string, List<string>> ConvertResultsToDict()
            {

                Dictionary<string, List<string>> teamData = new Dictionary<string, List<string>>();
                foreach (KeyValuePair<string, IncursionEventGame.EventTeam> team in eventTeams)
                {
                    List<string> players = new List<string>() { };
                    foreach (KeyValuePair<string, EventPlayer> player in team.Value.teamPlayers)
                    {
                        players.Add(player.Value.player.displayName + " score "
                            + player.Value.Score.ToString());
                    }

                    teamData.Add(team.Value.TeamName, players);

                }
                return teamData;
            }


            public List<IncursionUI.UiTeamResult> ConvertResultsToUiTeamResults()
            {

                List<IncursionUI.UiTeamResult> teamData = new List<IncursionUI.UiTeamResult>();
                foreach (KeyValuePair<string, IncursionEventGame.EventTeam> team in eventTeams)
                {
                    List<string> players = new List<string>() { };
                    foreach (KeyValuePair<string, EventPlayer> player in team.Value.teamPlayers)
                    {
                        players.Add(player.Value.player.displayName + " score "
                            + player.Value.Score.ToString());
                    }
                    IncursionUI.UiTeamResult result = new IncursionUI.UiTeamResult(team.Value.TeamName,
                        players,team.Value.Colour) { };

                    teamData.Add(result);

                }
                return teamData;
            }

            public void ShowGameResultUI()
            {
                var gameresult = ConvertResultsToUiTeamResults();
                foreach (IncursionEventGame.EventPlayer eventPlayer in gsm.eg.gamePlayers.Values)
                {

                    IncursionUI.ShowResultsUi(eventPlayer.player, gameresult);
                }

                
            }

            public void RemoveGameResultUI()
            {
                IncursionUI.RemoveGameResultUI();
            }

        }



        public class EventPlayer : MonoBehaviour
        {
            public BasePlayer player;
            public IncursionEventGame.EventTeam eventTeam;

            //@todo should the Monobehaviour be applied to the Statemanager?
            public PlayerStateManager psm;

            public EventPlayer()
            {

                IemUtils.DLog("calling constructor in eventplayer monobehavior");
                psm = new PlayerStateManager(PlayerUnknown.Instance);
            }

            public string PlayerId { get; set; }
            public int Score { get; set; }

            void Awake()
            {
                IemUtils.DLog("calling awake in eventplayer monobehavior");
                player = GetComponent<BasePlayer>();
                psm.eventPlayer = this;

            }
        }



        public class GameStateManager : IncursionStateManager.StateManager
        {
            public string Name { get; set; }
            public IncursionEventGame.EventPlayer eventPlayer;
            public IncursionEventGame.EventGame eg;

            public GameStateManager(IncursionStateManager.IStateMachine initialState, string name) : base(initialState)
            {
                IemUtils.DLog("creating a game state manager");
                Name = name;
            }



            public virtual void ReinitializeGame()
            {
                eg = new IncursionEventGame.EventGame(this);
            }
        }

        public class PlayerStateManager : IncursionStateManager.StateManager
        {

            public IncursionEventGame.EventGame eg;
            public EventPlayer eventPlayer;

            public PlayerStateManager(IncursionStateManager.IStateMachine initialState) : base(initialState)
            {
                IemUtils.DLog("creating the player state manager");
            }
        }



        /// <summary>
        /// Unknown - if a player is allocated to an event before having connect, their details
        /// are unknown. But we want to be able to hold a spot for a player before joining
        /// Disconnected - player has connected, but is not eligible for the lobby, or its not created yet
        /// player has been allocated to team/event, but has not yet connected
        /// players who are eligible to play, and have been moved to the pregame lobby
        /// allocated to team, ready to start if solo game
        /// playing, dead, sleeping
        /// used to track players who disconnect during play
        /// not sure if this is useful
        /// 
        /// </summary>


        void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
        {

        }

        public class PlayerUnknown : IncursionStateManager.StateBase<PlayerUnknown>,
            IncursionStateManager.IStateMachine
        {

        }


        public class Disconnected : IncursionStateManager.StateBase<Disconnected>,
            IncursionStateManager.IStateMachine
        {

        }

        public class PlayerInEventLobbyNoGame : IncursionStateManager.StateBase<PlayerInEventLobbyNoGame>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager psm)
            {
                IemUtils.DLog("player is in the event Lobby, no game");

                BasePlayer player = ((PlayerStateManager)psm).
                eventPlayer.player;

                IncursionUI.CreateAdminBanner2(player, "state:" + psm.GetState().ToString());
            }

        }


        public class PlayerInEventLobby : IncursionStateManager.StateBase<PlayerInEventLobby>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager psm)
            {
                BasePlayer player = ((PlayerStateManager)psm).
eventPlayer.player;
                EventPlayer eventPlayer = ((PlayerStateManager)psm).
                eventPlayer;
                IemUtils.DLog("player is entering the PlayerInEventLobby");


                IncursionUI.CreateAdminBanner2(player, "state:" + psm.GetState().ToString());
                IncursionUI.ShowTeamUiForPlayer(player, eventPlayer.psm.eg.ConvertTeamsToDict());
            }
            public new void Exit(IncursionStateManager.StateManager psm)
            {
                IemUtils.DLog("player is leaving the PlayerInEventLobby");
                EventPlayer eventPlayer = ((PlayerStateManager)psm).
                eventPlayer;
                IncursionUI.RemoveTeamUIForPlayer(eventPlayer.player);
            }
        }

        public class PlayerInEventLobbyNoTeam : IncursionStateManager.StateBase<PlayerInEventLobbyNoTeam>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager psm)
            {
                BasePlayer player = ((PlayerStateManager)psm).
eventPlayer.player;
                EventPlayer eventPlayer = ((PlayerStateManager)psm).
                eventPlayer;
                IemUtils.DLog("player is entering the PlayerInEventLobbyNoTeam");

                

                IncursionUI.CreateAdminBanner2(player, "state:" + psm.GetState().ToString());
                IncursionUI.ShowTeamUiForPlayer(player, 
                    eventPlayer.psm.eg.ConvertTeamsToDict());
            }

            public new void Exit(IncursionStateManager.StateManager psm)
            {
                IemUtils.DLog("player is leaving the EventLobbyNoTeam");
                EventPlayer eventPlayer = ((PlayerStateManager)psm).eventPlayer;
                IncursionUI.RemoveTeamUIForPlayer(eventPlayer.player);
            }
        }

        public class PlayerInEventLobbyTeamed : IncursionStateManager.StateBase<PlayerInEventLobbyTeamed>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager psm)
            {

                IemUtils.DLog("player is EventLobbyTeamed");
                BasePlayer player = ((PlayerStateManager)psm).
                eventPlayer.player;
                EventPlayer eventPlayer = ((PlayerStateManager)psm).
                eventPlayer;

                BroadcastChat("player " + player.displayName
                    + " has joined team " + eventPlayer.eventTeam.TeamName);


                IncursionUI.CreateAdminBanner2(player, "state:" + psm.GetState().ToString());
                IncursionUI.ShowTeamUi(eventPlayer.psm.eg.ConvertTeamsToDict());

                Interface.Oxide.CallHook("OnPlayerAddedToTeam", eventPlayer.eventTeam, player);
            }
            public new void Exit(IncursionStateManager.StateManager psm)
            {
                IemUtils.DLog("player is leaving EventLobbyTeamed");
                EventPlayer eventPlayer = ((PlayerStateManager)psm).
                eventPlayer;
                IncursionUI.RemoveTeamUIForPlayer(eventPlayer.player);
            }
        }


        public class PlayerInEventLobbySolo : IncursionStateManager.StateBase<PlayerInEventLobbySolo>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager psm)
            {
                IemUtils.DLog("player is EventLobbySolo");

                BasePlayer player = ((PlayerStateManager)psm).
                eventPlayer.player;

                IncursionUI.CreateAdminBanner2(player, "state:" + psm.GetState().ToString());
            }
        }

        public class PlayerInGameLobby : IncursionStateManager.StateBase<PlayerInGameLobby>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager psm)
            {
                IemUtils.DLog("player is PlayerInGameLobby");

                BasePlayer player = ((PlayerStateManager)psm).
                eventPlayer.player;

                IncursionUI.CreateAdminBanner2(player, "state:" + psm.GetState().ToString());
            }
        }


        public class PlayerInGameTeamed : IncursionStateManager.StateBase<PlayerInGameTeamed>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager psm)
            {
                IemUtils.DLog("player is PlayerInGameTeamed");

                BasePlayer player = ((PlayerStateManager)psm).
                eventPlayer.player;

                IncursionUI.CreateAdminBanner2(player, "state:" + psm.GetState().ToString());
            }
        }

        public abstract class PlayerStateBase<T> where T : IncursionStateManager.StateBase<T>,
            IncursionStateManager.IStateMachine, new()
        {
        }

        public class PlayerInGameTeamedDead : IncursionStateManager.StateBase<PlayerInGameTeamedDead>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager psm)
            {
                base.Enter(psm);


            }
        }

        public class PlayerInGameSolo : IncursionStateManager.StateBase<PlayerInGameSolo>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager psm)
            {
                IemUtils.DLog("player is PlayerInGameSolo");

                BasePlayer player = ((PlayerStateManager)psm).
                eventPlayer.player;

                IncursionUI.CreateAdminBanner2(player, "state:" + psm.GetState().ToString());
            }
        }



        public static EventPlayer GetEventPlayer(BasePlayer player)
        {
            EventPlayer eventPlayer
               = player.GetComponent<EventPlayer>();
            if (eventPlayer == null)
            {
                IemUtils.DLog("creating eventPlayer");

                eventPlayer = player.gameObject.AddComponent<EventPlayer>();
                eventPlayer.PlayerId = player.UserIDString;
                eventPlayer.player = player;
            }
            return eventPlayer;
        }


        public class EventTeam
        {
            public EventTeam(string teamId, 
                string teamName, 
                Vector3 teamLocation, 
                string colour = "white")
            {
                TeamId = TeamId;
                TeamName = teamName;
                Colour = colour;
                Location = teamLocation;
            }
            public string TeamName { get; set; }
            public string TeamId { get; set; }
            public string Colour { get; set; }
            public Vector3 Location { get; set; }
            public int Score { get; set; }
            public Dictionary<string, EventPlayer> teamPlayers 
                = new Dictionary<string, EventPlayer>();
        }


        static void BroadcastChat(string message)
        {
            incursionEventGame.rust.BroadcastChat(message);
        }

        //@todo add checks for game state
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
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
                            //Puts ("target maxHealth is " + target.MaxHealth ().ToString ());
                            target.CancelInvoke("ResetTarget");
                            //Puts ("target health is " + target.Health ().ToString ());
                            target.health = target.MaxHealth();
                            //target.ChangeHealth (target.MaxHealth ());
                            //Puts ("target health is " + target.Health ().ToString ());
                            target.SendNetworkUpdate();
                            //timer.Once (time, () => target.SetFlag (BaseEntity.Flags.On, true));

                            EventPlayer eventPlayer = GetEventPlayer(attacker);

                            eventPlayer.Score += 1;

                        }


                    }
                }
            }
            catch (Exception ex)
            {
                Puts("exception");
            }
        }


    }
}
