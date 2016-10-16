//Requires: ZoneManager
using System;
using System.Reflection;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using UnityEngine;
using Random = System.Random;
using System.IO;
using ConVar;
using Oxide.Core;
using Physics = UnityEngine.Physics;
using Rust;

namespace Oxide.Plugins
{

    [Info("Incursion Utilities", "tolland", "0.1.0")]
    public class IemUtils : RustPlugin
    {
        #region boilerplate
        [PluginReference]
        Plugin ZoneManager;
        static Game.Rust.Libraries.Rust rust = GetLibrary<Game.Rust.Libraries.Rust>();
        static FieldInfo monumentsField = typeof(TerrainPath).GetField("Monuments",
            (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
            BindingFlags.NonPublic));
        public static List<MonumentInfo> monuments = new List<MonumentInfo>();

        static IemUtils iemUtils = null;
        static IemUtils me = null;

        void Init()
        {
            iemUtils = this;
            me = this;
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

        #endregion

        #region player modifications

        private readonly HashSet<ulong> teleporting = new HashSet<ulong>();

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            var player = entity.ToPlayer();

            if (hitinfo.damageTypes.Has(DamageType.Fall))
            {
                me.Puts("detected fall damage in teleport");
                if (teleporting.Contains(player.userID))
                {
                    me.Puts("splatting fall damage in teleport");
                    hitinfo.damageTypes = new DamageTypeList();
                    teleporting.Remove(player.userID);
                }
            }

            if (player == null || hitinfo == null) return;
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (teleporting.Contains(player.userID))
                timer.Once(3, () => { teleporting.Remove(player.userID); });
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            teleporting.Remove(player.userID);
        }


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


        public static void InitHealthState(BasePlayer player)
        {
            player.metabolism.calories.value = 500;
            player.health = 100;
            player.metabolism.hydration.value = 250;
        }

        public static void NullifyDamage(ref HitInfo info)
        {
            info.damageTypes = new DamageTypeList();
            info.HitMaterial = 0;
            info.PointStart = Vector3.zero;
        }

        public static void ClearInventory(BasePlayer player)
        {
            //TODO should save and restore inventory
            player.inventory.Strip();
        }
        private Timer _timer;

        private class FrozenPlayerInfo
        {
            public BasePlayer Player { get; set; }
            public Vector3 FrozenPosition { get; set; }

            public FrozenPlayerInfo(BasePlayer player)
            {
                Player = player;
                FrozenPosition = player.transform.position;
            }
        }

        List<FrozenPlayerInfo> frozenPlayers = new List<FrozenPlayerInfo>();
        void OnTimer()
        {
            foreach (FrozenPlayerInfo current in frozenPlayers)
            {
                if (Vector3.Distance(current.Player.transform.position, current.FrozenPosition) < 1) continue;
                current.Player.ClientRPCPlayer(null, current.Player, "ForcePositionTo", new object[] { current.FrozenPosition });
                current.Player.TransformChanged();
            }
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
                SendReply(arg, "MessagesPermissionsNotAllowed");
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
            Server.Log("oxide/logs/ESMlog.txt", message);
            iemUtils.Puts(message);
            //Interface.Oxide.LogInfo("[{0}] {1}", (object)this.Title, (object)(args.Length <= 0 ? format : string.Format(format, args)));
        }

        public static void SLog(string strMessage)
        {
            Server.Log("oxide/logs/Statelog.txt", strMessage);
            //string strFilename = "oxide/logs/Statelog.txt";
            ////iemUtils.Puts(message);
            //File.AppendAllText(string.Format("{0}/{1}", (object)ConVar.Server.rootFolder, (object)strFilename), string.Format("[{0}] {1}\r\n", (object)DateTime.Now.ToString(), (object)strMessage));
        }

        public static void DDLog(string message)
        {
            Server.Log("oxide/logs/DDlog.txt", message);
            //iemUtils.Puts(message);
        }

        public static void GLog(string message)
        {
            Server.Log("oxide/logs/Glog.txt", message);
            //iemUtils.Puts(message);
        }

        public static void SchLog(string message)
        {
            Server.Log("oxide/logs/schedlog.txt", message);
            //iemUtils.Puts(message);
        }

        public static void TimerLog(string message)
        {
            Server.Log("oxide/logs/timerlog.txt", message);
            //iemUtils.Puts(message);
        }

        public static void LogL(string message)
        {
            Server.Log("oxide/logs/Loadlog.txt", message);
            Server.Log("oxide/logs/ESMlog.txt", message);
            //iemUtils.Puts(message);
        }

        private static string prefix;
        public static void SendMessage(BasePlayer player, string message, params object[] args)
        {
            prefix = Convert.ToString("<color=#FA58AC>Debug:</color> ");
            if (player != null)
            {
                if (args.Length > 0)
                    message = String.Format(message, args);
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
            iemUtils.Puts("creating zone at " + location);

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

        public static BaseEntity CreateSphere(Vector3 position, float radius)
        {
            // Puts("CreateSphere works!");
            BaseEntity sphere = GameManager.server.CreateEntity(SphereEnt,
                position, new Quaternion(), true);
            SphereEntity ent = sphere.GetComponent<SphereEntity>();
            //iemUtils.Puts("prefabID " + sphere.prefabID);

            ent.currentRadius = radius;
            ent.lerpSpeed = 0f;
            sphere?.Spawn();
            return sphere;

        }

        #endregion


        #region finding stuff

        static int doorColl = LayerMask.GetMask(new string[]
        {
            "Construction Trigger", "Construction"
        });

        static int collisionLayer = LayerMask.GetMask("Construction", "Construction Trigger",
            "Trigger", "Deployed", "Default");

        // TODO maybe find the nearest collider?
        public static T FindComponentNearestToLocation<T>(Vector3 location, int radius)
        {
            T component = default(T);

            // IemUtils.DDLog("in search at location " + location);

            float dist = 9999;
            foreach (Collider col in Physics.OverlapSphere(location, radius))
            {
                // IemUtils.DDLog("collider " + col.name);
                if (col.GetComponentInParent<T>() == null) continue;

                //IemUtils.DDLog("not null collider " + col.GetComponentInParent<T>().ToString());

                //IemUtils.DDLog("colx=" + col.transform.position.x);
                //IemUtils.DDLog("locx=" + location.x);
                //IemUtils.DDLog("coly=" + col.transform.position.y);
                //IemUtils.DDLog("locy=" + location.y);
                //IemUtils.DDLog("colz=" + col.transform.position.z);
                //IemUtils.DDLog("locz=" + location.z);

                float tempdist = Vector3.Distance(location, col.transform.position);

                //IemUtils.DDLog("dist is " + tempdist);

                if (tempdist < dist)
                {
                    dist = tempdist;
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

        public static BasePlayer FindPlayerByID(string steamid)
        {
            return FindPlayerByID(UInt64.Parse(steamid));
        }

        // found objects with TODO make this better
        public static List<GameObject> FindObjectsByPrefab(string prefab)
        {
            GameObject[] allobjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

            List<GameObject> foundobjects = new List<GameObject>();

            foreach (var monument_gobject in allobjects)
            {
                //DLog("object is " + monument_gobject.name);

                if (monument_gobject.name.EndsWith(prefab))
                {
                    //Puts (gobject.GetInstanceID ().ToString ());
                    //var pos = monument_gobject.transform.position;
                    //Puts(pos.ToString());
                    foundobjects.Add(monument_gobject);
                }
            }

            return foundobjects;
        }

        // found objects with TODO make this better
        public static GameObject FindObjectByID(int id)
        {
            GameObject[] allobjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

            //List<GameObject> foundobjects = new List<GameObject>();

            foreach (var gobject in allobjects)
            {
                //DLog("object is " + monument_gobject.name);

                if (gobject.GetInstanceID() == id)
                {
                    //Puts (gobject.GetInstanceID ().ToString ());
                    //var pos = monument_gobject.transform.position;
                    //Puts(pos.ToString());
                    return gobject;
                }
            }

            return null;
        }

        // public static GameObject FindObjectAtLocation(string prefab, Vector3 location)
        // {

        // }


        //StorageContainer box = (StorageContainer)BaseNetworkable.serverEntities.Find(net.ID);
        //StorageContainer box1 = BaseNetworkable.serverEntities.Find(net.ID).GetComponent<StorageContainer>();

        public static BaseEntity FindBaseEntityByNetId(uint netId)
        {
            var found = BaseNetworkable.serverEntities.Find(netId);
            if (found != null)
            {
                if (found.net != null)
                {
                    return (BaseEntity)found;
                }
            }

            //var foundentities = UnityEngine.Object.FindObjectsOfType<BaseEntity>();
            //if (foundentities != null)
            //{
            //    foreach (var entity in foundentities)
            //    {
            //        if (entity.net != null)
            //            if (entity.net.ID == netId)
            //                return entity;
            //    }
            //}
            return null;
        }

        private static readonly FieldInfo serverInputField
            = typeof(BasePlayer).GetField("serverInput", BindingFlags.Instance |
                BindingFlags.NonPublic);
        private static readonly FieldInfo instancesField
            = typeof(MeshColliderBatch).GetField("instances", BindingFlags.Instance |
                BindingFlags.NonPublic);

        public static Stack<BuildingBlock> GetTargetBuildingBlock(BasePlayer player)
        {
            var input = serverInputField?.GetValue(player) as InputState;
            if (input == null) return null;
            var direction = Quaternion.Euler(input.current.aimAngles);
            var stack = new Stack<BuildingBlock>();
            RaycastHit initial_hit;
            if (!Physics.Raycast(new Ray(player.transform.position + new Vector3(0f, 1.5f, 0f), direction * Vector3.forward), out initial_hit, 150f) || initial_hit.collider is TerrainCollider)
                return stack;
            var entity = initial_hit.collider.GetComponentInParent<BuildingBlock>();
            if (entity != null) stack.Push(entity);
            else
            {
                var batch = initial_hit.collider?.GetComponent<MeshColliderBatch>();
                if (batch == null) return stack;
                var colliders = (ListDictionary<Component, ColliderCombineInstance>)instancesField.GetValue(batch);
                if (colliders == null) return stack;
                foreach (var instance in colliders.Values)
                {
                    entity = instance.collider?.GetComponentInParent<BuildingBlock>();
                    if (entity == null) continue;
                    stack.Push(entity);
                }
            }
            return stack;
        }

        public static BuildingBlock GetTargetedBuildingBlock(BasePlayer player)
        {
            var input = serverInputField?.GetValue(player) as InputState;
            if (input == null) return null;
            var direction = Quaternion.Euler(input.current.aimAngles);
            var stack = new Stack<BuildingBlock>();
            RaycastHit initial_hit;
            if (!Physics.Raycast(new Ray(player.transform.position + new Vector3(0f, 1.5f, 0f), direction * Vector3.forward), out initial_hit, 150f) || initial_hit.collider is TerrainCollider)
                return null;
            var entity = initial_hit.collider.GetComponentInParent<BuildingBlock>();
            if (entity != null) return entity;
            IemUtils.DDLog("NotLookingAt null");
            return null;
        }

        #endregion

        #region formatting


        static double RoundToSignificantDigits(double d, int digits)

        {
            if (d == 0)
                return 0;

            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);
            return scale * Math.Round(d / scale, digits);
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
                var buf = hitinfo.point;
                DLog("returning in groundy: " + buf);
                return buf;
            }

            IemUtils.DLog("couldn't find ground point");
            return position;
        }

        public static void PlaySound(BasePlayer player)
        {

            Effect effectP = new Effect(
                "assets/prefabs/instruments/guitar/effects/guitarpluck.prefab",
                new Vector3(0, 0, 0), Vector3.forward);
            Effect effectS = new Effect(
                "assets/prefabs/instruments/guitar/effects/guitarpluck.prefab",
                new Vector3(0, 0, 0), Vector3.forward);


            effectP.worldPos = player.transform.position;
            effectP.origin = player.transform.position;
            effectP.scale = 0;
            // EffectNetwork.Send(effectP);
            effectP.scale = 1;
            //EffectNetwork.Send(effectP);
        }


        public static void TeleportPlayerPosition(BasePlayer player, Vector3 destination)
        {
            me.teleporting.Add(player.userID);
            //DLog("teleporting player from " + player.transform.position.ToString());
            //DLog("teleporting player to   " + destination.ToString());
            destination = GetGroundY(destination);
            player.MovePosition(destination);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            player.SendFullSnapshot();


            me.teleporting.Remove(player.userID);
        }

        public static void Teleport(BasePlayer player, Vector3 position)
        {
            //SaveLocation(player);
            me.teleporting.Add(player.userID);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            //StartSleeping(player);
            player.MovePosition(position);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", position);
            player.TransformChanged();
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            //player.UpdatePlayerCollider(true, false);
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            //TODO temporary for potential rust bug
            try { player.ClearEntityQueue(null); } catch { }
            player.SendFullSnapshot();

            me.teleporting.Remove(player.userID);
        }

        public static void MovePlayerTo(BasePlayer player, Vector3 loc)
        {
            me.teleporting.Add(player.userID);
            DLog("moving player " + player.UserIDString);
            if (player.inventory.loot.IsLooting())
            {
                player.EndLooting();
            }
            player.CancelInvoke("InventoryUpdate");
            player.inventory.crafting.CancelAll(true);
            IemUtils.DLog("loc " + loc);
            loc = GetGroundY(loc);
            IemUtils.DLog("loc " + loc);
            rust.ForcePlayerPosition(player, loc.x, loc.y, loc.z);
            player.SendNetworkUpdateImmediate();

            me.teleporting.Remove(player.userID);
        }

        //TODO implement this with SQr values?
        public static bool CheckPointNearToLocation(Vector3 location1, Vector3 location2, float radius)
        {
            double buf = Math.Sqrt(Math.Pow(location1.x - location2.x, 2) +
                      Math.Pow(location1.y - location2.y, 2) +
                      Math.Pow(location1.z - location2.z, 2));
            DLog("distance is " + buf);
            if (radius > buf)
                return true;
            return false;
        }

        static Random random = new Random();

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

        public Dictionary<Guid, ScheduledEvent> scheduledEvents =
    new Dictionary<Guid, ScheduledEvent>();

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
                EventName = "Default Team Game";
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
            //    EventName = "Default Team Game";
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

        #region base types

        public enum State
        {
            Before,
            Running,
            Paused,
            Cancelled,
            Complete
        };



        public interface IIemGame
        {
            string Name { get; set; }

            IemUtils.State CurrentState { get; set; }
            DateTime StartedTime { get; set; }
            DateTime EndedTime { get; set; }
            //TODO might need to populate this from the database
            Guid GetGuid();

            Dictionary<string, IemUtils.IIemPlayer> Players { get; set; }
            //List<IIemPlayer> GetPlayers();

            //is static method
            //IemGame CreateGame(string gamename);
            bool CanStart();
            bool StartGame();
            bool StartGame(BasePlayer player);
            bool EndGame();
            bool CancelGame();
            bool PauseGame();
            bool CleanUp();
        }

        public interface IIemTeamGame : IIemGame
        {

            Dictionary<string, IemUtils.IIemTeam> Teams { get; set; }
            int MinTeams { get; set; }
            int MaxTeams { get; set; }
            int MinPlayersPerTeam { get; set; }
            int MaxPlayersPerTeam { get; set; }
            IemUtils.IIemTeam Winner();
        }


        public interface IIemPlayer
        {
            string PlayerId { get; set; }
            string Name { get; set; }
            int Score { get; set; }
            PlayerState PlayerState { get; set; }

            //TODO might need to populate this from the database
            Guid GetGuid();
        }

        public enum PlayerState
        {
            Alive,
            Dead
        };

        public interface IIemTeamPlayer : IIemPlayer
        {
            IIemTeam Team { get; set; }
            IIemTeamGame TeamGame { get; set; }
            BasePlayer AsBasePlayer();
        }

        public enum TeamState
        {
            Before,
            Empty,
            Playing,
            Lost,
            Won

        }

        public interface IIemTeam
        {
            string Name { get; set; }
            Dictionary<string, IemUtils.IIemTeamPlayer> Players { get; set; }
            int MaxPlayers { get; set; }
            int MinPlayers { get; set; }
            int Score { get; set; }
            TeamState State { get; set; }
            string Color { get; set; }
            //TODO might need to populate this from the database
            Guid GetGuid();

            //is static method
            //IemGame CreateGame(string gamename);
            void AddPlayer(IIemTeamPlayer player);
            void RemovePlayer(IIemTeamPlayer player);
        }

        #endregion
    }
}