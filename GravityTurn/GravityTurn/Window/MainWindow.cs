using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GravityTurn.Window
{
    class MainWindow :  BaseWindow
    {

        HelpWindow helpWindow = null;
        StageSettings stagesettings = null;
        LaunchParameters parameters = null;

        public MainWindow(GravityTurner inTurner, int inWindowID)
            : base(inTurner,inWindowID)
        {
            turner = inTurner;
            parameters = GravityTurner.Parameters;
            helpWindow = new HelpWindow(inTurner,inWindowID+1);
            stagesettings = new StageSettings(inTurner, inWindowID + 2, helpWindow);
            windowPos.width = 300;
            windowPos.height = 100;
            Version v = typeof(GravityTurner).Assembly.GetName().Version;
            WindowTitle = String.Format("GravityTurn V {0}.{1}.{2}", v.Major, v.Minor, v.Build);
        }

        private void UiStartSpeed()
        {
            GUILayout.BeginHorizontal();
            ItemLabel("Start m/s");
            parameters.StartSpeed.setValue(GUILayout.TextField(string.Format("{0:0.0}", parameters.StartSpeed), GUILayout.Width(60)));
            parameters.StartSpeed.locked = GuiUtils.LockToggle(parameters.StartSpeed.locked);
            helpWindow.Button("At this speed, pitch to Turn Angle to begin the gravity turn.  Stronger rockets and extremely aerodynamically stable rockets should do this earlier.");
            GUILayout.EndHorizontal();

        }
        private void UiTurnAngle()
        {
            GUILayout.BeginHorizontal();
            ItemLabel("Turn Angle");
            parameters.TurnAngle.setValue(GUILayout.TextField(string.Format("{0:0.0}", parameters.TurnAngle), GUILayout.Width(60)));
            parameters.TurnAngle.locked = GuiUtils.LockToggle(parameters.TurnAngle.locked);
            helpWindow.Button("Angle to start turn at Start Speed.  Higher values may cause aerodynamic stress.");
            GUILayout.EndHorizontal();
        }
        private void UiAPTimeStart()
        {
            GUILayout.BeginHorizontal();
            ItemLabel("Hold AP Time Start");
            parameters.APTimeStart.setValue(GUILayout.TextField(parameters.APTimeStart.ToString(), GUILayout.Width(60)));
            parameters.APTimeStart.locked = GuiUtils.LockToggle(parameters.APTimeStart.locked);
            helpWindow.Button("Starting value for Time To Prograde.  Higher values will make a steeper climb.  Steeper climbs are usually worse.  Lower values may cause overheating or death.");
            GUILayout.EndHorizontal();
        }
        private void UiAPTimeFinish()
        {
            GUILayout.BeginHorizontal();
            ItemLabel("Hold AP Time Finish");
            parameters.APTimeFinish.setValue(GUILayout.TextField(parameters.APTimeFinish.ToString(), GUILayout.Width(60)));
            parameters.APTimeFinish.locked = GuiUtils.LockToggle(parameters.APTimeFinish.locked);
            helpWindow.Button("AP Time will fade to this value, to vary the steepness of the ascent during the ascent.");
            GUILayout.EndHorizontal();
        }
        private void UiSensitivity()
        {
            GUILayout.BeginHorizontal();
            ItemLabel("Sensitivity");
            parameters.Sensitivity.setValue(GUILayout.TextField(parameters.Sensitivity.ToString(), GUILayout.Width(60)));
            parameters.Sensitivity.locked = GuiUtils.LockToggle(parameters.Sensitivity.locked);
            helpWindow.Button("Will not throttle below this value.  Mostly a factor at the end of ascent.");
            GUILayout.EndHorizontal();
        }
        private void UiDestinationHeight()
        {
            GUILayout.BeginHorizontal();
            ItemLabel("Destination Height (km)");
            parameters.DestinationHeight.setValue(GUILayout.TextField(parameters.DestinationHeight.ToString(), GUILayout.Width(60)));
            parameters.DestinationHeight.locked = GuiUtils.LockToggle(parameters.DestinationHeight.locked);
            helpWindow.Button("Desired Apoapsis.");
            GUILayout.EndHorizontal();
        }
        private void UiRoll()
        {
            GUILayout.BeginHorizontal();
            ItemLabel("Roll");
            parameters.Roll.setValue(GUILayout.TextField(parameters.Roll.ToString(), GUILayout.Width(60)));
            parameters.Roll.locked = GuiUtils.LockToggle(parameters.Roll.locked);
            helpWindow.Button("If you want a particular side of your ship to face downwards.  Shouldn't matter for most ships.  May cause mild nausea.");
            GUILayout.EndHorizontal();
        }
        private void UiInclination()
        {
            GUILayout.BeginHorizontal();
            ItemLabel("Inclination");
            parameters.Inclination.setValue(GUILayout.TextField(parameters.Inclination.ToString(), GUILayout.Width(60)));
            parameters.Inclination.locked = GuiUtils.LockToggle(parameters.Inclination.locked);
            helpWindow.Button("Desired orbit inclination.  Any non-zero value WILL make your launch less efficient. Final inclination will also not be perfect.  Sorry about that, predicting coriolis is hard.");
            GUILayout.EndHorizontal();
        }
        private void UiPressureCutoff()
        {
            GUILayout.BeginHorizontal();
            ItemLabel("Pressure Cutoff");
            parameters.PressureCutoff.setValue(GUILayout.TextField(parameters.PressureCutoff.ToString(), GUILayout.Width(60)));
            parameters.PressureCutoff.locked = GuiUtils.LockToggle(parameters.PressureCutoff.locked);
            helpWindow.Button("Dynamic pressure where we change from Surface to Orbital velocity tracking\nThis will be a balance point between aerodynamic drag in the upper atmosphere vs. thrust vector loss.");
            GUILayout.EndHorizontal();
        }

        private string GetAscentPhaseString(LaunchCalculations.AscentPhase phase)
        {
            switch (phase)
            {
                case LaunchCalculations.AscentPhase.Landed:
                    return "Landed";
                case LaunchCalculations.AscentPhase.InLaunch:
                    return "Launching";
                case LaunchCalculations.AscentPhase.InInitialPitch:
                    return "Pitching";
                case LaunchCalculations.AscentPhase.InTurn:
                    return "Turning";
                case LaunchCalculations.AscentPhase.InInsertion:
                    return "Insertion";
                case LaunchCalculations.AscentPhase.InCoasting:
                    return "Coasting";
                case LaunchCalculations.AscentPhase.InCircularisation:
                    return "";
            }
            return "";
        }

        public override void WindowGUI(int windowID)
        {
            base.WindowGUI(windowID);
            if (!WindowVisible && turner.button.enabled)
            {
                turner.button.SetFalse(false);
                parameters.Save();
            }
            GUILayout.BeginVertical();
            UiStartSpeed();
            UiTurnAngle();
            UiAPTimeStart();
            UiAPTimeFinish();
            UiSensitivity();
            UiDestinationHeight();
            UiRoll();
            UiInclination();
            UiPressureCutoff();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Setup", GUILayout.ExpandWidth(false)))
                stagesettings.WindowVisible = !stagesettings.WindowVisible;
            parameters.EnableStageManager = GUILayout.Toggle(parameters.EnableStageManager, "Auto Stage");
            parameters.EnableSpeedup = GUILayout.Toggle(parameters.EnableSpeedup, "Use Timewarp");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            turner.flightMapWindow.WindowVisible = GUILayout.Toggle(turner.flightMapWindow.WindowVisible, "Show Launch Map", GUILayout.ExpandWidth(false));
            parameters.EnableStats = GUILayout.Toggle(parameters.EnableStats, "Show Stats", GUILayout.ExpandWidth(false));
            if (turner.statsWindow.WindowVisible != parameters.EnableStats)
            {
                turner.statsWindow.WindowVisible = parameters.EnableStats;
                turner.statsWindow.Save();
                if (!turner.statsWindow.WindowVisible)
                {
                    turner.statsWindow.windowPos.height = 200;
                    GravityTurner.DebugShow = false;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            // when not landed and not launching we are in orbit. allow to save.
            if (!GravityTurner.getVessel.Landed && !LaunchCalculations.Instance.Launching)
            {
                if (LaunchCalculations.Instance.Phase >= LaunchCalculations.AscentPhase.InCircularisation)
                    GUILayout.Label("Launch success! ", GUILayout.ExpandWidth(false));

                if (GUILayout.Button(GuiUtils.saveIcon, GUILayout.ExpandWidth(false), GUILayout.MinWidth(18), GUILayout.MinHeight(21)))
                    parameters.SaveDefaults();
            }
            else
                GUILayout.Label(string.Format("{0}, time to match: {1:0.0} s", GetAscentPhaseString(LaunchCalculations.Instance.Phase), parameters.HoldAPTime), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            // landed, not launched yet. Allow configuration
            if (GravityTurner.getVessel.Landed && !LaunchCalculations.Instance.Launching)
            {
                GUILayout.BeginHorizontal();
                string guess = turner.IsLaunchDBEmpty() ? "First Guess" : "Improve Guess";
                if (GUILayout.Button(guess, GUILayout.ExpandWidth(false)))
                    LaunchCalculations.Instance.CalculateSettings(GravityTurner.getVessel);
                if (GUILayout.Button("Previous Best Settings", GUILayout.ExpandWidth(false)))
                    LaunchCalculations.Instance.CalculateSettings(GravityTurner.getVessel, true);
                helpWindow.Button("Improve Guess will try to extrapolate the best settings based on previous launches.  This may end in fiery death, but it won't happen the same way twice.  Be warned, sometimes launches get worse before they get better.  But they do get better.");
                if (GUILayout.Button(GuiUtils.saveIcon, GUILayout.ExpandWidth(false), GUILayout.MinWidth(18), GUILayout.MinHeight(21)))
                    parameters.SaveDefaults();
                GUILayout.EndHorizontal();
            }
            // while landed, show launch button
            if (GravityTurner.getVessel.Landed && !LaunchCalculations.Instance.Launching && GUILayout.Button("Launch!", GUILayout.ExpandWidth(true), GUILayout.MinHeight(30)))
            {
                turner.Launch();
            }
            // while launching, show launch button
            if (LaunchCalculations.Instance.Launching && GUILayout.Button("Abort!", GUILayout.MinHeight(30)))
            {
                turner.Kill();
                turner.RecordAbortedLaunch();
            }
#if DEBUG
            // GUILayout.Label(GravityTurner.DebugMessage, GUILayout.ExpandWidth(true), GUILayout.MinHeight(200));
#endif

            GUILayout.EndVertical();
            double StopHeight = GravityTurner.getVessel.mainBody.atmosphereDepth;
            if (StopHeight <= 0)
                StopHeight = parameters.DestinationHeight * 1000;
            parameters.HoldAPTime = parameters.APTimeStart + ((float)GravityTurner.getVessel.altitude / (float)StopHeight * (parameters.APTimeFinish - parameters.APTimeStart));
            if (parameters.HoldAPTime > Math.Max(parameters.APTimeFinish, parameters.APTimeStart))
                parameters.HoldAPTime = Math.Max(parameters.APTimeFinish, parameters.APTimeStart);
            if (parameters.HoldAPTime < Math.Min(parameters.APTimeFinish, parameters.APTimeStart))
                parameters.HoldAPTime = Math.Min(parameters.APTimeFinish, parameters.APTimeStart);
            Rect r = GUILayoutUtility.GetLastRect();
            float minHeight = r.height + r.yMin + 10;
            if (windowPos.height != minHeight && minHeight>20)
            {
                windowPos.height = minHeight;
                Save();
            }
        }
    }
}
