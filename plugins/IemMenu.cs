//Requires: IemUtils
//Requires: IemGameBase
using UnityEngine;
using System.Collections.Generic;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("Incursion Menu", "Tolland", "0.1.0")]
    public class IemMenu : RustPlugin
    {
        [PluginReference]
        IemGameBase IemGameBase;

        static IemMenu iemMenu;
        static IemMenu me;

        private static List<string> guiList = new List<string>();
        private static Dictionary<string, PlayerMenu> playerkeys
            = new Dictionary<string, PlayerMenu>();

        void Init()
        {
            iemMenu = this;
            me = this;
            IemUtils.LogL("iemMenu: Init complete");
        }

        //TODO enum?
        public static List<string> filters = new List<string>() { "All", "Solo", "Team", "Individual" };

        class PlayerMenu
        {
            string steam_id;
            public Dictionary<string, string> keybindings = new Dictionary<string, string>();
            public bool showingMenu = false;
            public bool showingGameDetail = false;
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
                    AnchorMax = "0.5 0.9",
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
            gui = "IemMenuTilesPanel";
            CuiHelper.DestroyUi(player, gui);
            gui = "background";
            CuiHelper.DestroyUi(player, gui);
            gui = "background2";
            CuiHelper.DestroyUi(player, gui);
            playerkeys[player.UserIDString].showingMenu = false;
        }

        private static string[,] buttonSlots = {
            { "0.1 0.65", "0.9 0.7" },
            { "0.1 0.55", "0.9 0.6" },
            { "0.1 0.45", "0.9 0.5" },
            { "0.1 0.35", "0.9 0.4" },
                };

        private static string[,] gamebuttonSlots = {
            { "0.05 0.65", "0.15 0.7" },
            { "0.05 0.55", "0.15 0.6" },
            { "0.05 0.45", "0.15 0.5" },
            { "0.05 0.35", "0.15 0.4" },

                };

        public static void RemoveGameDetail(BasePlayer player)
        {
            string gui = "GameDetailMenu";
            CuiHelper.DestroyUi(player, gui);
            playerkeys[player.UserIDString].showingGameDetail = false;
        }

        public static void ShowIntroOverlay(BasePlayer player, string message, string game)
        {
            var gm = IemGameBase.gameManagers[game];
            playerkeys[player.UserIDString].showingGameDetail = true;
            string gui = "GameDetailMenu";
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

            elements.Add(new CuiButton
            {
                Button = {
                    Command = "iem.menu detail_toggle",
                    //Close = mainName,
                    Color = "128 64 0 1"
                },
                RectTransform = {
                    AnchorMin = "0.94 0.94",
                    AnchorMax = "0.98 0.98",
                },
                Text = {
                    Text = "X",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter,
                    Color = "255 255 255 1"
                }
            }, mainName);

            int buttonslot = 0;


            IemGameBase.IemGame currGame = null;

            if (gm.Mode == "Solo")
            {
                //  foreach (var thisgame in gm.games)
                // {

                //if ((thisgame.CurrentState == IemUtils.State.Before
                //||    thisgame.CurrentState == IemUtils.State.Running
                //         || thisgame.CurrentState == IemUtils.State.Paused) 
                //         && ((IemGameBase.IemSoloGame)thisgame).player==player)
                //{

                elements.Add(new CuiButton
                {
                    Button = {
                    Command = "iem.menu join "+gm.GetType().Name,
                    Color = "0 255 0 1"
                },
                    RectTransform = {
                    AnchorMin = gamebuttonSlots[0, 0],
                    AnchorMax = gamebuttonSlots[0, 1]
                },
                    Text = {
                    Text = "Play",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                }
                }, mainName);



                // }
            }
            else
            if (gm.Mode == "Team")
            {

                elements.Add(new CuiButton
                {
                    Button = {
                    Command = "iem.menu join "+gm.GetType().Name,
                    Color = "0 255 0 1"
                },
                    RectTransform = {
                    AnchorMin = gamebuttonSlots[0, 0],
                    AnchorMax = gamebuttonSlots[0, 1]
                },
                    Text = {
                    Text = "Join",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                }
                }, mainName);


            }







            elements.Add(new CuiLabel
            {
                Text = {
                    Text = gm.Name,
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


            elements.Add(new CuiLabel
            {
                Text = {
                    Text = gm.Description,
                    FontSize = 22,
                    Align = TextAnchor.MiddleLeft
    },
                RectTransform = {
                    AnchorMin = "0.25 0.55",
                    AnchorMax = "0.75 0.85",
                    OffsetMin = "0.1 0.1",
                    OffsetMax = "0.9 0.9"
                }
            }, mainName);



            elements.Add(new CuiElement
            {
                Parent = mainName,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Url = gm.TileImgUrl
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.65 0.75",
                       AnchorMax = "0.95 0.95",
                    }
                }
            });



            CuiHelper.AddUi(player, elements);
        }




        public static void ShowIntroOverlay(BasePlayer player, string message)
        {
            ShowMenuButtonPanel(player, message);
            ShowMenuTilesPanel(player, message);
        }

        public static void ShowMenuTilesPanel(BasePlayer player, string message)
        {
            string gui = "IemMenuTilesPanel";
            guiList.Add(gui);
            CuiHelper.DestroyUi(player, gui);
            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel
            {
                Image = {
                    Color = "0.1 0.1 0.1 1"
                },
                
                RectTransform = {
                    AnchorMin = "0.22 0.05",
                    AnchorMax = "0.95 0.95"
                },
                CursorEnabled = false
            }, "Overlay", gui);
            //for (int x = 0; x < array.GetLength(0); x += 1)
            //{
            //    for (int y = 0; y < array.GetLength(1); y += 1)
            //    {
            //        Console.Write(array[x, y]);
            //    }
            //}

            List<string> panels = new List<string>();

            float rows = 2;
            float cols = 5;
            float gap = 0.018f;
            float width = (1 - ((cols + 1) * gap)) / cols;
            float height = 0.2f;
            float starty = 0.7f;

            int curRow = 0;
            int curCol = 0;


            me.Puts("width=" + width);

            List<string[]> panelSlots = new List<string[]>();

            for (int x = 0; x < 10; x += 1)
            {
                if (curCol >= cols)
                {
                    curCol = 0;
                    curRow++;
                }

                var minx = (curCol * width) + ((curCol + 1) * gap);
                var miny = starty - ((curRow + 1) * gap) - (height * curRow) - height;
                var maxx = minx + width;
                var maxy = starty - ((curRow + 1) * gap) - (height * curRow);

                string panelSlotsmin = "" + minx + " " + miny;
                string panelSlotsmax = "" + maxx + " " + maxy;

                panelSlots.Add(new string[] { panelSlotsmin, panelSlotsmax });

                me.Puts("panelslot min=" + panelSlotsmin);
                me.Puts("panelslot max=" + panelSlotsmax);
                curCol++;
            }

            foreach (var item in panelSlots)
            {

            

                string panelname = elements.Add(new CuiPanel
                {
                    Image = {
                    Color = "0.7 0.2 0.1 1"
                },
                    RectTransform = {
                    AnchorMin = item[0],
                    AnchorMax = item[1]
                },
                    CursorEnabled = false
                }, mainName);

                IemUtils.DLog("panel name is " + panelname);
                panels.Add(panelname);
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
                    Parent = panels[typeslot],
                    Components =
                {
                    new CuiRawImageComponent
                    {
                        Url = gm.Value.TileImgUrl
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0 0.2",
                       AnchorMax = "1 1",
                    }
                }
                });


                elements.Add(new CuiButton
                {
                    Button = {
                                Command = "iem.menu game_detail "+gm.Key,
                                //Close = mainName,
                                Color = "0 255 0 0"
                            },
                    RectTransform = {
                    AnchorMin = panelSlots[typeslot][0],
                    AnchorMax = panelSlots[typeslot][1]
                            },
                    Text = {
                                Text = gm.Value.Name,
                                FontSize = 22,
                                Align = TextAnchor.LowerCenter,
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
                            AnchorMin = "0.1 0.85",
                            AnchorMax = "0.75 0.95",
                            OffsetMin = "0.1 0.1",
                            OffsetMax = "0.9 0.9"
                        }
            }, mainName);


            elements.Add(new CuiButton
            {
                Button = {
                    Command = "iem.menu toggle",
                    //Close = mainName,
                    Color = "128 64 0 1"
                },
                RectTransform = {
                    AnchorMin = "0.85 0.94",
                    AnchorMax = "0.98 0.98",
                },
                Text = {
                    Text = "X",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter,
                    Color = "255 255 255 1"
                }
            }, mainName);


            CuiHelper.AddUi(player, elements);
        }

        public static void ShowMenuButtonPanel(BasePlayer player, string message)
        {
            playerkeys[player.UserIDString].showingMenu = true;
            string gui = "IemMenuMain";
            guiList.Add(gui);
            CuiHelper.DestroyUi(player, gui);


            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel
            {
                Image = {
                    Color = "0.1 0.2 0.7 1"
                },
                RectTransform = {
                    AnchorMin = "0.05 0.05",
                    AnchorMax = "0.22 0.95"
                },
                CursorEnabled = true
            }, "Overlay", gui);


            guiList.Add("background2");
            CuiHelper.DestroyUi(player, "background2");
            var elements2 = new CuiElementContainer();
            var mainName2 = elements2.Add(new CuiPanel
            {
                Image = {
                    Color = "0.1 0.1 0.1 1"
                },

                RectTransform = {
                    AnchorMin = "0.22 0.05",
                    AnchorMax = "0.95 0.95"
                },
                CursorEnabled = false
            }, "Overlay", "background2");
            CuiHelper.AddUi(player, elements2);

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
                case "toggle":
                    if (playerkeys[arg.Player().UserIDString].showingMenu)
                        RemoveIntroOverlay(arg.Player());
                    else
                        ShowIntroOverlay(arg.Player(), "here be the message");
                    break;

                case "detail_toggle":
                    if (playerkeys[arg.Player().UserIDString].showingGameDetail)
                        CuiHelper.DestroyUi(arg.Player(), "GameDetailMenu");
                    //else
                    //ShowIntroOverlay(arg.Player(), "here be the message");
                    break;


                case "wound":
                    if (!IemUtils.hasAccess(arg)) return;
                    arg.Player().metabolism.calories.max = 180;
                    arg.Player().metabolism.calories.value = 250;
                    arg.Player().health = 75;
                    return;

                case "filter":

                    playerkeys[arg.Player().UserIDString].filter = arg.Args[1];
                    //ShowIntroOverlay(arg.Player(), "here be the message");
                    ShowMenuTilesPanel(arg.Player(), "here be the message");
                    break;
                case "start_solo":
                    Puts("starting : " + arg.Args[1].ToLower());
                    if (playerkeys[arg.Player().UserIDString].showingMenu)
                        RemoveIntroOverlay(arg.Player());
                    IemGameBase.StartFromMenu(arg.Player(), (string)arg.Args[1]);
                    break;
                case "join":
                    Puts("starting : " + arg.Args[1].ToLower());
                    if (playerkeys[arg.Player().UserIDString].showingGameDetail)
                        RemoveGameDetail(arg.Player());
                    IemGameBase.StartFromMenu(arg.Player(), (string)arg.Args[1]);
                    break;
                case "game_detail":
                    Puts("showing : " + arg.Args[1].ToLower());
                    if (playerkeys[arg.Player().UserIDString].showingMenu)
                        RemoveIntroOverlay(arg.Player());
                    ShowIntroOverlay(arg.Player(), "here be the message",
                        (string)arg.Args[1]);
                    break;
            }

        }
        #endregion

        #region stuff
        void Heal(BasePlayer basePlayer)
        {

            //var basePlayer = player.Object as BasePlayer;
            basePlayer.metabolism.bleeding.value = 0;
            basePlayer.metabolism.calories.value = basePlayer.metabolism.calories.max;
            basePlayer.metabolism.dirtyness.value = 0;
            basePlayer.metabolism.hydration.value = basePlayer.metabolism.hydration.max;
            basePlayer.metabolism.oxygen.value = 1;
            basePlayer.metabolism.poison.value = 0;
            basePlayer.metabolism.radiation_level.value = 0;
            basePlayer.metabolism.radiation_poison.value = 0;
            basePlayer.metabolism.wetness.value = 0;
            basePlayer.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, false);
            //basePlayer.CancelInvoke("WoundingEnd");
            basePlayer.CancelInvoke("WoundingTick");
            //basePlayer.Stop
            //CancelInvoke("WoundingEnd");
            basePlayer.Heal(100);
        }

        #endregion

        #region Commands

        [ConsoleCommand("heal")]
        private void ccmdZone(ConsoleSystem.Arg arg)
        {
            Heal(arg.Player());

        }
        #endregion
    }
}