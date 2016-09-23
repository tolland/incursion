//Requires: IncursionEventGame
//Requires: IncursionHoldingArea
//Requires: IncursionUI
//Requires: IncursionStateManager
//Requires: IemUtils
using Oxide.Ext.MySql;
using System;
using System.Collections.Generic;
using System.Linq;
using ConVar;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using Steamworks;
using Oxide.Core.Database;
using Physics = UnityEngine.Physics;
using Random = System.Random;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{

    [Info("Incursion Events", "Tolland", "0.1.0")]
    public class IncursionEvents : RustPlugin
    {
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

        public EventStateManager esm;
        static IncursionEvents incursionEvents = null;

        void Init()
        {
            incursionEvents = this;
            IemUtils.LogL("IncursionEvents: init complete");
        }

        StoredData storedData;

        void Loaded()
        {
            esm = new EventStateManager(PluginLoaded.Instance);
            IemUtils.LogL("IncursionEvents: Loaded complete");
        }

        void Unload()
        {
            esm = new EventStateManager(PluginUnload.Instance);
            IemUtils.LogL("IncursionEvents: Unload complete");
        }

        void OnServerInitialized()
        {
            esm.ChangeState(ServerInitialized.Instance);
            esm.Update();
            IemUtils.LogL("IncursionEvents: OnServerInitialized complete");
        }



        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            Config.Clear();
            Config["EventManagementMode"] = "repeating";  //{"scheduled","repeating","once","manual"}
            Config["SchedulerEnabled"] = false;
            Config["DefaultGame"] = "Example Team Game";
            Config["AutoStart"] = true;
            Config["JoinMessage"] = "Welcome to this server";
            Config["LeaveMessage"] = "Goodbye";
            SaveConfig();
        }

        ///This section is private methods for ESM
        /// 
        void MovePlayersToEsmLobby()
        {
            IemUtils.DLog("moving players to esm lobby");

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                //@todo move this to the game definition
                Vector3 loc = new Vector3(-236, 3, 18);
                float radius = 8.5f;
                loc = IemUtils.GetRandomPointOnCircle(loc, radius);
                IemUtils.MovePlayerTo(player, loc);
                IemUtils.SetMetabolismValues(player);

            }
        }

        System.Random rnd = new System.Random();

        void MovePlayerToEsmLobby(BasePlayer player)
        {
            IemUtils.DLog("moving player to esm lobby");



            //@todo move this to the game definition
            Vector3 loc = new Vector3(-236, 3, 18);
            float radius = 8.5f;
            loc = IemUtils.GetRandomPointOnCircle(loc, radius);
            IemUtils.MovePlayerTo(player, loc);
            IemUtils.SetMetabolismValues(player);
        }

        void CreateEsmLobby()
        {
            IemUtils.DLog("CreateEsmLobby");
            esm.eventLobby = new IncursionHoldingArea.Lobby(new Vector3(-231, 2, 14));
        }

        void DestroyEsmLobby()
        {
            IemUtils.DLog("Destroy ESM lobby");
        }





        public class EventStateManager : IncursionStateManager.StateManager
        {
            public IncursionHoldingArea.Lobby eventLobby;
            public Dictionary<string, IncursionEventGame.GameStateManager> gameStateManagers
                = new Dictionary<string, IncursionEventGame.GameStateManager>();
            public IncursionEventGame.GameStateManager currentGameStateManager;

            public EventStateManager(IncursionStateManager.IStateMachine initialState) : base(initialState)
            {
            }

            public override void ChangeState(IncursionStateManager.IStateMachine newState)
            {
                base.ChangeState(newState);
                IemUtils.DDLog("changing state in EventStateManager");
                IncursionUI.CreateAdminBanner("state:" + GetState().ToString());

            }

            public void RegisterGameStateManager(IncursionEventGame.GameStateManager gameStateManager)
            {
                gameStateManagers.Add(gameStateManager.Name, gameStateManager);
                //currentGameStateManager = gameStateManager;
            }

            public void GameComplete()
            {

                //ChangeState(EventManagementLobby.Instance);
            }

            internal void StartScheduledGame(IemUtils.ScheduledEvent sevent)
            {

                if (GetState() == EventManagementLobby.Instance)
                {
                    currentGameStateManager.ReinitializeGame();
                    currentGameStateManager.nextEvent = sevent;

                    ChangeState(EventManagementLobby.Instance);
                }
                else
                {
                    throw new Exception("can't start schedled game, not in EventManagementLobby");
                }


            }
        }

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
                IemUtils.DLog("entry in ServerRunning");
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

                Plugins.IemUtils.DLog("executing in event lobby before game");

                switch ((string)incursionEvents.Config["EventManagementMode"])
                {
                    // make the gsm available and open the event lobby
                    case "repeating":
                        if ((bool)incursionEvents.Config["AutoStart"] == true)
                        {
                            esm.currentGameStateManager = esm.gameStateManagers.First().Value;
                            esm.ChangeState(EventLobbyOpen.Instance);
                            esm.Update();
                        }
                        break;
                    case "once":
                        if ((bool)incursionEvents.Config["AutoStart"] == true)
                        {
                            esm.currentGameStateManager = esm.gameStateManagers.First().Value;
                            esm.ChangeState(EventLobbyOpen.Instance);
                            esm.Update();
                        }
                        break;

                    //gsm will be initialized by the scheduler
                    case "scheduled":
                        //esm.currentGameStateManager = esm.gameStateManagers.First().Value;
                        //

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
        /// a playable game has been loaded, but the lobby is not open to players
        /// </summary>
        public class GameLoaded : IncursionStateManager.StateBase<GameLoaded>,
            IncursionStateManager.IStateMachine
        {

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

                //@todo create a general function for lobby environment settings
                IemUtils.RunServerCommand("env.time", "12");

                //if players/teams exist in the scheduled event, move them to the event game
                //incursionEvents.ProcessScheduledEventToEventGame();

                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {

                    IncursionEventGame.EventPlayer eventPlayer
                        = IncursionEventGame.GetEventPlayer(player);

                    eventPlayer.psm.eg = esm.currentGameStateManager.eg;

                    eventPlayer.psm.ChangeState(IncursionEventGame.PlayerInEventLobbyNoTeam.Instance);
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

                //if (esm.currentGameStateManager.eg.CanGameStart())
                //{
                //    IncursionUI.CreateGameBanner("GAME CAN START");
                //    int count = 5;
                //    Timer countdown = incursionEvents.timer.Repeat(1f, 5, () =>
                //    {
                //        IncursionUI.CreateGameBanner("game starting in " + count.ToString());
                //        count--;
                //    });
                //    Timer warningTimer = incursionEvents.timer.Once(30f, () =>
                //    {
                //        esm.ChangeState(EventLobbyClosed.Instance);
                //        esm.ChangeState(EventRunning.Instance);
                //        esm.currentGameStateManager.eg.StartGame();
                //    });
                //}
            }

            public new void Exit(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("exiting in EventLobbyOpen");
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
                IemUtils.DLog("entry in EventLobbyClosed");
                //IncursionHoldingArea.CloseTeamDoors();
            }
            public new void Exit(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("exiting in EventLobbyClosed");
                IncursionUI.RemoveTeamUI();
            }
        }



        public class EventRunning : IncursionStateManager.StateBase<EventRunning>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("entry in EventRunning");
                incursionEvents.Unsubscribe(nameof(OnRunPlayerMetabolism));


            }
            public new void Exit(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("exiting in EventRunning");
                incursionEvents.Subscribe(nameof(OnRunPlayerMetabolism));
            }
        }

        public class EventComplete : IncursionStateManager.StateBase<EventComplete>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager sm)
            {
                EventStateManager esm = (EventStateManager)sm;
                IemUtils.DLog("entry in EventComplete");

                //don't want players to metablise while here
                incursionEvents.Subscribe(nameof(OnRunPlayerMetabolism));

                switch ((string)incursionEvents.Config["EventManagementMode"])
                {
                    // make the gsm available and open the event lobby
                    case "repeating":
                        if ((bool)incursionEvents.Config["AutoStart"] == true)
                        {
                            //esm.currentGameStateManager = esm.gameStateManagers.First().Value;
                            esm.currentGameStateManager.ReinitializeGame();
                            esm.ChangeState(EventManagementLobby.Instance);
                            esm.Update();
                        }
                        break;

                    //don't do anything, event is complete
                    case "once":


                        break;

                    //gsm will be initialized by the scheduler
                    case "scheduled":
                        //esm.currentGameStateManager = esm.gameStateManagers.First().Value;
                        //

                        break;
                }

            }

            public new void Exit(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("exiting in EventComplete");
                //incursionEvents.Subscribe(nameof(OnRunPlayerMetabolism));
            }
        }


        #endregion

        void ProcessScheduledEventToEventGame()
        {
            //process the list of teams and players who are preregistered for this
            //scheduled event into the concrete game event
            foreach (IemUtils.ScheduledEvent.ScheduledEventTeam seteam in esm.currentGameStateManager.nextEvent.seTeam)
            {
                Plugins.IemUtils.DLog("team is " + seteam.TeamName);
                foreach (IemUtils.ScheduledEvent.ScheduledEventPlayer scheduledEventPlayer in seteam.sePlayer)
                {
                    IemUtils.DLog("sched player is " + scheduledEventPlayer.steamId);
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

        void OnRunPlayerMetabolism(PlayerMetabolism m, BaseCombatEntity entity)
        {
            var player = entity.ToPlayer();
            if (player == null) return;
            IemUtils.SetMetabolismValues(player);
        }



        #region chat control

        [ChatCommand("Test")]
        void Test(BasePlayer player, string command, string[] args)
        {
            if (!IemUtils.isAdmin(player)) return;
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

        public void ShowTeamSelectHud(BasePlayer player)
        {
            IncursionEventGame.EventPlayer eventPlayer = IncursionEventGame.GetEventPlayer(player);
            eventPlayer.psm.SubState(IncursionEventGame.PlayerInTeamSelectHUD.Instance);

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
            IemUtils.DLog("team substring is " + ZoneID.Substring(5));
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
            IemUtils.DLog("calling update team");
            if (esm.GetState().Equals(EventManagementLobby.Instance)
                || esm.GetState().Equals(EventLobbyOpen.Instance)
                || esm.GetState().Equals(EventLobbyClosed.Instance)
            )
            {
                esm.Update();
            }
        }


        /// <summary>
        /// this is called when the player respawns from dead, or after
        /// waking from sleep after connecting
        /// </summary>
        /// <param name="player"></param>
        void CheckPlayer(BasePlayer player)
        {
            Puts("calling check player");
            //The event manager is loaded
            if (esm == null)
                return;

            IncursionEventGame.EventPlayer eventPlayer = IncursionEventGame.GetEventPlayer(player);

            //@todo if this triggers its a bug, seems to happen after dead player reconnects
            if (eventPlayer.psm.eg == null)
            {
                IemUtils.DLog("event Game was null");
                if (esm.currentGameStateManager.eg != null)
                {
                    IemUtils.DLog("adding eventgame to playerstatemanager");
                    eventPlayer.psm.eg = esm.currentGameStateManager.eg;
                }
            }

            //restore the player to the team, @todo fix this crappy hack
            if (esm.currentGameStateManager.eg.gamePlayers.ContainsKey(player.UserIDString))
            {
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

            if (esm.GetState().Equals(EventManagementLobby.Instance)
                || esm.GetState().Equals(EventLobbyOpen.Instance)
                || esm.GetState().Equals(EventLobbyClosed.Instance)
                )
            {
                Puts(eventPlayer.psm.GetState().ToString());
                if ((bool)ZoneManager.Call("isPlayerInZone", "lobby", player))
                {
                    Puts("player in zone for lobby");
                }
                else
                {
                    Puts("player not in zone for lobby");
                }

                eventPlayer.psm.ChangeState(IncursionEventGame.PlayerInEventLobbyNoTeam.Instance);
            }

            if (esm.GetState().Equals(EventRunning.Instance))
            {
                Puts(eventPlayer.psm.GetState().ToString());

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
                       = IncursionEventGame.GetEventPlayer(player);

                eventPlayer.psm.ChangeState(IncursionEventGame.PlayerInGameTeamedDead.Instance);
            }
        }


        // void OnPlayerInit(BasePlayer player) => CheckPlayer(player);
        // void OnPlayerSleepEnded(BasePlayer player) => CheckPlayer(player);

        void OnPlayerInit(BasePlayer player)
        {
            Puts("OnPlayerInit works!");
            IncursionUI.DisplayEnterLobbyUI(player);
        }



        void OnPlayerSleepEnded(BasePlayer player)
        {
            Puts("OnPlayerSleepEnded works!");
            CheckPlayer(player);
        }

        static string CheckUp = "1.0";
        static string CheckDown = "1.0";
        Vector3 vectorUp = new Vector3(0f, 1f, 0f);
        float checkDown = 2f;


        BasePlayer.SpawnPoint OnFindSpawnPoint()
        {
            if (esm.GetState().Equals(EventManagementLobby.Instance)
                || esm.GetState().Equals(EventLobbyOpen.Instance)
                || esm.GetState().Equals(EventLobbyClosed.Instance)
               )
            {
                //@todo move this code
                vectorUp = new Vector3(0f, Convert.ToSingle(CheckUp), 0f);
                checkDown = Convert.ToSingle(CheckUp) + Convert.ToSingle(CheckDown);
                BasePlayer.SpawnPoint point = new BasePlayer.SpawnPoint();

                Vector3 loc = (Vector3)esm.eventLobby.location;
                Random random = new Random();
                float randomAngle = (float)random.NextDouble() * (float)Math.PI * 2.0f;

                Puts("random angle is " + randomAngle.ToString());

                Puts("x modifyier is " + ((float)Math.Cos(randomAngle) * 4.0f));
                Puts("x modifyier is " + ((float)Math.Sin(randomAngle) * 4.0f));

                loc.x = loc.x + ((float)Math.Cos(randomAngle) * 4.0f);
                loc.y = loc.y + ((float)Math.Sin(randomAngle) * 4.0f);

                point.pos = loc;

                Debug.Log(point.pos.ToString());
                point.rot = new Quaternion(0f, 0f, 0f, 1f);
                RaycastHit hit;
                if (checkDown != 0f)
                {
                    if (Physics.Raycast(new Ray(point.pos + vectorUp, Vector3.down), out hit, checkDown, -1063190271))
                    {
                        point.pos = hit.point;
                    }
                }
                return point;
            }

            return null;
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
            Puts("playing respawn");
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
            if (!IemUtils.hasAccess(arg)) return;
            switch (arg.Args[0].ToLower())
            {
                //if there is a EventGame availabe, autostart it
                case "autostart":
                    SendReply(arg, "autostarting");
                    Config["AutoStart"] = true;
                    esm.Update();
                    return;
                case "mode.scheduled":
                    SendReply(arg, "autostarting");
                    Config["EventManagementMode"] = "scheduled";
                    Interface.Oxide.CallHook("OnEventManagementModeChanged");
                    return;
                case "mode.repeating":
                    SendReply(arg, "autostarting");
                    Config["EventManagementMode"] = "repeating";
                    Interface.Oxide.CallHook("OnEventManagementModeChanged");
                    return;
                case "mode once":
                    SendReply(arg, "autostarting");
                    Config["EventManagementMode"] = "once";
                    Interface.Oxide.CallHook("OnEventManagementModeChanged");
                    return;
                case "offline":
                    SendReply(arg, "setting offline");
                    return;

                //the following section are debug commands and generally not used
                //by admins unless there is a problem with the game
                case "init":
                    SendReply(arg, "creating event manager with stub");
                    return;
                //this loads a minimal stub game
                case "stub":
                    SendReply(arg, "creating event manager with stub");
                    return;

                case "start":
                    SendReply(arg, "starting game");
                    return;

                case "pause":
                    SendReply(arg, "pausing game");
                    //PauseGame();
                    return;
                case "state":
                    SendReply(arg, "showing state game");
                    SendReply(arg, esm.GetState().ToString());
                    SendReply(arg, esm.GetState().GetType().ToString());
                    return;
                case "closegame":
                    SendReply(arg, "pausing game");
                    //PauseGame();
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

