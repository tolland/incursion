//Requires: ZoneManager
using System;
using System.Reflection;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using UnityEngine;
using Random = System.Random;
using System.Collections.Generic;
using System.IO;
using Oxide.Core;

namespace Oxide.Plugins
{

    [Info("Incursion Utilities", "tolland", "0.1.0")]
    public class IemUtils : RustPlugin
    {

        [PluginReference]
        Plugin ZoneManager;
        static Oxide.Game.Rust.Libraries.Rust rust = GetLibrary<Oxide.Game.Rust.Libraries.Rust>();
        static FieldInfo monumentsField = typeof(TerrainPath).GetField("Monuments", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        public static List<MonumentInfo> monuments = new List<MonumentInfo>();

        static IemUtils iemUtils = null;

        void Init()
        {
            iemUtils = this;
            LogL("");
            LogL("Init in iemutils");
        }


        void Loaded()
        {
            LogL("iemutils: server loaded");
        }

        void OnServerInitialized()
        {
            LogL("iemutils: server initialized");
        }

        #region player modifications

        public static void SetMetabolismValues(BasePlayer player)
        {
            player.metabolism.calories.max = 500;
            player.metabolism.calories.value = 500;
            player.health = 100;
            //player.metabolism.health.max = 100;
            player.metabolism.hydration.max = 250;
            player.metabolism.hydration.value = 250;


        }


        public static void SetMetabolismNoNutrition(BasePlayer player)
        {
            player.metabolism.calories.max = 500;
            player.metabolism.calories.value = 500;
            player.metabolism.hydration.max = 250;
            player.metabolism.hydration.value = 250;
        }



        #endregion


        void DeleteEverything()
        {
            var objects = UnityEngine.Object.FindObjectsOfType<BuildingBlock>();
            if (objects != null)
                foreach (var gameObj in objects)
                    UnityEngine.Object.Destroy(gameObj);
        }


        #region Functions
        //from em
        public bool hasAccess(ConsoleSystem.Arg arg)
        {
            if (arg.connection?.authLevel < 1)
            {
                SendReply(arg, GetMessage("MessagesPermissionsNotAllowed"));
                return false;
            }
            return true;
        }

        //private bool hasPermission(BasePlayer player, string permname)
        //{
        //    return isAdmin(player) || permission.UserHasPermission(player.UserIDString, permname);
        //}

        public static bool isAdmin(BasePlayer player)
        {
            if (player?.net?.connection == null) return true;
            return player.net.connection.authLevel > 0;
        }

        private string GetMessage(string key) => lang.GetMessage(key, this);

        public static void DLog(string message)
        {
            ConVar.Server.Log("oxide/logs/ESMlog.txt", message);
            iemUtils.Puts(message);
            //Interface.Oxide.LogInfo("[{0}] {1}", (object)this.Title, (object)(args.Length <= 0 ? format : string.Format(format, args)));
        }

        public static void SLog(string strMessage)
        {
            ConVar.Server.Log("oxide/logs/Statelog.txt", strMessage);
            //string strFilename = "oxide/logs/Statelog.txt";
            ////iemUtils.Puts(message);
            //File.AppendAllText(string.Format("{0}/{1}", (object)ConVar.Server.rootFolder, (object)strFilename), string.Format("[{0}] {1}\r\n", (object)DateTime.Now.ToString(), (object)strMessage));
        }

        public static void DDLog(string message)
        {
            ConVar.Server.Log("oxide/logs/DDlog.txt", message);
            //iemUtils.Puts(message);
        }

        public static void SchLog(string message)
        {
            ConVar.Server.Log("oxide/logs/schedlog.txt", message);
            //iemUtils.Puts(message);
        }

        public static void LogL(string message)
        {
            ConVar.Server.Log("oxide/logs/Loadlog.txt", message);
            ConVar.Server.Log("oxide/logs/ESMlog.txt", message);
            iemUtils.Puts(message);
        }

        private static string prefix;
        public static void SendMessage(BasePlayer player, string message, params object[] args)
        {
            prefix = Convert.ToString("<color=#FA58AC>Debug:</color> ");
            if (player != null)
            {
                if (args.Length > 0)
                    message = string.Format(message, args);
                iemUtils.SendReply(player, $"{prefix}{message}");
            }
            else
                iemUtils.Puts(message);
        }


        public static void BroadcastChat(string message)
        {
            rust.BroadcastChat(message);
        }

        #endregion

        #region zone utils

        public static void CreateZone(string name, Vector3 location, int radius)
        {
            //iemUtils.Puts("creating zone");

            //ZoneManager.Call("EraseZone", "zone_" + name);

            iemUtils.ZoneManager.Call("CreateOrUpdateZone",
                "zone_" + name,
                new string[]
                {
                    "radius", radius.ToString(),
                    "autolights", "true",
                    "eject", "false",
                    "enter_message", "",
                    "leave_message", "",
                    "killsleepers", "true"
                }, location);

            CreateSphere(location, (radius * 2) + 1);

        }

        private const string SphereEnt = "assets/prefabs/visualization/sphere.prefab";

        public static void CreateSphere(Vector3 position, float radius)
        {
            // Puts("CreateSphere works!");
            BaseEntity sphere = GameManager.server.CreateEntity(SphereEnt,
                position, new Quaternion(), true);
            SphereEntity ent = sphere.GetComponent<SphereEntity>();
            //iemUtils.Puts("prefabID " + sphere.prefabID);

            ent.currentRadius = radius;
            ent.lerpSpeed = 0f;
            sphere?.Spawn();


        }

        #endregion


        #region finding stuff

        static int doorColl = UnityEngine.LayerMask.GetMask(new string[] { "Construction Trigger", "Construction" });


        T FindComponentNearestToLocation<T>(Vector3 location, int radius)
        {
            T component = default(T);
            foreach (Collider col in Physics.OverlapSphere(location, 2f, doorColl))
            {
                if (col.GetComponentInParent<Door>() == null) continue;


                if (Mathf.Ceil(col.transform.position.x) == Mathf.Ceil(location.x)
                    && Mathf.Ceil(col.transform.position.y) == Mathf.Ceil(location.y)
                    && Mathf.Ceil(col.transform.position.z) == Mathf.Ceil(location.z))
                {
                    //Plugins.IemUtils.DLog("found the door");
                    component = col.GetComponentInParent<T>();
                }
            }
            if (component != null)
                return component;
            return default(T);
        }

        public static BasePlayer FindPlayerByID(ulong steamid)
        {
            BasePlayer targetplayer = BasePlayer.FindByID(steamid);
            if (targetplayer != null)
            {
                return targetplayer;
            }
            targetplayer = BasePlayer.FindSleeping(steamid);
            if (targetplayer != null)
            {
                return targetplayer;
            }
            return null;
        }



        #endregion


        #region geo stuff

        public static Vector3 Vector3Down = new Vector3(0f, -1f, 0f);
        public static int groundLayer = LayerMask.GetMask("Construction", "Terrain", "World");

        public static Vector3 GetGroundY(Vector3 position)
        {

            position = position + Vector3.up;
            position = position + Vector3.up;
            RaycastHit hitinfo;
            if (Physics.Raycast(position, Vector3Down, out hitinfo, 100f, groundLayer))
            {
                DLog("returning in groundy: "+ hitinfo.point + Vector3.up + Vector3.up);
                return hitinfo.point + Vector3.up + Vector3.up;
            }

            IemUtils.DLog("couldn't find ground point");
            return position;
        }


        //static void TeleportPlayerPosition(BasePlayer player, Vector3 destination)
        //{
        //    //DLog("teleporting player from " + player.transform.position.ToString());
        //    //DLog("teleporting player to   " + destination.ToString());
        //    player.MovePosition(destination);
        //    player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
        //    player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
        //    player.UpdateNetworkGroup();
        //    player.SendNetworkUpdateImmediate(false);
        //    player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
        //    player.SendFullSnapshot();
        //}

        public static void MovePlayerTo(BasePlayer player, Vector3 loc)
        {
            DLog("moving player "+player.UserIDString);
            if (player.inventory.loot.IsLooting())
            {
                player.EndLooting();
            }
            player.CancelInvoke("InventoryUpdate");
            player.inventory.crafting.CancelAll(true);
            rust.ForcePlayerPosition(player, loc.x, loc.y, loc.z);
            player.SendNetworkUpdateImmediate();
        }

        static Random random = new System.Random();

        public static Vector3 GetRandomPointOnCircle(Vector3 centre, float radius)
        {

            //get a random angle in radians
            float randomAngle = (float)random.NextDouble() * (float)Math.PI * 2.0f;

            Vector3 loc = centre;

            loc.x = loc.x + ((float)Math.Cos(randomAngle) * radius);
            loc.z = loc.z + ((float)Math.Sin(randomAngle) * radius);

            return loc;
        }

        #endregion


        #region environment modifications

        public static void RunServerCommand(string key, string val)
        {

            rust.RunServerCommand("env.time", "12");
        }

        #endregion


        #region scheduled event global

        public Dictionary<Guid, IemUtils.ScheduledEvent> scheduledEvents =
    new Dictionary<Guid, IemUtils.ScheduledEvent>();

        public class ScheduledEvent
        {
            public DateTime Start { get; set; }
            DateTime End;   // ??
            public int Length { get; set; }
            public Dictionary<Guid, ScheduledEventTeam> schTeams = new Dictionary<Guid, ScheduledEventTeam>();
            public Dictionary<Guid, ScheduledEventPlayer> schPlayers = new Dictionary<Guid, ScheduledEventPlayer>();
            public Guid guid;
            public string EventName { get; set; }

            public ScheduledEvent(DateTime newStart, int newLength)
            {
                Start = newStart;
                Length = newLength;
                guid = Guid.NewGuid();
                EventName = "Default Name";
            }

            public ScheduledEventTeam GetTeam(string teamId)
            {
                foreach (var teamsValue in schTeams.Values)
                {
                    if (teamsValue.TeamId == teamId)
                    {
                        return teamsValue;
                    }
                }
                return null;
            }

            public ScheduledEventPlayer GetPlayer(string steamId)
            {
                foreach (var eplayer in schPlayers.Values)
                {
                    if (eplayer.steamId == steamId)
                    {
                        return eplayer;
                    }
                }
                return null;
            }




            public ScheduledEvent(DateTime newStart, int newLength, string newEventName)
            {
                Start = newStart;
                Length = newLength;
                guid = Guid.NewGuid();
                EventName = newEventName;
            }

            //public ScheduledEvent(DateTime newStart, int newLength, Guid newGuid)
            //{
            //    Start = newStart;
            //    Length = newLength;
            //    guid = newGuid;
            //    EventName = "Default Name";
            //}

            public ScheduledEvent(DateTime newStart, int newLength, Guid newGuid, string newEventName)
            {
                Start = newStart;
                Length = newLength;
                guid = newGuid;
                EventName = newEventName;
            }

            public class ScheduledEventTeam
            {
                public string TeamName { get; set; }
                public string TeamId { get; set; }
                public string Color { get; set; }
                public Dictionary<Guid, ScheduledEventPlayer> schPlayers = new Dictionary<Guid, ScheduledEventPlayer>();
                public string JoinCommand { get; set; }
                public bool TeamOpen { get; set; }
                public ScheduledEvent scheduledEvent { get; set; }
                public string AnchorMin = "";
                public string AnchorMax = "";
                public Guid guid;

                // create a team anew
                public ScheduledEventTeam(
                    string newTeamName,
                    string newColor,
                    string newJoinCommand,
                    bool newTeamOpen,
                    ScheduledEvent obj
                    )
                {
                    TeamName = newTeamName;
                    TeamId = "team_" + newColor;
                    Color = newColor;
                    JoinCommand = newJoinCommand;
                    TeamOpen = newTeamOpen;
                    scheduledEvent = obj;
                    guid = Guid.NewGuid();
                    scheduledEvent.schTeams[guid] = this;
                }

                // recreating a team from the database
                public ScheduledEventTeam(
                    string newTeamName,
                    string newColor,
                    string newJoinCommand,
                    bool newTeamOpen,
                    ScheduledEvent obj,
                    Guid newGuid)
                {
                    TeamName = newTeamName;
                    Color = newColor;
                    TeamId = "team_" + newColor;
                    JoinCommand = newJoinCommand;
                    TeamOpen = newTeamOpen;
                    scheduledEvent = obj;
                    scheduledEvent.schTeams[newGuid] = this;
                    guid = newGuid;
                }
            }

            public class ScheduledEventPlayer
            {
                public string steamId { get; set; }
                public string DisplayName { get; set; }
                public ScheduledEventTeam schTeam;
                public ScheduledEvent schEvent { get; set; }
                public Guid guid;

                //not sure this is being called
                //public ScheduledEventPlayer(string newSteamid)
                //{
                //    steamId = newSteamid;
                //    guid = Guid.NewGuid();
                //}

                /// <summary>
                /// probably being created during a player register for a future event in game
                /// know event, but don't have a guid
                /// </summary>
                /// <param name="newSteamid"></param>
                /// <param name="newSchEvent"></param>
                public ScheduledEventPlayer(string newSteamid, ScheduledEventTeam newSchEventTeam)
                {
                    steamId = newSteamid;
                    DisplayName = newSteamid; //for something to display if this isn't set
                    schEvent = newSchEventTeam.scheduledEvent;
                    guid = Guid.NewGuid();
                    schEvent.schTeams[newSchEventTeam.guid].schPlayers[guid] = this;
                    schEvent.schPlayers[guid] = this;
                    schTeam = newSchEventTeam;
                }

                //public ScheduledEventPlayer(string newSteamid, ScheduledEvent newSchEvent)
                //{
                //    steamId = newSteamid;
                //    DisplayName = newSteamid; //for something to display if this isn't set
                //    schEvent = newSchEvent;
                //    guid = Guid.NewGuid();
                //    schEvent.schPlayers[guid] = this;
                //}

                //public ScheduledEventPlayer(string newSteamid, ScheduledEvent newSchEvent, Guid newGuid)
                //{
                //    steamId = newSteamid;
                //    DisplayName = newSteamid; //for something to display if this isn't set
                //    schEvent = newSchEvent;
                //    guid = newGuid;
                //    schEvent.schPlayers[guid] = this;
                //}

                public ScheduledEventPlayer(string newSteamid, ScheduledEventTeam newSchEventTeam, Guid newGuid)
                {
                    steamId = newSteamid;
                    DisplayName = newSteamid; //for something to display if this isn't set
                    schEvent = newSchEventTeam.scheduledEvent;
                    guid = newGuid;
                    schEvent.schPlayers[guid] = this;
                    schEvent.schTeams[newSchEventTeam.guid].schPlayers[guid] = this;
                    schTeam = newSchEventTeam;
                }
            }
        }

        #endregion
    }
}