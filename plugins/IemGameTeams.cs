//Requires: IncursionEvents
//Requires: IncursionUI
//Requires: IncursionStateManager
//Requires: IemUtils
//Requires: IemScheduler
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
	[Info ("Incursion Team Game", "tolland", "0.1.0")]
	class IemGameTeams : RustPlugin
	{
		[PluginReference]
		IncursionEvents IncursionEvents;

		[PluginReference]
		IemUtils IemUtils;

		[PluginReference]
		IncursionStateManager IncursionStateManager;

		static IemGameTeams iemGameTeams = null;
    
		DynamicConfigFile incursionEventsConfig;
		private bool AutoStart = false;

		void Init ()
		{
			iemGameTeams = this;
			IemUtils.LogL ("iemGAmeTeams: Init complete");
		}

         
		public IncursionEvents.EventStateManager esm;

		void Loaded ()
		{
			Unsubscribe (nameof (OnRunPlayerMetabolism));

			incursionEventsConfig = new DynamicConfigFile (Manager.ConfigPath + "/IncursionEvents.json");
			incursionEventsConfig.Load ();

			AutoStart = (bool)incursionEventsConfig ["AutoStart"];

			IemUtils.LogL ("iemGAmeTeams: Loading started");
			esm = IncursionEvents.esm;

			//new instance of the Game State manager for this game
			teamGameStateManager = new TeamGameStateManager (GameStateCreated.Instance, "Example Team Game");

			if (esm == null)
				IemUtils.DLog ("esm is null");

			//tell the event manager about this game
			esm.RegisterGameStateManager (teamGameStateManager); 


			//load the game to be managed by the state manager
			teamGameStateManager.eg = new TeamEventGame (teamGameStateManager);
			teamGameStateManager.ChangeState (GameEventLoaded.Instance);

			//tell the Esm to check whether to update the lobby 
			IemUtils.DDLog ("esm state is " + esm.GetState ());
			esm.Update ();

			IemUtils.LogL ("iemGameTeams: Loaded complete");
		}
         
         
		private void OnServerInitialized ()
		{
			IemUtils.LogL ("iemGAmeTeams: OnServerInitialized complete");
		}

		void OnRunPlayerMetabolism (PlayerMetabolism m, BaseCombatEntity entity)
		{
			var player = entity.ToPlayer ();
			if (player == null)
				return;
			IemUtils.SetMetabolismNoNutrition (player);
		}


		private TeamGameStateManager teamGameStateManager;

		class TeamEventGame : IncursionEventGame.EventGame
		{


			public TeamEventGame (IncursionEventGame.GameStateManager gamestatemanager)
				: base (gamestatemanager)
			{
				TeamGame = true;
				FixedNumberOfTeams = true;

				MinPlayers = 1;
				MinPlayersPerTeam = 1;

				TimedGame = true;
				TimeLimit = 15;

				GameLobbyWait = 15;
			}


			public override bool StartGame ()
			{
				IemUtils.DLog ("calling startgame in team event game");
				iemGameTeams.rust.RunServerCommand ("env.time", "12");
				gsm.ChangeState (GameLobby.Instance);
				return true;
			}
		}
              
		public class TeamGameStateManager : IncursionEventGame.GameStateManager
		{

			public IncursionEventGame.EventPlayer eventPlayer;

			public TeamGameStateManager (IncursionStateManager.IStateMachine initialState,
			                             string Gamename) : base (initialState, Gamename)
			{
				IemUtils.DLog ("creating a game state manager - in TeamGameStateManager");
				Name = Gamename;
            }

            public override void ReinitializeGame()
            {
                IemUtils.DLog("reinit game in IemGameTeams");
                eg = new TeamEventGame(this);
                ChangeState(GameEventLoaded.Instance);

            }

            public override void CancelGame()
            {
                IemUtils.DLog("cancelled game in IemGameTeams");
                ChangeState(GameCancelled.Instance);

            }
        }

		/// <summary>
		/// this represents the stage before a specific game implementation has been
		/// loaded into the GameStateManager
		/// it's purpose is to allow reselecting game implementations
		/// </summary>
		public class GameStateCreated : IncursionStateManager.StateBase<GameStateCreated>,
            IncursionStateManager.IStateMachine
		{
			public new void Enter (IncursionStateManager.StateManager gsm)
			{
				IemUtils.DLog ("entry in GameStateCreated");
			}
		}

		/// <summary>
		/// so a Game event is loaded and teams and players are available
		/// </summary>
		public class GameEventLoaded : IncursionStateManager.StateBase<GameEventLoaded>,
            IncursionStateManager.IStateMachine
		{
			public new void Enter (IncursionStateManager.StateManager gsm)
			{
				IemUtils.DLog ("entry in GameEventLoaded");
			}

			public new void
                Execute (IncursionStateManager.StateManager sm)
			{
				TeamGameStateManager gsm = (TeamGameStateManager)sm;
				IemUtils.DLog ("execute in GameEventLoaded");
				if (gsm.eg.CanGameStart ()) {
					gsm.ChangeState (GameEventCanStart.Instance);
				} else {
					gsm.ChangeState (GameEventCannotStart.Instance);
				}

			}
		} 
         
		/// <summary>
		/// so a Game event is loaded and teams and players are available
		/// </summary>
		public class GameEventCanStart : IncursionStateManager.StateBase<GameEventCanStart>,
            IncursionStateManager.IStateMachine
		{
			private Timer warningTimer = null;
			//private static readonly Object obj = new Object();

			public new void Enter (IncursionStateManager.StateManager gsm)
			{
				IemUtils.DLog ("entry in GameEventCanStart");
			}
              
			public new void Execute (IncursionStateManager.StateManager sm)
			{
				TeamGameStateManager gsm = (TeamGameStateManager)sm;
				IemUtils.DLog ("GameEventCanStart: Execute");

				// Plugins.IemUtils.SLog(this.GetHashCode().ToString());

				if (!gsm.eg.CanGameStart ()) {
					gsm.ChangeState (GameEventCannotStart.Instance);
				} else {
					if (iemGameTeams.AutoStart) {

						IncursionUI.CreateGameBanner ("warning - game starting shortly");
						// IemUtils.SLog("setting up timer");

						if (warningTimer == null) {
							// IemUtils.SLog("is null");
						} else {

							if (warningTimer.Destroyed) {
								//   IemUtils.SLog("warningTimer.Destroyed");
							}
						}

						if (warningTimer == null || warningTimer.Destroyed) {
							//IemUtils.SLog("in conditional");
							warningTimer = iemGameTeams.timer.Once (5f, () => {
								//  IemUtils.SLog("in timer");
								IncursionUI.CreateGameBanner ("warning - got to game lobby call");
								gsm.ChangeState (GameLobby.Instance);
							});
						}
					}




				}
			}

			public new void Exit (IncursionStateManager.StateManager sm)
			{
				IemUtils.SLog ("before cancel timer");
				warningTimer.Destroy ();
				IemUtils.SLog ("after cancel timer");
				IncursionUI.CreateGameBanner ("EXITING");
			}
		}

		/// <summary>
		/// so a Game event is loaded and teams and players are available
		/// </summary>
		public class GameEventCannotStart : IncursionStateManager.StateBase<GameEventCannotStart>,
            IncursionStateManager.IStateMachine
		{
			public new void Enter (IncursionStateManager.StateManager gsm)
			{
				IemUtils.DLog ("GameEventCannotStart: entry");
			}

			public new void Execute (IncursionStateManager.StateManager sm)
			{
				TeamGameStateManager gsm = (TeamGameStateManager)sm;
				IemUtils.DLog ("GameEventCannotStart: Execute");

				if (gsm.eg.CanGameStart ()) {
					gsm.ChangeState (GameEventCanStart.Instance);
				}
			}
		}


		/// <summary>
		/// transitioning to this state is where players are moved to the field
		/// game has not yet started. A pre game message can be shown
		/// inventories can be reset
		/// however something must be done to prevent players attacking and building etc
		/// </summary>
		public class GameLobby : IncursionStateManager.StateBase<GameLobby>,
            IncursionStateManager.IStateMachine
		{
			private Timer warningTimer;

			public new void Enter (IncursionStateManager.StateManager sm)
			{
				IncursionEventGame.GameStateManager gsm = (IncursionEventGame.GameStateManager)sm;
				iemGameTeams.esm.ChangeState (IncursionEvents.EventRunning.Instance);
				iemGameTeams.Subscribe (nameof (OnRunPlayerMetabolism));
				gsm.eg.MovePlayersToGame ();

				foreach (IncursionEventGame.EventPlayer eventPlayer in gsm.eg.gamePlayers.Values) {
					eventPlayer.psm.ChangeState (IncursionEventGame.PlayerInGame.Instance);

					IncursionUI.ShowGameBanner (eventPlayer.player,
						gsm.eg.GameIntroBanner);
				}

				IncursionUI.CreateGameBanner ("GAME LOBBY");
				warningTimer = iemGameTeams.timer.Once (gsm.eg.GameLobbyWait, () => {
					gsm.ChangeState (GameStarted.Instance);
				});
			}

			public new void Exit (IncursionStateManager.StateManager esm)
			{
				warningTimer.Destroy ();

				foreach (BasePlayer player in BasePlayer.activePlayerList) {
					IncursionEventGame.EventPlayer eventPlayer
                        = IncursionEventGame.GetEventPlayer (player);
					//eventPlayer.psm.eg = ((IncursionEvents.EventStateManager)esm).eg;
					IncursionUI.HideGameBanner (player);
				}
				iemGameTeams.Unsubscribe (nameof (OnRunPlayerMetabolism));

			}
		}

		public class GameStarted : IncursionStateManager.StateBase<GameStarted>,
            IncursionStateManager.IStateMachine
		{

			private Timer warningTimer;
			private Timer finalWarningTimer;
			private Timer gameTimer;

			private DateTime startTime = DateTime.UtcNow;
			TimeSpan breakDuration = TimeSpan.FromSeconds (15);

			public new void Enter (IncursionStateManager.StateManager sm)
			{
				TeamGameStateManager gsm = (TeamGameStateManager)sm;
				IemUtils.DLog ("entry in Game Started And Open");
				IncursionUI.CreateGameStateManagerDebugBanner ("state:" + gsm.GetState ().ToString ());
				iemGameTeams.Subscribe (nameof (OnRunPlayerMetabolism));
				IncursionUI.CreateGameBanner ("GAME STARTED");
				if (gsm.eg.TimedGame) {
					warningTimer = iemGameTeams.timer.Once (gsm.eg.TimeLimit - 10, () => {
						IncursionUI.CreateGameBanner ("Game ending in 10 seconds - warning");
					});
					finalWarningTimer = iemGameTeams.timer.Once (gsm.eg.TimeLimit - 5, () => {
						IncursionUI.CreateGameBanner ("Game ending in 5 seconds - final warning");
					});
					gameTimer = iemGameTeams.timer.Once (gsm.eg.TimeLimit, () => {
						gsm.ChangeState (GameComplete.Instance);
					});
				}
			}


			public new void Execute (IncursionStateManager.StateManager sm)
			{
				TeamGameStateManager gsm = (TeamGameStateManager)sm;


			}


			public new void Exit (IncursionStateManager.StateManager esm)
			{
				warningTimer.Destroy ();
				finalWarningTimer.Destroy ();
				gameTimer.Destroy ();

				iemGameTeams.Unsubscribe (nameof (OnRunPlayerMetabolism));

			}
		}

		public class GamePaused : IncursionStateManager.StateBase<GamePaused>,
                IncursionStateManager.IStateMachine
		{
			public new void Enter (IncursionStateManager.StateManager sm)
			{
				iemGameTeams.Subscribe (nameof (OnRunPlayerMetabolism));
				IncursionUI.CreateGameBanner ("GAME PAUSED");
			}

			public new void Exit (IncursionStateManager.StateManager esm)
			{

				iemGameTeams.Unsubscribe (nameof (OnRunPlayerMetabolism));
			}
		}

		public class GameComplete : IncursionStateManager.StateBase<GameComplete>,
            IncursionStateManager.IStateMachine
		{
			private Timer warningTimer;

			public new void Enter (IncursionStateManager.StateManager sm)
			{
				TeamGameStateManager gsm = (TeamGameStateManager)sm;

				//tell the esm that the game is complete
				//esm.ChangeState(Plugins.IncursionEvents.EventComplete.Instance);

				iemGameTeams.Subscribe (nameof (OnRunPlayerMetabolism));
				foreach (IncursionEventGame.EventPlayer eventPlayer in gsm.eg.gamePlayers.Values) {
					eventPlayer.psm.ChangeState (IncursionEventGame.PlayerInPostGame.Instance);

				}

				IncursionUI.CreateGameBanner ("Game ended");

				((IncursionEventGame.GameStateManager)gsm).eg.ShowGameResultUI ();

				warningTimer = iemGameTeams.timer.Once (10f, () => {
					IemUtils.DLog ("calling game complete on the event manager");
					//iemGameTeams.esm.GameComplete();
					gsm.ChangeState (GameUnloaded.Instance);
					iemGameTeams.esm.ChangeState (IncursionEvents.EventComplete.Instance);
				});

			}

			public new void Exit (IncursionStateManager.StateManager esm)
			{
				warningTimer.Destroy ();
				((IncursionEventGame.GameStateManager)esm).eg.RemoveGameResultUI ();
				iemGameTeams.Unsubscribe (nameof (OnRunPlayerMetabolism));
			}

		}
           
		public class GameCancelled : IncursionStateManager.StateBase<GameCancelled>,
            IncursionStateManager.IStateMachine
		{
			private Timer warningTimer;

			public new void Enter (IncursionStateManager.StateManager sm)
			{
				TeamGameStateManager gsm = (TeamGameStateManager)sm;

				iemGameTeams.Subscribe (nameof (OnRunPlayerMetabolism));
				foreach (IncursionEventGame.EventPlayer eventPlayer in gsm.eg.gamePlayers.Values) {
					eventPlayer.psm.ChangeState (IncursionEventGame.PlayerInPostGame.Instance);

				}

				IncursionUI.CreateGameBanner ("Game cancelled");
                //@todo do we need to wait?
				//warningTimer = iemGameTeams.timer.Once (3f, () => {

					//gsm.ChangeState (GameUnloaded.Instance);
					iemGameTeams.esm.ChangeState (IncursionEvents.EventCancelled.Instance);
				//});
			}

			public new void Exit (IncursionStateManager.StateManager esm)
			{
				//warningTimer.Destroy ();

				iemGameTeams.Unsubscribe (nameof (OnRunPlayerMetabolism));
			}
		} 

		public class GameUnloaded : IncursionStateManager.StateBase<GameUnloaded>,
            IncursionStateManager.IStateMachine
		{
			public new void Enter (IncursionStateManager.StateManager gsm)
			{
				//@todo need to clean up game playing field here
			}
		}

		//from em
		[ConsoleCommand ("eg")]
		void ccmdEvent (ConsoleSystem.Arg arg)
		{

			if (!IemUtils.hasAccess (arg))
				return;
			switch (arg.Args [0].ToLower ()) {
			case "close":
				teamGameStateManager.ChangeState (GameComplete.Instance);
				return;

			case "open":
				esm.ChangeState (IncursionEvents.EventLobbyOpen.Instance);
				return;


			}
		}

		[ConsoleCommand ("gamex")]
		void ccmdEvent222 (ConsoleSystem.Arg arg)
		{
			if (!IemUtils.hasAccess (arg))
				return;
			switch (arg.Args [0].ToLower ())
            {
                case "cancel":
                    SendReply(arg, "setting cancelled");
                    teamGameStateManager.ChangeState(IemGameTeams.GameCancelled.Instance);
                    return;
                case "force":
                    SendReply(arg, "setting force");
                    teamGameStateManager.eg.ForceStart = true;
                    teamGameStateManager.Update();
                    return;
            }
		}
	}
}
