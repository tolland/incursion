//Requires: ZoneManager
//Requires: IemUtils
//Requires: IemObjectPlacement
//Requires: IemGameBase
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Newtonsoft.Json;
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

        [PluginReference]
        IemGameBase IemGameBase;

        [PluginReference]
        IemObjectPlacement IemObjectPlacement;

        List<string> zonelist = new List<string>();

        static IncursionHoldingArea me = null;

        void Init()
        {
            me = this;
        }

        static MethodInfo updatelayer;

        void Loaded()
        {
            LoadData();
            updatelayer = typeof(BuildingBlock).GetMethod("UpdateLayer",
                (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        }

        void Unload()
        {

            foreach (var zone in zonelist)
            {
                ZoneManager.Call("EraseZone", zone);
            }

        }


        void OnServerInitialized()
        {
            DestroyAllSpheres();
            //CloseDoors();
        }

        public delegate void EnteredZone(string ZoneID, BasePlayer player);

        private static EnteredZone PlayerEnteredZone = delegate { };


        void OnEnterZone(string ZoneID, BasePlayer player)
        {                        //is this a team zone?
            PlayerEnteredZone(ZoneID, player);

        }

        //void OnExitZone(string ZoneID, BasePlayer player)
        //bool AddPlayerToZoneKeepinlist(string ZoneID, BasePlayer player)


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

        int multiplier = 100;
        int multicount = 1;

        public class TeamSelectLobby : HoldingArea
        {
            public IemObjectPlacement.CopyPastePlacement partition;
            IemGameBase.IemTeamGame teamGame;

            List<string> zonelist = new List<string>();
            List<BaseEntity> spheres = new List<BaseEntity>();

            public TeamSelectLobby(string copypastefile, IemGameBase.IemTeamGame newTeamGame)
            {
                teamGame = newTeamGame;
                me.multicount += 1;
                int x = -120 + (me.multicount * me.multiplier);
                int y = 136;
                int z = -266 + (me.multicount * me.multiplier);
                location = new Vector3(x, y, z);
                Vector3 centre_location = new Vector3(location.x - 7, location.y, location.z - 2);


                partition = new IemObjectPlacement.CopyPastePlacement(
                    copypastefile, new Vector3(x, y, z));
                CreateZoneForLobby();

                int i = 0;
                foreach (var team in teamGame.Teams)
                {
                    //  me.Puts("centre loc is " + centre_location);
                    // me.Puts("loc is " + locs[i]);
                    IemUtils.CreateZone("team_" + team.Value.GetGuid(),
                        locs[i] + centre_location, 6);

                    zonelist.Add("zone_team_" + team.Value.GetGuid());
                    me.zonelist.Add("zone_team_" + team.Value.GetGuid());

                    i++;
                }
                PlayerEnteredZone += PlayerEnteredTeamZone;

            }

            void PlayerEnteredTeamZone(string ZoneID, BasePlayer player)
            {
                if (ZoneID.StartsWith("zone_team_"))
                {
                    //OnPlayerEnterTeamArea(player, GetTeamFromZone(ZoneID));
                    me.Puts("player is " + player.displayName + " has entered zone " + ZoneID);
                    var team = GetTeamFromZone(ZoneID);

                    if (teamGame.Players.ContainsKey(player.UserIDString))
                    {
                        var iemplayer = teamGame.Players[player.UserIDString];

                        if (iemplayer != null)
                        {
                            me.Puts("iemplayer is " + iemplayer.Name);
                        }


                        if (team == null)
                        {
                            me.Puts("team is null");
                        }

                        if (team != null && iemplayer != null)
                        {
                            team.AddPlayer((IemUtils.IIemTeamPlayer)iemplayer);
                            IemUI.ShowTeamUiForPlayer(player, teamGame);
                            me.Puts("can game start " + teamGame.CanStart());
                            if (teamGame.CanStart())
                            {
                                teamGame.StartGame();
                            }
                        }
                    }

                }
            }

            IemUtils.IIemTeam GetTeamFromZone(string ZoneID)
            {
                //IemUtils.DLog("team substring is " + ZoneID.Substring(10));
                foreach (var team in teamGame.Teams)
                {
                    // IemUtils.DLog("checking team is " + team.Key);
                    if (team.Key == ZoneID.Substring(10))
                    {
                        IemUtils.DLog("returning team is " + team.Value.Name);
                        return team.Value;
                    }
                }
                return null;
            }

            //new Vector3(-235, 3, 18)
            List<Vector3> locs = new List<UnityEngine.Vector3>() {
                new Vector3(29 , 0, -1),
            new Vector3(-26, 0, 3),
            new Vector3(27, 0, 8),
            new Vector3(20, 0, -2),
            new Vector3(7, 0, -10)};

            public void Destroy()
            {
                partition?.Remove();
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                    IemUI.RemoveTeamUiForPlayer(player, teamGame);
                foreach (var zone in zonelist)
                {
                    me.Puts("erasing zone " + zone);
                    me.ZoneManager.Call("EraseZone", zone);
                }

                foreach (BaseEntity sphere in spheres)
                {

                    uint prefabID = 2327559662;

                    if (sphere.prefabID == prefabID)
                    {
                        // Puts("found entity with prefabID "
                        //          + entity.prefabID.ToString());

                        sphere.KillMessage();
                        //entity.Kill ();
                    }

                }

            }




            void CreateZoneForLobby()
            {
                Vector3 centre_location = new Vector3(location.x - 7, location.y, location.z - 2);
                // me.CreateZone("lobby", new Vector3(-235, 3, 18), 16);
                me.zonelist.Add("lobby_" + centre_location.ToString());
                zonelist.Add("lobby_" + centre_location.ToString());
                me.ZoneManager.Call("CreateOrUpdateZone",
                    "lobby_" + centre_location.ToString(),
                    new string[]
                    {
                    "radius", "34",
                    "autolights", "true",
                    "eject", "false",
                    "enter_message", "",
                    "leave_message", "",
                    "killsleepers", "true",
                    "nosuicide", "false",
                    //"undestr", "true",nosuicide
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
                    }, location);

                spheres.Add(IemUtils.CreateSphere(centre_location, (34 * 2) + 1));
            }


        }

        static void CreateZonesForArenas(Vector3 location)
        {
            //  Puts("");
            //Puts(" >>>>>createArenaZones");s
            IemUtils.CreateZone("team_1", new Vector3(-255, 2, -1), 5);
            IemUtils.CreateZone("team_2", new Vector3(-214, 2, 38), 5);
            IemUtils.CreateZone("team_4", new Vector3(-262, 2, 26), 5);
            IemUtils.CreateZone("team_5", new Vector3(-215, 2, -2), 5);
            IemUtils.CreateZone("team_6", new Vector3(-228, 2, -10), 5);
        }

        static void CreateZonesForLobby()
        {

            // me.CreateZone("lobby", new Vector3(-235, 3, 18), 16);
            me.ZoneManager.Call("CreateOrUpdateZone",
                "lobby",
                new string[]
                {
                    "radius", "34",
                    "autolights", "true",
                    "eject", "false",
                    "enter_message", "",
                    "leave_message", "",
                    "killsleepers", "true",
                    "nosuicide", "false",
                    //"undestr", "true",nosuicide
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

            return true;
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
        public static void OpenTeamDoors()
        {
            if (teamLobbyDoors.Count > 0)
            {
                foreach (KeyValuePair<Vector3, TeamLobbyDoor> teamLobbyDoor in teamLobbyDoors)
                {
                    teamLobbyDoor.Value.OpenDoor();
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
                        me.Puts("is blue team door");
                        door.SetFlag(BaseEntity.Flags.Open, true);
                    }

                    if (door.GetInstanceID().Equals(-67716) || door.GetInstanceID().Equals(-63844))
                    {
                        me.Puts("is red team door");
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
                    teamLobbyDoor.Value.CloseDoor();
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
                        me.Puts("is blue team door");
                        door.SetFlag(BaseEntity.Flags.Open, false);
                    }

                    if (door.GetInstanceID().Equals(-67716) || door.GetInstanceID().Equals(-63844))
                    {
                        me.Puts("is red team door");
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

        List<MonumentInfo> FindMonuments(string name)
        {
            var monuments = new List<MonumentInfo>();
            foreach (var info in IemUtils.monuments)
            {
                if (info.name.Contains(name))
                {
                    monuments.Add(info);
                }
            }

            return monuments;
        }

        void FindWarehouses()
        {
            var warehouses = FindMonuments("warehouse");

            foreach (var warehouse in warehouses)
            {
                //CreateArena(monument_gobject, pos, "Lighthouse", 80);
                //DLog("CreateEsmLobby at " + monument_gobject.GetInstanceID());
                //esm.eventLobby = new me.Lobby(pos);
                //esm.eventLobby = new me.Lobby(new Vector3(-231, 2, 14));
            }
        }

        static int doorColl = UnityEngine.LayerMask.GetMask(new string[] { "Construction Trigger", "Construction" });

        class TeamLobbyDoor
        {
            Door door;
            public Vector3 position = default(Vector3);

            public string team;

            public TeamLobbyDoor()
            {
            }

            public TeamLobbyDoor(Vector3 pos)
            {
                this.position = pos;
                team = "red";
            }

            public void OpenDoor()
            {
                Door door;
                if (!TryGetDoor(out door))
                {
                    return;
                }

                door.SendNetworkUpdateImmediate(false);
                door.SetFlag(BaseEntity.Flags.Open, true);
                door.SendNetworkUpdateImmediate(true);
            }

            public void CloseDoor()
            {
                Door door;
                if (!TryGetDoor(out door))
                {
                    return;
                }

                door.SendNetworkUpdateImmediate(false);
                door.SetFlag(BaseEntity.Flags.Open, false);
                door.SendNetworkUpdateImmediate(true);
            }

            bool TryGetDoor(out Door door)
            {
                if (this.door != null)
                {
                    door = this.door;
                    return true;
                }

                List<Door> doors = new List<Door>();
                Vis.Entities<Door>(position, 1f, doors, doorColl);

                if (doors.Count == 0)
                {
                    door = null;
                    return false;
                    // Plugins.IemUtils.DLog("found the door");
                }
                door = doors[0];
                return true;
            }
        }

        static Hash<Vector3, TeamLobbyDoor> teamLobbyDoors
            = new Hash<Vector3, TeamLobbyDoor>();

        void LoadData()
        {
            teamLobbyDoors.Clear();
            try
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("TeamLobbyDoors");
            }
            catch
            {
                storedData = new StoredData();
            }
            foreach (var remote in storedData.TeamLobbyDoors)
                teamLobbyDoors[remote.position] = remote;
        }

        void Unloaded()
        {
            // SaveData();
        }

        void SaveData()
        {
            DynamicConfigFile file = Interface.Oxide.DataFileSystem.GetDatafile("TeamLobbyDoors");
            file.Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore; // allows you to store Vector3
            file.WriteObject<StoredData>(storedData);
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
                // IemUtils.DLog("test " + entity.GetType());

                // IemUtils.DLog(door.gameObject.name);

                // IemUtils.DLog(door.transform.position.ToString());
                // IemUtils.DLog(door.gameObject.transform.position.ToString());

                if (hitinfo.Weapon != null)
                {
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
                }
                //IemUtils.DLog(teamLobbyDoors.Count.ToString());
                //foreach (KeyValuePair<Vector3, TeamLobbyDoor> pair in teamLobbyDoors)
                //{
                //    IemUtils.DLog("key is " + pair.Key);
                //    IemUtils.DLog("value is " + pair.Value);
                //}

                //storedData.RemoteActivators.Remove(remoteActivators[remoteActivate]);
                //remoteActivators[remoteActivate].listedDoors.Add(new RemoteDoor(goodPos));
                //storedData.RemoteActivators.Add(remoteActivators[remoteActivate]);
                //
                //  else
                //    var rigidbody = door.AddComponent<TeamLobbyDoor>();


            }


            if (hitinfo.Weapon != null)
            {
                if (hitinfo.Weapon.LookupPrefab().name == "bone_club.entity")
                {

                    IemUtils.DDLog("entity net id is: " + entity.net.ID);
                }

            }

        }

        [ConsoleCommand("resetteamdoors")]
        void ccmdEvent14324(ConsoleSystem.Arg arg)
        {
            if (!IemUtils.hasAccess(arg)) return;
            storedData.TeamLobbyDoors.Clear();
            teamLobbyDoors.Clear();
            SaveData();
        }


        [ConsoleCommand("door1")]
        void ccmdEvent1(ConsoleSystem.Arg arg)
        {
            if (!IemUtils.hasAccess(arg)) return;
            OpenTeamDoors();
        }

        [ConsoleCommand("door")]
        void ccmdEvent(ConsoleSystem.Arg arg)
        {
            if (!IemUtils.hasAccess(arg)) return;
            OpenTeamDoors();
        }

        [ConsoleCommand("opendoors")]
        void ccmd2Event(ConsoleSystem.Arg arg)
        {
            if (!IemUtils.hasAccess(arg)) return;
            OpenDoors();
        }

        [ConsoleCommand("closedoors")]
        void ccmd21Event(ConsoleSystem.Arg arg)
        {
            if (!IemUtils.hasAccess(arg)) return;
            CloseDoors();
        }
        [ConsoleCommand("closeteamdoors")]
        void ccmdCloseDoors(ConsoleSystem.Arg arg)
        {
            if (!IemUtils.hasAccess(arg)) return;
            CloseTeamDoors();
        }
    }
}