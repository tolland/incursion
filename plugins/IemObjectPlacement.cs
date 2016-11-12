//Requires: IemUtils
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Core;
using Oxide.Plugins;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
     
    public class IemObjectPlacement : RustPlugin
    {
        [PluginReference]
        Plugin CopyPaste;

        //[PluginReference] Plugin 
        //[PluginReference]

        IemUtils IemUtils;

        static IemObjectPlacement iemObjectPlacement = null;
        static IemObjectPlacement me = null;


        #region boiler plate

        void Init()
        {
            iemObjectPlacement = this;
            me = this;
        }

        void Loaded()
        {
            LoadData();
            IemUtils.LogL("IemObjectPlacement: Loaded complete");
        }

        void Unload()
        {
            SaveData();
            IemUtils.LogL("IemObjectPlacement: unloaded complete");
        }

        void OnServerInitialized()
        {
            CleanUpGameData();
        }

        #endregion

        #region data storage

        static StoredData storedData;

        class StoredData
        {
            public Dictionary<uint, CreatedGameObject> Objects
                = new Dictionary<uint, CreatedGameObject>();

            public StoredData()
            {
            }
        }

        public class CreatedGameObject
        {
            public string prefab;
            public int instanceId;
            public float x;
            public float y;
            public float z;
            public uint netId;

            public CreatedGameObject()
            {
            }

            public CreatedGameObject(BaseEntity baseEntity)
            {
                prefab = baseEntity.gameObject.name;
                instanceId = baseEntity.gameObject.GetInstanceID();
                x = baseEntity.gameObject.transform.position.x;
                y = baseEntity.gameObject.transform.position.y;
                z = baseEntity.gameObject.transform.position.z;
                netId = baseEntity.net.ID;
            }
        }

        static void LoadData()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(
                "IemOjectPlacement");
        }

        static void SaveData()
        {

            Interface.Oxide.DataFileSystem.WriteObject("IemOjectPlacement", storedData);
        }

        /// <summary>
        /// if the server is quit while the game is in progress, the game objects created
        /// are not removed, this tracks them in data and removes them on server init
        /// </summary> 
        static void CleanUpGameData()
        {
            foreach (CreatedGameObject obj in storedData.Objects.Values)
            {
                //IemUtils.DDLog("looking for stored object: " + obj.netId);
                BaseEntity be = null;
                try
                {
                    be = IemUtils.FindBaseEntityByNetId(obj.netId);
                }

                catch (Exception e)
                {
                    Console.WriteLine("{0} Exception caught. obj.netId", e);
                } 
                

                if (be != null)
                {
                   // IemUtils.DDLog("killing");
                    be.Kill(BaseNetworkable.DestroyMode.None);
                    //be.KillMessage();  
                }  

            }
            IemUtils.DDLog("clearing");
            storedData.Objects.Clear();
            IemUtils.DDLog("saving, count is: " + storedData.Objects.Count);
            SaveData();
        }

        #endregion


        public class CopyPastePlacement
        {
            List<uint> entities = new List<uint>();


            public CopyPastePlacement(string pastename, Vector3 location)
            {
                // var success = iemObjectPlacement.CopyPaste.TryPlaceback(
                //    pastename, null, new string[] { });
               // var ViewAngles = Quaternion.Euler(player.GetNetworkRotation());
                //string[] args = { "height", location.y.ToString() };
                string[] args = {  };
              //  var success = iemObjectPlacement.CopyPaste.TryPaste(
                //    location, pastename, null, 0, args);

                var success = me.CopyPaste.Call("TryPaste", location, pastename, null, 0, args);

                //            iemUtils.ZoneManager.Call("CreateOrUpdateZone",
                //"zone_" + name,
                //new string[]
                //{
                //    "radius", radius.ToString(),
                //    "autolights", "true",
                //    "eject", "false",
                //    "enter_message", "",
                //    "leave_message", "",
                //    "killsleepers", "true"
                //}, location);


                if (success is List<BaseEntity>)
                {
                    List<BaseEntity> mysuccess = (List<BaseEntity>)success;

                    foreach (BaseEntity baseEntity in mysuccess)
                    {
                        
                        GameObject go = baseEntity.gameObject;

                        if (baseEntity.net != null)
                            entities.Add(baseEntity.net.ID);

                        if (storedData != null)
                        {
                            if (storedData.Objects != null)
                            {
                                //Plugins.IemUtils.DLog("net is " + baseEntity.net.ID);
                                storedData.Objects.Add(baseEntity.net.ID,
                                    new CreatedGameObject(
                                        baseEntity
                                    ));
                            }
                        }

                    }
                }else
                {
                    me.Puts("erere");
                   me.Puts("success is " + success.ToString());
                }
                SaveData();
            }


            public CopyPastePlacement(string pastename)
            {
              //  var success = iemObjectPlacement.CopyPaste.TryPlaceback(
                //    pastename, null, new string[] { });



                var success = me.CopyPaste.Call("TryPlaceback", pastename, null, new string[] { });

                if (success is List<BaseEntity>)
                {
                    List<BaseEntity> mysuccess = (List<BaseEntity>)success;

                    foreach (BaseEntity baseEntity in mysuccess)
                    {

                        GameObject go = baseEntity.gameObject;


                        if (baseEntity.net != null)
                            entities.Add(baseEntity.net.ID);

                        if (storedData != null)
                        {
                            if (storedData.Objects != null)
                            {
                                //Plugins.IemUtils.DLog("net is " + baseEntity.net.ID);
                                storedData.Objects.Add(baseEntity.net.ID,
                                    new CreatedGameObject(
                                        baseEntity
                                    ));
                            }
                        }

                    }
                }
                SaveData();
            }

            public void Remove()
            {

                
                foreach (var netid in entities)
                {
                    //IemUtils.DDLog("component found " + joint.name);
                    BaseEntity be = null;

                    try
                    {
                        be = IemUtils.FindBaseEntityByNetId(netid);
                        if (be != null)
                        {
                            if (be.net != null)
                            {
                                uint tempuint = be.net.ID;

                                be.Kill(BaseNetworkable.DestroyMode.None);

                                if (storedData.Objects.ContainsKey(tempuint))
                                    storedData.Objects.Remove(tempuint);
                            }
                            else
                            {
                                IemUtils.DDLog("be was null");
                            }
                        }
                        else
                        {
                            IemUtils.DDLog("be net was null");
                        }
                    }

                    catch (Exception e)
                    {
                        Console.WriteLine("{0} Exception caught. obj.netId", e);
                    }
                }
                SaveData();
            }

        }


    }
}
