using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GravityTurn.Window
{
    public class StageSettings : BaseWindow
    {
        HelpWindow helpWindow;
        public StageSettings(GravityTurner turner, int WindowID, HelpWindow inhelpWindow)
            : base(turner, WindowID)
        {
            helpWindow = inhelpWindow;
            WindowTitle = "GravityTurn Stage Settings";
        }

        public override void WindowGUI(int windowID)
        {
            LaunchParameters parameters = GravityTurner.Parameters;
            base.WindowGUI(windowID);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            ItemLabel("Fairing Pressure");
            parameters.FairingPressure.setValue(GUILayout.TextField(string.Format("{0:0}", parameters.FairingPressure), GUILayout.Width(60)));
            parameters.FairingPressure.locked = GuiUtils.LockToggle(parameters.FairingPressure.locked);
            helpWindow.Button("Dynamic pressure where we pop the procedural fairings.  Higher values will pop lower in the atmosphere, which saves weight, but can cause overheating.  Fairings are heavy, so it's definitely a good idea to pop them as soon as possible.");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            ItemLabel("Stage Post Delay");
            parameters.autostagePostDelay.setValue(GUILayout.TextField(string.Format("{0:0}", parameters.autostagePostDelay), GUILayout.Width(60)));
            parameters.autostagePostDelay.locked = GuiUtils.LockToggle(parameters.autostagePostDelay.locked);
            helpWindow.Button("Delay after a stage event before we consider the next stage.");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            ItemLabel("Stage Pre Delay");
            parameters.autostagePreDelay.setValue(GUILayout.TextField(string.Format("{0:0}", parameters.autostagePreDelay), GUILayout.Width(60)));
            parameters.autostagePreDelay.locked = GuiUtils.LockToggle(parameters.autostagePreDelay.locked);
            helpWindow.Button("Delay after running out of fuel before we activate the next stage.");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            ItemLabel("Stage Limit");
            parameters.autostageLimit.setValue(GUILayout.TextField(string.Format("{0:0}", parameters.autostageLimit), GUILayout.Width(60)));
            parameters.autostageLimit.locked = GuiUtils.LockToggle(parameters.autostageLimit.locked);
            helpWindow.Button("Stop at this stage number");
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
    }
}
