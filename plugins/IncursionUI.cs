//Requires: IemUtils
using UnityEngine;
using System.Collections.Generic;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{

    [Info("Incursion UI", "Tolland", "0.1.0")]
    public class IncursionUI : RustPlugin
    {

        [PluginReference]
        IemUtils IemUtils;

        static IncursionUI incursionUI = null;

        private static bool Debug = false;

        void Init()
        {
            incursionUI = this;
        }

        void OnServerInitialized()
        {
            //if the server restarted during a banner, these need to be removed
            IncursionUI.RemoveUIs();
        }

        public static void RemoveUIs()
        {
            List<BasePlayer> activePlayers = BasePlayer.activePlayerList;

            foreach (BasePlayer player in activePlayers)
            {
                CuiHelper.DestroyUi(player, "GameBanner");
                CuiHelper.DestroyUi(player, "adminbannerMessage");
                CuiHelper.DestroyUi(player, "jailTimer");
                CuiHelper.DestroyUi(player, "bannerMessage");
                CuiHelper.DestroyUi(player, "eventmessage");
                CuiHelper.DestroyUi(player, "team_overlay");
                CuiHelper.DestroyUi(player, "game_results_overlay");
            }
        }



        public static void ShowGameBanner(BasePlayer player, List<string> message)
        {
            var elements = new CuiElementContainer();

            var mainName = elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0.1 0.1 0.1 1"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                },
                CursorEnabled = true
            }, "Overlay", "GameBanner");

            elements.Add(new CuiLabel
            {
                Text =
                {
                    Text = string.Join("\n \n",message.ToArray()).ToString(),
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0 0.20",
                    AnchorMax = "1 0.9"
                }
            }, mainName);


            elements.Add(new CuiButton
            {
                Button =
                {
                    Close = mainName,
                    Color = "0 255 0 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.4 0.16",
                    AnchorMax = "0.6 0.2"
                },
                Text =
                {
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

            List<BasePlayer> activePlayers = BasePlayer.activePlayerList;

            foreach (BasePlayer player in activePlayers)
            {
                CuiHelper.DestroyUi(player, "bannerMessage");
                var container = UI.CreateElementContainer(
                    "bannerMessage",
                    "0.3 0.3 0.3 0.6",
                    "0.22 0.945",
                    "0.78 0.995",
                    false);
                UI.CreateLabel(ref container, "bannerMessage", "",
                    $"{ARENA}", 18, "0 0", "1 1");
                CuiHelper.AddUi(player, container);
            }
        }
        public static void CreateAdminBanner(string message)
        {
            string ARENA = $"{message}";

            List<BasePlayer> activePlayers = BasePlayer.activePlayerList;

            foreach (BasePlayer player in activePlayers)
            {
                CuiHelper.DestroyUi(player, "adminbannerMessage");
                var container = UI.CreateElementContainer(
                    "adminbannerMessage",
                    "0.3 0.3 0.3 0.6",
                    "0.22 0.895",
                    "0.78 0.945",
                    false);
                UI.CreateLabel(ref container, "adminbannerMessage", "",
                    $"{ARENA}", 18, "0 0", "1 1");
                CuiHelper.AddUi(player, container);
            }
        }
        public static void CreateEventMessage(string message)
        {
            string ARENA = $"msg: {message}";

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

                IemUtils.DLog("removing " + "team_overlay");
                CuiHelper.DestroyUi(player, "team_overlay");

            }
        }

        public static void RemoveTeamUIForPlayer(BasePlayer player)
        {

            IemUtils.DLog("removing " + "team_overlay");
            CuiHelper.DestroyUi(player, "team_overlay");


        }


        public static void RemoveGameResultUI()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "game_results_overlay");
            }
        }


        private static string[,] teamSlotsResults =
        {
            {"0.0 0.55","0.4 0.9" },
           {"0.6 0.55","1.0 0.9" },
            {"0.0 0.1","0.4 0.50" },
           {"0.6 0.1","1.0 0.50" }
        };
        public static void ShowResultsUi(List<UiTeamResult> teamData)
        {


            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                int teamSlot = 0;

                CuiHelper.DestroyUi(player, "game_results_overlay");

                var container = UI.CreateElementContainer(
                    "game_results_overlay",
                    "0.6 0.3 0.3 1.0",
                    "0.15 0.12",
                    "0.85 0.9",
                    false);

                foreach (UiTeamResult item in teamData)
                {
                    IemUtils.DLog(item.TeamName);
                    CuiHelper.DestroyUi(player, item.TeamName);

                    IemUtils.DLog("teamSlots[teamSlot,0] = " + teamSlots[teamSlot, 0]);
                    IemUtils.DLog("teamSlots[teamSlot,1] = " + teamSlots[teamSlot, 1]);


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

                //UI.CreateButton(ref container, "game_results_overlay", "",
                //   "I agree",
                //    22,
                //    "0.4 0.16",
                //    "0.6 0.2",
                //    TextAnchor.LowerCenter);

            //elements.Add(new CuiButton
            //{
            //    Button =
            //    {
            //        Close = mainName,
            //        Color = "0 255 0 1"
            //    },
            //    RectTransform =
            //    {
            //        AnchorMin = "0.4 0.16",
            //        AnchorMax = "0.6 0.2"
            //    },
            //    Text =
            //    {
            //        Text = "I Agree",
            //        FontSize = 22,
            //        Align = TextAnchor.MiddleCenter
            //    }
            //}, mainName); ;

                CuiHelper.AddUi(player, container);
            }
        }

        private static string[,] teamSlots =
{
            {"0.0 0.55","0.15 0.9" },
           {"0.85 0.55","1.0 0.9" },
            {"0.0 0.1","0.15 0.50" },
           {"0.85 0.1","1.0 0.50" }
        };



        public class UiTeam
        {
            public string TeamName { get; set; }
            public string Color { get; set; }
            public List<string> Players;

            public UiTeam(string teamName, List<string> players, string color)
            {
                TeamName = teamName;
                Players = players;
                Color = color;

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

            CuiHelper.DestroyUi(player, "team_overlay");

            var container = Hud.CreateElementContainer(
                "team_overlay",
                "0.3 0.3 0.3 0.0",
                "0.0 0.12",
                "1.0 1.0",
                false);

            foreach (UiTeam item in teamData)
            {
                IemUtils.DLog(item.TeamName);
                CuiHelper.DestroyUi(player, item.TeamName);

                IemUtils.DLog("teamSlots[teamSlot,0] = " + teamSlots[teamSlot, 0]);
                IemUtils.DLog("teamSlots[teamSlot,1] = " + teamSlots[teamSlot, 1]);



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
                    Image =
                    {
                        Color = "0.3 0.3 0.3 0.5"
                    },
                    RectTransform =
                    {
                        AnchorMin = aMin,
                        AnchorMax = aMax
                    }
                },
                    panel);

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


        #region console control

        //from em
        [ConsoleCommand("ui")]
        void ccmdEvent(ConsoleSystem.Arg arg)
        {
            if (!IemUtils.hasAccess(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "event open - Open a event");
                SendReply(arg, "event cancel - Cancel a event");
                return;
            }
            switch (arg.Args[0].ToLower())
            {
                //if there is a EventGame availabe, autostart it
                case "showteamstest":
                    SendReply(arg, "showing teams UI");
                    ShowTeamUi(new List<UiTeam>{
                        new UiTeam( "Blue Team", new List<string>() {"Dave the Rave","Phil the pill","Jake the snake"},"blue"),
                    new UiTeam(
                        "Red Team", new List<string>() {"Eric the cleric","Ian the being","Ivan the Liven"},"red"
                    ),
                    new UiTeam(
                        "Green Team", new List<string>() {"Eric the cleric","Ian the being","Ivan the Liven"},"green"
                    ),
                    new UiTeam(
                        "Yellow Team", new List<string>() {"Eric the cleric","Ian the being","Ivan the Liven","Todd the mod"},"yellow"
                    ) });
                    return;

                case "removeteamsui":
                    SendReply(arg, "removing teams UI");
                    RemoveTeamUI();
                    return;



            }
        }
        #endregion
    }
}
