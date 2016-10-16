//Requires: IemUtils
//Requires: IemGameBase
using UnityEngine;
using Oxide.Core;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Configuration;
using Rust;
using System;

namespace Oxide.Plugins
{
    [Info("Incursion Menu", "Tolland", "0.1.0")]
    public class IemMenu : RustPlugin
    {

        [PluginReference]
        IemGameBase IemGameBase;

        static IemMenu iemMenu;

        private static List<string> guiList = new List<string>();
        private static Dictionary<string, PlayerMenu> playerkeys
            = new Dictionary<string, PlayerMenu>();

        void Init()
        {
            iemMenu = this;
            IemUtils.LogL("iemMenu: Init complete");
        }

        //TODO enum?
        public static List<string> filters = new List<string>() { "All", "Solo", "Team", "Individual" };

        class PlayerMenu
        {
            string steam_id;
            public Dictionary<string, string> keybindings = new Dictionary<string, string>();
            public bool showingMenu = false;
            public string filter;

            public PlayerMenu(string newId,
                bool newShowingMenu = false
                )
            {
                steam_id = newId;
                showingMenu = newShowingMenu;
                filter = "";
            }

            public void AddBinding(string key, string binding)
            {
                keybindings[key] = binding;
            }

        }

        [PluginReference]
        IemUtils IemUtils;

        void Loaded()
        {

        }

        void OnServerInitialized()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!playerkeys.ContainsKey(player.UserIDString))
                    playerkeys[player.UserIDString] = new PlayerMenu(player.UserIDString);

                player.SendConsoleCommand("bind f5 chat.say \"/iem.menu toggle\"");

                CheckPlayer(player); 
            }
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            Puts("OnPlayerSleepEnded works!");
            CheckPlayer(player);
        }

        void CheckPlayer(BasePlayer player)
        {
            Puts("calling check player");
            ShowMenuHud(player);
        }

        void ShowMenuHud(BasePlayer player)
        {
            string gui = "MenuHud";
            guiList.Add(gui);

            CuiHelper.DestroyUi(player, gui);

            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel
            {
                Image = {
                    Color = "0.1 0.1 0.1 0.4"
                },
                RectTransform = {
                    AnchorMin = "0.15 0.01",
                    AnchorMax = "0.35 0.09"
                },
                CursorEnabled = false
            }, "Overlay", gui);

            elements.Add(new CuiLabel
            {
                Text = {
                    Text = "F5 for game menu",
                    FontSize = 22,
                    Align = TextAnchor.MiddleLeft
            },
                RectTransform = {
                    AnchorMin = "0.1 0.1",
                    AnchorMax = "0.43 0.9",
                    //OffsetMin = "0.1 0.1",
                    //OffsetMax = "0.9 0.9"
                }
            }, mainName);

            CuiHelper.AddUi(player, elements);
        }



        #region full page overlays


        public static void RemoveIntroOverlay(BasePlayer player)
        {
            string gui = "IemMenuMain";
            CuiHelper.DestroyUi(player, gui);
            playerkeys[player.UserIDString].showingMenu = false;
        }


        private static string[,] panelSlots = {
            { "0.232 0.5", "0.392 0.7" },
            { "0.424 0.5", "0.584 0.7" },
            { "0.618 0.5", "0.776 0.7" },
            { "0.808 0.5", "0.968 0.7" },
            { "0.232 0.25", "0.392 0.45" },
            { "0.424 0.25", "0.584 0.45" },
            { "0.618 0.25", "0.776 0.45" },
            { "0.808 0.25", "0.968 0.45" }
        };

        private static string[,] buttonSlots = {
            { "0.05 0.65", "0.15 0.7" },
            { "0.05 0.55", "0.15 0.6" },
            { "0.05 0.45", "0.15 0.5" },
            { "0.05 0.35", "0.15 0.4" },

                };

        public static void ShowIntroOverlay(BasePlayer player, string message)
        {
            playerkeys[player.UserIDString].showingMenu = true;
            string gui = "IemMenuMain";
            guiList.Add(gui);

            CuiHelper.DestroyUi(player, gui);

            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel
            {
                Image = {
                    Color = "0.1 0.1 0.1 1"
                },
                RectTransform = {
                    AnchorMin = "0.05 0.05",
                    AnchorMax = "0.95 0.95"
                },
                CursorEnabled = false
            }, "Overlay", gui);

            elements.Add(new CuiPanel
            {
                Image = {
                    Color = "0.1 0.2 0.7 1"
                },
                RectTransform = {
                    AnchorMin = "0.0 0.0",
                    AnchorMax = "0.2 1.0"
                },
                CursorEnabled = true
            }, mainName);

            //for (int x = 0; x < array.GetLength(0); x += 1)
            //{
            //    for (int y = 0; y < array.GetLength(1); y += 1)
            //    {
            //        Console.Write(array[x, y]);
            //    }
            //}

            for (int x = 0; x < panelSlots.GetLength(0); x += 1)
            {

                // iemMenu.Puts(panelSlots[x, 0]);
                // iemMenu.Puts(panelSlots[x, 1]);

                elements.Add(new CuiPanel
                {
                    Image = {
                    Color = "0.7 0.2 0.1 1"
                },
                    RectTransform = {
                    AnchorMin = panelSlots[x, 0],
                    AnchorMax = panelSlots[x, 1]
                },
                    CursorEnabled = false
                }, mainName);
            }

            int typeslot = 0;
            foreach (KeyValuePair<string, IemGameBase.GameManager> gm in IemGameBase.GetGameManagers())
            {
                if (!gm.Value.Enabled)
                    continue;


                //TODO bleh
                if (playerkeys[player.UserIDString].filter == "Team")
                {
                    if (gm.Value.Mode != "Team")
                        continue;
                }
                if (playerkeys[player.UserIDString].filter == "Solo")
                {
                    if (gm.Value.Mode != "Solo")
                        continue;
                }
                if (playerkeys[player.UserIDString].filter == "Individual")
                {
                    if (gm.Value.Mode != "Individual")
                        continue;
                }
                
                elements.Add(new CuiElement
                {
                    Parent = mainName,
                    Components =
                {
                    new CuiRawImageComponent
                    {
                        Url = gm.Value.TileImgUrl
                    },
                    new CuiRectTransformComponent
                    {
                                AnchorMin = panelSlots[typeslot, 0],
                                AnchorMax = panelSlots[typeslot, 1],
                    }
                }
                });


                elements.Add(new CuiButton
                {
                    Button = {
                    Command = "iem.menu start_solo "+gm.Key,
                    //Close = mainName,
                    Color = "0 255 0 0.2"
                },
                    RectTransform = {
                    AnchorMin = panelSlots[typeslot, 0],
                    AnchorMax = panelSlots[typeslot, 1],
                },
                    Text = {
                    Text = gm.Value.Name,
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter,
                    Color = "0 0 0 1"
                }
                }, mainName);


                typeslot++;
            }

            elements.Add(new CuiLabel
            {
                Text = {
                    Text = "Incursion Game Manager",
                    FontSize = 22,
                    Align = TextAnchor.MiddleLeft
    },
                RectTransform = {
                    AnchorMin = "0.25 0.85",
                    AnchorMax = "0.75 0.95",
                    OffsetMin = "0.1 0.1",
                    OffsetMax = "0.9 0.9"
                }
            }, mainName);

            int buttonslot = 0;
            foreach (var type in filters)
            {
                elements.Add(new CuiButton
                {
                    Button = {
                    Command = "iem.menu filter "+type,
                    Color = "0 255 0 1"
                },
                    RectTransform = {
                    AnchorMin = buttonSlots[buttonslot, 0],
                    AnchorMax = buttonSlots[buttonslot, 1]
                },
                    Text = {
                    Text = type,
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                }
                }, mainName);
                buttonslot++;
            }

            CuiHelper.AddUi(player, elements);
        }

        #endregion

        #region bindkeys

        void OnPlayerConnected(Network.Message packet)
        {
            Puts("OnPlayerConnected works!");
        }

        void OnPlayerInit(BasePlayer player)
        {
            Puts("OnPlayerInit works!");
            playerkeys[player.UserIDString] = new PlayerMenu(player.UserIDString);
            player.SendConsoleCommand("bind f5 chat.say \"/iem.menu toggle\"");
        }

        void OnPlayerRespawn(BasePlayer player)
        {
            Puts("OnPlayerRespawn works!");
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            Puts("OnPlayerRespawned works!");
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            Puts("OnPlayerDisconnected works!");
            player.SendConsoleCommand("bind f5 \"\"");
        }

        #endregion

        #region chat command

        [ChatCommand("iem.menu")]
        void cmdChatCount(BasePlayer player, string command, string[] args)
        {
            Puts("is: " + args[0].ToLower());
            switch (args[0].ToLower())
            {
                case "toggle":
                    if (playerkeys[player.UserIDString].showingMenu)
                        RemoveIntroOverlay(player);
                    else
                        ShowIntroOverlay(player, "here be the message");
                    break;
                case "set":
                    player.SendConsoleCommand("bind f5 chat.say \"/iem.menu toggle\"");

                    break;
                case "filter":

                    playerkeys[player.UserIDString].filter = args[0];
                    ShowIntroOverlay(player, "here be the message");
                    break;

                default:
                    break;
            }
        }


        #endregion

        #region console commands   

        [ConsoleCommand("iem.menu")]
        void ccmdEvent(ConsoleSystem.Arg arg)
        {
            switch (arg.Args[0].ToLower())
            {
                case "wound":
                    if (!IemUtils.hasAccess(arg)) return;
                    arg.Player().metabolism.calories.max = 180;
                    arg.Player().metabolism.calories.value = 250;
                    arg.Player().health = 75;
                    return;
                      
                case "filter": 

                    playerkeys[arg.Player().UserIDString].filter = arg.Args[1];
                    ShowIntroOverlay(arg.Player(), "here be the message");
                    break;
                case "start_solo":
                    Puts("starting : " + arg.Args[1].ToLower());
                    if (playerkeys[arg.Player().UserIDString].showingMenu)
                        RemoveIntroOverlay(arg.Player());
                    IemGameBase.StartFromMenu(arg.Player(), (string)arg.Args[1]);
                    break;
            }
           
        }
        #endregion

    }
}