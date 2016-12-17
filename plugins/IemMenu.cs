//Requires: IemUtils
//Requires: IemGameBase
//Requires: Kits
using UnityEngine;
using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using System;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Facepunch;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

using System.Linq;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("Incursion Menu", "Tolland", "0.1.0")]
    public class IemMenu : RustPlugin
    {

        #region header

        [PluginReference]
        IemGameBase IemGameBase;

        [PluginReference]
        IemUtils IemUtils;

        [PluginReference]
        Kits Kits;

        static IemMenu iemMenu;
        static IemMenu me;

        private static Dictionary<string, PlayerMenu> playerkeys
            = new Dictionary<string, PlayerMenu>();

        private static Dictionary<string, Action<BasePlayer, string[]>> menus
            = new Dictionary<string, Action<BasePlayer, string[]>>();

        #endregion

        #region hooks

        void Loaded()
        {
            //bleh
            menus["player_controls"] = new Action<BasePlayer, string[]>(ShowPlayerControls);
            menus["copy_paste"] = new Action<BasePlayer, string[]>(ShowCopyPaste);
            menus["kits"] = new Action<BasePlayer, string[]>(ShowKits);
            menus["main_menu"] = new Action<BasePlayer, string[]>(ShowMainMenu);
            menus["game_stats"] = new Action<BasePlayer, string[]>(ShowGameStats);
            menus["player_stats"] = new Action<BasePlayer, string[]>(ShowPlayerStats);
            menus["game_detail"] = new Action<BasePlayer, string[]>(ShowGameDetail);
            menus["in_game_menu"] = new Action<BasePlayer, string[]>(ShowInGameMenu);
        }

        void OnServerInitialized()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!playerkeys.ContainsKey(player.UserIDString))
                    playerkeys[player.UserIDString] = new PlayerMenu(player.UserIDString);

                player.SendConsoleCommand("bind f5 iem.menu toggle");
                player.SendConsoleCommand("bind f6 iem.menu move");

                CheckPlayer(player);
            }
        }
        static void DoRemove(BaseEntity Entity, bool gibs = true)
        {
            if (Entity != null)
            {
                //Interface.Oxide.CallHook("OnRemovedEntity", Entity);
                if (!Entity.isDestroyed)
                    Entity.Kill(BaseNetworkable.DestroyMode.None);
            }
        }
        public static IEnumerator DelayRemove(List<BuildingBlock> entities)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                DoRemove(entities[i], false);
                yield return new WaitWhile(new Func<bool>(() => (!entities[i].isDestroyed)));
            }
        }
        public Dictionary<string, uint> lastpaste = new Dictionary<string, uint>();

        void OnPlayerSleepEnded(BasePlayer player)
        {
            //Puts("OnPlayerSleepEnded works!");
            CheckPlayer(player);
        }

        void Init()
        {
            iemMenu = this;
            me = this;
            IemUtils.LogL("iemMenu: Init complete");
        }

        #endregion

        #region playermenu

        // from Kits.cs
        static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() { return DateTime.UtcNow.Subtract(epoch).TotalSeconds; }

        public class Cooldown
        {
            public BasePlayer player { get; set; }
            public double KitsCooldown { get; set; }
            public double CopyPasteCooldown { get; set; }
            public Dictionary<string, double> buildings = new Dictionary<string, double>();

            public Cooldown(BasePlayer player)
            {
                this.player = player;
                this.KitsCooldown = 0.0;
                this.CopyPasteCooldown = 0.0;
            }
        }

        public Dictionary<string, Cooldown> cooldowns = new Dictionary<string, Cooldown>();

        public double GetCooldown(BasePlayer player, string plugin)
        {
            if (!cooldowns.ContainsKey(player.UserIDString))
            {
                cooldowns[player.UserIDString] = new Cooldown(player);
            }

            string reason = "";
            double ct = CurrentTime();
            Puts("ct is " + ct);
            switch (plugin)
            {
                case "Kits":
                    var temp1 = cooldowns[player.UserIDString].KitsCooldown;
                    Puts("temp1 is " + temp1);
                    if (temp1 > ct && temp1 != 0.0)
                    {
                        reason += $"- {Math.Abs(Math.Ceiling(temp1 - ct))} seconds";
                        Puts("reason " + reason);
                        return Math.Abs(Math.Ceiling(temp1 - ct));
                    }
                    break;

                case "CopyPaste":
                    var temp2 = cooldowns[player.UserIDString].CopyPasteCooldown;
                    Puts("temp2 is " + temp2);
                    if (temp2 > ct && temp2 != 0.0)
                    {
                        reason += $"- {Math.Abs(Math.Ceiling(temp2 - ct))} seconds";
                        Puts("reason " + reason);
                        return Math.Abs(Math.Ceiling(temp2 - ct));
                    }
                    break;

                default:
                    break;
            }
            return 0;
        }

        public object StartCooldown(BasePlayer player, string plugin)
        {
            if (!cooldowns.ContainsKey(player.UserIDString))
            {
                cooldowns[player.UserIDString] = new Cooldown(player);
            }
            switch (plugin)
            {
                case "Kits":
                    cooldowns[player.UserIDString].KitsCooldown = CurrentTime() + 99;
                    break;

                case "CopyPaste":
                    cooldowns[player.UserIDString].CopyPasteCooldown = CurrentTime() + 99;

                    break;

                default:
                    break;
            }
            return true;
        }


        //TODO enum?
        public static List<string> filters = new List<string>() { "All", "Solo", "Team", "Individual" };

        class PlayerMenu
        {
            string steam_id;
            public Dictionary<string, string> keybindings = new Dictionary<string, string>();
            //use this to store state of menus, but hide them when needing to quickly
            //switch back to game with F5, --- currently not implemented
            //@TODO
            public bool hideMenus = false;
            public string filter;

            public Dictionary<string, bool> menus = new Dictionary<string, bool>();

            public PlayerMenu(string newId,
                bool newShowingMenu = false
                )
            {
                steam_id = newId;
                filter = "";
            }

            public void AddBinding(string key, string binding)
            {
                keybindings[key] = binding;

            }
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



        #endregion

        #region full page overlays
        /// <summary>
        /// this is some hard coded crap to put things in place
        /// needs to be switched over to some dynamically generated x,y coords
        /// </summary>

        private static string[,] buttonSlots = {
            { "0.1 0.73", "0.9 0.78" },
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

        #endregion

        #region Base class for encapsulating the leftrightpanels

        public class ContainerPanel
        {

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
            string panel_name)
        {

            string gui = panel_name + "_left";
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

            me.Puts("command is " + "iem.menu close " + panel_name);

            elements.Add(new CuiButton
            {
                Button = {
                    Command = "iem.menu close "+panel_name,
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
            Label,
            TextBox
        }


        public class BaseElement
        {
            public ElementType type { get; set; }
            public string Text { get; set; }
            public int FontSize { get; set; }
            public string ImgUrl { get; set; }
            public string Command { get; set; }
            public bool Close { get; set; }
            public float xmin { get; set; }
            public float xmax { get; set; }
            public float ymin { get; set; }
            public float ymax { get; set; }
            public string color { get; set; }
            public string backcolor { get; set; }
            public TextAnchor align { get; set; }

            public BaseElement()
            {
                FontSize = 40;
                Text = "";
                color = "1 1 1 1";
                backcolor = "0.1 0.1 0.1 0.6";
                align = TextAnchor.MiddleLeft;
            }

        }

        #endregion


        #region right content panel

        /// <summary>
        /// Lays out the main content panel for player
        /// </summary>
        /// <param name="player">the BasePlayer who receives the menu</param>
        /// <param name="baseElements">a collection of elements to place</param>
        /// <param name="panel_name">track the panel name so it can be managed</param>
        public static void ShowMainContentPanel(BasePlayer player,
            List<BaseElement> baseElements,
            string panel_name)
        {
            string gui = panel_name + "_right";
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
                else if (baseelement.type == ElementType.Panel)
                {
                    elements.Add(
                    new CuiPanel
                    {
                        Image = {
                            Color = baseelement.color
                        },
                        RectTransform = {
                            AnchorMin = $"{baseelement.xmin} {baseelement.ymin}",
                            AnchorMax = $"{baseelement.xmax} {baseelement.ymax}",
                        }
                    }, mainName);

                }
                else if (baseelement.type == ElementType.Label)
                {
                    elements.Add(
                       new CuiElement
                       {
                           // Name = name,
                           Parent = mainName,
                           // FadeOut = label.FadeOut,
                           Components =
                           {
                               new CuiTextComponent
                               {
                                    Text = baseelement.Text,
                                    FontSize = 22,
                                    Align = TextAnchor.MiddleLeft
                               },
                               new CuiRectTransformComponent
                               {
                                    AnchorMin = $"{baseelement.xmin} {baseelement.ymin}",
                                    AnchorMax = $"{baseelement.xmax} {baseelement.ymax}",
                                 //   OffsetMin = "0.1 0.1",
                                  //  OffsetMax = "0.9 0.9"
                                }
                            }
                       });
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
                else if (baseelement.type == ElementType.TextBox)
                {
                    elements.Add(
                        new CuiPanel
                        {
                            Image = {
                               Color = baseelement.backcolor
                            },
                            RectTransform = {
                               AnchorMin = $"{baseelement.xmin} {baseelement.ymin}",
                               AnchorMax = $"{baseelement.xmax} {baseelement.ymax}",
                            },
                        }, mainName);

                    me.Puts("setting text component color to " + baseelement.color);

                    elements.Add(new CuiElement
                    {
                        Parent = mainName,
                        Components =
                    {
                        new CuiTextComponent
                        {
                            Text = baseelement.Text,
                            Color = baseelement.color,
                            FontSize = baseelement.FontSize,
                            Align = baseelement.align,
                            //FadeIn = 3
                        },
                        new CuiRectTransformComponent
                        {
                                AnchorMin = $"{baseelement.xmin} {baseelement.ymin}",
                                AnchorMax = $"{baseelement.xmax} {baseelement.ymax}"
                        }
                    }
                    });

                }
                else if (baseelement.type == ElementType.Button)
                {
                    elements.Add(new CuiButton
                    {
                        Button = {
                        Command = baseelement.Command,
                        Color = baseelement.backcolor
                        },
                        RectTransform = {
                                AnchorMin = $"{baseelement.xmin} {baseelement.ymin}",
                                AnchorMax = $"{baseelement.xmax} {baseelement.ymax}"
                        },
                        Text = {
                            Text = baseelement.Text,
                            FontSize = 22,
                            Align = baseelement.align,

                        }
                    }, mainName);



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

        // section for actual concrete pages

        #region Main Menu

        public static void ShowMainMenu(BasePlayer player, string[] args = null)
        {
            args = args ?? new string[0];

            var buttons = new List<BaseButton>() { };

            foreach (var type in filters)
            {
                buttons.Add(new BaseButton()
                {
                    Text = type,
                    Command = "iem.menu filter " + type,
                });
            }

            buttons.Add(new BaseButton()
            {
                Text = "Plugin Control",
                Command = "iem.menu menu player_controls",
                ButtonColor = "0 0 128 1"
            });

            // stuff to ban players, and find them etc
            if (IemUtils.IsAdmin(player))
            {
                buttons.Add(new BaseButton()
                {
                    Text = "admin stuff",
                    Command = "iem.menu filter ",
                });
            }

            //section for main content of page
            var elements = new List<BaseElement>() { };

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

            List<float[]> panelSlots = new List<float[]>();

            for (int x = 0; x < 10; x += 1)
            {
                if (curCol >= cols)
                {
                    curCol = 0;
                    curRow++;
                }

                float minx = (curCol * width) + ((curCol + 1) * gap);
                float miny = starty - ((curRow + 1) * gap) - (height * curRow) - height;
                float maxx = minx + width;
                float maxy = starty - ((curRow + 1) * gap) - (height * curRow);

                string panelSlotsmin = "" + minx + " " + miny;
                string panelSlotsmax = "" + maxx + " " + maxy;

                panelSlots.Add(new float[] {minx,
                    miny,
                    maxx,
                    maxy });

                elements.Add(new BaseElement()
                {
                    type = ElementType.Panel,
                    color = "0.7 0.2 0.1 1",
                    xmin = minx,
                    xmax = maxx,
                    ymin = miny,
                    ymax = maxy
                });

                //  me.Puts("panelslot min=" + panelSlotsmin);
                // me.Puts("panelslot max=" + panelSlotsmax);
                curCol++;
            }

            int slot = 0;
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

                elements.Add(new BaseElement()
                {
                    type = ElementType.Image,
                    ImgUrl = gm.Value.TileImgUrl,
                    xmin = panelSlots[slot][0],
                    xmax = panelSlots[slot][2],
                    ymin = panelSlots[slot][1] + 0.025f,
                    ymax = panelSlots[slot][3]

                });

                elements.Add(new BaseElement()
                {
                    type = ElementType.Button,
                    Text = gm.Value.Name,
                    Command = "iem.menu game_detail " + gm.Key,
                    xmin = panelSlots[slot][0],
                    xmax = panelSlots[slot][2],
                    ymin = panelSlots[slot][1],
                    ymax = panelSlots[slot][3],
                    align = TextAnchor.LowerCenter,
                    backcolor = "0.1 0.1 0.1 0"
                });
                slot++;
            }

            elements.Add(new BaseElement()
            {
                type = ElementType.OutlineText,
                Text = $"<color=#ffffff>Main menu</color>",
                xmin = 0.05f,
                xmax = 0.75f,
                ymin = 0.75f,
                ymax = 0.95f
            });

            ShowLeftButtonPanel(player, buttons, "main_menu");
            ShowMainContentPanel(player, elements, "main_menu");

        }


        #endregion

        #region Game Menu

        public static void ShowGameMenu(BasePlayer player)
        {

            var buttons = new List<BaseButton>() { };

            foreach (var type in filters)
            {
                buttons.Add(new BaseButton()
                {
                    Text = type,
                    Command = "iem.menu filter " + type,
                });
            }

            // stuff to ban players, and find them etc
            if (IemUtils.IsAdmin(player))
            {
                buttons.Add(new BaseButton()
                {
                    Text = "admin stuff",
                    Command = "iem.menu filter ",
                });
            }

            //section for main content of page
            var elements = new List<BaseElement>() { };

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

            List<float[]> panelSlots = new List<float[]>();

            for (int x = 0; x < 10; x += 1)
            {
                if (curCol >= cols)
                {
                    curCol = 0;
                    curRow++;
                }

                float minx = (curCol * width) + ((curCol + 1) * gap);
                float miny = starty - ((curRow + 1) * gap) - (height * curRow) - height;
                float maxx = minx + width;
                float maxy = starty - ((curRow + 1) * gap) - (height * curRow);

                string panelSlotsmin = "" + minx + " " + miny;
                string panelSlotsmax = "" + maxx + " " + maxy;

                panelSlots.Add(new float[] {minx,
                    miny,
                    maxx,
                    maxy });

                elements.Add(new BaseElement()
                {
                    type = ElementType.Panel,
                    color = "0.7 0.2 0.1 1",
                    xmin = minx,
                    xmax = maxx,
                    ymin = miny,
                    ymax = maxy
                });

                //  me.Puts("panelslot min=" + panelSlotsmin);
                // me.Puts("panelslot max=" + panelSlotsmax);
                curCol++;
            }

            int slot = 0;
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

                elements.Add(new BaseElement()
                {
                    type = ElementType.Image,
                    ImgUrl = gm.Value.TileImgUrl,
                    xmin = panelSlots[slot][0],
                    xmax = panelSlots[slot][2],
                    ymin = panelSlots[slot][1] + 0.025f,
                    ymax = panelSlots[slot][3]

                });

                elements.Add(new BaseElement()
                {
                    type = ElementType.Button,
                    Text = gm.Value.Name,
                    Command = "iem.menu game_detail " + gm.Key,
                    xmin = panelSlots[slot][0],
                    xmax = panelSlots[slot][2],
                    ymin = panelSlots[slot][1],
                    ymax = panelSlots[slot][3],
                    align = TextAnchor.LowerCenter,
                    backcolor = "0.1 0.1 0.1 0"
                });
                slot++;
            }

            elements.Add(new BaseElement()
            {
                type = ElementType.OutlineText,
                Text = $"<color=#ffffff>Main menu</color>",
                xmin = 0.05f,
                xmax = 0.75f,
                ymin = 0.75f,
                ymax = 0.95f
            });

            ShowLeftButtonPanel(player, buttons, "game_menu_left");
            ShowMainContentPanel(player, elements, "game_menu_main");

        }

        #endregion

        #region Player Control Menu

        //test for non-existing plugin
        [PluginReference]
        Plugin CopyPaste11;

        [PluginReference]
        Plugin CopyPaste;

        //[PluginReference]
        //Plugin Kits;

        public static void ShowPlayerControls(BasePlayer player, string[] args)
        {
            me.Puts("showing player controls");

            var buttons = new List<BaseButton>() { };

            if (me.CopyPaste != null)
            {
                buttons.Add(new BaseButton()
                {
                    Text = "CopyPaste",
                    Command = "iem.menu menu copy_paste",
                });
            }

            if (me.Kits != null)
            {
                buttons.Add(new BaseButton()
                {
                    Text = "Kits",
                    Command = "iem.menu menu kits",
                });
            }
            if (me.CopyPaste11 == null)
            {
                me.Puts("Copy paste 11 is null");
            }

            // stuff to ban players, and find them etc
            if (IemUtils.IsAdmin(player))
            {
                buttons.Add(new BaseButton()
                {
                    Text = "admin stuff",
                    Command = "iem.menu filter ",
                });
            }

            //section for main content of page
            var elements = new List<BaseElement>() { };

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

            List<float[]> panelSlots = new List<float[]>();

            for (int x = 0; x < 10; x += 1)
            {
                if (curCol >= cols)
                {
                    curCol = 0;
                    curRow++;
                }

                float minx = (curCol * width) + ((curCol + 1) * gap);
                float miny = starty - ((curRow + 1) * gap) - (height * curRow) - height;
                float maxx = minx + width;
                float maxy = starty - ((curRow + 1) * gap) - (height * curRow);

                string panelSlotsmin = "" + minx + " " + miny;
                string panelSlotsmax = "" + maxx + " " + maxy;

                panelSlots.Add(new float[] {minx,
                    miny,
                    maxx,
                    maxy });

                //elements.Add(new BaseElement()
                //{
                //    type = ElementType.Panel,
                //    color = "0.7 0.2 0.1 1",
                //    xmin = minx,
                //    xmax = maxx,
                //    ymin = miny,
                //    ymax = maxy
                //});

                //  me.Puts("panelslot min=" + panelSlotsmin);
                // me.Puts("panelslot max=" + panelSlotsmax);
                curCol++;
            }


            elements.Add(new BaseElement()
            {
                type = ElementType.OutlineText,
                Text = $"<color=#ffffff>Plugin Control</color>",
                xmin = 0.05f,
                xmax = 0.75f,
                ymin = 0.75f,
                ymax = 0.95f
            });


            //elements.Add(new BaseElement()
            //{
            //    type = ElementType.Button,
            //    Text = "bleh bleh",
            //    Command = "iem.menu game_detail ",
            //    xmin = 0.25f,
            //    xmax = 0.75f,
            //    ymin = 0.55f,
            //    ymax = 0.95f,
            //    align = TextAnchor.LowerCenter,
            //    backcolor = "0.1 0.1 0.1 0"
            //});

            ShowLeftButtonPanel(player, buttons, "player_controls");
            ShowMainContentPanel(player, elements, "player_controls");
        }

        #endregion


        #region Copy Paste Menu

        public static void ShowCopyPaste(BasePlayer player, string[] args)
        {
            me.Puts("showing ShowCopyPaste");

            var buttons = new List<BaseButton>() { };

            // stuff to ban players, and find them etc
            if (IemUtils.IsAdmin(player))
            {
                buttons.Add(new BaseButton()
                {
                    Text = "admin stuff",
                    Command = "iem.menu filter ",
                });
            }

            //section for main content of page
            var elements = new List<BaseElement>() { };

            List<string> panels = new List<string>();

            float rows = 2;
            float cols = 5;
            float gap = 0.018f;
            float width = (1 - ((cols + 1) * gap)) / cols;
            float height = 0.2f;
            float starty = 0.7f;

            int curRow = 0;
            int curCol = 0;

            List<float[]> panelSlots = new List<float[]>();

            for (int x = 0; x < 10; x += 1)
            {
                if (curCol >= cols)
                {
                    curCol = 0;
                    curRow++;
                }

                float minx = (curCol * width) + ((curCol + 1) * gap);
                float miny = starty - ((curRow + 1) * gap) - (height * curRow) - height;
                float maxx = minx + width;
                float maxy = starty - ((curRow + 1) * gap) - (height * curRow);

                string panelSlotsmin = "" + minx + " " + miny;
                string panelSlotsmax = "" + maxx + " " + maxy;

                panelSlots.Add(new float[] {minx,
                    miny,
                    maxx,
                    maxy });

                elements.Add(new BaseElement()
                {
                    type = ElementType.Panel,
                    color = "0.7 0.2 0.1 1",
                    xmin = minx,
                    xmax = maxx,
                    ymin = miny,
                    ymax = maxy
                });

                // me.Puts("panelslot min=" + panelSlotsmin);
                // me.Puts("panelslot max=" + panelSlotsmax);
                curCol++;
            }

            var mypastes = new List<Dictionary<string, string>>() {
                new Dictionary<string, string>()
                {
                    { "Name", "Mushroom" },
                    { "Filename", "Mushroom" },
                    { "TileImgUrl", "http://90.213.126.3:8080/images/mushroom.jpg" }
                },
                new Dictionary<string, string>()
                {
                    { "Name", "HomeSweetHome" },
                    { "Filename", "HomeSweetHome" },
                    { "TileImgUrl", "http://90.213.126.3:8080/images/homesweethome.png" }
                },
                new Dictionary<string, string>()
                {
                    { "Name", "3 by 3 by 3" },
                    { "Filename", "3by3by3" },
                    { "TileImgUrl", "http://90.213.126.3:8080/images/3by3by3.png" }
                },
                new Dictionary<string, string>()
                {
                    { "Name", "2 by 2" },
                    { "Filename", "2by2" },
                    { "TileImgUrl", "http://90.213.126.3:8080/images/2by2.png" }
                },
                new Dictionary<string, string>()
                {
                    { "Name", "Hexarena" },
                    { "Filename", "Hexarena" },
                    { "TileImgUrl", "http://assets.rustserversio.netdna-cdn.com/beancan/hexarena3.png" }
                },
                new Dictionary<string, string>()
                {
                    { "Name", "Bridge" },
                    { "Filename", "bridgev1" },
                    { "TileImgUrl", "http://90.213.126.3:8080/images/bridgev1.png" }
                }
                };

            //player.Add

            int slot = 0;
            foreach (Dictionary<string, string> item in mypastes)
            {
                elements.Add(new BaseElement()
                {
                    type = ElementType.Image,
                    ImgUrl = item["TileImgUrl"],
                    xmin = panelSlots[slot][0],
                    xmax = panelSlots[slot][2],
                    ymin = panelSlots[slot][1] + 0.025f,
                    ymax = panelSlots[slot][3]

                });

                elements.Add(new BaseElement()
                {
                    type = ElementType.Button,
                    Text = item["Name"],
                    Command = "iem.menu preview " + item["Filename"],
                    xmin = panelSlots[slot][0],
                    xmax = panelSlots[slot][2],
                    ymin = panelSlots[slot][1],
                    ymax = panelSlots[slot][3],
                    align = TextAnchor.LowerCenter,
                    backcolor = "0.1 0.1 0.1 0"
                });
                slot++;
            }



            elements.Add(new BaseElement()
            {
                type = ElementType.OutlineText,
                Text = $"<color=#ffffff>Copy Paste Buildings</color>",
                xmin = 0.05f,
                xmax = 0.75f,
                ymin = 0.75f,
                ymax = 0.95f
            });

            ShowLeftButtonPanel(player, buttons, "copy_paste");
            ShowMainContentPanel(player, elements, "copy_paste");
        }

        #endregion

        #region Kits


        public static void ShowKits(BasePlayer player, string[] args)
        {
            //me.Puts("showing kits");

            var buttons = new List<BaseButton>() { };

            // stuff to ban players, and find them etc
            if (IemUtils.IsAdmin(player))
            {
                buttons.Add(new BaseButton()
                {
                    Text = "admin stuff",
                    Command = "iem.menu filter ",
                });
            }

            //section for main content of page
            var elements = new List<BaseElement>() { };

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

            List<float[]> panelSlots = new List<float[]>();

            for (int x = 0; x < 10; x += 1)
            {
                if (curCol >= cols)
                {
                    curCol = 0;
                    curRow++;
                }

                float minx = (curCol * width) + ((curCol + 1) * gap);
                float miny = starty - ((curRow + 1) * gap) - (height * curRow) - height;
                float maxx = minx + width;
                float maxy = starty - ((curRow + 1) * gap) - (height * curRow);

                string panelSlotsmin = "" + minx + " " + miny;
                string panelSlotsmax = "" + maxx + " " + maxy;

                panelSlots.Add(new float[] {minx,
                    miny,
                    maxx,
                    maxy });

                //elements.Add(new BaseElement()
                //{
                //    type = ElementType.Panel,
                //    color = "0.7 0.2 0.1 1",
                //    xmin = minx,
                //    xmax = maxx,
                //    ymin = miny,
                //    ymax = maxy
                //});

                //  me.Puts("panelslot min=" + panelSlotsmin);
                // me.Puts("panelslot max=" + panelSlotsmax);
                curCol++;
            }


            elements.Add(new BaseElement()
            {
                type = ElementType.OutlineText,
                Text = $"<color=#ffffff>Kits menu</color>",
                xmin = 0.05f,
                xmax = 0.75f,
                ymin = 0.75f,
                ymax = 0.95f
            });

            var kits = me.Kits.GetAllKits();
            int count = 0;
            foreach (var item in kits)
            {
                // me.Puts("kit " + " " + item);
                //me.Kits.CanRedeemKit(player, "admin");
                var canredeem = me.Kits.CallHook("CanRedeemKit", player, item);
                me.Puts("can redeem kit " + item + " " + canredeem);

                string buff = "";
                var seekit = me.Kits.CallHook("CanSeeKit", player, item, false, buff);
                me.Puts("can see kit " + " " + seekit);
                me.Puts("can see kit " + " " + buff);
                if ((bool)seekit)
                {
                    elements.Add(new BaseElement()
                    {
                        type = ElementType.TextBox,
                        Text = $"<color=#ffffff>" + item + "</color>" + buff,
                        FontSize = 22,
                        xmin = 0.05f,
                        xmax = 0.75f,
                        ymin = 0.61f - (float)(count * 0.05),
                        ymax = 0.65f - (float)(count * 0.05)
                    });
                    if (canredeem is bool && (bool)canredeem)
                    {
                        elements.Add(new BaseElement()
                        {
                            type = ElementType.Button,
                            Text = "Redeem",
                            Command = "iem.menu redeem_kit " + item,
                            xmin = 0.25f,
                            xmax = 0.35f,
                            ymin = 0.61f - (float)(count * 0.05),
                            ymax = 0.65f - (float)(count * 0.05),
                            align = TextAnchor.MiddleCenter,
                            backcolor = "1 0 0 1"
                        });
                    }
                    else
                    {
                        elements.Add(new BaseElement()
                        {
                            type = ElementType.TextBox,
                            Text = $"<color=#ffffff>" + canredeem + "</color>",
                            FontSize = 22,
                            xmin = 0.25f,
                            xmax = 0.65f,
                            ymin = 0.61f - (float)(count * 0.05),
                            ymax = 0.65f - (float)(count * 0.05),
                        });

                    }
                    count++;
                }
            }

            count++;
            elements.Add(new BaseElement()
            {
                type = ElementType.Button,
                Text = "Clear inventory",
                Command = "iem.menu inventory_strip",
                xmin = 0.05f,
                xmax = 0.75f,
                ymin = 0.61f - (float)(count * 0.05),
                ymax = 0.65f - (float)(count * 0.05),
                align = TextAnchor.MiddleCenter,
                backcolor = "0 1 0 1"
            });

            ShowLeftButtonPanel(player, buttons, "kits");
            ShowMainContentPanel(player, elements, "kits");
        }

        #endregion

        #region Game Detail

        public static void ShowGameDetail(BasePlayer player, string[] args)
        {
            if (args.Length == 1)
            {
                // me.Puts("hiding game_detail");
                HideMenu(player, "game_detail");
            }
            else
            {
                //   me.Puts("showing game_detail");
                //ShowIntroOverlay(arg.Player(), "here be the message", arg.Args[1]);
                ShowGameDetail(player, args[1]);
            }
        }

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

            //add blank button as separator
            buttons.Add(new BaseButton()
            {
                Text = "",
                Command = "",
                ButtonColor = "128 0 128 0"
            });

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

            ShowLeftButtonPanel(player, buttons, "game_detail");
            ShowMainContentPanel(player, elements, "game_detail");

        }

        #endregion

        #region Player Stats

        public static void ShowPlayerStats(BasePlayer player, string[] args = null)
        {
            var level = "";
            if (args.Length == 1)
            {
                HideMenu(player, "player_stats");
            }
            else if (args.Length == 2)
            {
                ShowPlayerStats(player, args[1]);
            }
            else if (args.Length > 2)
            {
                me.Puts("player_stats : has level");
                level = args[2];
                ShowPlayerStats(player, args[1], level);
            }
        }

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
                //   me.Puts("dlevel is " + dlevel);
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

            elements.Add(new BaseElement()
            {
                type = ElementType.TextBox,
                Text = $"Loading Image",
                color = "0 0 0 1",
                backcolor = "25 25 25 1",
                align = TextAnchor.MiddleCenter,
                xmin = 0.05f,
                xmax = 0.7f,
                ymin = 0.2f,
                ymax = 0.7f
            });

            //string chart_url = "https://docs.google.com/spreadsheets/d/19U5ZWP-sLZdyGI9UVfeaMa5eXl79CMg8Trdv5nGyZcc/pubchart?oid=1877531591&format=image";
            string chart_url = "http://90.213.126.3:8080/test2.php?playername=" +
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

            ShowLeftButtonPanel(player, buttons, "player_stats");
            ShowMainContentPanel(player, elements, "player_stats");

        }

        #endregion

        #region Game Stats

        public static void ShowGameStats(BasePlayer player, string[] args = null)
        {
            if (args.Length == 1)
            {
                HideMenu(player, "game_stats");
            }
            else if (args.Length == 2)
            {
                ShowGameStats(player, args[1]);
            }
            else if (args.Length > 2)
            {
                //       me.Puts("game_stats : has length > 2");
                string level = args[2];
                ShowGameStats(player, args[1], level);
            }
        }

        public static void ShowGameStats(BasePlayer player, string gameManagerName, string level = "")
        {
            var gm = IemGameBase.gameManagers[gameManagerName];
            var buttons = new List<BaseButton>() { };

            foreach (var dlevel in gm.difficultyModes.Keys)
            {
                //      me.Puts("dlevel is " + dlevel);
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

            elements.Add(new BaseElement()
            {
                type = ElementType.TextBox,
                Text = $"Loading Image",
                color = "0 0 0 1",
                backcolor = "25 25 25 1",
                align = TextAnchor.MiddleCenter,
                xmin = 0.05f,
                xmax = 0.45f,
                ymin = 0.2f,
                ymax = 0.7f
            });

            //string chart_url = "https://docs.google.com/spreadsheets/d/19U5ZWP-sLZdyGI9UVfeaMa5eXl79CMg8Trdv5nGyZcc/pubchart?oid=1877531591&format=image";
            string chart_url = "http://90.213.126.3:8080/test5.php?playername=" +
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

            ShowLeftButtonPanel(player, buttons, "game_stats");
            ShowMainContentPanel(player, elements, "game_stats");

        }



        #endregion

        #region In Game Menus

        public static void ShowInGameMenu(BasePlayer player, string[] args)
        {
            var game = IemGameBase.FindActiveGameForPlayer(player);
            if (game != null)
            {
                ShowInGameMenu(player, game);
            }
        }

        public static void ShowInGameMenu(BasePlayer player, IemGameBase.IemGame game)
        {
            //var gm = game.
            var buttons = new List<BaseButton>() { };

            buttons.Add(new BaseButton()
            {
                Text = "Quit Game",
                Command = "iem.menu quit_game " + game.Name
            });

            buttons.Add(new BaseButton()
            {
                Text = "Restart Level",
                Command = "iem.menu restart_level " + game.Name
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

            string message = $"Difficulty is {game.difficultyLevel} \nStarted at XX:XX\n Level is {game.level} \n\n You can quit this " +
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

            ShowLeftButtonPanel(player, buttons, "in_game_menu");
            ShowMainContentPanel(player, elements, "in_game_menu");
        }

        #endregion

        #region menu controls generic

        public static void Showmenu(BasePlayer player, string menuname, string[] args = null)
        {
            //  me.Puts("invoking " + menuname);
            menus[menuname].Invoke(player, args);
            playerkeys[player.UserIDString].menus[menuname] = true;
        }

        public static void HideMenu(BasePlayer player, string menuName)
        {
            CuiHelper.DestroyUi(player, menuName + "_left");
            CuiHelper.DestroyUi(player, menuName + "_right");
            playerkeys[player.UserIDString].menus[menuName] = false;
        }


        public static void ToggleMenu(BasePlayer player, string[] args)
        {

            //weird problem that a single arg is appended with value True
            if (args.Length > 0 && args[1] != "True")
            {
                //    me.Puts("arg is " + args[1]);
                // ToggleMenu(player, args[1]);
            }

            // if any menus are showing, close them
            if (playerkeys[player.UserIDString].menus.Values.Any(x => x == true))
            {
                var keys = new List<string>(playerkeys[player.UserIDString].menus.Keys);

                foreach (var item in keys)
                {
                    //      me.Puts("menu is >>> " + item);
                    //            me.Puts("state is >>> " + playerkeys[player.UserIDString].menus[item]);
                    HideMenu(player, item);
                }
            }
            // no menus are showing, so do ingame menu or main
            else
            {
                //   me.Puts("show main or game");
                var game = IemGameBase.FindActiveGameForPlayer(player);
                if (game != null)
                {
                    Showmenu(player, "in_game_menu");
                }
                else
                {
                    Showmenu(player, "main_menu");
                }
            }

            //if (!playerkeys[player.UserIDString].menus.ContainsKey(menuname))
            //{
            //    playerkeys[player.UserIDString].menus[menuname] = false;
            //}
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
            Puts("OnPlayerDisconnected works! " + reason);
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
                    ShowMainMenu(player);
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
            var keys = new List<string>(playerkeys[player.UserIDString].menus.Keys);

            foreach (var item in keys)
            {
                //     me.Puts("state is " + item);
                //       me.Puts("state is " + playerkeys[player.UserIDString].menus[item]);
                HideMenu(player, item);
            }
        }
        int rayLayer = LayerMask.GetMask(new string[] { "Construction", "Deployed", "Tree", "Terrain", "Resource", "World", "Water", "Default", "Prevent Building" });
        bool FindRayEntity(Vector3 sourcePos, Vector3 sourceDir, out Vector3 point, out BaseEntity entity)
        {
            RaycastHit hitinfo;
            entity = null;
            point = default(Vector3);

            if (!Physics.Raycast(sourcePos, sourceDir, out hitinfo, 1000f, rayLayer)) { return false; }

            point = hitinfo.point;
            entity = hitinfo.GetEntity();
            return true;
        }

        [ConsoleCommand("iem.menu")]
        void ccmdEvent(ConsoleSystem.Arg arg)
        {
            //    me.Puts("command is " + arg.Args[0]);
            var player = arg.Player();

            for (int i = 0; i < arg.Args.GetLength(0); i++)
            {
                //      Puts($"command pos {i} is {arg.Args[i]}");
            }

            var keys = new List<string>(playerkeys[player.UserIDString].menus.Keys);

            foreach (var item in keys)
            {
                //    me.Puts("menu is  " + item);
                //    me.Puts("state is " + playerkeys[player.UserIDString].menus[item]);
            }

            switch (arg.Args[0].ToLower())
            {
                //takes an argument that specifies the name of the panel to exit
                case "close":
                    //weird problem that a single arg is appended with value True
                    if (arg.Args.Length > 0 && arg.Args[1] != "True")
                    {
                        //     me.Puts("closing menu " + arg.Args[1]);
                        HideMenu(arg.Player(), arg.Args[1]);
                    }

                    break;
                //closes all showing menus
                case "exit":
                    HideMenuUIs(arg.Player());


                    break;

                //if any menus are open, close them all
                //if no menus are open, either open the ingame menu, or the general menu
                case "toggle":

                    ToggleMenu(arg.Player(), arg.Args);
                    break;

                case "filter":

                    playerkeys[arg.Player().UserIDString].filter = arg.Args[1];
                    //ShowIntroOverlay(arg.Player(), "here be the message");
                    Showmenu(arg.Player(), "main_menu");
                    break;

                case "menu":

                    //weird problem that a single arg is appended with value True
                    if (arg.Args.Length > 0 && arg.Args[1] != "True")
                    {
                        //      me.Puts("opening menu " + arg.Args[1]);
                        Showmenu(arg.Player(), arg.Args[1]);
                    }
                    break;

                case "wound":
                    if (!IemUtils.hasAccess(arg)) return;
                    arg.Player().metabolism.calories.max = 180;
                    arg.Player().metabolism.calories.value = 250;
                    arg.Player().health = 75;
                    return;

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

                    Showmenu(arg.Player(), "game_detail", arg.Args);
                    break;
                //toggle player stats
                case "player_stats":
                    Puts("player_stats : toggle " + arg.Args.Length);
                    //if (playerkeys[arg.Player().UserIDString].showingMenu)
                    //    RemoveIntroOverlay(arg.Player());
                    Showmenu(arg.Player(), "player_stats", arg.Args);
                    break;
                //toggle player stats
                case "game_stats":
                    Puts("game_stats : toggle " + arg.Args.Length);
                    Showmenu(arg.Player(), "game_stats", arg.Args);

                    break;
                case "quit_game":
                    HideMenuUIs(arg.Player());
                    var livegame = IemGameBase.FindActiveGameForPlayer(arg.Player());
                    if (livegame != null)
                    {
                        livegame.CancelGame();
                    }
                    break;
                case "restart_level":
                    HideMenuUIs(arg.Player());
                    var thisgame = IemGameBase.FindActiveGameForPlayer(arg.Player());
                    if (thisgame != null)
                    {
                        thisgame.RestartLevel();
                    }
                    break;
                case "redeem_kit":
                    HideMenuUIs(arg.Player());
                    var game1 = IemGameBase.FindActiveGameForPlayer(arg.Player());
                    //don't allow redeeming kit in game
                    if (game1 == null)
                    {
                        bool redeemkit = (bool)me.Kits.CallHook("CanRedeemKit", player, arg.Args[1]);
                        me.Puts("can redeem kit " + " " + redeemkit);

                        string buff = "";
                        bool seekit = (bool)me.Kits.CallHook("CanSeeKit", player, arg.Args[1], false, buff);
                        if (seekit && redeemkit)
                        {
                            me.Kits?.Call("GiveKit", player, arg.Args[1]);
                        }
                    }
                    break;
                case "inventory_strip":
                    //HideMenuUIs(arg.Player());
                    var game3 = IemGameBase.FindActiveGameForPlayer(arg.Player());
                    //don't allow redeeming kit in game
                    if (game3 == null)
                    {
                        //bool redeemkit = (bool)me.Kits.CallHook("CanRedeemKit", player, arg.Args[1]);
                        player.inventory.Strip();
                    }
                    break;
                case "preview":
                    HideMenuUIs(arg.Player());
                    var params11 = new List<string> {
                           "autoheight", "true",
                           "height", "0.5",
                           "blockcollision", "0",
                           "deployables", "true",
                           "inventories", "true" };


                    var success1 = CopyPaste?.CallHook("TryPasteFromPlayer", player,
                        arg.Args[1], params11.ToArray(), true);
                    Puts("output is " + success1);

                    if (success1 is string)
                    {
                        SendReply(player, (string)success1);
                    }
                    else
                    {
                        //StartCooldown(player, "CopyPaste");
                        if (success1 is List<Dictionary<string, object>>)
                        {
                            var timers = new List<Timer>();
                            //  Puts("is preload data");
                            foreach (var data in (List<Dictionary<string, object>>)success1)
                            {
                                Vector3 point1, point2, point3, point4, point5, point6, point7, point8;
                                try
                                {
                                    var prefabname = (string)data["prefabname"];
                                    var skinid = ulong.Parse(data["skinid"].ToString());
                                    var pos = (Vector3)data["position"];
                                    var rot = (Quaternion)data["rotation"];

                                    // Puts("prefab is " + prefabname);
                                    if (((string)data["prefabname"]).Contains("/foundation.triangle/"))
                                    {
                                        // Puts("triangle foundation at " + pos);
                                        var x = (float)Math.Pow((Math.Pow(3.0, 2) - Math.Pow(1.5, 2.0)), (1.0 / 2.0));

                                        point1 = new Vector3(pos.x + 1.5f,
                                            pos.y, pos.z) - pos;
                                        point2 = new Vector3(pos.x, pos.y, pos.z + x) - pos;
                                        point3 = new Vector3(pos.x - 1.5f, pos.y, pos.z) - pos;

                                        point1 = (rot * point1) + pos;
                                        point2 = (rot * point2) + pos;
                                        point3 = (rot * point3) + pos;

                                        point5 = IemUtils.GetGroundY(point1 + Vector3.down);
                                        point6 = IemUtils.GetGroundY(point2 + Vector3.down);
                                        point7 = IemUtils.GetGroundY(point3 + Vector3.down);

                                        IemUtils.DrawTriangleFoundation(player,
                                              point1,
                                              point2,
                                              point3,
                                              point5,
                                              point6,
                                              point7,
                                              10f
                                         );
                                        timers.Add(timer.Every(1f, () =>
                                        {
                                            IemUtils.DrawTriangleFoundation(player,
                                              point1,
                                              point2,
                                              point3,
                                              point5,
                                              point6,
                                              point7,
                                              1f
                                         );
                                        }));

                                        //player.SendConsoleCommand("ddraw.line", 130f, Color.red, pos, pos + Vector3.up + Vector3.up);
                                    }
                                    else if (((string)data["prefabname"]).Contains("/foundation/"))
                                    {
                                        //  Puts("foundation at " + pos);

                                        //var dir: Vector3 = point - pivot; // get point direction relative to pivot
                                        //dir = Quaternion.Euler(angles) * dir; // rotate it
                                        //point = dir + pivot; // calculate rotated point

                                        point1 = new Vector3(pos.x + 1.45f, pos.y, pos.z + 1.45f) - pos;
                                        point2 = new Vector3(pos.x + 1.45f, pos.y, pos.z - 1.45f) - pos;
                                        point3 = new Vector3(pos.x - 1.45f, pos.y, pos.z - 1.45f) - pos;
                                        point4 = new Vector3(pos.x - 1.45f, pos.y, pos.z + 1.45f) - pos;

                                        point1 = (rot * point1) + pos;
                                        point2 = (rot * point2) + pos;
                                        point3 = (rot * point3) + pos;
                                        point4 = (rot * point4) + pos;


                                        point5 = IemUtils.GetGroundY(point1 + Vector3.down);
                                        point6 = IemUtils.GetGroundY(point2 + Vector3.down);
                                        point7 = IemUtils.GetGroundY(point3 + Vector3.down);
                                        point8 = IemUtils.GetGroundY(point4 + Vector3.down);


                                        IemUtils.DrawFoundation(player,
                                              point1,
                                              point2,
                                              point3,
                                              point4,
                                              point5,
                                              point6,
                                              point7,
                                              point8,
                                              10f
                                         );

                                        timers.Add(timer.Every(1f, ()=> {
                                            IemUtils.DrawFoundation(player,
                                            point1,
                                            point2,
                                            point3,
                                            point4,
                                            point5,
                                            point6,
                                            point7,
                                            point8,
                                            1f
                                       );
                                        } ));
                                    }


                                }
                                catch (Exception e)
                                {
                                    PrintError(string.Format("Trying to paste {0} send this error: {1}", data["prefabname"].ToString(), e.Message));
                                }
                            }

                            var ViewAngles = Quaternion.Euler(player.GetNetworkRotation());
                            BaseEntity sourceEntity;
                            Vector3 sourcePoint;

                            if (!FindRayEntity(player.eyes.position, ViewAngles * Vector3.forward,
                                out sourcePoint, out sourceEntity))
                            {
                                SendReply(player, "Couldn't ray something valid in front of you.");
                            }

                            IemUI.ConfirmCancel(player, "Place building", "Confirm?", "Cancel?",
                                 () =>
                                 {
                                     foreach (var item in timers)
                                     {
                                         item.Destroy();
                                     }


                                     var params111 = new List<string> {
                           "autoheight", "true",
                           "height", "0.5",
                           "blockcollision", "0",
                           "deployables", "true",
                           "inventories", "true" };


                                     var success = CopyPaste?.CallHook("TryPaste", sourcePoint, arg.Args[1], player,
                                        ViewAngles.ToEulerAngles().y, params111.ToArray());
                                     Puts("output is " + success);

                                     if (success is string)
                                     {
                                         SendReply(player, (string)success);
                                     }
                                     else
                                     {
                                         SendReply(player, "pasted " + success);
                                     }
                                 }, () => {
                                     foreach (var item in timers)
                                     {
                                         item.Destroy();
                                     }

                                 }, true);

                        }
                    }


                    break;
                case "paste":
                    HideMenuUIs(arg.Player());

                    //TODO need to move in game paste logic to the game itself
                    // as some games will require player driven paste feature
                    var game2 = IemGameBase.FindActiveGameForPlayer(arg.Player());

                    var cd = GetCooldown(player, "CopyPaste");
                    //SendReply(player, "Copy paste cooldown is " + cd); 
                    if (cd > 0)
                    {
                        SendReply(player, "Copy paste cooldown is " + cd);
                        return;
                    }

                    //don't allow pasting in game
                    if (game2 == null)
                    {

                        var params1 = new List<string> {
                           "autoheight", "true",
                           "height", "0.5",
                           "blockcollision", "0",
                           "deployables", "true",
                           "inventories", "true" };


                        var success = CopyPaste?.CallHook("TryPasteFromPlayer", player,
                            arg.Args[1], params1.ToArray());
                        Puts("output is " + success);

                        if (success is string)
                        {
                            SendReply(player, (string)success);
                        }
                        else
                        {
                            //StartCooldown(player, "CopyPaste");
                            if (success is List<BaseEntity>)
                            {
                                SendReply(player, "is list");
                                var buildingblock = ((List<BaseEntity>)success)[0].GetComponentInParent<BuildingBlock>();

                                HashSet<uint> bids = new HashSet<uint>();

                                if (buildingblock != null)
                                {
                                    //if (!lastpaste.ContainsKey(player.UserIDString)){
                                    lastpaste[player.UserIDString] = buildingblock.buildingID;
                                    //}
                                }

                                foreach (var item in (List<BaseEntity>)success)
                                {
                                    var bb = (item.GetComponentInParent<BuildingBlock>());
                                    if (bb != null)
                                    {
                                        bids.Add(bb.buildingID);
                                        //  SendReply(player, "add was " + bb);

                                    }
                                    else
                                    {
                                        //SendReply(player, "bb was nyull");
                                    }
                                }

                                foreach (var b in bids)
                                {
                                    SendReply(player, "bid was " + b);
                                }

                            }
                        }

                    }
                    break;
                case "move":
                    if (lastpaste.ContainsKey(player.UserIDString))
                    {
                        //lastpaste[player.UserIDString] = buildingblock.buildingID;

                        SendReply(player, "removing building id " + lastpaste[player.UserIDString]);

                        var removeList = UnityEngine.GameObject.FindObjectsOfType<BuildingBlock>().Where(
                            x => x.buildingID == lastpaste[player.UserIDString]).ToList();

                        ServerMgr.Instance.StartCoroutine(DelayRemove(removeList));

                    }
                    else
                    {
                        SendReply(player, "no lastepaste");
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