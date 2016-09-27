//Requires: CopyPaste
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Plugins;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{

    public class IemObjectPlacement : RustPlugin
    {
        [PluginReference]
        CopyPaste CopyPaste;

        static IemObjectPlacement iemObjectPlacement = null;
        

        #region boiler plate

        void Init()
        {
            iemObjectPlacement = this;
        }

        #endregion

        void OnServerInitialized()
        {
            var placement = new CopyPastePlacement("partition1");
            Timer mytimer = iemObjectPlacement.timer.Once(1, () =>
            {
                placement.Remove(); 
            });
        }

        public class ObjectPlacement
        {
            

        }

        public class CopyPastePlacement
        {
            List<BaseEntity> entities = new List<BaseEntity>();

            public CopyPastePlacement(string pastename)
            {
                var success = iemObjectPlacement.CopyPaste.TryPlaceback(
                    pastename, null, new string[] { });
                if (success is List<BaseEntity>)
                {
                    List<BaseEntity> mysuccess = (List<BaseEntity>)success;

                    //iemObjectPlacement.Puts("is list of baseentity of size " + mysuccess.Count);

                    foreach (BaseEntity baseEntity in mysuccess)
                    {
                        //iemObjectPlacement.Puts("base entity is " + baseEntity.name);
                        entities.Add(baseEntity);
                    }

                }


            }

            public void Remove()
            {
                
                foreach (BaseEntity baseEntity in entities)
                    {
                        baseEntity.Kill(true ? BaseNetworkable.DestroyMode.Gib :
                            BaseNetworkable.DestroyMode.None);
                    }

            }

        }



    }
}
