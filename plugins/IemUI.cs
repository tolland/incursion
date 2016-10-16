//Requires: IemUtils

using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Incursion UI Class", "tolland", "0.1.0")]

    class IemUI : RustPlugin
    {

        #region header 

        [PluginReference]
        IemUtils IemUtils;

        static IemUI iemUI;

        private static List<string> guiList = new List<string>();

        #endregion

        #region boiler plate

        void Init()
        {
            iemUI = this;
            IemUtils.LogL("IemUI: Init complete");
        }

        void Unload()
        {
            RemoveUIs();
            IemUtils.LogL("IemUI: Unload complete");

        }

        void OnServerInitialized()
        {
            //if the server restarted during a banner, these need to be removed
            RemoveUIs();
            IemUtils.LogL("IncursionUI: on server initialized");
        }

        #endregion

        #region clean up

        public static void RemoveUIs()
        {
            List<BasePlayer> activePlayers = BasePlayer.activePlayerList;

            foreach (BasePlayer player in activePlayers)
            {
                CleanUpGui(player);
            }
        }

        public static void CleanUpGui(BasePlayer player)
        {
            foreach (string gui in guiList)
            {
                CuiHelper.DestroyUi(player, gui);
            }
        }

        #endregion

        #region full page overlays

        public static void ShowIntroOverlay(BasePlayer player, string message)
        {
            string gui = "ShowIntroOverlay";
            guiList.Add(gui);
            CuiHelper.DestroyUi(player, gui);
            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel
            {
                Image = {
                    Color = "0.1 0.1 0.1 1"
                },
                RectTransform = {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                },
                CursorEnabled = true
            }, "Overlay", gui);

            elements.Add(new CuiLabel
            {
                Text = {
                    Text = message,
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform = {
                    AnchorMin = "0.25 0.25",
                    AnchorMax = "0.75 0.75"
                }
            }, mainName);


            elements.Add(new CuiButton
            {
                Button = {
                    Close = mainName,
                    Color = "0 255 0 1"
                },
                RectTransform = {
                    AnchorMin = "0.4 0.16",
                    AnchorMax = "0.6 0.2"
                },
                Text = {
                    Text = "I Agree",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                }
            }, mainName);


            CuiHelper.AddUi(player, elements);
        }

        #endregion

        #region Top Message banners

        public static void CreateGameBanner(BasePlayer player, string message)
        {
            string ARENA = $"{message}";
            IemUtils.DLog(message);
            string gui = "CreateGameBanner";
            guiList.Add(gui);

            CuiHelper.DestroyUi(player, gui);

            var elements = new CuiElementContainer();
            CuiElement textElement = new CuiElement
            {
                Name = gui,
                Parent = "Hud.Under",
                //FadeOut = 10,
                Components =
                    {
                        new CuiTextComponent
                        {
                            Text = $"<color=#cc0000>{message}</color>",
                            FontSize = 22,
                            Align = TextAnchor.MiddleCenter,
                            //FadeIn = 5
                        },
                        new CuiOutlineComponent
                        {
                            Distance = "1.0 1.0",
                            Color = "1.0 1.0 1.0 1.0"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.22 0.945",
                            AnchorMax =  "0.78 0.995"
                        }
                    }
            };
            elements.Add(textElement);
            CuiHelper.AddUi(player, elements);

        }

        public static void CreateGameStatusBanner(BasePlayer player, string message)
        {
            string ARENA = $"{message}";
            string gui = "CreateGameStatusBanner";
            guiList.Add(gui);

            CuiHelper.DestroyUi(player, gui);
            var elements = new CuiElementContainer();
            CuiElement textElement = new CuiElement
            {
                Name = gui,
                Parent = "Hud.Under",
                //FadeOut = 10,
                Components =
                    {
                new CuiTextComponent
                {
                    Text = $"<color=#cc0000>{message}</color>",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter,
                    //FadeIn = 5
                },
                        new CuiOutlineComponent
                        {
                            Distance = "1.0 1.0",
                            Color = "0.3 0.3 0.3 0.6"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.22 0.895",
                            AnchorMax = "0.78 0.945"
                        }
                    }
            };
            elements.Add(textElement);
            CuiHelper.AddUi(player, elements);

        }

        #endregion

        #region Debug banners


        public static string cachedEventBanner = "";

        public static void CreateEventDebugBanner(string message)
        {
            if (message.Equals(cachedEventBanner))
                return;
            IemUtils.DLog("caching event banner");
            cachedEventBanner = message;

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CreateEventDebugBanner(player, message);
            }
        }

        public static void CreateEventDebugBanner(BasePlayer player, string message)
        {
            string ARENA = $"{message}";
            string gui = "CreateEventDebugBanner";
            guiList.Add(gui);

            CuiHelper.DestroyUi(player, "CreateEventDebugBanner");

            var elements = new CuiElementContainer();
            CuiElement textElement = new CuiElement
            {
                Name = gui,
                Parent = "Hud.Under",
                //FadeOut = 10,
                Components =
                    {
                new CuiTextComponent
                {
                    Text = $"<color=#000000>{message}</color>",
                    FontSize = 16,
                    Align = TextAnchor.MiddleCenter,
                    FadeIn = 5
                },
                        new CuiOutlineComponent
                        {
                            Distance = "1.0 1.0",
                            Color = "0.3 0.3 0.3 0.6"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.22 0.895",
                            AnchorMax = "0.78 0.945"
                        }
                    }
            };
            elements.Add(textElement);
            CuiHelper.AddUi(player, elements);
        }

        #endregion

        #region Timer GUIs



        private static List<Timer> screent = new List<Timer>();

        public static void ShowGameTimer(BasePlayer player,
           float seconds,
           string message = "remaining: ")
        {
            int repeats = (int)Math.Round(seconds, 0);
            int countdown = (int)Math.Round(seconds, 0);
            CreateGameTimer(player,
            countdown,
            message);
            screent.Add(iemUI.timer.Repeat(1f, repeats, () =>
            {
                countdown -= 1;
                CreateGameTimer(player, countdown, message);

            }));
        }

        public static void CreateGameTimer(BasePlayer player,
            int seconds,
            string message = "remaining: ")
        {

            int minutes = seconds / 60;
            int secs = seconds % 60;

            string ARENA = $"{message}";
            string gui = "CreateGameTimer";
            guiList.Add(gui);

            CuiHelper.DestroyUi(player, gui);
            var elements = new CuiElementContainer();
            CuiElement textElement = new CuiElement
            {
                Name = gui,
                Parent = "Hud.Under",
                //FadeOut = 10,
                Components =
                    {
                new CuiTextComponent
                {
                    Text = $"<color=#cc0000>{message}</color>\n<color=white>{minutes.ToString("00")}:{secs.ToString("00")}</color>",
                    FontSize = 22,
                    Align = TextAnchor.UpperLeft,
                    //FadeIn = 5
                },
                        new CuiOutlineComponent
                        {
                            Distance = "1.0 1.0",
                            Color = "0.3 0.3 0.3 0.6"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.01 0.90",
                            AnchorMax = "0.22 0.99"
                        }
                    }
            };
            elements.Add(textElement);
            CuiHelper.AddUi(player, elements);

        }

        #endregion

        #region message dialog


        public static bool Confirm(BasePlayer player, string message)
        {

            string ARENA = $"{message}";
            IemUtils.DLog(message);
            string gui = "ConfirmMessage";
            guiList.Add(gui);

            CuiHelper.DestroyUi(player, gui);

            var elements = new CuiElementContainer();
            CuiElement textElement = new CuiElement
            {
                Name = gui,
                Parent = "Overlay",
                //FadeOut = 10,
                Components =
                    {
                        new CuiTextComponent
                        {
                            Text = $"<color=#cc0000>{message}</color>",
                            FontSize = 22,
                            Align = TextAnchor.MiddleCenter,
                            //FadeIn = 5
                        },
                        new CuiOutlineComponent
                        {
                            Distance = "1.0 1.0",
                            Color = "1.0 1.0 1.0 1.0"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.22 0.445",
                            AnchorMax =  "0.78 0.595"
                        }
                    }
            };
            elements.Add(textElement);
            CuiHelper.AddUi(player, elements);
            return true;
        }


        #endregion

        #region results UI


        private static string[,] teamSlotsResults = {
            { "0.0 0.55", "0.4 0.9" },
            { "0.6 0.55", "1.0 0.9" },
            { "0.0 0.1", "0.4 0.50" },
            { "0.6 0.1", "1.0 0.50" }
        };

        public static Timer ShowResultsUiFor(List<IemUtils.IIemPlayer> iemPlayers,
            IemUtils.IIemTeamGame teamData,
            int showForSeconds)
        {

            foreach (IemUtils.IIemTeamPlayer iemPlayer in iemPlayers)
            {
                ShowResultsUiFor(iemPlayer.AsBasePlayer(),
                        teamData);

                iemUI.timer.Once(10f, () =>
                {
                    CuiHelper.DestroyUi(iemPlayer.AsBasePlayer(), "game_results_overlay");
                });
            }
            return null;
        }

        public static void ShowResultsUiFor(BasePlayer player,
            IemUtils.IIemTeamGame teamData)
        {
            int teamSlot = 0;
            string gui = "game_results_overlay";
            guiList.Add(gui);
            CuiHelper.DestroyUi(player, gui);

            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel
            {
                Image = {
                    Color = "0.6 0.3 0.3 1.0"
                },
                RectTransform = {
                    AnchorMin = "0.15 0.12",
                    AnchorMax = "0.85 0.9"
                },
                CursorEnabled = false
            }, "Overlay", gui);

            string winnerMessage = "";

            if (teamData.Winner() != null)
            {
                winnerMessage += "Team " + teamData.Winner().Name + "won!";
            }

            elements.Add(new CuiLabel
            {
                Text = {
                    Text = winnerMessage,
                    FontSize = 28,
                    Align = TextAnchor.UpperCenter
                },
                RectTransform = {
                    AnchorMin = "0.2 0.8",
                    AnchorMax = "0.8 0.95"
                }
            }, mainName);

            foreach (var team in teamData.Teams.Values)
            {
                //IemUtils.DLog(item.TeamName);
                CuiHelper.DestroyUi(player, team.Name);



                elements.Add(new CuiLabel
                {
                    Text = {
                    Text = team.Name,
                    FontSize = 28,
                    Align = TextAnchor.UpperCenter
                },
                    RectTransform = {
                    AnchorMin = teamSlotsResults[teamSlot, 0],
                    AnchorMax = teamSlotsResults[teamSlot, 1]
                }
                }, mainName);

                string playerListString = "\n\n";
                foreach (var teamPlayer in team.Players.Values)
                {
                    playerListString = playerListString + teamPlayer.Name + ":" + teamPlayer.Score + "\n";
                }

                elements.Add(new CuiLabel
                {
                    Text = {
                    Text =  $"{playerListString}",
                    FontSize = 18,
                    Align = TextAnchor.UpperCenter
                },
                    RectTransform = {
                    AnchorMin = teamSlotsResults[teamSlot, 0],
                    AnchorMax = teamSlotsResults[teamSlot, 1]
                }
                }, mainName);


                teamSlot++;
            }
            CuiHelper.AddUi(player, elements);

        }

        #endregion

        #region team lobby UI



        class Hud
        {
            static public CuiElementContainer CreateElementContainer(
                string panelName,
                string color,
                string aMin,
                string aMax,
                bool useCursor)
            {
                var NewElement = new CuiElementContainer() { {
                        new CuiPanel {
                            Image = { Color = color },
                            RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                            CursorEnabled = useCursor
                        },
                        new CuiElement ().Parent,
                        panelName
                    }
                };
                return NewElement;
            }

            public static void CreatePanel(
                ref CuiElementContainer container,
                string panel,
                string color,
                string text,
                int size,
                string aMin,
                string aMax,
                TextAnchor align = TextAnchor.UpperCenter)
            {
                container.Add(new CuiPanel
                {
                    Image = {
                        Color = "0.3 0.3 0.3 0.5"
                    },
                    RectTransform = {
                        AnchorMin = aMin,
                        AnchorMax = aMax
                    }
                },
                    panel);

            }

            public static void CreateLabel(
                ref CuiElementContainer container,
                string panel,
                string color,
                string text,
                int size,
                string aMin,
                string aMax,
                TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                    panel);

            }


        }



        public static
            void RemoveTeamUiForPlayer(BasePlayer player, IemUtils.IIemTeamGame teamGame)
        {

            string gui = "team_overlay_" + teamGame.GetGuid().ToString();
            CuiHelper.DestroyUi(player, gui);
            guiList.Remove(gui);
        }


        public static
            void ShowTeamUiForPlayer(BasePlayer player, IemUtils.IIemTeamGame teamGame)
        {

            int teamSlot = 0;

            string gui = "team_overlay_"+ teamGame.GetGuid().ToString();
            guiList.Add(gui);
            CuiHelper.DestroyUi(player, gui);

            var container = Hud.CreateElementContainer(
                                         gui,
                                         "0.3 0.3 0.3 0.0",
                                         "0.0 0.12",
                                         "1.0 1.0",
                                         false);

                   string[,] teamSlots =            {
                { "0.0 0.55", "0.15 0.9" },
                { "0.85 0.55", "1.0 0.9" },
                { "0.0 0.1", "0.15 0.50" },
                { "0.85 0.1", "1.0 0.50" }
            };

            foreach (IemUtils.IIemTeam team in teamGame.Teams.Values)
            {
                IemUtils.DLog(team.Name);
                string team_gui = "team_";
                //CuiHelper.DestroyUi(player, item.GetGuid().To);

                //IemUtils.DLog("teamSlots[teamSlot,0] = " + teamSlots[teamSlot, 0]);
                //IemUtils.DLog("teamSlots[teamSlot,1] = " + teamSlots[teamSlot, 1]);



                Hud.CreatePanel(ref container,
                    gui,
                    "1.0 1.0 1.0 0.6",
                    team.Name,
                    14,
                    teamSlots[teamSlot, 0],
                    teamSlots[teamSlot, 1],
                    TextAnchor.UpperCenter);

                Hud.CreateLabel(ref container,
                    gui,
                    GetColor(team.Color),
                    team.Name,
                    22,
                    teamSlots[teamSlot, 0],
                    teamSlots[teamSlot, 1],
                    TextAnchor.UpperCenter);





                string playerListString = "\n\n";
                foreach (var playerName in team.Players.Values)
                {
                    IemUtils.DLog("adding player: "+playerName.Name);
                    playerListString = playerListString + playerName.Name + "\n";
                }

                Hud.CreateLabel(ref container, gui, "",
                    $"{playerListString}",
                    18,
                    teamSlots[teamSlot, 0],
                    teamSlots[teamSlot, 1],
                    TextAnchor.UpperCenter);

                teamSlot++;
            }
            CuiHelper.AddUi(player, container);

        }


        #endregion

        public static string GetColor(string color)
        {
            switch (color)
            {
                case "white":
                    return "1.0 1.0 1.0 0.6";

                case "blue":
                    return "0.1 0.1 1.0 1";

                case "red":
                    return "1.0 0.0 0.0 1";

                case "green":
                    return "0.1 1.0 0.1 1";

                case "yellow":
                    return "1.0 1.0 0.0 1";

            }
            return "1.0 1.0 1.0 0.6";
        }
    }
}
