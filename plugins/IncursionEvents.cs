//Requires: IncursionEventGame
//Requires: IncursionHoldingArea
//Requires: IncursionUI
//Requires: IncursionStateManager
//Requires: IemUtils
using System;
using System.Collections.Generic;
using ConVar;
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

        private EventStateManager esm;
        static IncursionEvents incursionEvents = null;

        void Init()
        {
            incursionEvents = this;
        }



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


        StoredData storedData;

        void Loaded()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("MyDataFile");
        }

        void OnServerInitialized()
        {
            rust.RunServerCommand("weather.fog", "0");
            rust.RunServerCommand("weather.rain", "0");
            rust.RunServerCommand("heli.lifetimeminutes", "0");

            esm = new EventStateManager(ServerRunning.Instance);
            esm.ChangeState(EventManagementLobby.Instance);
            esm.eg = new IncursionEventGame.EventGame();
            esm.ChangeState(GameLoaded.Instance);
            esm.ChangeState(EventLobbyOpen.Instance);

            //@todo this needs to come from config
            //InitializeESM();
            //StartEventManagementLobby();
            //LoadGameStub();
            //OpenEvent();
        }

        static void RunServerCommand(string key, string val)
        {

            incursionEvents
            .rust.RunServerCommand("env.time", "12");
        }

        void RepeatGameStub()
        {
            esm.eg = new IncursionEventGame.EventGame();
            esm.ChangeState(GameLoaded.Instance);
            //probably shouldn't do this.... @todo
            esm.ChangeState(EventLobbyOpen.Instance);
        }

        void OpenEvent()
        {
            esm.ChangeState(EventLobbyOpen.Instance);
        }

        void CloseEvent()
        {
            esm.ChangeState(EventLobbyClosed.Instance);
        }


        void StartGame()
        {
            esm.ChangeState(GameLobby.Instance);
        }

        void PauseGame()
        {
            esm.ChangeState(GameStartedAndPaused.Instance);
        }

        void CloseGame()
        {
            esm.ChangeState(GameStartedAndClosed.Instance);
        }

        void EndGameWarning()
        {
            rust.BroadcastChat("end game warning");
        }

        void EndGameFinalWarning()
        {
            rust.BroadcastChat("end game final warning");
        }

        void EndGame()
        {
            esm.ChangeState(GameComplete.Instance);
        }

        ///This section is private methods for ESM
        /// 


        void MovePlayersToEsmLobby()
        {
            IemUtils.DLog("moving players to esm lobby");
            Puts("teleporting player to");

            System.Random rnd = new System.Random();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                //@todo move this to the game definition
                Vector3 loc = new Vector3(-236,3,18);
                float radius = 9.5f;
                loc = IemUtils.GetRandomPointOnCircle(loc, radius);
                IemUtils.MovePlayerTo(player, loc);


            }
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

        void OnEnterZone(string ZoneID, BasePlayer player)
        {
            //is this a team zone?
            if (ZoneID.StartsWith("zone_team_"))
            {
                OnPlayerEnterTeamArea(player, GetTeamFromZone(ZoneID));
            }
        }

        IncursionEventGame.EventTeam GetTeamFromZone(string ZoneID)
        {
            IemUtils.DLog("team substring is " + ZoneID.Substring(5));
            return esm.eg.GetTeamById(ZoneID.Substring(5));
        }


        void OnExitZone(string ZoneID, BasePlayer player)
        {
            // if (Started)
            //     if (prisonIDs.Contains(ZoneID))
            //         if (jailData.Prisoners.ContainsKey(player.userID)) { SendMsg(player, lang.GetMessage("keepIn", this, player.UserIDString)); }
        }

        void OnPlayerEnterTeamArea(BasePlayer player, IncursionEventGame.EventTeam team)
        {
            esm.eg.AddPlayerToTeam(player, team);
           
        }
   

        void ResetPlayerStatuses()
        {
            List<string> zones = (List<string>)ZoneManager.Call("GetZoneList");
            foreach (string zone in zones)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    ZoneManager.Call("RemovePlayerFromZoneKeepinlist", zone, player);
                }
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

            if (eventPlayer.psm.eg == null)
            {
                IemUtils.DLog("event Game was null");
                if (esm.eg != null)
                {
                    IemUtils.DLog("adding eventgame to playerstatemanager");
                    eventPlayer.psm.eg = esm.eg;
                }
            }

            if (esm.GetState().Equals(EventManagementLobby.Instance)
                || esm.GetState().Equals(EventLobbyOpen.Instance)
                || esm.GetState().Equals(EventLobbyClosed.Instance)
                )
            {
                //TeleportPlayerPosition(player, esm.eventLobby.location);
                Puts("need to teleport player");
                Puts(eventPlayer.psm.GetState().ToString());
                if ((bool)ZoneManager.Call("isPlayerInZone", "lobby", player))
                {
                    Puts("player in zone for lobby");
                }
                else
                {
                    Puts("player not in zone for lobby");
                    //TeleportPlayerPosition(player, esm.eventLobby.location);
                }

                eventPlayer.psm.ChangeState(IncursionEventGame.PlayerInEventLobbyNoTeam.Instance);
            }

            if (esm.GetState().Equals(GameStartedAndOpen.Instance)
                || esm.GetState().Equals(GameStartedAndClosed.Instance)
                || esm.GetState().Equals(GameStartedAndPaused.Instance)
            )
            {
                player.MovePosition(eventPlayer.eventTeam.Location);
                player.ClientRPCPlayer(null, player, "ForcePositionTo", player.transform.position);
                player.TransformChanged();
                player.SendNetworkUpdateImmediate();
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

                Vector3 loc = (Vector3) esm.eventLobby.location;
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



        void OnPlayerDisconnected(BasePlayer player)
        {

        }

        #endregion



        public class EventStateManager : IncursionStateManager.StateManager
        {
            public IncursionEventGame.EventGame eg;
            public IncursionHoldingArea.Lobby eventLobby;

            public EventStateManager(IncursionStateManager.IStateMachine initialState) : base(initialState)
            {
            }
        }

        public class ServerRunning : IncursionStateManager.StateBase<ServerRunning>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("entry in ServerRunning");
                IncursionUI.CreateAdminBanner("state:" + esm.GetState().ToString());
            }
        }

        public class EventManagementLobby : IncursionStateManager.StateBase<EventManagementLobby>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager esm)
            {
                incursionEvents.ResetPlayerStatuses();
                IncursionUI.CreateAdminBanner("state:" + esm.GetState().ToString());
                incursionEvents.CreateEsmLobby();
                IncursionHoldingArea.CloseTeamDoors();
                incursionEvents.MovePlayersToEsmLobby();
                
            }
        }

        public class GameLoaded : IncursionStateManager.StateBase<GameLoaded>,
            IncursionStateManager.IStateMachine
        {

        }

        public class EventLobbyOpen : IncursionStateManager.StateBase<EventLobbyOpen>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("entry into EventLobbyOpen");

                IncursionUI.CreateAdminBanner("state:" + esm.GetState().ToString());
                RunServerCommand("env.time", "12");
                esm = ((EventStateManager)esm);
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    IncursionEventGame.EventPlayer eventPlayer
                        = IncursionEventGame.GetEventPlayer(player);
                    eventPlayer.psm.eg = ((EventStateManager)esm).eg;
                    eventPlayer.psm.ChangeState(IncursionEventGame.PlayerInEventLobbyNoTeam.Instance);
                }
                incursionEvents.rust.BroadcastChat("Opening lobby");
                IncursionHoldingArea.OpenTeamDoors();
            }

            public new void Execute(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("executing in EventLobbyOpen");

                if (((EventStateManager)esm).eg.CanGameStart())
                {
                    incursionEvents.rust.BroadcastChat("Game starting in 5 seconds");
                    int count = 5;
                    Timer countdown = incursionEvents.timer.Repeat(1f, 5, () =>
                    {
                        IncursionUI.CreateBanner("game starting in "+count.ToString());
                        count--;
                    });
                        Timer warningTimer = incursionEvents.timer.Once(5f, () =>
                    {
                        incursionEvents.StartGame();
                    });
                }
            }
            public new void Exit(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("exiting in EventLobbyOpen");
                IncursionUI.RemoveTeamUI();
                IncursionHoldingArea.CloseTeamDoors();

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


        public class EventLobbyClosed : IncursionStateManager.StateBase<EventLobbyClosed>, IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager esm)
            {
                IncursionUI.CreateAdminBanner("state:" + esm.GetState().ToString());
                IemUtils.DLog("entry in EventLobbyClosed");
                IncursionHoldingArea.CloseTeamDoors();
            }
            public new void Exit(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("exiting in EventLobbyClosed");
                IncursionUI.RemoveTeamUI();
            }
        }

   
        public class GameLobby : IncursionStateManager.StateBase<GameLobby>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("entry in GameLobby");
                ((EventStateManager)esm).eg.MovePlayersToGame();

                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    IncursionEventGame.EventPlayer eventPlayer
                        = IncursionEventGame.GetEventPlayer(player);
                    eventPlayer.psm.eg = ((EventStateManager) esm).eg;
                    IncursionUI.ShowGameBanner(player,((EventStateManager) esm).eg.GameIntroBanner);
                }

                IncursionUI.CreateBanner("Play starting in 10 seconds");
                incursionEvents.rust.BroadcastChat("Play starting in 10 seconds");
                int count = 9;
                Timer countdown = incursionEvents.timer.Repeat(1f, 10, () =>
                {
                    IncursionUI.CreateBanner("play starting in " + count.ToString());
                    count--;
                });
                Timer warningTimer = incursionEvents.timer.Once(10f, () =>
                {       
                    esm.ChangeState(GameStartedAndOpen.Instance);
                });
            }

            public new void Exit(IncursionStateManager.StateManager esm)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    IncursionEventGame.EventPlayer eventPlayer
                        = IncursionEventGame.GetEventPlayer(player);
                    eventPlayer.psm.eg = ((EventStateManager)esm).eg;
                    IncursionUI.HideGameBanner(player);
                }
                
            }
        }



        public class GameStartedAndOpen : IncursionStateManager.StateBase<GameStartedAndOpen>, IncursionStateManager.IStateMachine
        {
            private Timer warningTimer;
            private Timer finalWarningTimer;
            private Timer gameTimer;

            private DateTime startTime = DateTime.UtcNow;
            TimeSpan breakDuration = TimeSpan.FromSeconds(15);
            public new void Enter(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("entry in Game Started And Open");
                IncursionUI.CreateAdminBanner("state:" + esm.GetState().ToString());

                incursionEvents.rust.BroadcastChat("Game Started And Open");
                warningTimer = incursionEvents.timer.Once(20f, () =>
                {
                    incursionEvents.EndGameWarning();
                    IncursionUI.CreateBanner("Game ending in 10 seconds - warning");
                });
                finalWarningTimer = incursionEvents.timer.Once(25f, () =>
                {
                    incursionEvents.EndGameFinalWarning();
                    IncursionUI.CreateBanner("Game ending in 5 seconds - final warning");
                });
                gameTimer = incursionEvents.timer.Once(30f, () =>
                {
                    IncursionUI.CreateBanner("Game ended");
                    incursionEvents.EndGame();
                });

            }
        }


        public class GameStartedAndClosed : IncursionStateManager.StateBase<GameStartedAndClosed>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("entry in GameStartedAndClosed");
                IncursionUI.CreateAdminBanner("state:" + esm.GetState().ToString());
            }
        }
        
        public class GameStartedAndPaused : IncursionStateManager.StateBase<GameStartedAndPaused>,
            IncursionStateManager.IStateMachine { }
        
        public class GameComplete : IncursionStateManager.StateBase<GameComplete>,
            IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("entry in GameComplete");
                ((EventStateManager)esm).eg.ShowGameResultUI();
                Timer warningTimer = incursionEvents.timer.Once(10f, () =>
                {
                    incursionEvents.RepeatGameStub();
                });
            }
            public new void Exit(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("exiting in GameComplete");
                ((EventStateManager)esm).eg.RemoveGameResultUI();
                incursionEvents.MovePlayersToEsmLobby();
            }
        }

       


        #region chat control

        [ChatCommand("Test")]
        void Test(BasePlayer player, string command, string[] args)
        {
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

        [ChatCommand("testmove")]
        private void cmdChatZoneRemove(BasePlayer player, string command, string[] args)
        {
            player.MovePosition(new Vector3(-376, 3, 4));
            player.ClientRPCPlayer(null, player, "ForcePositionTo", player.transform.position);
            player.TransformChanged();
            player.SendNetworkUpdateImmediate();

            IemUtils.SendMessage(player, "Value missing...");
        }
        
        #endregion



        #region console control

        //from em
        [ConsoleCommand("event")]
        void ccmdEvent(ConsoleSystem.Arg arg)
        {
            if (!IemUtils.hasAccess(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "event open - Open a event");
                SendReply(arg, "event cancel - Cancel a event");
                return;
            }
            switch (arg.Args[0].ToLower())
            {
                //if there is a EventGame availabe, autostart it
                case "autostart":
                    SendReply(arg, "autostarting");
                    //InitializeESM();
                    //StartEventManagementLobby();
                    //LoadGameStub();
                    OpenEvent();
                    return;
                case "offline":
                    SendReply(arg, "setting offline");
                    //InitializeESM();
                    return;

                //the following section are debug commands and generally not used
                //by admins unless there is a problem with the game
                case "init":
                    SendReply(arg, "creating event manager with stub");
                    //StartEventManagementLobby();
                    return;
                //this loads a minimal stub game
                case "stub":
                    SendReply(arg, "creating event manager with stub");
                    //LoadGameStub();
                    return;

                case "start":
                    SendReply(arg, "starting game");
                    StartGame();
                    return;

                case "pause":
                    SendReply(arg, "pausing game");
                    PauseGame();
                    return;
                case "state":
                    SendReply(arg, "showing state game");
                    SendReply(arg, esm.GetState().ToString());
                    SendReply(arg, esm.GetState().GetType().ToString());
                    return;



            }
        }
        #endregion


    }


}

