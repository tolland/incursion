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
using Steamworks;
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

        public EventStateManager esm;
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
            Unsubscribe(nameof(OnRunPlayerMetabolism));
            //Unsubscribe(nameof(OnEntityTakeDamage));
            //Unsubscribe(nameof(OnPlayerRespawned));
            //Unsubscribe(nameof(OnPlayerAttack));
            //Unsubscribe(nameof(OnItemPickup));
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("MyDataFile");
        }

        void OnRunPlayerMetabolism(PlayerMetabolism m, BaseCombatEntity entity)
        {
            var player = entity.ToPlayer();
            if (player == null) return;
            IemUtils.SetMetabolismValues(player);
        }

        void OnServerInitialized()
        {
            rust.RunServerCommand("weather.fog", "0");
            rust.RunServerCommand("weather.rain", "0");
            rust.RunServerCommand("heli.lifetimeminutes", "0");

            esm = new EventStateManager(ServerRunning.Instance);

            // the purpose of the event management lobby is for player
            // when there is no game state manager available
            // this is the default esm state until a gamemanager is loaded
            IemUtils.DLog("creating the event management lobby");
            esm.ChangeState(EventManagementLobby.Instance);




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



        void EndGameWarning()
        {
            rust.BroadcastChat("end game warning");
        }

        void EndGameFinalWarning()
        {
            rust.BroadcastChat("end game final warning");
        }



        ///This section is private methods for ESM
        /// 


        void MovePlayersToEsmLobby()
        {
            IemUtils.DLog("moving players to esm lobby");

            System.Random rnd = new System.Random();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                //@todo move this to the game definition
                Vector3 loc = new Vector3(-236, 3, 18);
                float radius = 9.5f;
                loc = IemUtils.GetRandomPointOnCircle(loc, radius);
                IemUtils.MovePlayerTo(player, loc);
                IemUtils.SetMetabolismValues(player);

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



        IncursionEventGame.EventTeam GetTeamFromZone(string ZoneID)
        {
            IemUtils.DLog("team substring is " + ZoneID.Substring(5));
            return esm.eg.GetTeamById(ZoneID.Substring(5));
        }

        void OnPlayerEnterTeamArea(BasePlayer player, IncursionEventGame.EventTeam team)
        {
            esm.eg.AddPlayerToTeam(player, team);

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

        #endregion



        public class EventStateManager : IncursionStateManager.StateManager
        {
            public IncursionEventGame.EventGame eg;
            public IncursionHoldingArea.Lobby eventLobby;
            private Dictionary<string, IncursionEventGame.GameStateManager> gameStateManagers
                = new Dictionary<string, IncursionEventGame.GameStateManager>();
            private IncursionEventGame.GameStateManager currentGameStateManager;

            public EventStateManager(IncursionStateManager.IStateMachine initialState) : base(initialState)
            {
            }

            public void RegisterGameStateManager(IncursionEventGame.GameStateManager gameStateManager)
            {
                gameStateManagers.Add(gameStateManager.Name, gameStateManager);
                ChangeState(GameLoaded.Instance);
                eg = gameStateManager.eg;
                //@todo this is crap. to fix
                currentGameStateManager = gameStateManager;
            }

            public void GameComplete()
            {
                IemUtils.DLog("game is complete, and event manager has been informed");
                currentGameStateManager.ChangeState(IemGameTeams.GameUnloaded.Instance);
                ChangeState(EventManagementLobby.Instance);
                currentGameStateManager.ReinitializeGame();
                eg = currentGameStateManager.eg;
                ChangeState(EventLobbyOpen.Instance);
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

                incursionEvents.Subscribe(nameof(OnRunPlayerMetabolism));
                //incursionEvents.Subscribe(nameof(OnEntityTakeDamage));
                //Subscribe(nameof(OnPlayerRespawned));

                incursionEvents.MovePlayersToEsmLobby();

            }

            public new void Exit(IncursionStateManager.StateManager esm)
            {
                incursionEvents.Unsubscribe(nameof(OnRunPlayerMetabolism));
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
                incursionEvents.Subscribe(nameof(OnRunPlayerMetabolism));
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


            public new void Execute(IncursionStateManager.StateManager sm)
            {

                EventStateManager esm = (EventStateManager)sm;
                IemUtils.DLog("executing in EventLobbyOpen");


                if (esm.eg.CanGameStart())
                {
                    IncursionUI.CreateBanner("GAME CAN START");
                    int count = 5;
                    Timer countdown = incursionEvents.timer.Repeat(1f, 5, () =>
                    {
                        IncursionUI.CreateBanner("game starting in " + count.ToString());
                        count--;
                    });
                    Timer warningTimer = incursionEvents.timer.Once(5f, () =>
                    {
                        esm.ChangeState(EventLobbyClosed.Instance);
                        esm.ChangeState(EventRunning.Instance);
                        esm.eg.StartGame();
                        //currentGameStateManager.ReinitializeGame();
                    });
                }
                IemUtils.DLog("here3");
            }

            public new void Exit(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("exiting in EventLobbyOpen");
                IncursionUI.RemoveTeamUI();
                //IncursionHoldingArea.CloseTeamDoors();
            }
        }



        public class EventLobbyClosed : IncursionStateManager.StateBase<EventLobbyClosed>, IncursionStateManager.IStateMachine
        {
            public new void Enter(IncursionStateManager.StateManager esm)
            {
                IncursionUI.CreateAdminBanner("state:" + esm.GetState().ToString());
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
                IncursionUI.CreateAdminBanner("state:" + esm.GetState().ToString());
                IemUtils.DLog("entry in EventRunning");
                incursionEvents.Unsubscribe(nameof(OnRunPlayerMetabolism));


            }
            public new void Exit(IncursionStateManager.StateManager esm)
            {
                IemUtils.DLog("exiting in EventRunning");

                incursionEvents.Subscribe(nameof(OnRunPlayerMetabolism));
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
                    OpenEvent();
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



            }
        }
        #endregion

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


    }


}

