//Requires: IemUtils
using UnityEngine;
using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using System;

namespace Oxide.Plugins
{
    /// <summary>
    /// This is all dreadful rubbish.</summary>
    /// <remarks>
    /// </remarks>
    [Info("Incursion UI Library", "Tolland", "0.1.0")]
    public class IncursionUI : RustPlugin
    {



        [PluginReference]
        IemUtils IemUtils;

        static IncursionUI incursionUI = null;

        private static bool Debug = false;

        private static List<string> guiList = new List<string>();

        #region Boiler plate

        void Init()
        {
            incursionUI = this;
            IemUtils.LogL("IncursionUI: init complete");
        }

        void Loaded()
        {
            IemUtils.LogL("IncursionUI: Loaded complete");
        }

        void Unload()
        {
            IemUtils.LogL("IncursionUI: Unload complete");

        }

        void OnServerInitialized()
        {
            //if the server restarted during a banner, these need to be removed
            IncursionUI.RemoveUIs();
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

        public static void ShowCountDownTimerGui(BasePlayer player, string time)
        {
            string gui = "TimerGui";
            guiList.Add(gui);
            CuiHelper.DestroyUi(player, gui);
            var elements = new CuiElementContainer();

            var mainName = elements.Add(new CuiPanel
            {
                Image = {
                    Color = "0.4 0.4 0.4 0.5"
                },
                RectTransform = {
                    AnchorMin = "0 0.925",
                    AnchorMax = "0.1 1"
                },
                CursorEnabled = true
            }, "Hud", gui);

            elements.Add(new CuiLabel
            {
                Text = {
                    Text = time,
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform = {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, mainName);
            CuiHelper.AddUi(player, elements);
        }

        public static void ShowGameBanner(BasePlayer player, List<string> message)
        {
            string gui = "GameBanner";
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
                    Text = string.Join ("\n \n", message.ToArray ()).ToString (),
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform = {
                    AnchorMin = "0 0.20",
                    AnchorMax = "1 0.9"
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

        public static void HideGameBanner(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "GameBanner");

        }

        public static void CreateBanner(string message)
        {
            string ARENA = $"{message}";
            string gui = "bannerMessage";
            guiList.Add(gui);
            List<BasePlayer> activePlayers = BasePlayer.activePlayerList;

            foreach (BasePlayer player in activePlayers)
            {
                CuiHelper.DestroyUi(player, gui);
                var container = UI.CreateElementContainer(
                                    "bannerMessage",
                                    "0.3 0.3 0.3 0.6",
                                    "0.22 0.945",
                                    "0.78 0.995",
                                    false);
                UI.CreateLabel(ref container, gui, "",
                    $"{ARENA}", 18, "0 0", "1 1");
                CuiHelper.AddUi(player, container);
            }
        }

        public static void CreateGameBanner(BasePlayer player, string message)
        {
            string ARENA = $"{message}";
            string gui = "CreateGameBanner";
            guiList.Add(gui);

            CuiHelper.DestroyUi(player, "CreateGameBanner");
            var container = UI.CreateElementContainer(
                                "CreateGameBanner",
                                "0.3 0.3 0.3 0.6",
                                "0.22 0.945",
                                "0.78 0.995",
                                false);
            UI.CreateLabel(ref container, "CreateGameBanner", "",
                $"{ARENA}", 16, "0 0", "1 0.5");
            CuiHelper.AddUi(player, container);

        }

        public static void CreateEventBanner(BasePlayer player, string message)
        {
            string ARENA = $"{message}";
            string gui = "CreateEventBanner";
            guiList.Add(gui);

            CuiHelper.DestroyUi(player, "CreateEventBanner");
            var container = UI.CreateElementContainer(
                                "CreateEventBanner",
                                "0.3 0.3 0.3 0.6",
                                "0.22 0.945",
                                "0.78 0.995",
                                false);
            UI.CreateLabel(ref container, "CreateEventBanner", "",
                $"{ARENA}", 16, "0 0.5", "1 1");
            CuiHelper.AddUi(player, container);

        }


        public static void CreateEventStateManagerDebugBanner(string message)
        {
            string ARENA = $"{message}";
            string gui = "adminbannerMessage";
            guiList.Add(gui);
            List<BasePlayer> activePlayers = BasePlayer.activePlayerList;

            foreach (BasePlayer player in activePlayers)
            {
                CuiHelper.DestroyUi(player, "adminbannerMessage");
                var container = UI.CreateElementContainer(
                                    "adminbannerMessage",
                                    "0.3 0.3 0.3 0.6",
                                    "0.22 0.920",
                                    "0.78 0.945",
                                    false);
                UI.CreateLabel(ref container, "adminbannerMessage", "",
                    $"{ARENA}", 14, "0 0", "1 1");
                CuiHelper.AddUi(player, container);
            }
        }

        public static void CreatePlayerStateManagerDebugBanner(BasePlayer player, string message)
        {
            string ARENA = $"{message}";

            string gui = "adminbannerMessage2";
            guiList.Add(gui);

            if (player == null)
                IemUtils.SLog("player is null");

            CuiHelper.DestroyUi(player, "adminbannerMessage2");
            var container = UI.CreateElementContainer(
                                "adminbannerMessage2",
                                "0.3 0.3 0.3 0.6",
                                "0.22 0.895",
                                "0.78 0.920",
                                false);
            UI.CreateLabel(ref container, "adminbannerMessage2", "",
                $"{ARENA}", 14, "0 0", "1 1");
            CuiHelper.AddUi(player, container);

        }

        public static void CreateGameStateManagerDebugBanner(string message)
        {
            string ARENA = $"{message}";

            string gui = "adminbannerMessage3";
            guiList.Add(gui);
            List<BasePlayer> activePlayers = BasePlayer.activePlayerList;

            foreach (BasePlayer player in activePlayers)
            {
                CuiHelper.DestroyUi(player, "adminbannerMessage3");
                var container = UI.CreateElementContainer(
                                    "adminbannerMessage3",
                                    "0.3 0.3 0.3 0.6",
                                    "0.22 0.870",
                                    "0.78 0.895",
                                    false);
                UI.CreateLabel(ref container, "adminbannerMessage3", "",
                    $"{ARENA}", 14, "0 0", "1 1");
                CuiHelper.AddUi(player, container);
            }
        }
        public static void CreateSchedulerStateManagerDebugBanner(string message)
        {
            string ARENA = $"{message}";

            string gui = "scheduleradminbannerMessage";
            guiList.Add(gui);
            List<BasePlayer> activePlayers = BasePlayer.activePlayerList;

            foreach (BasePlayer player in activePlayers)
            {
                CuiHelper.DestroyUi(player, "scheduleradminbannerMessage");
                var container = UI.CreateElementContainer(
                                    "scheduleradminbannerMessage",
                                    "0.3 0.3 0.3 0.6",
                                    "0.22 0.845",
                                    "0.78 0.87",
                                    false);
                UI.CreateLabel(ref container, "scheduleradminbannerMessage", "",
                    $"{ARENA}", 14, "0 0", "1 1");
                CuiHelper.AddUi(player, container);
            }
        }

        public static void CreateEventMessage(string message)
        {
            string ARENA = $"msg: {message}";

            string gui = "eventmessage";
            guiList.Add(gui);
            List<BasePlayer> activePlayers = BasePlayer.activePlayerList;

            foreach (BasePlayer player in activePlayers)
            {
                CuiHelper.DestroyUi(player, "eventmessage");
                var container = UI.CreateElementContainer(
                                    "eventmessage",
                                    "0.3 0.3 0.3 0.6",
                                    "0.22 0.945",
                                    "0.78 0.995",
                                    false);
                UI.CreateLabel(ref container, "eventmessage", "",
                    $"{ARENA}", 18, "0 0", "1 1");
                CuiHelper.AddUi(player, container);
            }
        }


        public static void ShowTeamUi(List<UiTeam> teamData)
        {
            UpdateTeamUi(teamData);
        }

        public static void RemoveTeamUI()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {

                //IemUtils.DLog("removing " + "team_overlay");
                CuiHelper.DestroyUi(player, "team_overlay");

            }
        }

        public static void RemoveTeamUIForPlayer(BasePlayer player)
        {

            // IemUtils.DLog("removing " + "team_overlay");
            CuiHelper.DestroyUi(player, "team_overlay");


        }


        public static void RemoveGameResultUI()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "game_results_overlay");
            }
        }


        private static string[,] teamSlotsResults = {
            { "0.0 0.55", "0.4 0.9" },
            { "0.6 0.55", "1.0 0.9" },
            { "0.0 0.1", "0.4 0.50" },
            { "0.6 0.1", "1.0 0.50" }
        };

        public static void ShowResultsUi(BasePlayer player, List<UiTeamResult> teamData)
        {


            int teamSlot = 0;
            string gui = "game_results_overlay";
            guiList.Add(gui);
            CuiHelper.DestroyUi(player, "game_results_overlay");

            var container = UI.CreateElementContainer(
                                         "game_results_overlay",
                                         "0.6 0.3 0.3 1.0",
                                         "0.15 0.12",
                                         "0.85 0.9",
                                         false);

            foreach (UiTeamResult item in teamData)
            {
                //IemUtils.DLog(item.TeamName);
                CuiHelper.DestroyUi(player, item.TeamName);

                //IemUtils.DLog("teamSlots[teamSlot,0] = " + teamSlots[teamSlot, 0]);
                //IemUtils.DLog("teamSlots[teamSlot,1] = " + teamSlots[teamSlot, 1]);


                UI.CreateLabel(ref container,
                    "game_results_overlay",
                    GetColor(item.Color),
                    item.TeamName,
                    28,
                    teamSlotsResults[teamSlot, 0],
                    teamSlotsResults[teamSlot, 1],
                    TextAnchor.UpperCenter);



                string playerListString = "\n\n";
                foreach (string playerName in item.Players)
                {
                    playerListString = playerListString + playerName + "\n";
                }

                UI.CreateLabel(ref container, "game_results_overlay", "",
                    $"{playerListString}",
                    18,
                    teamSlotsResults[teamSlot, 0],
                    teamSlotsResults[teamSlot, 1],
                    TextAnchor.UpperCenter);

                teamSlot++;
            }
            CuiHelper.AddUi(player, container);

        }

        private static string[,] teamSlots =            {
                { "0.0 0.55", "0.15 0.9" },
                { "0.85 0.55", "1.0 0.9" },
                { "0.0 0.1", "0.15 0.50" },
                { "0.85 0.1", "1.0 0.50" }
            };



        public class UiTeam
        {
            public string TeamName { get; set; }

            public string Color { get; set; }

            public List<string> Players;

            public string JoinCommand { get; set; }

            public bool TeamOpen { get; set; }


            public UiTeam(string teamName, List<string> players, string color)
            {
                TeamName = teamName;
                Players = players;
                Color = color;
                TeamOpen = true;
                JoinCommand = "";
            }
        }

        public class UiTeamResult
        {
            public string TeamName { get; set; }

            public string Color { get; set; }

            public List<string> Players;

            public UiTeamResult(string teamName, List<string> players, string color)
            {
                TeamName = teamName;
                Players = players;
                Color = color;

            }
        }

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

        public static
            void ShowTeamUiForPlayer(BasePlayer player, List<UiTeam> teamData)
        {

            int teamSlot = 0;

            string gui = "team_overlay";
            guiList.Add(gui);
            CuiHelper.DestroyUi(player, "team_overlay");

            var container = Hud.CreateElementContainer(
                                         "team_overlay",
                                         "0.3 0.3 0.3 0.0",
                                         "0.0 0.12",
                                         "1.0 1.0",
                                         false);

            foreach (UiTeam item in teamData)
            {
                //IemUtils.DLog(item.TeamName);
                CuiHelper.DestroyUi(player, item.TeamName);

                //IemUtils.DLog("teamSlots[teamSlot,0] = " + teamSlots[teamSlot, 0]);
                //IemUtils.DLog("teamSlots[teamSlot,1] = " + teamSlots[teamSlot, 1]);



                Hud.CreatePanel(ref container,
                    "team_overlay",
                    "1.0 1.0 1.0 0.6",
                    item.TeamName,
                    14,
                    teamSlots[teamSlot, 0],
                    teamSlots[teamSlot, 1],
                    TextAnchor.UpperCenter);

                Hud.CreateLabel(ref container,
                    "team_overlay",
                    GetColor(item.Color),
                    item.TeamName,
                    22,
                    teamSlots[teamSlot, 0],
                    teamSlots[teamSlot, 1],
                    TextAnchor.UpperCenter);





                string playerListString = "\n\n";
                foreach (string playerName in item.Players)
                {
                    playerListString = playerListString + playerName + "\n";
                }

                Hud.CreateLabel(ref container, "team_overlay", "",
                    $"{playerListString}",
                    18,
                    teamSlots[teamSlot, 0],
                    teamSlots[teamSlot, 1],
                    TextAnchor.UpperCenter);

                teamSlot++;
            }
            CuiHelper.AddUi(player, container);

        }

        public static
            void UpdateTeamUi(List<UiTeam> teamData)
        {


            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                ShowTeamUiForPlayer(player, teamData);
            }
        }

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

        class UI
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

            static public void CreateLabel(
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


            static public void CreateButton(
                ref CuiElementContainer container,
                string panel,
                string color,
                string text,
                int size,
                string aMin,
                string aMax,
                TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                    panel);

            }


        }

        void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
        {
            if (victim == null)
                return;

            if (victim.ToPlayer() != null)
            {
                //if (victim.ToPlayer().IsWounded())
                //   info = TryGetLastWounded(victim.ToPlayer().userID, info);

                CleanUpGui(victim.ToPlayer());


            }

        }



        public static void DisplayEnterLobbyUI(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                incursionUI.timer.In(1, () => DisplayEnterLobbyUI(player));
            }
            else
            {
                string steamId = Convert.ToString(player.userID);
                //display on every connect
                if (true)
                {
                    string msg = "<color>some stuff here</color>";

                    IntroGUI(player, msg.ToString());
                }
            }
        }



        public static void DisplayTeamSelectUi(BasePlayer player, IemUtils.ScheduledEvent sevent)
        {


            CuiHelper.DestroyUi(player, "DisplayTeamSelectUi");
            CuiHelper.DestroyUi(player, "DisplayEnterLobbyUIHeader");
            CuiHelper.DestroyUi(player, "DisplayEnterLobbyUIOpenLobby");
            CuiHelper.DestroyUi(player, "DisplayEnterLobbyUIScheduled");

            DisplayEnterLobbyUIHeader(player);
            DisplayEnterLobbyUIOpenLobby(player);
            DisplayEnterLobbyUIScheduled(player, sevent);
        }


        public static void DisplayEnterLobbyUIHeader(BasePlayer player)
        {
            var elements = new CuiElementContainer();

            var mainName = elements.Add(new CuiPanel
            {
                Image = {
                    Color = "0.1 0.1 0.1 1"
                },
                RectTransform = {
                    AnchorMin = "0 0.8",
                    AnchorMax = "1 1"
                },
                CursorEnabled = true
            }, "Overlay", "DisplayEnterLobbyUIHeader");
            elements.Add(new CuiLabel
            {
                Text = {
                    Text = "Welcome to Incursion!",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform = {
                    AnchorMin = "0 0.20",
                    AnchorMax = "1 0.9"
                }
            }, mainName);

            elements.Add(new CuiButton
            {
                Button = {
                        //Close = "DisplayEnterLobbyUIHeader",
                        Command = "global.ui removehud",
                        Color = "255 0 0 1"
                    },
                RectTransform = {
                        AnchorMin = "0.95 0.80",
                        AnchorMax = "1.0 1.0"
                    },
                Text = {
                        Text = "X",
                        FontSize = 22,
                        Align = TextAnchor.MiddleCenter
                    }
            }, mainName);


            CuiHelper.AddUi(player, elements);
        }


        public static void DisplayEnterLobbyUIOpenLobby(BasePlayer player)
        {
            var elements = new CuiElementContainer();

            var mainName = elements.Add(new CuiPanel
            {
                Image = {
                    Color = "0.1 0.1 0.1 1"
                },
                RectTransform = {
                    AnchorMin = "0 0.4",
                    AnchorMax = "1 0.8"
                },
                CursorEnabled = true
            }, "Overlay", "DisplayEnterLobbyUIOpenLobby");
            elements.Add(new CuiLabel
            {
                Text = {
                    Text = "Event Lobby Open Messages",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform = {
                    AnchorMin = "0 0.20",
                    AnchorMax = "1 0.9"
                }
            }, mainName);
            var BlueTeam = new CuiButton
            {
                Button = {
                    Command = "global.event joinblueteam",
                    Close = mainName,
                    Color = "0 0 255 1"
                },
                RectTransform = {
                    AnchorMin = "0.5 0.26",
                    AnchorMax = "0.75 0.3"
                },
                Text = {
                    Text = "Blue Team",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                }
            };
            var RedTeam = new CuiButton
            {
                Button = {
                    Command = "global.event joinredteam",
                    Close = mainName,
                    Color = "255 0 0 1"
                },
                RectTransform = {
                    AnchorMin = "0.5 0.3",
                    AnchorMax = "0.75 0.34"
                },
                Text = {
                    Text = "Red Team",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                }
            };
            var RandomTeam = new CuiButton
            {
                Button = {
                    Command = "global.event joinrandomteam",
                    Close = mainName,
                    Color = "0.5 0.5 0.5 1"
                },
                RectTransform = {
                    AnchorMin = "0.5 0.16",
                    AnchorMax = "0.75 0.2"
                },
                Text = {
                    Text = "Join Random Team",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                }
            };
            elements.Add(RedTeam, mainName);
            elements.Add(BlueTeam, mainName);
            elements.Add(RandomTeam, mainName);
            CuiHelper.AddUi(player, elements);
        }

        public class ScheduledEventUiObject
        {
            List<UiTeam> teams = new List<UiTeam>();

            private DateTime startTime { get; set; }

            private int Length { get; set; }

            public ScheduledEventUiObject()
            {

            }

        }

        public static void DisplayEnterLobbyUIScheduled(BasePlayer player, IemUtils.ScheduledEvent sevent)
        {

            var elements = new CuiElementContainer();

            var mainName = elements.Add(new CuiPanel
            {
                Image = {
                    Color = "0.1 0.1 0.1 1"
                },
                RectTransform = {
                    AnchorMin = "0 0.0",
                    AnchorMax = "1 0.4"
                },
                CursorEnabled = true
            }, "Overlay", "DisplayEnterLobbyUIScheduled");


            if (sevent == null)
            {
                elements.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = "There is no scheduled event",
                        FontSize = 22,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0.80",
                        AnchorMax = "1 1.0"
                    }
                }, mainName);

            }
            else
            {

                elements.Add(new CuiLabel
                {
                    Text = {
                    Text = "Next Scheduled event!",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                },
                    RectTransform = {
                    AnchorMin = "0 0.80",
                    AnchorMax = "1 1.0"
                }
                }, mainName);

                string[,] teamSlots =            {
                { "0.25 0.50", "0.5 0.75" },
                { "0.5 0.50", "0.75 0.75" },
                { "0.25 0.25", "0.5 0.5" },
                { "0.5 0.25", "0.75 0.5" }
            };

                int count = 0;


                foreach (var team in sevent.schTeams.Values)
                {
                    Plugins.IemUtils.DLog("team is " + team.TeamName);
                    elements.Add(new CuiButton
                    {
                        Button = {
                        Command = team.JoinCommand,
                        Color = GetColor(team.Color)
                    },
                        RectTransform = {
                        AnchorMin = teamSlots[count,0],
                        AnchorMax = teamSlots[count,1]
                    },
                        Text = {
                        Text = team.TeamName,
                        FontSize = 22,
                        Align = TextAnchor.MiddleCenter
                    }
                    }, mainName);

                    count++;
                }
            }







            CuiHelper.AddUi(player, elements);
        }

        public static void IntroGUI(BasePlayer player, string msg)
        {
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
            }, "Overlay", "IntroGUI");
            var Agree = new CuiButton
            {
                Button = {
                    Close = mainName,
                    Color = "0 255 0 1"
                },
                RectTransform = {
                    AnchorMin = "0.2 0.16",
                    AnchorMax = "0.35 0.2"
                },
                Text = {
                    Text = "I Agree",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                }
            };
            elements.Add(new CuiLabel
            {
                Text = {
                    Text = msg,
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform = {
                    AnchorMin = "0 0.20",
                    AnchorMax = "1 0.9"
                }
            }, mainName);
            elements.Add(Agree, mainName);
            CuiHelper.AddUi(player, elements);
        }

        #region console control

        //from em
        [ConsoleCommand("ui")]
        void ccmdEvent(ConsoleSystem.Arg arg)
        {
            //if (!IemUtils.hasAccess(arg)) return;
            switch (arg.Args[0].ToLower())
            {
                //if there is a EventGame availabe, autostart it
                case "showteamstest":
                    SendReply(arg, "showing teams UI");
                    ShowTeamUi(new List<UiTeam> {
                    new UiTeam ("Blue Team", new List<string> () { "Dave the Rave", "Phil the pill", "Jake the snake" }, "blue"),
                    new UiTeam (
                        "Red Team", new List<string> () { "Eric the cleric", "Ian the being", "Ivan the Liven" }, "red"
                    ),
                    new UiTeam (
                        "Green Team", new List<string> () { "Eric the cleric", "Ian the being", "Ivan the Liven" }, "green"
                    ),
                    new UiTeam (
                        "Yellow Team", new List<string> () {
                        "Eric the cleric",
                        "Ian the being",
                        "Ivan the Liven",
                        "Todd the mod"
                    }, "yellow"
                    )
                });
                    return;

                case "removeteamsui":
                    SendReply(arg, "removing teams UI");
                    RemoveTeamUI();
                    return;

                case "ab1":
                    SendReply(arg, "admin banner 1");
                    CreateEventStateManagerDebugBanner("test1");
                    return;

                case "ab2":
                    SendReply(arg, "2");
                    CreatePlayerStateManagerDebugBanner(arg.Player(), "test2");
                    return;

                case "ab3":
                    SendReply(arg, "3");
                    CreateGameStateManagerDebugBanner("test3");
                    return;


                case "timer":
                    SendReply(arg, "3");
                    ShowCountDownTimerGui(arg.Player(), "3 minutes");
                    return;
                case "removehud":
                    SendReply(arg, "3");

                    CuiHelper.DestroyUi(arg.Player(), "DisplayTeamSelectUi");
                    CuiHelper.DestroyUi(arg.Player(), "DisplayEnterLobbyUIHeader");
                    CuiHelper.DestroyUi(arg.Player(), "DisplayEnterLobbyUIOpenLobby");
                    CuiHelper.DestroyUi(arg.Player(), "DisplayEnterLobbyUIScheduled");
                    return;



            }
        }

        #endregion


    }
}
