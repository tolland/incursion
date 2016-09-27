//Requires: IncursionEventGame
//Requires: IncursionHoldingArea
//Requires: IncursionUI
//Requires: IncursionStateManager
//Requires: IemUtils

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using Physics = UnityEngine.Physics;
using Random = System.Random;

namespace Oxide.Plugins
{

    [Info("Incursion Events", "Tolland", "0.1.0")]
    public class IncursionEvents : RustPlugin
    {

        #region Variables

        [PluginReference]
        IncursionEventGame IncursionEventGame;

        [PluginReference]
        IncursionStateManager IncursionStateManager;

        [PluginReference]
        IncursionHoldingArea IncursionHoldingArea;

        [PluginReference]
        IncursionUI IncursionUI;

        [PluginReference]
        IemUtils IemUtils;

        [PluginReference]
        Plugin ZoneManager;

        public static EventStateManager esm;
        private static IncursionEvents incursionEvents = null;

        #endregion

        #region Boilerplate


        void Init()
        {
            incursionEvents = this;
            IemUtils.LogL("IncursionEvents: init complete");
        }

        StoredData storedData;

        void Loaded()
        {


            esm = new EventStateManager(PluginLoaded.Instance);
            //IemUtils.LogL("IncursionEvents: Loaded complete");



        }

        void Unload()
        {
            esm.ChangeState(PluginUnload.Instance);
            //IemUtils.LogL("IncursionEvents: Unload complete");

            Unsubscribe(nameof(OnPlayerRespawn));
        }

        void OnServerInitialized()
        {
            esm.ChangeState(ServerInitialized.Instance);
            esm.Update();
            //IemUtils.LogL("IncursionEvents: OnServerInitialized complete");
        }



        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            Config.Clear();
            Config["EventManagementMode"] = "repeating";  //{"scheduled","repeating","once","manual"}
            Config["SchedulerEnabled"] = false;
            Config["DefaultGame"] = "Default Team Game";
            Config["AutoStart"] = true;
            Config["JoinMessage"] = "Welcome to this server";
            Config["LeaveMessage"] = "Goodbye";
            SaveConfig();
        }

        #endregion

        #region stuff that needs to  be fixed

        ///This section is private methods for ESM
        /// 
        void MovePlayersToEsmLobby()
        {
            IemUtils.DLog("moving players to esm lobby");

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                MovePlayerToEsmLobby(player);

            }
        }

        System.Random rnd = new System.Random();

        void MovePlayerToEsmLobby(BasePlayer player)
        {
            //IemUtils.DLog("moving player to esm lobby");
            //@todo move this to the game definition
            Vector3 loc = new Vector3(-236, 3, 18);
            float radius = 8.5f;
            loc = IemUtils.GetRandomPointOnCircle(loc, radius);

            IemUtils.MovePlayerTo(player, IemUtils.GetGroundY(loc));
        }

        void CreateEsmLobby()
        {
            //IemUtils.DLog("CreateEsmLobby");
            esm.eventLobby = new IncursionHoldingArea.Lobby(new Vector3(-231, 2, 14));
        }

        void DestroyEsmLobby()
        {
            //IemUtils.DLog("Destroy ESM lobby");
        }

        void ProcessScheduledEventToEventGame()
        {
            //process the list of teams and players who are preregistered for this
            //scheduled event into the concrete game event
            foreach (IemUtils.ScheduledEvent.ScheduledEventTeam seteam in esm.currentGameStateManager.nextEvent.schTeams.Values)
            {
                //Plugins.IemUtils.DLog("team is " + seteam.TeamName);
                foreach (IemUtils.ScheduledEvent.ScheduledEventPlayer scheduledEventPlayer in seteam.schPlayers.Values)
                {
                    //IemUtils.DLog("sched player is " + scheduledEventPlayer.steamId);
                    ulong id;
                    ulong.TryParse(scheduledEventPlayer.steamId, out id);
                    BasePlayer idplayer = IemUtils.FindPlayerByID(id);
                    if (idplayer != null)
                    {
                        IemUtils.DLog("base player is " + idplayer.displayName);

                    }

                }
            }
        }


        #endregion

        #region event state manager


        public class EventStateManager : IncursionStateManager.StateManager
        {
            public IncursionHoldingArea.Lobby eventLobby;
            public Dictionary<string, IncursionEventGame.GameStateManager> gameStateManagers
                = new Dictionary<string, IncursionEventGame.GameStateManager>();
            public IncursionEventGame.GameStateManager currentGameStateManager;
            public IncursionStateManager.StateManager scheduler;
            public string cachedEventBanner = "";

            public EventStateManager(IncursionStateManager.IStateMachine initialState) : base(initialState)
            {

                IncursionUI.CreateEventStateManagerDebugBanner("state:" + GetState().ToString());
            }

            public override void ChangeState(IncursionStateManager.IStateMachine newState)
            {
                base.ChangeState(newState);
                //IemUtils.DDLog("changing state in EventStateManager");
                IncursionUI.CreateEventStateManagerDebugBanner("state:" + GetState().ToString());

            }


            public void CreateEventBanner(string message)
            {
                if (message.Equals(cachedEventBanner))
                    return;
                IemUtils.DLog("caching event banner");
                cachedEventBanner = message;

                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    IncursionUI.CreateEventBanner(player, message);
                }
            }

            public void CreateEventBanner(BasePlayer player, string message)
            {
                IncursionUI.CreateEventBanner(player, message);
            }

            public void RegisterGameStateManager(IncursionEventGame.GameStateManager gameStateManager)
            {
                gameStateManagers.Add(gameStateManager.Name, gameStateManager);
                //currentGameStateManager = gameStateManager;
            }


            public void RegisterScheduler(IncursionStateManager.StateManager stateManager)
            {
                scheduler = stateManager;
            }



            public void GameComplete()
            {

                //ChangeState(EventManagementLobby.Instance);
            }

            internal void StartScheduledGame(IemUtils.ScheduledEvent sevent)
            {
                currentGameStateManager.nextEvent = sevent;

                if (IsAny(EventRunning.Instance))
                {
                    incursionEvents.rust.BroadcastChat("Can't start scheduled game, other game running");
                    return;
                }

                // gsm is sitting in the lobby, which is open or lacking players
                //TODO handle case where game is about to start
                if (IsAny(EventLobbyOpen.Instance))
                {
                    // gsm is sitting in the lobby, which is open or lacking players
                    //if (currentGameStateManager.IsAny(IemGameTeams.GameEventCannotStart.Instance,
                    //    IemGameTeams.GameEventLoaded.Instance))
                    //{
                    currentGameStateManager.CancelGame();
                    //}
                }

                if (IsAny(EventCancelled.Instance))
                {
                    ChangeState(EventManagementLobby.Instance);
                    Update();
                }

                if (IsAny(EventManagementLobby.Instance))
                {
                    //this should trigger a switch to GameLoaded
                    //if everything is available
                    Update();
                }

                if (IsAny(GameLoaded.Instance))
                {
                    //this will open the event lobby
                    Update();
                }
                else
                {
                    //IemUtils.SLog("not game loaded here:" + GetState());
                }

                if (IsAny(EventLobbyOpen.Instance))
                {
                    //this will open the event lobby
                    Update();
                }
                else
                {
                    //IemUtils.SLog("not game loaded here:" + GetState());
                }


            }
        }

        #endregion

        #region event manager states

        /// <summary>
        /// This state represents when the IncursionEvents plugin is loaded
        /// but before any Game has been loaded
        /// </summary>
        public class PluginLoaded : IncursionStateManager.StateBase<PluginLoaded>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager esm)
            {
                //unsubscribe from events until correct state
                //this is the entrypoint for the statemanager
                incursionEvents.Unsubscribe(nameof(OnRunPlayerMetabolism));
                incursionEvents.Unsubscribe(nameof(OnPlayerDisconnected));

                // don't handle spawn location until required
                incursionEvents.Unsubscribe(nameof(OnPlayerRespawn));

                incursionEvents.storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("MyDataFile");
            }
        }

        /// <summary>
        /// This state represents when the IncursionEvents plugin is PluginUnload
        /// implicit event triggered by Unload hook
        /// </summary>
        public class PluginUnload : IncursionStateManager.StateBase<PluginUnload>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager esm)
            {
            }
        }

        public class ServerRunning : IncursionStateManager.StateBase<ServerRunning>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager esm)
            {
                //IemUtils.DLog("entry in ServerRunning");
            }
        }


        public class ServerInitialized : IncursionStateManager.StateBase<ServerInitialized>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager esm)
            {
                incursionEvents.rust.RunServerCommand("weather.fog", "0");
                incursionEvents.rust.RunServerCommand("weather.rain", "0");
                incursionEvents.rust.RunServerCommand("heli.lifetimeminutes", "0");

            }

            public new void Execute(IncursionStateManager.StateManager esm)
            {
                //there are no checks to do to proceed to the event management lobby
                esm.ChangeState(EventManagementLobby.Instance);
            }
        }





        /// <summary>
        /// In this state, the players are moved to the event lobby
        /// however the players cannot yet select teams
        /// </summary>
        public class EventManagementLobby : IncursionStateManager.StateBase<EventManagementLobby>,
            IncursionStateManager.IStateMachine
        {
            private Timer daytime = null;

            public new void Enter(IncursionStateManager.StateManager esm)
            {

                // want players to spawn into lobby from here on in
                incursionEvents.Subscribe(nameof(OnPlayerRespawn));

                //touch each player
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    IncursionEventGame.EventPlayer eplayer = IncursionEventGame.EventPlayer.GetEventPlayer(player);
                }


                incursionEvents.ResetPlayerStatuses();

                incursionEvents.CreateEsmLobby();

                IncursionHoldingArea.CloseTeamDoors();

                incursionEvents.Subscribe(nameof(OnRunPlayerMetabolism));

                incursionEvents.MovePlayersToEsmLobby();
                daytime = incursionEvents.timer.Once(120f, () => SetLobbyEnvironment());

            }

            private void SetLobbyEnvironment()
            {
                IemUtils.RunServerCommand("env.time", "12");
                daytime = incursionEvents.timer.Once(120f, () => SetLobbyEnvironment());
            }

            public new void Execute(IncursionStateManager.StateManager sm)
            {
                //work with the child class methods
                EventStateManager esm = ((EventStateManager)sm);

                IemUtils.DLog((string) incursionEvents.Config["EventManagementMode"]);
                switch ((string)incursionEvents.Config["EventManagementMode"])
                {
                    // make the gsm available and open the event lobby
                    case "repeating":

                        break;

                    //gsm will be initialized by the scheduler
                    case "scheduled":
                        esm.currentGameStateManager = esm.gameStateManagers.First().Value;
                        esm.currentGameStateManager.ReinitializeGame();
                        esm.ChangeState(GameLoaded.Instance);
                        break;
                }


            }

            public new void Exit(IncursionStateManager.StateManager esm)
            {
                daytime?.Destroy();
                incursionEvents.Unsubscribe(nameof(OnRunPlayerMetabolism));




            }

        }

        /// <summary>
        /// a playable game has been loaded, but players cant join until its open
        /// </summary>
        public class GameLoaded : IncursionStateManager.StateBase<GameLoaded>,
            IncursionStateManager.IStateMachine
        {

            public new void Execute(IncursionStateManager.StateManager sm)
            {
                EventStateManager esm = ((EventStateManager)sm);

                switch ((string)incursionEvents.Config["EventManagementMode"])
                {
                    // make the gsm available and open the event lobby
                    case "repeating":
                        if ((bool)incursionEvents.Config["AutoStart"] == true)
                        {
                        }
                        break;
                    case "once":
                        if ((bool)incursionEvents.Config["AutoStart"] == true)
                        {

                        }
                        break;

                    //gsm will be initialized by the scheduler
                    case "scheduled":
                        esm.ChangeState(EventLobbyOpen.Instance);
                        break;
                }
            }
        }


        private void OnPlayerDisconnected(BasePlayer player)
        {
            esm.currentGameStateManager.eg.RemovePlayerFromTeams(player);
            IncursionEventGame.EventPlayer eventPlayer
                     = IncursionEventGame.EventPlayer.GetEventPlayer(player);
            if (eventPlayer.eventTeam!=null)
                eventPlayer.eventTeam.disconnectedPlayers.Add(eventPlayer.PlayerId);
            
            

        }
        /// <summary>
        /// players can join teams
        /// there should be a game state manager registered, and an eventgame available
        /// to enter in here
        /// </summary>
        public class EventLobbyOpen : IncursionStateManager.StateBase<EventLobbyOpen>,
        IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager sm)
            {
                //work with the child class methods
                EventStateManager esm = ((EventStateManager)sm);

                //protect players from metabolism when in lobby
                incursionEvents.Subscribe(nameof(OnRunPlayerMetabolism));
                incursionEvents.Subscribe(nameof(OnPlayerDisconnected));

                //@todo create a general function for lobby environment settings
                IemUtils.RunServerCommand("env.time", "12");

                //if players/teams exist in the scheduled event, move them to the event game
                //incursionEvents.ProcessScheduledEventToEventGame();
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    //IemUtils.DLog("found player " + player.UserIDString);
                    IncursionEventGame.EventPlayer eventPlayer
                        = IncursionEventGame.EventPlayer.GetEventPlayer(player);

                    eventPlayer.psm.eg = esm.currentGameStateManager.eg;

                    eventPlayer.psm.ChangeState(IncursionEventGame.PlayerInEventLobbyNoTeam.Instance);


                }

                if (esm.currentGameStateManager.nextEvent != null)
                {
                    foreach (var team in esm.currentGameStateManager.nextEvent.schTeams.Values)
                    {
                        //if (team.schPlayers.ContainsKey(player.UserIDString))
                        //{
                        //    Plugins.IemUtils.DDLog("found the player");
                        //}
                        foreach (var splayer in team.schPlayers.Values)
                        {
                            foreach (BasePlayer player in BasePlayer.activePlayerList)
                            {
                                if (player.UserIDString == splayer.steamId)
                                {
                                    esm.currentGameStateManager.eg.AddPlayerToTeam(player,
                                        esm.currentGameStateManager.eg.GetTeamById("team_1"));
                                }

                            }
                        }

                    }
                }
                else
                {
                    IemUtils.DLog("next event null in EventLobby Open");
                }

                incursionEvents.rust.BroadcastChat("Opening lobby");
                IncursionHoldingArea.OpenTeamDoors();

            }

            /// <summary>
            /// this method is called by events which might require the event manager
            /// to update the lobby, ie players joining teams
            /// </summary>
            /// <param name="sm"></param>
            public new void Execute(IncursionStateManager.StateManager sm)
            {

                EventStateManager esm = (EventStateManager)sm;

                esm.currentGameStateManager.Update();

            }

            public new void Exit(IncursionStateManager.StateManager esm)
            {
                IncursionUI.RemoveTeamUI();
            }
        }


        /// <summary>
        ///  players are not yet moved to the playing field, but joining teams is now closed
        /// </summary>
        public class EventLobbyClosed : IncursionStateManager.StateBase<EventLobbyClosed>, IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager esm)
            {
                //IemUtils.DLog("entry in EventLobbyClosed");
                //IncursionHoldingArea.CloseTeamDoors();
            }

            public new void Exit(IncursionStateManager.StateManager esm)
            {
                IncursionUI.RemoveTeamUI();
            }
        }



        public class EventRunning : IncursionStateManager.StateBase<EventRunning>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager esm)
            {
                //IemUtils.DLog("entry in EventRunning");
                incursionEvents.Unsubscribe(nameof(OnRunPlayerMetabolism));

                //don't manager spawning while game is running
                incursionEvents.Unsubscribe(nameof(OnPlayerRespawn));
                //incursionEvents.Subscribe(nameof(OnPlayerRespawn));

            }

            public new void Exit(IncursionStateManager.StateManager esm)
            {
                //IemUtils.DLog("exiting in EventRunning");
                incursionEvents.Subscribe(nameof(OnRunPlayerMetabolism));
                incursionEvents.Unsubscribe(nameof(OnPlayerDisconnected));
            }
        }

        public class EventComplete : IncursionStateManager.StateBase<EventComplete>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager sm)
            {
                EventStateManager esm = (EventStateManager)sm;
                //IemUtils.DLog("entry in EventComplete");

                //don't want players to metablise while here
                incursionEvents.Subscribe(nameof(OnRunPlayerMetabolism));

                //take control of spawning
                //incursionEvents.Unsubscribe(nameof(OnPlayerRespawn));
                incursionEvents.Subscribe(nameof(OnPlayerRespawn));

                incursionEvents.MovePlayersToEsmLobby();


                Interface.Oxide.CallHook("OnEventComplete");

            }
        }

        public class EventCancelled : IncursionStateManager.StateBase<EventCancelled>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager sm)
            {
                EventStateManager esm = (EventStateManager)sm;

                incursionEvents.Subscribe(nameof(OnPlayerRespawn));

                incursionEvents.MovePlayersToEsmLobby();

                Interface.Oxide.CallHook("OnEventCancelled");

            }
        }

        #endregion

        #region data

        class StoredEventPlayer
        {
            public string UserId;
            public string Name;

            public StoredEventPlayer()
            {
            }

            public StoredEventPlayer(BasePlayer player)
            {
                UserId = player.userID.ToString();
                Name = player.displayName;
            }
        }

        class StoredData
        {
            public HashSet<StoredEventPlayer> Players
                = new HashSet<StoredEventPlayer>();

            public StoredData()
            {
            }
        }

        #endregion

        #region chat control

        [ChatCommand("Test")]
        void Test(BasePlayer player, string command, string[] args)
        {
            if (!IemUtils.isAdmin(player))
                return;
            var info = new StoredEventPlayer(player);
            //@todo not working... check this
            if (storedData.Players.Contains(info))
                PrintToChat(player, "Your data has already been added to the file");
            else
            {
                PrintToChat(player, "Saving your data to the file");
                storedData.Players.Add(info);
                Interface.Oxide.DataFileSystem.WriteObject("MyDataFile", storedData);
            }
        }

        #endregion

        #region player hooks and calls

        void OnRunPlayerMetabolism(PlayerMetabolism m, BaseCombatEntity entity)
        {
            var player = entity.ToPlayer();
            if (player == null)
                return;
            IemUtils.SetMetabolismValues(player);
        }



        public void ShowTeamSelectHud(BasePlayer player)
        {
            IncursionEventGame.EventPlayer eventPlayer = IncursionEventGame.EventPlayer.GetEventPlayer(player);
            //IemUtils.DLog("enabled is " + eventPlayer.isActiveAndEnabled);

            if (eventPlayer.psm.IsAny(IncursionEventGame.PlayerInTeamSelectHUD.Instance))
            {
                eventPlayer.psm.SubStateReturn();
            }
            else
            {
                eventPlayer.psm.SubState(IncursionEventGame.PlayerInTeamSelectHUD.Instance);
            }

        }


        void OnEnterZone(string ZoneID, BasePlayer player)
        {
            if (esm != null)
            {
                IemUtils.DLog(esm.GetState().ToString());
                if (esm.GetState().Equals(EventLobbyOpen.Instance))
                {
                    //is this a team zone?
                    if (ZoneID.StartsWith("zone_team_"))
                    {
                        OnPlayerEnterTeamArea(player, GetTeamFromZone(ZoneID));
                    }
                }
            }

        }



        IncursionEventGame.EventTeam GetTeamFromZone(string ZoneID)
        {
            //IemUtils.DLog("team substring is " + ZoneID.Substring(5));
            return esm.currentGameStateManager.eg.GetTeamById(ZoneID.Substring(5));
        }

        void OnPlayerEnterTeamArea(BasePlayer player, IncursionEventGame.EventTeam team)
        {
            esm.currentGameStateManager.eg.AddPlayerToTeam(player, team);
            esm.Update();

        }

        void ResetPlayerStatuses()
        {
            List<string> zones = (List<string>)ZoneManager.Call("GetZoneList");
            if (zones != null)
            {
                foreach (string zone in zones)
                {
                    foreach (BasePlayer player in BasePlayer.activePlayerList)
                    {
                        ZoneManager.Call("RemovePlayerFromZoneKeepinlist", zone, player);
                    }
                }
            }
        }


        void OnPlayerAddedToTeam(IncursionEventGame.EventTeam team, BasePlayer player)
        {
            //IemUtils.DLog("calling update team");
            if (esm.IsAny(EventManagementLobby.Instance,
                             EventLobbyOpen.Instance,
                             EventLobbyClosed.Instance))
            {
                esm.Update();
            }
        }

        static void RestoreUI(IncursionEventGame.EventPlayer eventPlayer)
        {
            IncursionUI.CreateSchedulerStateManagerDebugBanner("state:" + esm.scheduler.GetState().ToString());
            IncursionUI.CreateEventStateManagerDebugBanner("state:" + esm.GetState().ToString());

            IemUtils.DLog("adding GSM debug banner");
            IncursionUI.CreateGameStateManagerDebugBanner("state:" + esm.currentGameStateManager.GetState().ToString());
            IncursionUI.CreateGameBanner(eventPlayer.player, esm.currentGameStateManager.cachedGameBanner);

            IncursionUI.CreateEventBanner(eventPlayer.player, esm.cachedEventBanner);
        }

        /// <summary>
        /// this is called when the player is waking from sleep after connecting
        /// </summary>
        /// <param name="player"></param>
        /// 
        void CheckPlayer(BasePlayer player)
        {
            Puts("calling check player");

            //The event manager is loaded, if its not, then no point going further
            if (esm == null)
            {
                IemUtils.DLog("Event State manager is not loaded, can't initialise player");
                return;
            }

            IncursionEventGame.EventPlayer eventPlayer = IncursionEventGame.EventPlayer.GetEventPlayer(player);

            IemUtils.DLog("psm is " + eventPlayer.psm);

            
            //@todo if this triggers its a bug, seems to happen after dead player reconnects
            if (eventPlayer.psm == null)
            {
                IemUtils.DLog("psm is null");
            }
            else
            {
                if (eventPlayer.psm.eg == null)
                {
                    IemUtils.DLog("eventPlayer.psm.eg");
                    //IemUtils.DLog("event Game was null");
                    if (esm.currentGameStateManager.eg != null)
                    {
                        //IemUtils.DLog("adding eventgame to playerstatemanager");
                        IemUtils.DLog("adding psm eg back");
                        eventPlayer.psm.eg = esm.currentGameStateManager.eg;
                    }
                }
            }
            //@todo fix this crappy hack
            if (esm.currentGameStateManager == null)
            {
                IemUtils.DLog("currentGameStateManager is null - in checkplayer");
            }
            else
            {


                if (esm.currentGameStateManager.eg.gamePlayers.ContainsKey(player.UserIDString))
                {
                    IemUtils.DLog("adding GSM debug banner");
                    if (eventPlayer.eventTeam == null)
                    {
                        foreach (IncursionEventGame.EventTeam eventTeamsValue
                            in esm.currentGameStateManager.eg.eventTeams.Values)
                        {
                            if (eventTeamsValue.teamPlayers.ContainsKey(player.UserIDString))
                            {
                                eventPlayer.eventTeam = eventTeamsValue;
                            }
                        }
                    }
                }
                if(eventPlayer.eventTeam==null)
                    IemUtils.DLog("here");

                

                foreach (var team in esm.currentGameStateManager.eg.eventTeams.Values)
                {
                    if (team.disconnectedPlayers.Contains(eventPlayer.PlayerId))
                    {
                        esm.currentGameStateManager.eg.AddPlayerToTeam(player, team);
                    }
                }
                if (eventPlayer.eventTeam != null &&
                    !esm.currentGameStateManager.eg.gamePlayers.ContainsKey(player.UserIDString))
                {
                    esm.currentGameStateManager.eg.AddPlayerToTeam(player, eventPlayer.eventTeam);

                }
            }

            //IncursionUI.CreatePlayerStateManagerDebugBanner(eventPlayer.player,
            //    "player state:" + eventPlayer.psm.GetState().ToString());

            if (esm.IsAny(
                    GameLoaded.Instance,
                    EventManagementLobby.Instance))
            {
                Puts(eventPlayer.psm.GetState().ToString());
                if ((bool)ZoneManager.Call("isPlayerInZone", "lobby", player))
                {
                    //Puts("player in zone for lobby");
                }
                else
                {
                    //Puts("player not in zone for lobby");
                }

                eventPlayer.psm.ChangeState(IncursionEventGame.PlayerWaitingInEventLobbyNoGame.Instance);
            }

            if (esm.IsAny(
                    EventLobbyOpen.Instance,
                    EventLobbyClosed.Instance))
            {
                Puts(eventPlayer.psm.GetState().ToString());
                if ((bool)ZoneManager.Call("isPlayerInZone", "lobby", player))
                {
                    //Puts("player in zone for lobby");
                }
                else
                {
                    //Puts("player not in zone for lobby");
                }

                eventPlayer.psm.ChangeState(IncursionEventGame.PlayerInEventLobbyNoTeam.Instance);
            }

            if (esm.IsAny(EventRunning.Instance))
            {
                //Puts(eventPlayer.psm.GetState().ToString());

                if (esm.currentGameStateManager.eg.gamePlayers.ContainsKey(player.UserIDString))
                {
                    esm.currentGameStateManager.eg.MovePlayerToGame(eventPlayer);
                    eventPlayer.psm.ChangeState(IncursionEventGame.PlayerInGame.Instance);
                }
                else
                {
                    eventPlayer.psm.ChangeState(IncursionEventGame.PlayerWaiting.Instance);
                }


            }

            RestoreUI(eventPlayer);

        }

        void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
        {
            if (victim == null)
                return;

            if (victim.ToPlayer() != null)
            {
                Puts("player died");
                BasePlayer player = victim.ToPlayer();
                IncursionUI.RemoveTeamUIForPlayer(victim.ToPlayer());
                IncursionEventGame.EventPlayer eventPlayer
                       = IncursionEventGame.EventPlayer.GetEventPlayer(player); 

                //                if(eventPlayer.psm.IsAny(IncursionEventGame.Pl))
                eventPlayer.psm.ChangeState(IncursionEventGame.PlayerDead.Instance);
            }
        }


        // void OnPlayerInit(BasePlayer player) => CheckPlayer(player);
        // void OnPlayerSleepEnded(BasePlayer player) => CheckPlayer(player);

        void OnPlayerInit(BasePlayer player)
        {
            //Puts("OnPlayerInit works!");
            IncursionUI.DisplayEnterLobbyUI(player);
        }



        void OnPlayerSleepEnded(BasePlayer player)
        {
            Puts("OnPlayerSleepEnded works!");
            CheckPlayer(player);
        }

        void OnAirdrop(CargoPlane plane, Vector3 location)
        {
            Puts("OnAirdrop works!");
        }


        Vector3 FindSpawnPoint(BasePlayer player)
        {
            IemUtils.DLog("finding spawn point in lobby");


            Vector3 loc = (Vector3)esm.eventLobby.location;

            //IncursionEventGame.EventPlayer eventPlayer
            //    = IncursionEventGame.GetEventPlayer(player);

            Vector3 circpoint = IemUtils.GetRandomPointOnCircle(loc, 4f);

            return IemUtils.GetGroundY(circpoint);


        }

        #endregion

        #region player hooks

        void OnPlayerRespawned(BasePlayer player)
        {
            Puts("playing respawned");

            if (player.IsSleeping())
                player.EndSleeping();

            //CheckPlayer(player);
        }

        void OnPlayerRespawn(BasePlayer player)
        {
            Puts(player.transform.position.ToString());
            player.transform.position = FindSpawnPoint(player);
            //return false;
        }

        //Boolean OnItemRemovedFromContainer(ItemContainer container, Item item)
        //{
        //    Puts("OnItemRemovedFromContainer works!");
        //    Puts(container.GetType().FullName);
        //    return true;
        //}


        //void OnItemRemoved(ItemContainer container, Item item)
        //{
        //    Puts("OnItemRemovedFromContainer works!");
        //}

        //void CanEquipItem(PlayerInventory inventory, Item item)
        //{
        //    Puts("CanEquipItem works!");
        //}

        //void CanWearItem(PlayerInventory inventory, Item item)
        //{
        //    Puts("CanWearItem works!");
        //}


        #endregion

        #region console control

        [ConsoleCommand("event")]
        void ccmdEvent(ConsoleSystem.Arg arg)
        {
            if (!IemUtils.hasAccess(arg))
                return;
            switch (arg.Args[0].ToLower())
            {
                //if there is a EventGame availabe, autostart it
                case "autostart":
                    SendReply(arg, "autostarting");
                    Config["AutoStart"] = true;
                    esm.Update();
                    return;


                case "state":
                    SendReply(arg, "showing state game");
                    SendReply(arg, esm.GetState().ToString());
                    SendReply(arg, esm.GetState().GetType().ToString());
                    return;
                case "hud":
                    ShowTeamSelectHud(arg.Player());
                    //PauseGame();
                    return;
                case "joinblueteam":
                    SendReply(arg, "joining blue team");
                    esm.currentGameStateManager.eg.AddPlayerToTeam(arg.Player(),
                        esm.currentGameStateManager.eg.GetTeamById("team_1"));
                    //PauseGame();
                    return;
                case "joinredteam":
                    SendReply(arg, "joining red team");

                    esm.currentGameStateManager.eg.AddPlayerToTeam(arg.Player(),
                        esm.currentGameStateManager.eg.GetTeamById("team_2"));
                    return;
                case "joinrandomteam":
                    SendReply(arg, "joining random team");
                    //PauseGame();
                    return;




            }
        }

        #endregion

    }
}
