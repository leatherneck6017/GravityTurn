using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using UnityEngine;
using KSP.IO;
using System.Diagnostics;
using KSP.UI.Screens;
using KramaxReloadExtensions;
using KSP.UI.Screens.Flight;

namespace GravityTurn
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GravityTurner : ReloadableMonoBehaviour
    {

        public static LaunchParameters Parameters = new LaunchParameters();

        public static Vessel getVessel { get { return FlightGlobals.ActiveVessel; } }

        #region Misc. Public Variables
        public string LaunchName = "";
        public CelestialBody LaunchBody = null;

        #endregion

        #region Window Stuff

        public ApplicationLauncherButton button;
        Window.MainWindow mainWindow = null;
        public Window.WindowManager windowManager = new Window.WindowManager();
        public Window.FlightMapWindow flightMapWindow;
        public Window.StatsWindow statsWindow;
        static public string DebugMessage = "";
        static public bool DebugShow = false;

        #endregion


        #region Controllers and such

        public static AttitudeController attitude = null;
        public static StageController stage;
        public static StageStats stagestats = null;
        public static MechjebWrapper mucore = new MechjebWrapper();
        public static LaunchDB launchdb = null;
        LaunchCalculations Calculations = null;

        public bool IsLaunchDBEmpty()
        {
            return launchdb.IsEmpty();
        }

        #endregion

        private int lineno { get { StackFrame callStack = new StackFrame(1, true); return callStack.GetFileLineNumber(); } }
        public static void Log(
            string format,
            params object[] args
            )
        {

            string method = "";
#if DEBUG
            StackFrame stackFrame = new StackFrame(1, true);
            method = string.Format(" [{0}]|{1}", stackFrame.GetMethod().ToString(), stackFrame.GetFileLineNumber());
#endif
            string incomingMessage;
            if (args == null)
                incomingMessage = format;
            else
                incomingMessage = string.Format(format, args);
            UnityEngine.Debug.Log(string.Format("GravityTurn{0} : {1}", method, incomingMessage));
        }

        private void OnGUI()
        {
            // hide UI if F2 was pressed
            if (!Window.BaseWindow.ShowGUI)
                return;
            if (Event.current.type == EventType.Repaint || Event.current.isMouse)
            {
                //myPreDrawQueue(); // Your current on preDrawQueue code
            }
            windowManager.DrawGuis(); // Your current on postDrawQueue code
        }

        private void ShowGUI()
        {
            Window.BaseWindow.ShowGUI = true;
        }
        private void HideGUI()
        {
            Window.BaseWindow.ShowGUI = false;
        }

        /*
         * Called after the scene is loaded.
         */
        public void Awake()
        {
            Log("GravityTurn: Awake {0}", this.GetInstanceID());
        }

        void Start()
        {
            Log("Starting");
            try
            {
                Calculations = LaunchCalculations.Instance;
                mucore.init();
                attitude = new AttitudeController(this);
                stage = new StageController(this);
                attitude.OnStart();
                Calculations.stagestats = new StageStats(stage);
                stagestats = Calculations.stagestats;
                stagestats.editorBody = getVessel.mainBody;
                stagestats.OnModuleEnabled();
                stagestats.OnFixedUpdate();
                stagestats.RequestUpdate(this);
                stagestats.OnFixedUpdate();
                CreateButtonIcon();
                LaunchName = new string(getVessel.vesselName.ToCharArray());
                LaunchBody = getVessel.mainBody;
                launchdb = new LaunchDB(this);
                launchdb.Load();

                mainWindow = new Window.MainWindow(this, 6378070);
                flightMapWindow = new Window.FlightMapWindow(this, 548302);
                statsWindow = new Window.StatsWindow(this, 6378070 + 4);

                GameEvents.onShowUI.Add(ShowGUI);
                GameEvents.onHideUI.Add(HideGUI);
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        private void SetWindowOpen()
        {
            mainWindow.WindowVisible = true;
            if (!Calculations.Launching)
            {
                if (!Parameters.Load())
                {
                    Calculations.CalculateSettings(getVessel);
                }
                InitializeNumbers(getVessel);
            }
        }

        void InitializeNumbers(Vessel vessel)
        {
            Calculations.InitializeNumbers(vessel);
            Calculations.Message = "";
            bool openFlightmap = false;
            openFlightmap = flightMapWindow.WindowVisible;
            flightMapWindow.flightMap = new FlightMap(this);
            flightMapWindow.WindowVisible = openFlightmap;
        }

        private void CreateButtonIcon()
        {
            button = ApplicationLauncher.Instance.AddModApplication(
                SetWindowOpen,
                () => mainWindow.WindowVisible = false,
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.ALWAYS,
                GameDatabase.Instance.GetTexture("GravityTurn/Textures/icon", false)
                );
        }

        private void DebugGUI(int windowID)
        {
            GUILayout.Box(LaunchCalculations.Instance.PreflightInfo(getVessel));
            GUI.DragWindow(new Rect(0, 0, 10000, 2000));
        }

        public void Launch()
        {
            LaunchCalculations.Instance.Killed += OnLaunchKilled;
            StageController.topFairingDeployed = false;
            if (StageManager.CurrentStage == StageManager.StageCount)
                StageManager.ActivateNextStage();
            InitializeNumbers(getVessel);
            getVessel.OnFlyByWire += new FlightInputCallback(fly);
            Calculations.Launching = true;
            Calculations.PitchSet = false;
            DebugShow = false;
            Calculations.Phase = LaunchCalculations.AscentPhase.Landed;
            Parameters.Save();
            LaunchName = new string(getVessel.vesselName.ToCharArray());
            LaunchBody = getVessel.mainBody;
        }

        private void OnLaunchKilled()
        {
            Kill();
            button.SetFalse();
            LaunchCalculations.Instance.Killed -= OnLaunchKilled;
        }

        void Update()
        {
            if (Calculations.Launching)
            {
                attitude.OnUpdate();
            }
        }

        void FixedUpdate()
        {
            Calculations.FixedUpdate();
/*            if (Calculations.Launching)
            {
                stagestats.editorBody = GravityTurner.getVessel.mainBody;
                if (flightMapWindow.flightMap != null && Calculations.Launching)
                    flightMapWindow.flightMap.UpdateMap(getVessel);
            }*/
        }

        public string GetFlightMapFilename()
        {
            return LaunchDB.GetBaseFilePath(this.GetType(), string.Format("gt_vessel_{0}_{1}.png", LaunchName, LaunchBody.name));
        }

        public void Kill()
        {
            if (flightMapWindow.flightMap != null)
            {
                flightMapWindow.flightMap.WriteParameters(Parameters.TurnAngle, Parameters.StartSpeed);
                flightMapWindow.flightMap.WriteResults(Calculations.DragLoss, Calculations.GravityDragLoss, Calculations.VectorLoss);
                Log("Flightmap with {0:0.00} loss", flightMapWindow.flightMap.TotalLoss());
                FlightMap previousLaunch = FlightMap.Load(GetFlightMapFilename(), this);
                if (getVessel.vesselName != "Untitled Space Craft" // Don't save the default vessel name
                    && getVessel.altitude > getVessel.mainBody.atmosphereDepth
                    && (previousLaunch == null
                    || previousLaunch.BetterResults(Calculations.DragLoss, Calculations.GravityDragLoss, Calculations.VectorLoss))) // Only save the best result
                    flightMapWindow.flightMap.Save(GetFlightMapFilename());
            }
            Calculations.Launching = false;
            getVessel.OnFlyByWire -= new FlightInputCallback(fly);
            FlightInputHandler.state.mainThrottle = 0;
            attitude.enabled = false;
            getVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
        }

        // this records an aborted launch as not sucessful
        public void RecordAbortedLaunch()
        {
            launchdb.RecordLaunch();
            launchdb.Save();
        }

        private void fly(FlightCtrlState s)
        {
            if (!Calculations.Launching)
            {
                Kill();
                return;
            }
            Calculations.Process(s, ref Parameters);
        }

        void OnDestroy()
        {
            try
            {
                Kill();
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
            DebugShow = false;
            windowManager.OnDestroy();
            ApplicationLauncher.Instance.RemoveModApplication(button);
        }

    }
}
