//Requires: ZoneManager
//Requires: IemUtils
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{

    [Info("Incursion Holding Area", "tolland", "0.1.0")]
    public class IncursionHoldingArea : RustPlugin
    {

        [PluginReference]
        Plugin ZoneManager;

        [PluginReference]
        IemUtils IemUtils;
        

        static IncursionHoldingArea incursionHoldingArea = null;

        /// <summary>
        /// Called when a plugin is being initialized
        /// Other plugins may or may not be present, dependant on load order
        /// </summary>
        void Init()
        {
            incursionHoldingArea = this;
        }


        static MethodInfo updatelayer;

        void Loaded()
        {
            LoadData();
            updatelayer = typeof(BuildingBlock).GetMethod("UpdateLayer",
                (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        }

        void OnServerInitialized()
        {
            DestroyAllSpheres();
            CloseDoors();
        }

        void OnEnterZone(string ZoneID, BasePlayer player)
        {
            Puts("player is " + player.displayName + " has entered zone " + ZoneID);
        }

        //void OnExitZone(string ZoneID, BasePlayer player)
        //bool AddPlayerToZoneKeepinlist(string ZoneID, BasePlayer player)


        protected void OpenDoors()
        {
            Door[] doors = GameObject.FindObjectsOfType<Door>();

            if (doors.Length > 0)
            {
                foreach (Door target in doors.ToList())
                {
                    target.SetFlag(BaseEntity.Flags.Open, true);
                }
            }
        }

        protected void CloseDoors()
        {
            Door[] doors = GameObject.FindObjectsOfType<Door>();

            if (doors.Length > 0)
            {
                foreach (Door target in doors.ToList())
                {
                    target.SetFlag(BaseEntity.Flags.Open, false);
                }
            }
            KeyLock[] keyLocks = GameObject.FindObjectsOfType<KeyLock>();

            if (keyLocks.Length > 0)
            {
                foreach (KeyLock keyLock in keyLocks.ToList())
                {
                    keyLock.SetFlag(BaseEntity.Flags.Locked, true);
                }
            }
        }

        void OnDoorClosed(Door door, BasePlayer player)

        {
            //Puts("OnDoorOpened works!");
            //Puts("door is " + door.name);
            //Puts("door is " + door.GetHashCode().ToString());
            //Puts("door is " + door.GetInstanceID().ToString());

            //CloseDoors();
        }

        bool CanUseDoor(BasePlayer player, BaseLock door)
        {
            Puts("CanUseDoor works!");
            Puts("BaseLock is " + door.GetInstanceID().ToString());
            if (door.GetInstanceID().Equals(-267990) || door.GetInstanceID().Equals(-267932))
            {
                Puts("is blue team keylock");
                //return true;
            }

            if (door.GetInstanceID().Equals(-268258) || door.GetInstanceID().Equals(-268200))
            {
                Puts("is red team keylock");
                //return true;
            }

            return false;
        }


        void OnDoorOpened(Door door, BasePlayer player)
        {
            Puts("OnDoorOpened works!");
            Puts("door is " + door.name);
            Puts("door is " + door.GetHashCode().ToString());
            Puts("door is " + door.GetInstanceID().ToString());

            if (door.GetInstanceID().Equals(-57154) || door.GetInstanceID().Equals(-57260))
            {
                Puts("is blue team door");
            }

            if (door.GetInstanceID().Equals(-57154) || door.GetInstanceID().Equals(-57260))
            {
                Puts("is red team door");
            }



            //OpenDoors();

            var buildingBlock = door.GetComponent<BuildingBlock>();
            if (buildingBlock == null)
            {
                Puts("is null");
                return;
            }
            var buildingId = buildingBlock.buildingID;

            var removeList = UnityEngine.GameObject
                .FindObjectsOfType<BuildingBlock>()
                .Where(x => x.buildingID == buildingId).ToList();

            foreach (var foo in removeList)
            {
                Puts("foo is" + foo.blockDefinition.ToString());

            }
        }



        /// <summary>
        /// The HoldingArea class represents a generic closed area in which players
        /// can be held during periods of the game
        /// </summary>
        /// <param name="data"></param>

        public class HoldingArea
        {
            public Vector3 location;
            Quaternion rotation;

            protected HoldingArea(Vector3 position)
            {
                location = position;
            }

            public HoldingArea()
            {

            }
        }

        static void CreateZonesForArenas(Vector3 location)
        {
            //  Puts("");
            //Puts(" >>>>>createArenaZones");
            IemUtils.CreateZone("team_1", new Vector3(-255, 2, -1), 5);
            IemUtils.CreateZone("team_2", new Vector3(-214, 2, 38), 5);
            IemUtils.CreateZone("team_4", new Vector3(-262, 2, 26), 5);
            IemUtils.CreateZone("team_5", new Vector3(-215, 2, -2), 5);
            IemUtils.CreateZone("team_6", new Vector3(-228, 2, -10), 5);



        }

        static void CreateZonesForLobby()
        {

            // incursionHoldingArea.CreateZone("lobby", new Vector3(-235, 3, 18), 16);
            incursionHoldingArea.ZoneManager.Call("CreateOrUpdateZone",
                "lobby",
                new string[]
                {
                    "radius", "34",
                    "autolights", "true",
                    "eject", "false",
                    "enter_message", "lobby zone",
                    "leave_message", "leaving lobby zone",
                    "killsleepers", "true",
                    //"undestr", "true",
                    "nobuild", "true",
                    "nodecay", "true",
                    //"nocorpse", "true",
                    "nogather", "true",
                    "noplayerloot", "true",
                    //"nowounded", "true",
                    "nodrown", "true",
                    "nostability", "true",
                    "noupgrade", "true",
                    //"nobleed", "true",
                    //"pvpgod", "true",
                    "nodeploy", "true"
                }, new Vector3(-235, 3, 18));

            IemUtils.CreateSphere(new Vector3(-235, 3, 18), (34 * 2) + 1);
        }


        public static void OpenTeamDoors()
        {



            if (teamLobbyDoors.Count > 0)
            {
                foreach (KeyValuePair<Vector3, TeamLobbyDoor> teamLobbyDoor in teamLobbyDoors)
                {

                    teamLobbyDoor.Value.OpenDoor(true);

                }
            }

        }

        public static void OpenTeamDoors111()
        {

            Door[] doors = GameObject.FindObjectsOfType<Door>();

            if (doors.Length > 0)
            {
                foreach (Door door in doors.ToList())
                {
                    if (door.GetInstanceID().Equals(-57154) || door.GetInstanceID().Equals(-57260))
                    {
                        incursionHoldingArea.Puts("is blue team door");
                        door.SetFlag(BaseEntity.Flags.Open, true);
                    }

                    if (door.GetInstanceID().Equals(-67716) || door.GetInstanceID().Equals(-63844))
                    {
                        incursionHoldingArea.Puts("is red team door");
                        door.SetFlag(BaseEntity.Flags.Open, true);
                    }

                }
            }

        }



        public static void CloseTeamDoors()
        {

            if (teamLobbyDoors.Count > 0)
            {
                foreach (KeyValuePair<Vector3, TeamLobbyDoor> teamLobbyDoor in teamLobbyDoors)
                {

                    teamLobbyDoor.Value.OpenDoor(false);

                }
            }
        }

        public static void CloseTeamDoors1111()
        {

            Door[] doors = GameObject.FindObjectsOfType<Door>();

            if (doors.Length > 0)
            {
                foreach (Door door in doors.ToList())
                {
                    if (door.GetInstanceID().Equals(-57154) || door.GetInstanceID().Equals(-57260))
                    {
                        incursionHoldingArea.Puts("is blue team door");
                        door.SetFlag(BaseEntity.Flags.Open, false);
                    }

                    if (door.GetInstanceID().Equals(-67716) || door.GetInstanceID().Equals(-63844))
                    {
                        incursionHoldingArea.Puts("is red team door");
                        door.SetFlag(BaseEntity.Flags.Open, false);
                    }

                }
            }
        }


        public class Lobby : HoldingArea
        {
            public Lobby(Vector3 position)
            {
                location = position;

                CreateZonesForArenas(location);
                CreateZonesForLobby();
            }
        }

        public class TeamLobby : HoldingArea
        {


        }

        public void DestroyAllSpheres()
        {

            //Vis.Entit
            //Puts("  >>>>>>Destroy all spheres");

            //foreach (SphereEntity se in BaseEntity.FindObjectsOfType<SphereEntity> ()) {
            //se.KillMessage ();
            //Puts ("here");
            //}

            var foundentities = UnityEngine.Object.FindObjectsOfType<BaseEntity>();
            uint prefabID = 2327559662;
            foreach (var entity in foundentities)
            {
                if (entity.prefabID == prefabID)
                {
                   // Puts("found entity with prefabID "
               //          + entity.prefabID.ToString());

                    entity.KillMessage();
                    //entity.Kill ();
                }
            }
        }

        void FindMonument()
        {
            GameObject[] allobjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

            foreach (var monument_gobject in allobjects)
            {
                //DLog("object is " + monument_gobject.name);

                if (monument_gobject.name.Contains("autospawn/monument"))
                {
                    //Puts (gobject.GetInstanceID ().ToString ());


                    var pos = monument_gobject.transform.position;
                    //Puts(pos.ToString());

                    if (monument_gobject.name.Contains("warehouse"))
                    {
                        //  if (configData.Zones.Lighthouse.Activate)
                        //CreateArena(monument_gobject, pos, "Lighthouse", 80);
                        //DLog("CreateEsmLobby at " + monument_gobject.GetInstanceID());
                        //esm.eventLobby = new IncursionHoldingArea.Lobby(pos);
                        //esm.eventLobby = new IncursionHoldingArea.Lobby(new Vector3(-231, 2, 14));
                        break;
                    }


                }
            }

        }

        static int doorColl = UnityEngine.LayerMask.GetMask(new string[] { "Construction Trigger", "Construction" });

        class TeamLobbyDoor
        {
            public string x;
            public string y;
            public string z;

            Door door;
            Vector3 pos = default(Vector3);

            public string team;

            public TeamLobbyDoor()
            {
            }

            public TeamLobbyDoor(Vector3 pos)
            {
                this.x = pos.x.ToString();
                this.y = pos.y.ToString();
                this.z = pos.z.ToString();
                this.pos = new Vector3(float.Parse(this.x), float.Parse(this.y), float.Parse(this.z));
                team = "red";
            }
            public bool OpenDoor(bool openclose)
            {
                if (door == null)
                    door = FindDoor();
                if (door == null) return false;
                //door.SetFlag(BaseEntity.Flags.Open, openclose);
                door.SendNetworkUpdateImmediate(false);
                if (openclose)
                {
                    door.SetFlag(BaseEntity.Flags.Open, true);
                    door.SendNetworkUpdateImmediate(true);
                }
                else
                {
                    door.SetFlag(BaseEntity.Flags.Open, false);
                    door.SendNetworkUpdateImmediate(true);
                }
                return true;
            }
            public Vector3 Pos()
            {
                if (pos == default(Vector3))
                    pos = new Vector3(float.Parse(this.x), float.Parse(this.y), float.Parse(this.z));
                return pos;
            }
            Door FindDoor()
            {
                foreach (Collider col in Physics.OverlapSphere(Pos(), 2f, doorColl))
                {
                    if (col.GetComponentInParent<Door>() == null) continue;


                    if (Mathf.Ceil(col.transform.position.x) == Mathf.Ceil(pos.x)
                        && Mathf.Ceil(col.transform.position.y) == Mathf.Ceil(pos.y)
                        && Mathf.Ceil(col.transform.position.z) == Mathf.Ceil(pos.z))
                    {
                       // Plugins.IemUtils.DLog("found the door");
                        door = col.GetComponentInParent<Door>();
                    }
                }
                return door;
            }


        }

        static Hash<Vector3, TeamLobbyDoor> teamLobbyDoors
            = new Hash<Vector3, TeamLobbyDoor>();

        void LoadData()
        {
            teamLobbyDoors.Clear();
            try
            {
                storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("TeamLobbyDoors");
            }
            catch
            {
                storedData = new StoredData();
            }
            foreach (var remote in storedData.TeamLobbyDoors)
                teamLobbyDoors[remote.Pos()] = remote;
        }

        void Unloaded()
        {
            SaveData();
        }

        void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("TeamLobbyDoors", storedData);
        }

        static StoredData storedData;

        class StoredData
        {
            public HashSet<TeamLobbyDoor> TeamLobbyDoors = new HashSet<TeamLobbyDoor>();

            public StoredData()
            {
            }
        }

        //&& hitinfo.Initiator is BasePlayer

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            if (entity == null || entity.GetComponent<ResourceDispenser>() != null)
                return;
            var door = entity as Door;
            if (door != null)
            {
                IemUtils.DLog("test " + entity.GetType());

                IemUtils.DLog(door.gameObject.name);

                IemUtils.DLog(door.transform.position.ToString());
                IemUtils.DLog(door.gameObject.transform.position.ToString());

                IemUtils.DLog(hitinfo.Weapon.LookupPrefab().name);
                if (hitinfo.Weapon.LookupPrefab().name == "bone_club.entity")
                {
                    TeamLobbyDoor teamLobbyDoor = new TeamLobbyDoor(door.transform.position);
                    teamLobbyDoor.team = "red";
                    storedData.TeamLobbyDoors.Remove(teamLobbyDoor);
                    storedData.TeamLobbyDoors.Add(teamLobbyDoor);
                    teamLobbyDoors[door.transform.position] = teamLobbyDoor;
                    SaveData();
                }
                if (hitinfo.Weapon.LookupPrefab().name == "knife_bone.entity")
                {
                    TeamLobbyDoor teamLobbyDoor = new TeamLobbyDoor(door.transform.position);
                    teamLobbyDoor.team = "blue";
                    storedData.TeamLobbyDoors.Remove(teamLobbyDoor);
                    storedData.TeamLobbyDoors.Add(teamLobbyDoor);
                    teamLobbyDoors[door.transform.position] = teamLobbyDoor;
                    SaveData();
                }

                IemUtils.DLog(teamLobbyDoors.Count.ToString());
                foreach (KeyValuePair<Vector3, TeamLobbyDoor> pair in teamLobbyDoors)
                {
                    IemUtils.DLog("key is " + pair.Key);
                    IemUtils.DLog("value is " + pair.Value);
                }

                //storedData.RemoteActivators.Remove(remoteActivators[remoteActivate]);
                //remoteActivators[remoteActivate].listedDoors.Add(new RemoteDoor(goodPos));
                //storedData.RemoteActivators.Add(remoteActivators[remoteActivate]);
                //
                //  else
                //    var rigidbody = door.AddComponent<TeamLobbyDoor>();


            }



        }

        [ConsoleCommand("resetteamdoors")]
        void ccmdEvent14324(ConsoleSystem.Arg arg)
        {
            storedData.TeamLobbyDoors.Clear();
            teamLobbyDoors.Clear();
            SaveData();
        }


        [ConsoleCommand("door1")]
        void ccmdEvent1(ConsoleSystem.Arg arg)
        {
            OpenTeamDoors();
        }

        [ConsoleCommand("door")]
        void ccmdEvent(ConsoleSystem.Arg arg)
        {
            OpenTeamDoors();
        }

        [ConsoleCommand("opendoors")]
        void ccmd2Event(ConsoleSystem.Arg arg)
        {
            OpenDoors();
        }

        [ConsoleCommand("closedoors")]
        void ccmd21Event(ConsoleSystem.Arg arg)
        {
            CloseDoors();
        }
        [ConsoleCommand("closeteamdoors")]
        void ccmdCloseDoors(ConsoleSystem.Arg arg)
        {
            CloseTeamDoors();
        }
    }

}