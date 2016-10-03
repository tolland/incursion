//Requires: CopyPaste
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
        CopyPaste CopyPaste;


        [PluginReference]
        IemUtils IemUtils;

        static IemObjectPlacement iemObjectPlacement = null;


        #region boiler plate

        void Init()
        {
            iemObjectPlacement = this;
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
                IemUtils.DDLog("looking for stored object: " + obj.netId);
                BaseEntity be = IemUtils.FindBaseEntityByNetId(obj.netId);
                if (be != null)
                {
                    be.Kill(BaseNetworkable.DestroyMode.Gib);
                    //be.KillMessage();  
                } 



                //Deployable found = IemUtils.FindComponentNearestToLocation<Deployable>(
                //    new Vector3(obj.x, obj.y, obj.z), 8);
                ////var found = IemUtils.FindObjectByID(obj.instanceId);
                //if (found != null)
                //{
                //    if (!found.isActiveAndEnabled)
                //    {
                //        IemUtils.DDLog("NOT ACTIVE AND ENABLED");
                //    }


                //    IemUtils.DDLog("found the object: " + found.prefabID);
                //    if (found.name == obj.prefab)
                //    {
                //        IemUtils.DDLog("Deleting object with id ... " + obj.instanceId);

                //        IemUtils.DDLog("name =" + found.gameObject.name);
                //        found.gameObject.ToBaseEntity().Kill();

                //        BaseEntity be = found.GetComponentInParent<BaseEntity>();
                //        var realEntity = found.GetComponent<BaseNetworkable>().net;
                //        IemUtils.DDLog("base entity at " + be.transform.position);

                //        if (be != null)
                //        {
                //            //be.Kill(BaseNetworkable.DestroyMode.Gib);
                //            //be.KillMessage();  
                //        }
                //        else
                //            IemUtils.DDLog("no base entity");
                //    }
                //    else
                //    {
                //        IemUtils.DDLog("not matched prefab");
                //        IemUtils.DDLog(obj.prefab);
                //        IemUtils.DDLog(found.name);
                //    }
                //}
                //else
                //{
                //    IemUtils.DDLog("not found");
                //}
            }
            IemUtils.DDLog("clearing");
            storedData.Objects.Clear();
            IemUtils.DDLog("saving, count is: " + storedData.Objects.Count);
            SaveData();
        }

        #endregion

        //[Serializable]
        //public class Tagg1
        //{
        //    public string tempstring;
        //}

        public class PartitionComponent : MonoBehaviour
        {

            //[SerializeField]
            //public string tempstring;


            //public Tagg1 obj;

            void Awake()
            {
                //if (tempstring == null)
                //{
                //    IemUtils.DDLog("tempstring is null, resetting");
                //    tempstring = DateTime.Now.ToString();
                //}
                //if (obj == null)
                //{
                //    IemUtils.DDLog("obj is null, resetting");
                //    obj = new Tagg1();
                //}
            }

            public static PartitionComponent GetPartitionComponent(GameObject go)
            {
                PartitionComponent partitionComponent
                    = go.GetComponent<PartitionComponent>();

                if (partitionComponent == null)
                {
                    //IemUtils.DDLog("creating partitionComponent from fresh");
                    partitionComponent = go.gameObject.AddComponent<PartitionComponent>();
                }

                //IemUtils.DDLog("obj was: " + partitionComponent.tempstring);

                return partitionComponent;
            }
        }


        public static void List()
        {

            PartitionComponent[] partitionComponents = GameObject.FindObjectsOfType<PartitionComponent>();
            foreach (PartitionComponent joint in partitionComponents)
            {
                IemUtils.DDLog("component found " + joint.name);

            }
        }

        public class CopyPastePlacement
        {
            //List<BaseEntity> entities = new List<BaseEntity>();

            public CopyPastePlacement(string pastename)
            {
                var success = iemObjectPlacement.CopyPaste.TryPlaceback(
                    pastename, null, new string[] { });
                if (success is List<BaseEntity>)
                {
                    List<BaseEntity> mysuccess = (List<BaseEntity>)success;

                    foreach (BaseEntity baseEntity in mysuccess)
                    {
                        PartitionComponent.GetPartitionComponent(baseEntity.gameObject);
                        //IemUtils.DLog("id is " + baseEntity.gameObject.GetInstanceID());
                        //IemUtils.DLog("name is " + baseEntity.gameObject.name);
                        //IemUtils.DLog("type is " + baseEntity.gameObject.GetType());
                        //IemUtils.DLog("type is " + baseEntity.gameObject.);
                        //IemUtils.DLog("hash is " + baseEntity.gameObject.GetHashCode());
                        GameObject go = baseEntity.gameObject;
                        //go.GetComponents()

                        //IemUtils.DDLog("net id is " + baseEntity.net.ID);

                        //Component[] hingeJoints = go.GetComponents(typeof(Component));

                        //foreach (Component joint in hingeJoints)
                        //    IemUtils.DLog("component is " + joint.ToString());

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
                            else
                            {
                                IemUtils.DDLog("stored data objects was null");
                            }
                        }
                        else
                        {
                            IemUtils.DDLog("stored data was null");
                        }

                    }
                }
                SaveData();
            }

            public void Remove()
            {

                PartitionComponent[] partitionComponents = GameObject.FindObjectsOfType<PartitionComponent>();
                foreach (PartitionComponent partitionComponent in partitionComponents)
                {
                    //IemUtils.DDLog("component found " + joint.name);
                    BaseEntity be = partitionComponent.gameObject.GetComponent<BaseEntity>();
                    if (be != null)
                    {
                        if (be.net != null)
                        {
                            uint tempuint = be.net.ID;

                            be.Kill(BaseNetworkable.DestroyMode.Gib);

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
                SaveData();
            }

        }


    }
}
