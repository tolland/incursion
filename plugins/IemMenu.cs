//Requires: IemUtils
//Requires: IemGameBase
using UnityEngine;
using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using System;

namespace Oxide.Plugins
{
    [Info("Incursion Menu", "Tolland", "0.1.0")]
    public class IemMenu : RustPlugin
    {
        [PluginReference]
        IemGameBase IemGameBase;

        [PluginReference]
        IemUtils IemUtils;

        static IemMenu iemMenu;
        static IemMenu me;

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
            public bool showingPlayerStats = false;
            public bool showingGameStats = false;
            public bool showingInGameMenu = false;
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

        void Loaded()
        {
            
        }

        void OnServerInitialized()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!playerkeys.ContainsKey(player.UserIDString))
                    playerkeys[player.UserIDString] = new PlayerMenu(player.UserIDString);

                player.SendConsoleCommand("bind f5 iem.menu toggle");

                CheckPlayer(player);
            }
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            //Puts("OnPlayerSleepEnded works!");
            CheckPlayer(player);
        }

        void CheckPlayer(BasePlayer player)
        {
            //Puts("calling check player");
            ShowMenuHud(player);
        }

        void ShowMenuHud(BasePlayer player)
        {
            string gui = "MenuHud";
            IemUtils.guiList.Add(gui);

            CuiHelper.DestroyUi(player, gui);

            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel
            {
                Image = {
                    Color = "0.1 0.1 0.1 0.4"
                },
                RectTransform = {
                    AnchorMin = "0.15 0.01",
                    AnchorMax = "0.30 0.09"
                },
                CursorEnabled = false
            }, "Overlay", gui);

            elements.Add(new CuiLabel
            {
                Text = {
                    Text = "Menu\nf5",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
            },
                RectTransform = {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1",
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
            { "0.1 0.57", "0.9 0.62" },
            { "0.1 0.49", "0.9 0.54" },
            { "0.1 0.41", "0.9 0.46" },
            { "0.1 0.33", "0.9 0.38" },
            { "0.1 0.25", "0.9 0.3" },
            { "0.1 0.17", "0.9 0.22" },
            { "0.1 0.09", "0.9 0.14" },
                };

        private static string[,] gamebuttonSlots = {
            { "0.025 0.73", "0.175 0.78" },
            { "0.025 0.65", "0.175 0.7" },
            { "0.025 0.57", "0.175 0.62" },
            { "0.025 0.49", "0.175 0.54" },
            { "0.025 0.41", "0.175 0.46" },
            { "0.025 0.33", "0.175 0.38" },
            { "0.025 0.25", "0.175 0.30" },
            { "0.025 0.17", "0.175 0.22" },
            { "0.025 0.09", "0.175 0.14" },

                };

        public static void ShowIntroOverlay(BasePlayer player, string message)
        {
            ShowMenuButtonPanel(player, message);
            ShowMenuTilesPanel(player, message);
        }

        public static void ShowMenuTilesPanel(BasePlayer player, string message)
        {
            string gui = "IemMenuTilesPanel";
            IemUtils.guiList.Add(gui);
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


            //me.Puts("width=" + width);

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

                //  me.Puts("panelslot min=" + panelSlotsmin);
                // me.Puts("panelslot max=" + panelSlotsmax);
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

                // IemUtils.DLog("panel name is " + panelname);
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
                    Color = "255 255 255 1"
                },
                RectTransform = {
                    AnchorMin = "0.9 0.94",
                    AnchorMax = "0.98 0.98",
                },
                Text = {
                    Text = "X",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter,
                    Color = "0 0 0 1"
                }
            }, mainName);


            CuiHelper.AddUi(player, elements);
        }

        public static void ShowMenuButtonPanel(BasePlayer player, string message)
        {
            playerkeys[player.UserIDString].showingMenu = true;
            string gui = "IemMenuMain";
            IemUtils.guiList.Add(gui);
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


            IemUtils.guiList.Add("background2");
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


            elements.Add(new CuiButton
            {
                Button = {
                    Command = "iem.menu toggle",
                    Color = "128 0 128 1"
                },
                RectTransform = {
                    AnchorMin = buttonSlots[buttonSlots.GetLength(0)-1, 0],
                    AnchorMax = buttonSlots[buttonSlots.GetLength(0)-1, 1]
                },
                Text = {
                    Text = "Exit",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                }
            }, mainName);

            CuiHelper.AddUi(player, elements);
        }

        #endregion

        #region left button panel

        public class BaseButton
        {
            public string Text { get; set; }
            public string Command { get; set; }
            public bool Close { get; set; }
            public string ButtonColor { get; set; }

            public BaseButton()
            {
                ButtonColor = "0 255 0 1";
                Text = "";
                Command = "";
            }

        }

        public static void ShowLeftButtonPanel(BasePlayer player,
            List<BaseButton> buttons,
            string panel_name = "ShowLeftButtonPanel")
        {
            //playerkeys[player.UserIDString].showingMenu = true;
            string gui = panel_name;
            IemUtils.guiList.Add(gui);
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

            int buttonSlot = 0;

            if (buttons != null)
                foreach (var button in buttons)
                {
                    elements.Add(new CuiButton
                    {
                        Button = {
                    Command = button.Command,
                    Color = button.ButtonColor
                        },
                        RectTransform = {
                            AnchorMin = buttonSlots[buttonSlot, 0],
                            AnchorMax = buttonSlots[buttonSlot, 1]
                        },
                        Text = {
                            Text = button.Text,
                            FontSize = 22,
                            Align = TextAnchor.MiddleCenter
                        }
                    }, mainName);

                    buttonSlot++;
                }

            elements.Add(new CuiButton
            {
                Button = {
                    Command = "iem.menu toggle",
                    Color = "128 0 128 1"
                },
                RectTransform = {
                    AnchorMin = buttonSlots[buttonSlots.GetLength(0)-1, 0],
                    AnchorMax = buttonSlots[buttonSlots.GetLength(0)-1, 1]
                },
                Text = {
                    Text = "Back",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                }
            }, mainName);

            CuiHelper.AddUi(player, elements);

        }

        public enum ElementType
        {
            OutlineText,
            Panel,
            Button,
            Image,
            Label
        }


        public class BaseElement
        {
            public ElementType type { get; set; }
            public string Text { get; set; }
            public string ImgUrl { get; set; }
            public string Command { get; set; }
            public bool Close { get; set; }
            public float xmin { get; set; }
            public float xmax { get; set; }
            public float ymin { get; set; }
            public float ymax { get; set; }
        }

        public static void ShowMainContentPanel(BasePlayer player,
            List<BaseElement> baseElements,
            string panel_name = "ShowMainContentPanel")
        {
            string gui = panel_name;
            IemUtils.guiList.Add(gui);
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



            // Uri.EscapeDataString(player.displayName);
            foreach (var baseelement in baseElements)
            {

                if (baseelement.type == ElementType.Image)
                {
                    elements.Add(new CuiElement
                    {
                        Parent = mainName,
                        Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Url = baseelement.ImgUrl
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"{baseelement.xmin} {baseelement.ymin}",
                                   AnchorMax = $"{baseelement.xmax} {baseelement.ymax}"
                                }
                            }
                    });
                }
                else if (baseelement.type == ElementType.Label)
                {

                    elements.Add(new CuiLabel
                    {
                        Text = {
                            Text = baseelement.Text,
                                FontSize = 22,
                                Align = TextAnchor.MiddleLeft
                             },
                        RectTransform = {
                            AnchorMin = $"{baseelement.xmin} {baseelement.ymin}",
                            AnchorMax = $"{baseelement.xmax} {baseelement.ymax}",
                            OffsetMin = "0.1 0.1",
                            OffsetMax = "0.9 0.9"
                        }
                    }, mainName);
                }
                else if (baseelement.type == ElementType.OutlineText)
                {
                    elements.Add(new CuiElement
                    {
                        Parent = mainName,
                        Components =
                    {
                        new CuiTextComponent
                        {
                            Text = baseelement.Text,
                            FontSize = 40,
                            Align = TextAnchor.MiddleLeft,
                            //FadeIn = 3
                        },
                        new CuiOutlineComponent
                        {
                            Distance = "1.0 1.0",
                            Color = "1 0.3 0.3 0.6"
                        },
                        new CuiRectTransformComponent
                        {
                                AnchorMin = $"{baseelement.xmin} {baseelement.ymin}",
                                AnchorMax = $"{baseelement.xmax} {baseelement.ymax}"
                        }
                    }
                    });
                }

            }

            elements.Add(new CuiButton
            {
                Button = {
                    Command = "iem.menu exit",
                    //Close = mainName,
                    Color = "255 255 255 1"
                },
                RectTransform = {
                    AnchorMin = "0.9 0.94",
                    AnchorMax = "0.98 0.98",
                },
                Text = {
                    Text = "X",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter,
                    Color = "0 0 0 1"
                }
            }, mainName);

            CuiHelper.AddUi(player, elements);
        }

        #endregion

        #region Game Detail

        public static void ShowGameDetail(BasePlayer player, string gameManagerName, string level = "")
        {
            var gm = IemGameBase.gameManagers[gameManagerName];
            var buttons = new List<BaseButton>() { };

            if (gm.Mode == "Solo")
            {
                if (gm.HasDifficultyModes)
                {
                    foreach (var dlevel in gm.difficultyModes.Values)
                    {
                        buttons.Add(new BaseButton()
                        {
                            Text = "Play " + dlevel.Name,
                            Command = "iem.menu join_level " + gm.GetType().Name + " " + dlevel.Name,
                        });
                    }
                }
                else
                {
                    buttons.Add(new BaseButton()
                    {
                        Text = "Play",
                        Command = "iem.menu join " + gm.GetType().Name,
                    });
                }
            }
            else if (gm.Mode == "Team")
            {

                buttons.Add(new BaseButton()
                {
                    Text = "Join",
                    Command = "iem.menu join " + gm.GetType().Name,
                });
            }


            if (gm.HasStats)
            {
                buttons.Add(new BaseButton()
                {
                    Text = "Player Stats",
                    Command = "iem.menu player_stats " + gm.GetType().Name,
                    ButtonColor = "128 0 128 1"
                });
            }

            if (gm.HasGameStats)
            {
                buttons.Add(new BaseButton()
                {
                    Text = "Game Stats",
                    Command = "iem.menu game_stats " + gm.GetType().Name,
                    ButtonColor = "128 0 128 1"
                });
            }


            var elements = new List<BaseElement>() { };

            elements.Add(new BaseElement()
            {
                type = ElementType.OutlineText,
                Text = $"<color=#ffffff>{gm.Name}</color>",
                xmin = 0.1f,
                xmax = 0.75f,
                ymin = 0.75f,
                ymax = 0.95f

            });

            elements.Add(new BaseElement()
            {
                type = ElementType.Label,
                Text = gm.Description,
                xmin = 0.1f,
                xmax = 0.75f,
                ymin = 0.15f,
                ymax = 0.75f

            });

            elements.Add(new BaseElement()
            {
                type = ElementType.Image,
                ImgUrl = gm.TileImgUrl,
                xmin = 0.65f,
                xmax = 0.88f,
                ymin = 0.65f,
                ymax = 0.95f
            });

            ShowLeftButtonPanel(player, buttons, "game_detail_left");
            ShowMainContentPanel(player, elements, "game_detail_main");

            playerkeys[player.UserIDString].showingGameDetail = true;
        }

        public static void HideGameDetail(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "game_detail_left");
            CuiHelper.DestroyUi(player, "game_detail_main");
            playerkeys[player.UserIDString].showingGameDetail = false;
        }

        #endregion

        #region Player Stats

        public static void ShowPlayerStats(BasePlayer player, string gameManagerName, string level = "")
        {
            var gm = IemGameBase.gameManagers[gameManagerName];
            var buttons = new List<BaseButton>() { };

            buttons.Add(new BaseButton()
            {
                Text = "All Levels",
                Command = "iem.menu player_stats " + gameManagerName
            });

            foreach (var dlevel in gm.difficultyModes.Keys)
            {
                me.Puts("dlevel is " + dlevel);
                buttons.Add(new BaseButton()
                {
                    Text = dlevel + " Level",
                    Command = "iem.menu player_stats " + gameManagerName + " " + dlevel
                });
            }

            var elements = new List<BaseElement>() { };

            elements.Add(new BaseElement()
            {
                type = ElementType.OutlineText,
                Text = $"Player Stats: <color=#ffffff>{gm.Name}</color>",
                xmin = 0.05f,
                xmax = 0.75f,
                ymin = 0.75f,
                ymax = 0.95f

            });

            //string chart_url = "https://docs.google.com/spreadsheets/d/19U5ZWP-sLZdyGI9UVfeaMa5eXl79CMg8Trdv5nGyZcc/pubchart?oid=1877531591&format=image";
            string chart_url = "http://2.122.153.252:8080/test2.php?playername=" +
                Uri.EscapeDataString(player.displayName) + "&steamid=" + player.UserIDString + "&difficulty=" + level;
            elements.Add(new BaseElement()
            {
                type = ElementType.Image,
                Text = level + " Level",
                ImgUrl = chart_url,
                xmin = 0.05f,
                xmax = 0.7f,
                ymin = 0.2f,
                ymax = 0.7f

            });

            ShowLeftButtonPanel(player, buttons, "player_stats_left");
            ShowMainContentPanel(player, elements, "player_stats_main");

            playerkeys[player.UserIDString].showingPlayerStats = true;
        }

        public static void HidePlayerStats(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "player_stats_left");
            CuiHelper.DestroyUi(player, "player_stats_main");
            playerkeys[player.UserIDString].showingPlayerStats = false;
        }


        #endregion

        #region Game Stats

        public static void ShowGameStats(BasePlayer player, string gameManagerName, string level = "")
        {
            var gm = IemGameBase.gameManagers[gameManagerName];
            var buttons = new List<BaseButton>() { };

            foreach (var dlevel in gm.difficultyModes.Keys)
            {
                me.Puts("dlevel is " + dlevel);
                buttons.Add(new BaseButton()
                {
                    Text = dlevel + " Level",
                    Command = "iem.menu game_stats " + gameManagerName + " " + dlevel
                });
            }

            var elements = new List<BaseElement>() { };

            elements.Add(new BaseElement()
            {
                type = ElementType.OutlineText,
                Text = $"Game Stats: <color=#ffffff>{gm.Name}</color>",
                xmin = 0.05f,
                xmax = 0.75f,
                ymin = 0.75f,
                ymax = 0.95f

            });

            //string chart_url = "https://docs.google.com/spreadsheets/d/19U5ZWP-sLZdyGI9UVfeaMa5eXl79CMg8Trdv5nGyZcc/pubchart?oid=1877531591&format=image";
            string chart_url = "http://2.122.153.252:8080/test5.php?playername=" +
                Uri.EscapeDataString(player.displayName) + "&steamid=" + player.UserIDString + "&difficulty=" + level;

            elements.Add(new BaseElement()
            {
                type = ElementType.Image,
                Text = level + " Level",
                ImgUrl = chart_url,
                xmin = 0.05f,
                xmax = 0.45f,
                ymin = 0.2f,
                ymax = 0.7f

            });

            ShowLeftButtonPanel(player, buttons, "game_stats_left");
            ShowMainContentPanel(player, elements, "game_stats_main");

            playerkeys[player.UserIDString].showingPlayerStats = true;
        }

        public static void HideGameStats(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "game_stats_left");
            CuiHelper.DestroyUi(player, "game_stats_main");
            playerkeys[player.UserIDString].showingGameStats = false;
        }


        #endregion

        #region In Game Menus

        public static void ShowInGameMenu(BasePlayer player, IemGameBase.IemGame game)
        {
            //var gm = game.
            var buttons = new List<BaseButton>() { };

            buttons.Add(new BaseButton()
            {
                Text = "Quit Game",
                Command = "iem.menu quit_game " + game.Name
            });

            var elements = new List<BaseElement>() { };

            elements.Add(new BaseElement()
            {
                type = ElementType.OutlineText,
                Text = $"You are currently playing <color=red>{game.Name}</color>",
                xmin = 0.05f,
                xmax = 0.75f,
                ymin = 0.75f,
                ymax = 0.95f
                 
            });

            string message = $"Level is {game.difficultyLevel} \nStarted at XX:XX\n\n You can quit this "+
                "game and return to the map with the button on the left";

            elements.Add(new BaseElement()
            {
                type = ElementType.Label,
                Text = message,
                xmin = 0.1f,
                xmax = 0.75f,
                ymin = 0.15f,
                ymax = 0.75f

            });


            ShowLeftButtonPanel(player, buttons, "in_game_menu_left");
            ShowMainContentPanel(player, elements, "in_game_menu_main");

            playerkeys[player.UserIDString].showingInGameMenu = true;
        }

        public static void HideInGameMenu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "in_game_menu_left");
            CuiHelper.DestroyUi(player, "in_game_menu_main");
            playerkeys[player.UserIDString].showingInGameMenu = false;
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
            player.SendConsoleCommand("bind f5 iem.menu toggle");
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            Puts("OnPlayerDisconnected works! "+ reason);
            //player.SendConsoleCommand(@"bind f5 badgers");
        }
         
        #endregion

        #region chat command

        [ChatCommand("iem.menu")]
        void cmdChatCount(BasePlayer player, string command, string[] args)
        {
            Puts("is: " + args[0].ToLower());
            switch (args[0].ToLower())
            {
                case "set":
                    player.SendConsoleCommand("bind f5 chat.say \"/iem.menu toggle\"");

                    break;
                case "filter":

                    playerkeys[player.UserIDString].filter = args[0];
                    ShowIntroOverlay(player, "here be the message");
                    break;

                case "open":
                    Application.OpenURL("http://unity3d.com/");
                    break;
                default:

                    break;
            }
        }


        #endregion

        #region console commands   

        public void HideMenuUIs(BasePlayer player)
        {
            HideInGameMenu(player);
            HidePlayerStats(player);
            HideGameStats(player);
            HideGameDetail(player);
            RemoveIntroOverlay(player);
        }

        [ConsoleCommand("iem.menu")]
        void ccmdEvent(ConsoleSystem.Arg arg)
        {
            me.Puts("command is " + arg.Args[0]);

            switch (arg.Args[0].ToLower())
            {
                case "exit":

                    HideMenuUIs(arg.Player());

                    break;
                case "toggle":
                    var game = IemGameBase.FindActiveGameForPlayer(arg.Player());
                    if (game != null)
                    {
                        if (playerkeys[arg.Player().UserIDString].showingInGameMenu)
                        {
                            HideInGameMenu(arg.Player());
                        }
                        else
                        {
                            ShowInGameMenu(arg.Player(), game);
                        }
                    }
                    else if (playerkeys[arg.Player().UserIDString].showingPlayerStats
                        || playerkeys[arg.Player().UserIDString].showingGameStats)
                    {
                        HidePlayerStats(arg.Player());
                        HideGameStats(arg.Player());
                    }
                    else if (playerkeys[arg.Player().UserIDString].showingGameDetail)
                    {
                        HideGameDetail(arg.Player());
                    }
                    else if (playerkeys[arg.Player().UserIDString].showingMenu)
                    {
                        RemoveIntroOverlay(arg.Player());
                    }
                    else
                    {
                        ShowIntroOverlay(arg.Player(), "here be the message");
                    }
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
                    HideMenuUIs(arg.Player());
                    IemGameBase.StartFromMenu(arg.Player(), (string)arg.Args[1]);
                    break;
                case "join":
                    Puts("joining : " + arg.Args[1].ToLower());
                    HideMenuUIs(arg.Player());
                    IemGameBase.StartFromMenu(arg.Player(), (string)arg.Args[1]);
                    break;
                case "join_level":
                    Puts("join_level : " + arg.Args[1].ToLower());
                    HideMenuUIs(arg.Player());
                    IemGameBase.StartFromMenu(arg.Player(), (string)arg.Args[1], (string)arg.Args[2]);
                    break;
                case "game_detail":
                    Puts("game_detail : " + arg.Args[1].ToLower());
                    if (arg.Args.Length == 1)
                    {
                        HideGameDetail(arg.Player());
                    }
                    else
                    {
                        //ShowIntroOverlay(arg.Player(), "here be the message", arg.Args[1]);
                        ShowGameDetail(arg.Player(), (string)arg.Args[1]);
                    }
                    break;
                //toggle player stats
                case "player_stats":
                    Puts("player_stats : toggle " + arg.Args.Length);
                    //if (playerkeys[arg.Player().UserIDString].showingMenu)
                    //    RemoveIntroOverlay(arg.Player());
                    var level = "";
                    if (arg.Args.Length == 1)
                    {
                        HidePlayerStats(arg.Player());

                    }
                    else if (arg.Args.Length == 2)
                    {
                        ShowPlayerStats(arg.Player(), (string)arg.Args[1]);
                    }
                    else if (arg.Args.Length > 2)
                    {
                        Puts("player_stats : has level");
                        level = (string)arg.Args[2];
                        ShowPlayerStats(arg.Player(), (string)arg.Args[1], level);
                    }
                    break;
                //toggle player stats
                case "game_stats":
                    Puts("game_stats : toggle " + arg.Args.Length);
                    if (arg.Args.Length == 1)
                    {
                        HideGameStats(arg.Player());
                    }
                    else if (arg.Args.Length == 2)
                    {
                        ShowGameStats(arg.Player(), (string)arg.Args[1]);
                    }
                    else if (arg.Args.Length > 2)
                    {
                        Puts("game_stats : has length > 2");
                        level = (string)arg.Args[2];
                        ShowGameStats(arg.Player(), (string)arg.Args[1], level);
                    }
                    break;
                case "quit_game":
                    var livegame = IemGameBase.FindActiveGameForPlayer(arg.Player());
                    if (livegame != null)
                    {
                        HideInGameMenu(arg.Player());
                        livegame.CancelGame();
                    }
                    break;
                default:
                    for (int i = 0; i < arg.Args.GetLength(0); i++)
                    {
                        Puts($"command pos {i} is {arg.Args[i]}");
                    }

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