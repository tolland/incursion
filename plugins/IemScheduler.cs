// Reference: Oxide.Ext.MySql
//Requires: IncursionUI
//Requiers: IemUtils
//Requires: IncursionEvents

using Facepunch;
using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Database;
using Oxide.Core.Plugins;
using UnityEngine;
using Physics = UnityEngine.Physics;
using Random = System.Random;

namespace Oxide.Plugins
{

    [Info("Incursion Scheduler", "Tolland", "0.1.0")]
    public class IemScheduler : RustPlugin
    {

        #region plugin includes

        //[PluginReference]
        //IncursionEventGame IncursionEventGame;

        [PluginReference]
        IncursionEvents IncursionEvents;

        [PluginReference]
        IncursionUI IncursionUI;

        [PluginReference]
        IemUtils IemUtils;

        #endregion

        #region var inits

        static IemScheduler iemScheduler;

        DynamicConfigFile incursionEventsConfig;

        public SchedulerStateManager ssm;

        //private List<DateTime> gameTimes = new List<DateTime>();

        #endregion

        #region mysql init stuffs

        private readonly Ext.MySql.Libraries.MySql _mySql
            = Interface.GetMod().GetLibrary<Ext.MySql.Libraries.MySql>();

        private Connection _mySqlConnection;

        private const string InsertData
            = "INSERT INTO scheduled_event (`start`,`length`,`event_name`) VALUES (@0,@1,@2);";

        private const string SelectEvents = "SELECT guid, start, length, event_name FROM scheduled_event "
                                            + "WHERE start > NOW();";

        private const string SelectTeams = "SELECT g.guid e_guid, g.start, length, "
                                           + "event_name, t.guid t_guid, team_name, color, team_id "
                                           + " FROM scheduled_event_team t, scheduled_event g "
                                           + "WHERE g.start > NOW() AND t.e_guid = g.guid;";

        //@todo this doesn't support non teamed players
        private const string SelectPlayers =
            "SELECT g.guid e_guid, g.start, p.steam_id, p.guid p_guid, p.display_name, p.t_guid, t.team_name " +
            "FROM scheduled_event_player p, scheduled_event g, scheduled_event_team t " +
            "WHERE g.start > NOW() AND p.e_guid = g.guid AND p.t_guid = t.guid";

        private const string SelectPlayer =
            "SELECT g.guid e_guid, g.start, p.steam_id, p.guid p_guid, p.display_name, p.t_guid, t.team_name, g.event_name FROM scheduled_event_player p, scheduled_event g, scheduled_event_team t WHERE p.e_guid = g.guid AND p.t_guid = t.guid AND p.steam_id = @0 ORDER BY g.start ASC";

        private const string SelectPlayerByGuid =
            "SELECT g.guid e_guid, g.start, p.steam_id, p.guid p_guid, p.display_name, p.t_guid, t.team_name " +
            "FROM scheduled_event_player p, scheduled_event g, scheduled_event_team t " +
            "WHERE p.e_guid = g.guid AND p.t_guid = t.guid AND p.guid = @0";

        private const string InsertEvent =
            "REPLACE INTO scheduled_event_team (`guid`,`e_guid`,`team_name`,`color`,`team_id`) " +
            "VALUES (@0,@1,@2,@3,@4);";

        private const string InsertTeam =
            "REPLACE INTO scheduled_event_team (`guid`,`e_guid`,`team_name`,`color`,`team_id`) " +
            "VALUES (@0,@1,@2,@3,@4);";


        string InsertPlayer =
            "REPLACE INTO scheduled_event_player (`guid`,`steam_id`,`display_name`,`e_guid`,`t_guid`) " +
            "VALUES (@0,@1,@2,@3,@4);";

        private const string DropScheduledRecords = @"SET SQL_SAFE_UPDATES = 0;
                                                    DELETE FROM scheduled_event_player;
                                                    DELETE FROM scheduled_event_team;
                                                    DELETE FROM `rust`.`scheduled_event`;
                                                    SET SQL_SAFE_UPDATES = 1;";


        #endregion

        #region boiler plate

        void Init()
        {

            iemScheduler = this;
            IemUtils.LogL("IemScheduler: Init complete");
        }

        public IncursionEvents.EventStateManager esm;
        void Loaded()
        {
            // don't listen unless scheduling is enabled
            Unsubscribe(nameof(OnTick));
            Unsubscribe(nameof(OnEventComplete));
            Unsubscribe(nameof(OnEventCancelled));

            esm = IncursionEvents.esm;

            if (esm == null)
                throw new Exception("esm is null");

            _mySqlConnection = _mySql.OpenDb("localhost", 3306, "rust", "root", "1234", this);

            ssm = new SchedulerStateManager(SchedulerStateManager.SchedulerPluginLoaded.Instance);

            //tell the event manager about this game
            esm.RegisterScheduler(ssm);

            //Enabled = (bool)incursionEventsConfig["SchedulerEnabled"];
            IemUtils.LogL("IemScheduler: Loaded complete");

        }

        void Unload()
        {
            ssm.ChangeState(SchedulerStateManager.SchedulerPluginUnloaded.Instance);
            IemUtils.LogL("IemScheduler: UnLoad complete");
        }

        private void OnServerInitialized()
        {
            Puts("OnServerInitialized works!");

            LoadEventsFromMySQL();
        }


        private void ServerInitializedContinued()
        {

            ListScheduledEvents();

            GenerateAutoEvents("Default Team Game", 20, 0.05, 1);


            ListScheduledEvents();

            IemUtils.SchLog("done");


            //Config["EventManagementMode"] = "manual"; //for example
            Interface.Oxide.CallHook("OnEventManagementModeChanged");


            IemUtils.LogL("IemScheduler: OnServerInitialized complete");
        }

        #endregion

        #region retrieve data from mysql

        void LoadEventsFromMySQL()
        {

            _mySql.Query(Sql.Builder.Append(SelectEvents), _mySqlConnection, list =>
                    ParseSchEventResults(list));


        }


        void ParseSchEventResults(List<Dictionary<string, object>> events)
        {
            //foreach (Dictionary<string, object> result in events)
            //{
            //    DateTime time = DateTime.Now;
            //    //Puts("event start is " + result["start"]);
            //    //Puts("event name is " + result["event_name"]);
            //}

            foreach (Dictionary<string, object> result in events)
            {
                DateTime time = DateTime.Now;
                time = DateTime.Parse(result["start"].ToString());
                //iemScheduler.Puts("this start time " + time.ToString("yyyy-MM-dd H:mm:ss"));
                int newLength;
                int.TryParse(result["length"].ToString(), out newLength);
                IemUtils.ScheduledEvent event1 = new IemUtils.ScheduledEvent(
                    time,
                    newLength,
                    new Guid((string)result["guid"]),
                    (string)result["event_name"]
                );
                IemUtils.scheduledEvents.Add(event1.guid, event1);

            }
            _mySql.Query(Sql.Builder.Append(SelectTeams), _mySqlConnection, list =>
                    ParseSchTeamResults(list));


        }




        void ParseSchTeamResults(List<Dictionary<string, object>> teams)
        {
            //foreach (Dictionary<string, object> result in teams)
            //{
            //    DateTime time = DateTime.Now;
            //    Puts("team is " + result["team_name"]);
            //}

            foreach (Dictionary<string, object> result in teams)
            {
                DateTime time = DateTime.Now;
                time = DateTime.Parse(result["start"].ToString());


                parseResultToSchTeam(result);

            }



            _mySql.Query(Sql.Builder.Append(SelectPlayers), _mySqlConnection, list =>
                    ParseSchPlayerResults(list));
        }

        //private const string SelectTeams = "SELECT g.guid e_guid, g.start, length, "
        //                                   + "event_name, t.guid t_guid, team_name, color, team_id "
        //                                   + " FROM scheduled_event_team t, scheduled_event g "
        //                                   + "WHERE g.start > NOW() AND t.e_guid = g.guid;";

        IemUtils.ScheduledEvent.ScheduledEventTeam parseResultToSchTeam(Dictionary<string, object> sqlTeam)
        {
            Guid eventGuid = new Guid((string)sqlTeam["e_guid"]);
            IemUtils.ScheduledEvent SchEvent = IemUtils.scheduledEvents[eventGuid];



            IemUtils.ScheduledEvent.ScheduledEventTeam team = new IemUtils.ScheduledEvent.ScheduledEventTeam(
                (string)sqlTeam["team_name"],
                (string)sqlTeam["color"],
                "schedx joinblueteam",
                true,
                SchEvent,
                new Guid((string)sqlTeam["t_guid"])
            );
            return team;
        }


        void ParseSchPlayerResults(List<Dictionary<string, object>> players)
        {
            foreach (Dictionary<string, object> result in players)
            {
                DateTime time = DateTime.Now;
                Puts("ParseSchPlayerResults player is " + result["display_name"]);
            }

            foreach (Dictionary<string, object> result in players)
            {
                DateTime time = DateTime.Now;
                time = DateTime.Parse(result["start"].ToString());

                parseResultToSchPlayer(result);


            }

            ServerInitializedContinued();
        }

        //iemScheduler.scheduledGames[(string)result[(string)result["e_guid"]]].schTeams[].Add((string)result["guid"],);

        IemUtils.ScheduledEvent.ScheduledEventPlayer parseResultToSchPlayer(Dictionary<string, object> sqlPlayer)
        {
            Guid eventGuid = new Guid((string)sqlPlayer["e_guid"]);
            IemUtils.ScheduledEvent SchEvent = IemUtils.scheduledEvents[eventGuid];

            Guid teamGuid = new Guid((string)sqlPlayer["t_guid"]);
            IemUtils.ScheduledEvent.ScheduledEventTeam SchEventTeam =
                IemUtils.scheduledEvents[eventGuid].schTeams[teamGuid];

            IemUtils.ScheduledEvent.ScheduledEventPlayer player = new IemUtils.ScheduledEvent.ScheduledEventPlayer(
                (string)sqlPlayer["steam_id"],
                SchEventTeam,
                new Guid((string)sqlPlayer["p_guid"])
            );
            return player;
        }

        private void ListScheduledEvents()
        {
            if (IemUtils.scheduledEvents.Count == 0)
            {
                IemUtils.SchLog("no events");
            }
            foreach (KeyValuePair<Guid, IemUtils.ScheduledEvent> scheduledEvent in IemUtils.scheduledEvents)
            {
                if (scheduledEvent.Value.Start < DateTime.Now.Add(new TimeSpan(0, 0, 10, 0)))
                {
                    IemUtils.SchLog("event is " + scheduledEvent.Key);
                    IemUtils.SchLog("time is " + scheduledEvent.Value.Start);
                    foreach (var team in scheduledEvent.Value.schTeams)
                    {
                        IemUtils.SchLog("team key is " + team.Key);
                        IemUtils.SchLog("team name is " + team.Value.TeamName);
                        if (team.Value.schPlayers.Count == 0)
                        {
                            IemUtils.SchLog("player count is zero");
                        }
                        else
                        {
                            foreach (var player in team.Value.schPlayers)
                            {
                                IemUtils.SchLog("player key is " + player.Key);
                                IemUtils.SchLog("player steamId is " + player.Value.steamId);

                            }
                        }
                    }
                    IemUtils.SchLog("");
                }
            }
        }

        #endregion

        #region generate auto events
        /// <summary>
        /// generate a series of ScheduledEvents and persist them
        /// must be called after ParseDbEventsToScheduledEvent
        /// </summary>
        /// <param name="gameName"></param>
        /// <param name="minuteIncrements"></param>
        /// <param name="lookAheadDays"></param>
        static void GenerateAutoEvents(string gameName, int minuteIncrements, double lookAheadDays, int length)
        {
            List<DateTime> times = GenerateTimeList(minuteIncrements, lookAheadDays);

            bool inserted = false;
            foreach (DateTime time in times)
            {
                IemUtils.SchLog("");
                if (!CheckExistsGame(gameName, time))
                {

                    IemUtils.ScheduledEvent event1 = new IemUtils.ScheduledEvent(
                        time, minuteIncrements, gameName);

                    iemScheduler.IemUtils.scheduledEvents.Add(event1.guid, event1);

                    IemUtils.ScheduledEvent.ScheduledEventTeam team1 =
                        new IemUtils.ScheduledEvent.ScheduledEventTeam(
                        "Blue Team",
                        "blue",
                        "schedx joinblueteam",
                        true,
                        event1
                    );

                    IemUtils.ScheduledEvent.ScheduledEventTeam team2 =
                        new IemUtils.ScheduledEvent.ScheduledEventTeam(
                        "Red Team",
                        "red",
                        "schedx joinredteam",
                        true,
                        event1
                    );

                    var sql = Sql.Builder;
                    //sql.Append(InsertData, new object[]
                    //{
                    //    time.ToString("yyyy-MM-dd H:mm:ss"), 90.ToString(), "stubgame"
                    //});
                    string timestring = time.ToString("yyyy-MM-dd H:mm:ss");

                    sql.Append(
                        $"SET FOREIGN_KEY_CHECKS=0;" +

                        $"REPLACE INTO scheduled_event (`guid`,`start`,`length`,`event_name`) VALUES ('{event1.guid}','{timestring}','{minuteIncrements.ToString()}','{event1.EventName}');" +
                        "SET FOREIGN_KEY_CHECKS = 1;");

                    //   private const string InsertTeam =
                    //   "REPLACE INTO scheduled_event_team (`guid`,`e_guid`,`team_name`,`color`,`team_id`) " +
                    //   "VALUES (@0,@1,@2,@3,@4);";

                    sql.Append(InsertTeam, team1.guid, event1.guid, "Blue Bandits", "blue", team1.TeamId);
                    sql.Append(InsertTeam, team2.guid, event1.guid, "Red Devils", "red", team2.TeamId);

                    IemUtils.SchLog(sql.SQL);
                    iemScheduler._mySql.Insert(sql, iemScheduler._mySqlConnection);
                    inserted = true;
                }
            }

            // if (inserted)
            // {
            //     IemUtils.SchLog("creating game333");
            //     IemUtils.SchLog(sql.SQL);
            //     iemScheduler._mySql.Insert(sql, iemScheduler._mySqlConnection);
            // }

        }

        //this date check seems to be working on the local side
        //@todo check UTC dates?
        private static bool CheckExistsGame(string v, DateTime time)
        {
            foreach (IemUtils.ScheduledEvent iemSchedulerScheduledGame in iemScheduler.IemUtils.scheduledEvents.Values)
            {
                if (iemSchedulerScheduledGame.Start == time &&
                    iemSchedulerScheduledGame.EventName == v)
                {
                    return true;
                }
            }

            return false;
        }

        private static Timer autoevents;

        static void AutoEventTimerRegister()
        {
            autoevents = iemScheduler.timer.Repeat(30f, 0, () =>
            {
                IemUtils.TimerLog("running generate autoevents");
                GenerateAutoEvents(Plugins.IncursionEvents.esm.currentGameStateManager.Name,
                    10, 0.05, 10);
            });
        }


        static void AutoEventTimerRemove()
        {
            autoevents.Destroy();
        }



        static List<DateTime> GenerateTimeList(int minuteIncrements, double lookAheadDays)
        {
            List<DateTime> times = new List<DateTime>();

            //if the server is busy, don't want to schedule events too close to now
            //so have a start delay
            int paddingMinutes = 3;

            //find the time to create events up until
            DateTime date = DateTime.Now;
            TimeSpan time = new TimeSpan(0, (int)Math.Round(lookAheadDays * 24f), 0, 0);
            DateTime lookto = DateTime.Now.Add(time);
            IemUtils.SchLog(String.Format("looking to {0:yyyy-MM-dd H:mm:ss zzz}", lookto));

            //find the time to create the first event
            //@todo rewrite this...
            int startMinute = date.Minute + paddingMinutes;

            //for (int minute = date.Minute + paddingMinutes;
            //  minute % minuteIncrements != 0; minute++)
            while (true)
            {
                if (startMinute % minuteIncrements == 0)
                    break;

                startMinute++;
            }


            DateTime start = new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0);
            TimeSpan step2 = new TimeSpan(0, 0, startMinute, 0);
            DateTime combined2 = start.Add(step2);

            TimeSpan step3 = new TimeSpan(0, 0, minuteIncrements, 0);
            for (DateTime nextTime = combined2; nextTime < lookto; nextTime = nextTime.Add(step3))
            {
                //iemScheduler.Puts("{0:yyyy-MM-dd H:mm:ss zzz}", nextTime);
                times.Add(nextTime);
            }

            return times;


        }

        #endregion

        #region state management

        private Timer nextGameTimer;
        private Timer cooldownTimer;

        private bool cooldown = false;

        void OnEventManagementModeChanged()
        {
            switch ((string)IncursionEvents.Config["EventManagementMode"])
            {
                case "scheduled":
                    ssm.ChangeState(SchedulerStateManager.Scheduled.Instance);
                    break;
                case "repeating":
                    ssm.ChangeState(SchedulerStateManager.Repeating.Instance);
                    break;
                case "manual":
                    ssm.ChangeState(SchedulerStateManager.Manual.Instance);
                    break;
                case "once":
                    ssm.ChangeState(SchedulerStateManager.Once.Instance);
                    break;
            }
        }


        object OnInjectSchedulingInfos()
        {
            return GetNextEvent();
        }


        void OnEventComplete()
        {
            ssm.Update();
        }

        void OnEventCancelled()
        {
            ssm.Update();
        }

        void ForceRunNextScheduledEvent()
        {
            if ((nextGameTimer != null) && (!nextGameTimer.Destroyed))
            {
                //IemUtils.DDLog("destroying");
                nextGameTimer.Destroy();
            }
            cooldown = true;
            IemUtils.ScheduledEvent nevent = GetNextEvent();

            if (nevent != null)
            {
                IemUtils.DDLog("force running event scheduled for " + nevent.Start);
                nevent.Start = DateTime.Now;
                IemUtils.DDLog("start time set to " + nevent.Start);
                IemUtils.DDLog("time set to " + DateTime.Now);
                foreach (IemUtils.ScheduledEvent sevent in IemUtils.scheduledEvents.Values)
                {
                    IemUtils.DDLog("event:" + sevent.Start);
                }
                IncursionEvents.esm.StartScheduledGame(nevent);
                esm.CreateEventBanner("Scheduled game starts at: " + GetNextEvent().Start);
            }
            else
            {
                IemUtils.DDLog("nevent was null");
            }

        }

        [HookMethod("OnTick")]
        private void OnTick()
        {

            if (!cooldown)
            {
                cooldown = true;
                cooldownTimer = timer.Once(10f, () => cooldown = false);

                if (GetNextEvent() != null)
                {
                    esm.CreateEventBanner("Scheduled game starts at: " + GetNextEvent().Start +
                        " (" + GetNextEvent().EventName + ")");
                    IemUtils.DDLog("Scheduled game starts at: " + GetNextEvent().Start);

                    if (GetNextEvent() != null)
                    {
                        double countdown = GetNextEvent().Start.Subtract(DateTime.Now).TotalSeconds;

                        if ((nextGameTimer == null) || (nextGameTimer.Destroyed))
                            nextGameTimer = timer.Once((float)countdown, () =>
                           {
                               IemUtils.TimerLog("game starting");
                               if (IncursionEvents.esm == null)
                               {
                                   IemUtils.TimerLog("esm is null");
                               }
                               else
                               {
                                   IncursionEvents.esm.StartScheduledGame(GetNextEvent());
                               }
                           });



                        if ((nextGameTimer == null) || (nextGameTimer.Destroyed))
                            nextGameTimer = timer.Once((float)countdown, () =>
                            {
                                IemUtils.TimerLog("game starting");
                                if (IncursionEvents.esm == null)
                                {
                                    IemUtils.TimerLog("esm is null");
                                }
                                else
                                {
                                    IncursionEvents.esm.StartScheduledGame(GetNextEvent());
                                }
                            });

                    }
                }
            }
        }


        private IemUtils.ScheduledEvent cachedNextEvent = null;

        private IemUtils.ScheduledEvent GetNextEvent()
        {
            if (cachedNextEvent != null)
            {
                if (cachedNextEvent.Start > DateTime.Now)
                {
                    IemUtils.DDLog("returning cached event with time " + cachedNextEvent.Start);
                    return cachedNextEvent;
                }
                else
                {
                    IemUtils.DDLog("cached event is old " + cachedNextEvent.Start);

                }
            }
            else
            {
                IemUtils.DDLog("no cached event");
            }


            DateTime buffEndDateTime = DateTime.Now.Add(new TimeSpan(99, 0, 0, 0));
            IemUtils.ScheduledEvent nextevent = null;
            foreach (IemUtils.ScheduledEvent sevent in IemUtils.scheduledEvents.Values)
            {
                IemUtils.DDLog("event:" + sevent.Start);
                if ((sevent.Start > DateTime.Now) && (sevent.Start < buffEndDateTime))
                {

                    nextevent = sevent;
                    cachedNextEvent = sevent;
                    buffEndDateTime = sevent.Start;
                }
            }
            IemUtils.DDLog("returning event" + nextevent.Start);
            return nextevent;
        }

        #endregion

        #region debugging ouput

        void ShowMyDbEvents(BasePlayer player)
        {

            _mySql.Query(Sql.Builder.Append(SelectPlayer, player.UserIDString), _mySqlConnection, players =>
            {

                string message = "";
                if (players.Count == 0)
                {
                    message += "no results in db" + "\n";
                }

                foreach (Dictionary<string, object> result in players)
                {


                    if (player.UserIDString == (string)result["steam_id"])
                    {
                        DateTime time = DateTime.Now;
                        time = DateTime.Parse(result["start"].ToString());
                        Guid eventGuid = new Guid((string)result["e_guid"]);
                        message += "event start: " + time + "\n";
                        message += "event name: " + (string)result["event_name"] + "\n";
                        message += "team name: " + (string)result["team_name"] + "\n";
                        message += "player steam id: " + (string)result["steam_id"] + "\n";
                        message += "\n";
                    }

                }
                player.ConsoleMessage(message);
                IemUtils.DDLog(message);
            });

        }

        string ShowMyEvents(BasePlayer player)
        {
            string message = "";
            if (IemUtils.scheduledEvents.Count == 0)
            {
                message += "no events";
            }
            foreach (KeyValuePair<Guid, IemUtils.ScheduledEvent> scheduledEvent in IemUtils.scheduledEvents)
            {
                if (scheduledEvent.Value.Start < DateTime.Now.Add(new TimeSpan(0, 0, 60, 0)))
                {
                    foreach (var team in scheduledEvent.Value.schTeams)
                    {
                        if (team.Value.schPlayers.Count == 0)
                        {

                        }
                        else
                        {
                            foreach (var schplayer in team.Value.schPlayers.Values)
                            {
                                if (schplayer.steamId == player.UserIDString)
                                {
                                    message += "you are registered for event: " + scheduledEvent.Value.Start + "\n";
                                    message += "you are registered for event: " + scheduledEvent.Value.EventName + "\n";
                                    message += "you are registered for event: " + scheduledEvent.Value.EventName + "\n";
                                    if (schplayer.schTeam != null)
                                        message += "on the " + schplayer.schTeam.TeamName;
                                    else
                                    {
                                        message += "no team";
                                    }
                                }

                                IemUtils.SchLog("");

                            }
                        }
                    }
                    IemUtils.SchLog("");
                }
            }

            return message;
        }


        #endregion

        #region CRUD on scheduled events

        void addPlayerToScheduledEvent(BasePlayer player)
        {
            IemUtils.ScheduledEvent sevent = GetNextEvent();

            if (CheckExistsGame("Default Team Game", sevent.Start))
            {
                IemUtils.SchLog("adding player to game " + sevent.Start);

                if (sevent.GetPlayer(player.UserIDString) != null)
                {
                    IemUtils.SchLog("player exists");
                }
                else
                {
                    IemUtils.SchLog("player NOT exists!!!");
                    var eplayer = new IemUtils.ScheduledEvent.ScheduledEventPlayer(player.UserIDString, sevent.GetTeam("team_blue"));
                    var sql = Sql.Builder;
                    sql.Append(InsertPlayer,
                        eplayer.guid,
                        eplayer.steamId,
                        eplayer.DisplayName,
                        sevent.guid.ToString(),
                        sevent.GetTeam("team_blue").guid.ToString()
                        );

                    IemUtils.SchLog(sql.SQL);
                    iemScheduler._mySql.Insert(sql, iemScheduler._mySqlConnection);
                }
            }
        }

        void outputScheduledInfos()
        {
            foreach (KeyValuePair<Guid, IemUtils.ScheduledEvent> scheduledEvent in IemUtils.scheduledEvents)
            {
                IemUtils.ScheduledEvent sevent = scheduledEvent.Value;
                if (sevent.Start > DateTime.Now
                    && sevent.Start < DateTime.Now.Add(new TimeSpan(0, 0, 10, 0)))
                {
                    IemUtils.DLog("event is " + scheduledEvent.Key);
                    IemUtils.DLog("time is " + scheduledEvent.Value.Start);
                    foreach (
                        IemUtils.ScheduledEvent.ScheduledEventTeam scheduledEventTeam in scheduledEvent.Value.schTeams.Values)
                    {
                        //IemUtils.DLog("team is " + scheduledEventTeam.TeamName);
                        foreach (
                            IemUtils.ScheduledEvent.ScheduledEventPlayer scheduledEventPlayer in
                            scheduledEventTeam.schPlayers.Values)
                        {
                            IemUtils.DLog("player is " + scheduledEventPlayer.steamId);
                        }
                    }

                }
            }
        }

        #endregion

        #region Scheduler state manager

        public class SchedulerStateManager : IncursionStateManager.StateManager
        {

            public SchedulerStateManager(IncursionStateManager.IStateMachine initialState) : base(initialState)
            {
                IncursionUI.CreateSchedulerStateManagerDebugBanner("state:" + GetState().ToString());
            }

            public override void ChangeState(IncursionStateManager.IStateMachine newState)
            {
                base.ChangeState(newState);
                //IemUtils.DDLog("changing state in EventStateManager");
                IncursionUI.CreateSchedulerStateManagerDebugBanner("state:" + GetState().ToString());

            }

            public class SchedulerPluginLoaded : IncursionStateManager.StateBase<SchedulerPluginLoaded>,
                IncursionStateManager.IStateMachine
            {


            }

            public class SchedulerPluginUnloaded : IncursionStateManager.StateBase<SchedulerPluginUnloaded>,
                IncursionStateManager.IStateMachine
            {
                public new void Enter(IncursionStateManager.StateManager esm)
                {
                    iemScheduler.cooldownTimer?.Destroy();
                    iemScheduler.nextGameTimer?.Destroy();
                }


            }

            public class Scheduled : IncursionStateManager.StateBase<Scheduled>,
                IncursionStateManager.IStateMachine
            {
                public new void Enter(IncursionStateManager.StateManager esm)
                {
                    //start creating new events on a timer
                    AutoEventTimerRegister();

                    iemScheduler.Subscribe(nameof(OnTick));
                }

                public new void Exit(IncursionStateManager.StateManager sm)
                {
                    IncursionEvents.EventStateManager esm = IncursionEvents.esm;
                    //don't want to generate events anymore
                    AutoEventTimerRemove();

                    iemScheduler.Unsubscribe(nameof(OnTick));
                    iemScheduler.cooldownTimer?.Destroy();
                    iemScheduler.nextGameTimer?.Destroy();
                    esm.CreateEventBanner("Scheduling disabled");


                    iemScheduler.cooldown = false;
                }


            }




            public class Repeating : IncursionStateManager.StateBase<Repeating>,
                IncursionStateManager.IStateMachine
            {
                public new void Enter(IncursionStateManager.StateManager sm)
                {
                    IncursionEvents.EventStateManager esm = IncursionEvents.esm;
                    esm.CreateEventBanner("Game starts as soon as possible (" +
                        iemScheduler.IncursionEvents.Config["DefaultGame"] + ")");

                    if (esm.IsAny(IncursionEvents.EventManagementLobby.Instance))
                    {
                        esm.currentGameStateManager = 
                            esm.gameStateManagers[(string)iemScheduler.IncursionEvents.Config["DefaultGame"]];

                        // esm.currentGameStateManager = esm.gameStateManagers.First().Value;
                        esm.currentGameStateManager.ReinitializeGame();
                        esm.ChangeState(IncursionEvents.GameLoaded.Instance);
                        esm.Update();


                        if (esm.IsAny(IncursionEvents.GameLoaded.Instance))
                        {
                            esm.ChangeState(IncursionEvents.EventLobbyOpen.Instance);
                            esm.Update();
                        }


                    }
                    else if (esm.IsAny(IncursionEvents.GameLoaded.Instance))
                    {
                        esm.CreateEventBanner("Next game will start immediately");
                        esm.ChangeState(IncursionEvents.EventLobbyOpen.Instance);
                        esm.Update();
                    }
                    else if (esm.IsAny(IncursionEvents.EventRunning.Instance))
                    {
                        esm.CreateEventBanner("Waiting for previous event to finish");
                    }
                    else if (esm.IsAny(
                        IncursionEvents.EventLobbyOpen.Instance,
                        IncursionEvents.EventLobbyClosed.Instance))
                    {
                        //esm.CreateEventBanner("Event lobby is opeb");
                        esm.CreateEventBanner("Waiting for previous event to start");
                    }

                    iemScheduler.Subscribe(nameof(OnEventComplete));
                    iemScheduler.Subscribe(nameof(OnEventCancelled));

                }

                public new void Execute(IncursionStateManager.StateManager sm)
                {
                    IncursionEvents.EventStateManager esm = IncursionEvents.esm;

                    IemUtils.DLog("calling update on state " + esm.GetState());
                    if (esm.IsAny(IncursionEvents.EventComplete.Instance,
                        IncursionEvents.EventCancelled.Instance))
                    {
                        esm.ChangeState(IncursionEvents.EventManagementLobby.Instance);
                        esm.Update();
                    }


                    if (esm.IsAny(IncursionEvents.EventManagementLobby.Instance))
                    {
                        esm.currentGameStateManager = esm.gameStateManagers.First().Value;
                        esm.currentGameStateManager.ReinitializeGame();
                        esm.ChangeState(IncursionEvents.GameLoaded.Instance);
                        esm.Update();
                    }


                    if (esm.IsAny(IncursionEvents.GameLoaded.Instance))
                    {
                        esm.ChangeState(IncursionEvents.EventLobbyOpen.Instance);
                        esm.Update();
                    }




                }

                public new void Exit(IncursionStateManager.StateManager sm)
                {

                    IncursionEvents.EventStateManager esm = IncursionEvents.esm;
                    esm.CreateEventBanner("");

                    iemScheduler.Unsubscribe(nameof(OnEventComplete));
                    iemScheduler.Unsubscribe(nameof(OnEventCancelled));
                }


            }

            public class Once : IncursionStateManager.StateBase<Once>,
                IncursionStateManager.IStateMachine
            {
                public new void Enter(IncursionStateManager.StateManager sm)
                {
                    IncursionEvents.EventStateManager esm = IncursionEvents.esm;
                    esm.CreateEventBanner("One off game");
                    throw new Exception("unsupported");
                }

            }

            public class Manual : IncursionStateManager.StateBase<Manual>,
                IncursionStateManager.IStateMachine
            {
                public new void Enter(IncursionStateManager.StateManager sm)
                {
                    IncursionEvents.EventStateManager esm = IncursionEvents.esm;
                    esm.CreateEventBanner("Manually controlled game");
                    throw new Exception("unsupported");
                }


            }

        }



        #endregion

        #region console commands   

        [ConsoleCommand("schedx")]
        void ccmdEvent(ConsoleSystem.Arg arg)
        {
            if (!IemUtils.hasAccess(arg)) return;
            switch (arg.Args[0].ToLower())
            {
                case "joinblueteam":
                    SendReply(arg, "joining blue team");
                    CallSelectScheduledEventTeamHandler(arg.Player());

                    //PauseGame();
                    return;
                case "dump":
                    outputScheduledInfos();
                    //PauseGame();
                    return;
                case "cycle":
                    //join the blue team for the next scheduled event
                    CallSelectScheduledEventTeamHandler(arg.Player());
                    SendReply(arg, ShowMyEvents(arg.Player()));
                    ForceRunNextScheduledEvent();
                    outputScheduledInfos();
                    //PauseGame();
                    return;
                case "startnow":
                    ForceRunNextScheduledEvent();
                    //PauseGame();
                    return;
                case "myevents":
                    SendReply(arg, ShowMyEvents(arg.Player()));
                    //PauseGame();
                    return;
                case "mydbevents":
                    SendReply(arg, "sending...");
                    ShowMyDbEvents(arg.Player());
                    //PauseGame();
                    return;
                case "cleardb":
                    SendReply(arg, "sending...");
                    ResetDataInMySQL(arg);
                    //PauseGame();
                    return;
                case "mode.scheduled":
                case "scheduled":
                    SendReply(arg, "scheduled");
                    IncursionEvents.Config["EventManagementMode"] = "scheduled";
                    Interface.Oxide.CallHook("OnEventManagementModeChanged");
                    return;
                case "mode.repeating":
                case "repeating":
                    SendReply(arg, "repeating");
                    IncursionEvents.Config["EventManagementMode"] = "repeating";
                    Interface.Oxide.CallHook("OnEventManagementModeChanged");
                    return;
                case "mode.once":
                case "once":
                    SendReply(arg, "once");
                    IncursionEvents.Config["EventManagementMode"] = "once";
                    Interface.Oxide.CallHook("OnEventManagementModeChanged");
                    return;
                case "mode.manual":
                    SendReply(arg, "manual");
                    IncursionEvents.Config["EventManagementMode"] = "manual";
                    Interface.Oxide.CallHook("OnEventManagementModeChanged");
                    return;
            }
        }

        private void ResetDataInMySQL(ConsoleSystem.Arg arg)
        {
            _mySql.Update(Sql.Builder.Append(DropScheduledRecords), _mySqlConnection);
        }

        private void CallSelectScheduledEventTeamHandler(BasePlayer basePlayer)
        {
            addPlayerToScheduledEvent(basePlayer);
            IncursionEventGame.EventPlayer eventPlayer
                = IncursionEventGame.EventPlayer.GetEventPlayer(basePlayer);
            // eventPlayer.psm.SubStateReturn();
            ListScheduledEvents();

        }

        #endregion
    }
}
