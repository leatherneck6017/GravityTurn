using System;
using System.IO;

namespace GravityTurn
{
    public class LaunchParameters
    {
        #region GUI Variables

        [Persistent]
        public EditableValue StartSpeed = new EditableValue(100, locked: false);
        [Persistent]
        public EditableValue HoldAPTime = new EditableValue(50, locked: false);
        [Persistent]
        public EditableValue APTimeStart = new EditableValue(50, locked: true);
        [Persistent]
        public EditableValue APTimeFinish = new EditableValue(50, locked: true);
        [Persistent]
        public EditableValue TurnAngle = new EditableValue(10, locked: false);
        [Persistent]
        public EditableValue Sensitivity = new EditableValue(0.3, locked: true);
        [Persistent]
        public EditableValue Roll = new EditableValue(0, locked: true);
        [Persistent]
        public EditableValue DestinationHeight = new EditableValue(80, locked: true);
        [Persistent]
        public EditableValue PressureCutoff = new EditableValue(1200, locked: false);
        [Persistent]
        public EditableValue Inclination = new EditableValue(0, locked: true);
        [Persistent]
        public bool EnableStageManager = true;
        [Persistent]
        public bool EnableSpeedup = false;
        [Persistent]
        public EditableValue FairingPressure = new EditableValue(1000, "{0:0}");
        [Persistent]
        public EditableValue autostagePostDelay = new EditableValue(0.3d, "{0:0.0}");
        [Persistent]
        public EditableValue autostagePreDelay = new EditableValue(0.7d, "{0:0.0}");
        [Persistent]
        public EditableValue autostageLimit = new EditableValue(0, "{0:0}");
        [Persistent]
        public bool EnableStats = false;

        #endregion

        public Vessel CurrentVessel
        {
            get
            { return GravityTurner.getVessel; }
        }

        public LaunchParameters()
        {
        }

        string DefaultConfigFilename(Vessel vessel)
        {
            return LaunchDB.GetBaseFilePath(this.GetType(), string.Format("gt_vessel_default_{0}.cfg", vessel.mainBody.name));
        }
        string ConfigFilename(Vessel vessel)
        {
            return LaunchDB.GetBaseFilePath(this.GetType(), string.Format("gt_vessel_{0}_{1}.cfg", vessel.id.ToString(), vessel.mainBody.name));
        }
        public bool Load()
        {
            ConfigNode savenode;
            try
            {
                savenode = ConfigNode.Load(ConfigFilename(CurrentVessel));
                if (savenode != null)
                {
                    ConfigNode.LoadObjectFromConfig(this, savenode);
                }
                else
                {
                    // now try to get defaults
                    savenode = ConfigNode.Load(DefaultConfigFilename(CurrentVessel));
                    if (savenode != null)
                    {
                        if (ConfigNode.LoadObjectFromConfig(this, savenode))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                GravityTurner.Log("Vessel Load error " + ex.GetType());
                return false;
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilename(CurrentVessel)));
                ConfigNode savenode = ConfigNode.CreateConfigFromObject(this);
                // save this vehicle
                savenode.Save(ConfigFilename(CurrentVessel));
            }
            catch (Exception)
            {
                GravityTurner.Log("Exception, vessel NOT saved!");
            }
        }

        public void SaveDefaults()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilename(CurrentVessel)));
            ConfigNode savenode = ConfigNode.CreateConfigFromObject(this);
            // save defaults for new vehicles
            savenode.Save(DefaultConfigFilename(CurrentVessel));

            GravityTurner.Log("Defaults saved to " + DefaultConfigFilename(CurrentVessel));
        }

    }
}
