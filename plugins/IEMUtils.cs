//Requires: ZoneManager
using System;
using Oxide.Core.Plugins;
using UnityEngine;
using Random = System.Random;

namespace Oxide.Plugins
{

    [Info("Incursion Utilities", "tolland", "0.1.0")]
    public class IemUtils : RustPlugin
    {

        [PluginReference]
        Plugin ZoneManager;

        static IemUtils iemUtils = null;

        void Init()
        {
            iemUtils = this;
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

        private string GetMessage(string key) => lang.GetMessage(key, this);

        public static void DLog(string message)
        {
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

        #endregion

        #region zone utils

        public static void CreateZone(string name, Vector3 location, int radius)
        {
            iemUtils.Puts("creating zone");

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
            iemUtils.Puts("prefabID " + sphere.prefabID);

            ent.currentRadius = radius;
            ent.lerpSpeed = 0f;
            sphere?.Spawn();


        }

        #endregion

        #region geo stuff

        public static Vector3 Vector3Down = new Vector3(0f, -1f, 0f);
        public static int groundLayer = LayerMask.GetMask("Construction", "Terrain", "World");

        public static float? GetGroundY(Vector3 position)
        {

            position = position + Vector3.up;
            RaycastHit hitinfo;
            if (Physics.Raycast(position, Vector3Down, out hitinfo, 100f, groundLayer))
            {
                return hitinfo.point.y;
            }
            return null;
        }


        static void TeleportPlayerPosition(BasePlayer player, Vector3 destination)
        {
            DLog("teleporting player from " + player.transform.position.ToString());
            DLog("teleporting player to   " + destination.ToString());
            player.MovePosition(destination);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            player.SendFullSnapshot();
        }

        public static void MovePlayerTo(BasePlayer player, Vector3 loc)
        {
            player.MovePosition(loc);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", player.transform.position);
            player.TransformChanged();
            player.SendNetworkUpdateImmediate();
        }

        public static Vector3 GetRandomPointOnCircle(Vector3 centre, float radius)
        {
            Random random = new System.Random();
            //get a random angli in radians
            float randomAngle = (float)random.NextDouble() * (float)Math.PI * 2.0f;

            iemUtils.Puts("random angle is " + randomAngle.ToString());

            Vector3 loc = centre;

            iemUtils.Puts("x modifyier is " + ((float)Math.Cos(randomAngle) * radius));
            iemUtils.Puts("z modifyier is " + ((float)Math.Sin(randomAngle) * radius));

            loc.x = loc.x + ((float)Math.Cos(randomAngle) * radius);
            loc.z = loc.z + ((float)Math.Sin(randomAngle) * radius);

            return loc;
        }

        #endregion
    }
}
