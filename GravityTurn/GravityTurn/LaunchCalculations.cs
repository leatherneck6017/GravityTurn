using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GravityTurn
{
    public class LaunchCalculations
    {
        public enum AscentPhase
        {
            Landed,
            InLaunch,
            InTurn,
            InInitialPitch,
            InInsertion,
            InCoasting,
            InCircularisation
        }
        public bool Launching = false;
        public AscentPhase Phase = AscentPhase.Landed;
        public MovingAverage Throttle = new MovingAverage(10, 1);
        public double HorizontalDistance = 0;
        public double MaxThrust = 0;
        public float NeutralThrottle = 0.5f;
        public float lastTimeMeasured = 0.0f;
        public VesselState vesselState = new VesselState();
        public double TimeSpeed = 0;
        public double PrevTime = 0;
        public MovingAverage PitchAdjustment = new MovingAverage(4, 0);
        public float YawAdjustment = 0.0f;
        public float maxAoA = 0;
        public LaunchParameters Parameters;
        public StageStats stagestats = null;
        public string Message = "";


        #region Loss and related variables

        public double TotalLoss = 0;
        public double MaxHeat = 0;
        public double VelocityLost = 0;
        public double DragLoss = 0;
        public double GravityDragLoss = 0;
        public double FlyTimeInterval = 0;
        public double VectorLoss = 0;
        public double TotalBurn = 0;
        public bool PitchSet = false;
        public double inclinationHeadingCorrectionSpeed = 0;
        MovingAverage DragRatio = new MovingAverage();

        #endregion

        public double delayUT = double.NaN;
        
        private static LaunchCalculations _instance = null;
        public static LaunchCalculations Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new LaunchCalculations();
                return _instance;
            }
        }

        private LaunchCalculations()
        {
        }

        public void InitializeNumbers(Vessel vessel)
        {
            NeutralThrottle = 0.5f;
            PrevTime = 0;
            VelocityLost = 0;
            DragLoss = 0;
            GravityDragLoss = 0;
            FlyTimeInterval = Time.time;
            VectorLoss = 0;
            HorizontalDistance = 0;
            inclinationHeadingCorrectionSpeed = LaunchCalculations.CircularOrbitSpeed(vessel.mainBody, vessel.mainBody.Radius + Parameters.DestinationHeight * 1000);
            GravityTurner.Log("Orbit velocity {0:0.0}", inclinationHeadingCorrectionSpeed);
            inclinationHeadingCorrectionSpeed /= 1.7;
            GravityTurner.Log("inclination heading correction {0:0.0}", inclinationHeadingCorrectionSpeed);
            MaxThrust = GetMaxThrust(vessel);
        }

        double GetMaxThrust(Vessel vessel)
        {
            double thrust = 0;
            FuelFlowSimulation.Stats[] stats;
            if (vessel.mainBody.atmosphere && vessel.altitude < vessel.mainBody.atmosphereDepth)
                stats = stagestats.atmoStats;
            else
                stats = stagestats.vacStats;
            for (int i = stats.Length - 1; i >= 0; i--)
            {
                if (stats[i].startThrust > thrust)
                    thrust = stats[i].startThrust;
            }
            return thrust;
        }

        public static double TimeToReachAP(VesselState vesselState, double StartSpeed, double TargetAPTime)
        {
            // gravityForce isn't force, it's acceleration
            double targetSpeed = vesselState.gravityForce.magnitude * TargetAPTime;
            double timeToSpeed = (targetSpeed - StartSpeed) / vesselState.maxVertAccel;
            return timeToSpeed;
        }

        public float APThrottle(VesselState vesselState, double timeToAP)
        {
            Parameters = GravityTurner.Parameters;
            Vessel vessel = GravityTurner.getVessel;
            GravityTurner.DebugMessage += "-\n";
            if (vessel.speed < Parameters.StartSpeed)
            {
                Throttle.value = 1.0f;
            }
            else
            {
                if (timeToAP > vessel.orbit.timeToPe) // We're falling
                    timeToAP = 0;
                float diff = 0.1f * (float)Math.Abs(Parameters.HoldAPTime - timeToAP) * 0.5f;
                TimeSpeed = (PrevTime - timeToAP) / (Time.time - lastTimeMeasured);
                if (Math.Abs(TimeSpeed) < 0.02 && PitchAdjustment == 0)
                    NeutralThrottle = (float)Throttle.value;
                if (Math.Abs(timeToAP - Parameters.HoldAPTime) < 0.1)
                {
                    if (PitchAdjustment > 0)
                        PitchAdjustment.value -= 0.1f;
                    else
                        Throttle.force(NeutralThrottle);
                }
                else if (timeToAP < Parameters.HoldAPTime)
                {
                    if (Throttle.value >= 1 && (timeToAP < PrevTime || (timeToAP - Parameters.HoldAPTime) / TimeSpeed > 20))
                    {
                        NeutralThrottle = 1;
                        PitchAdjustment.value += 0.1f;
                    }
                    Throttle.value += diff;

                    if (0 < (timeToAP - Parameters.HoldAPTime) / TimeSpeed && (timeToAP - Parameters.HoldAPTime) / TimeSpeed < 20)  // We will reach desired AP time in <20 second
                    {
                        PitchAdjustment.value -= 0.1f;
                    }
                }
                else if (timeToAP > Parameters.HoldAPTime)
                {
                    if (PitchAdjustment > 0)
                        PitchAdjustment.value -= 0.1f;
                    else
                        Throttle.value -= diff;
                }

                if (Math.Abs(maxAoA) < Math.Abs(vesselState.AoA))
                    maxAoA = vesselState.AoA;

                GravityTurner.DebugMessage += String.Format("max Angle of Attack: {0:0.00}\n", maxAoA);
                GravityTurner.DebugMessage += String.Format("cur Angle of Attack: {0:0.00}\n", vesselState.AoA.value);
                GravityTurner.DebugMessage += String.Format("-\n");

            }
            if (PitchAdjustment < 0)
                PitchAdjustment.value = 0;
            if (PitchAdjustment > MaxAngle(vessel, vesselState))
                PitchAdjustment.value = MaxAngle(vessel, vesselState);

            // We don't want to do any pitch correction during the initial lift
            if (vessel.ProgradePitch(true) < -45)
                PitchAdjustment.force(0);

            PrevTime = vessel.orbit.timeToAp;
            lastTimeMeasured = Time.time;
            if (Throttle.value < Parameters.Sensitivity)
                Throttle.force(Parameters.Sensitivity);
            if (Throttle.value > 1)
                Throttle.force(1);

            // calculate Yaw correction for inclination
            if (vessel.ProgradePitch(true) > -45 
                && Math.Abs(Parameters.Inclination) > 2
                && Phase != AscentPhase.InLaunch)
            {
                float heading = (Mathf.Sign(Parameters.Inclination) * (float)vesselState.orbitInclination.value - Parameters.Inclination);
                GravityTurner.DebugMessage += String.Format("  Heading: {0:0.00}\n", heading);
                heading *= 1.2f;
                if (Math.Abs(heading) < 0.3)
                    heading = 0;
                else if (Mathf.Abs(YawAdjustment) > 0.1)
                    heading = (YawAdjustment*7.0f + heading)/8.0f;

                if (Mathf.Abs(YawAdjustment) > Mathf.Abs(heading) || YawAdjustment == 0.0)
                    YawAdjustment = heading;
                GravityTurner.DebugMessage += String.Format("  YawCorrection: {0:0.00}\n", YawAdjustment);
            }
            else
                YawAdjustment = 0;

            // Inrease the AP time if needed for SRB lifter stages
            if (vessel.HasActiveSRB() && vessel.orbit.timeToAp > Parameters.HoldAPTime && TimeSpeed < 0)
            {
                double StopHeight = GravityTurner.getVessel.mainBody.atmosphereDepth;
                if (StopHeight <= 0)
                    StopHeight = Parameters.DestinationHeight * 1000;
                Parameters.APTimeStart = (StopHeight * vessel.orbit.timeToAp - vessel.altitude * Parameters.APTimeFinish) / (StopHeight - vessel.altitude);
                Parameters.APTimeStart *= 0.99; // We want to be just a bit less than what we calculate, so we don't stay throttled up
            }

            return (float)Throttle.value;
        }

        public void Process(FlightCtrlState s, ref LaunchParameters parameters)
        {
            Vessel vessel = GravityTurner.getVessel;
            Parameters = parameters;
            GravityTurner.DebugMessage = "";
            if (Phase != AscentPhase.InCoasting && vessel.orbit.ApA > parameters.DestinationHeight * 1000 && vessel.altitude < vessel.StableOrbitHeight())
            {
                CalculateLosses(vessel);
                // save launch, ignoring losses due to coasting losses, but so we get results earlier
                GravityTurner.launchdb.RecordLaunch();
                GravityTurner.launchdb.Save();
                Phase = AscentPhase.InCoasting;
                Throttle.force(0);
                GravityTurner.Log("minorbit {0}, {1}", vessel.mainBody.minOrbitalDistance, vessel.StableOrbitHeight());
                // time warp to speed up things (if enabled)
                SpeedupController.ApplySpeedup(2);
            }
            else if (vessel.orbit.ApA > parameters.DestinationHeight * 1000 && vessel.altitude > vessel.StableOrbitHeight())
            {
                Phase = AscentPhase.InCircularisation;
                SpeedupController.StopSpeedup();
                GravityTurner.Log("Saving launchDB");
                GravityTurner.launchdb.RecordLaunch();
                GravityTurner.launchdb.Save();
                if (GravityTurner.mucore.Initialized)
                {
                    Phase = AscentPhase.InCircularisation;
                    GravityTurner.mucore.CircularizeAtAP();
                }

                OnKilled();
            }
            else
            {
                double minInsertionHeight = vessel.mainBody.atmosphere ? vessel.StableOrbitHeight() / 4 : Math.Max(parameters.DestinationHeight * 667, vessel.StableOrbitHeight() * 0.667);

                if (parameters.EnableStageManager && GravityTurner.stage != null)
                    GravityTurner.stage.Update();

                if (vessel.orbit.ApA < parameters.DestinationHeight * 1000)
                    s.mainThrottle = APThrottle(vesselState, vessel.orbit.timeToAp);
                else
                    s.mainThrottle = 0;
                if (Phase == AscentPhase.InInitialPitch && PitchSet)
                {
                    if (vessel.ProgradePitch() + 90 >= parameters.TurnAngle - 0.1)
                    {
                        delayUT = double.NaN;
                        // continue any previous timewarp
                        SpeedupController.RestoreTimeWarp();
                        SpeedupController.ApplySpeedup(1);
                        Phase = AscentPhase.InTurn;
                    }
                }
                if (vessel.speed < parameters.StartSpeed)
                {
                    Phase = AscentPhase.InLaunch;
                    if (vesselState.altitudeBottom > vesselState.vesselHeight)
                        GravityTurner.attitude.attitudeTo(Quaternion.Euler(-90, LaunchHeading(vessel), 0) * RollRotation(), AttitudeReference.SURFACE_NORTH, this);
                    else
                        GravityTurner.attitude.attitudeTo(Quaternion.Euler(-90, 0, 0), AttitudeReference.SURFACE_NORTH, this);
                }
                else if (Phase == AscentPhase.InLaunch || Phase == AscentPhase.InInitialPitch)
                {
                    if (!PitchSet)
                    {
                        // remember and stop timewarp for pitching
                        SpeedupController.StoreTimeWarp();
                        SpeedupController.StopSpeedup();
                        PitchSet = true;
                        Phase = AscentPhase.InInitialPitch;
                        delayUT = Planetarium.GetUniversalTime();
                    }
                    double diffUT = Planetarium.GetUniversalTime() - delayUT;
                    float newPitch = Mathf.Min((float)(((double)parameters.TurnAngle * diffUT) / 5.0d + 2.0d), parameters.TurnAngle);
                    double pitch = (90d - vesselState.vesselPitch + vessel.ProgradePitch() + 90) / 2;
                    GravityTurner.attitude.attitudeTo(Quaternion.Euler(-90 + newPitch, LaunchHeading(vessel), 0) * RollRotation(), AttitudeReference.SURFACE_NORTH, this);
                    GravityTurner.DebugMessage += String.Format("TurnAngle: {0:0.00}\n", parameters.TurnAngle.value);
                    GravityTurner.DebugMessage += String.Format("Target pitch: {0:0.00}\n", newPitch);
                    GravityTurner.DebugMessage += String.Format("Current pitch: {0:0.00}\n", pitch);
                    GravityTurner.DebugMessage += String.Format("Prograde pitch: {0:0.00}\n", vessel.ProgradePitch() + 90);
                }
                else if (vesselState.dynamicPressure > vesselState.maxQ * 0.5 || vesselState.dynamicPressure > parameters.PressureCutoff || vessel.altitude < minInsertionHeight)
                { // Still ascending, or not yet below the cutoff pressure or below min insertion heigt
                    GravityTurner.attitude.attitudeTo(Quaternion.Euler(vessel.ProgradePitch() - PitchAdjustment, LaunchHeading(vessel), 0) * RollRotation(), AttitudeReference.SURFACE_NORTH, this);
                }
                else
                {
                    // did we reach the desired inclination?
                    Quaternion q = Quaternion.Euler(0 - PitchAdjustment, YawAdjustment, parameters.Roll);
                    // smooth out change from surface to orbital prograde
                    if (Phase != AscentPhase.InInsertion && Phase != AscentPhase.InCoasting)
                    {
                        // start timer
                        if (Double.IsNaN(delayUT))
                        {
                            // slow down timewarp
                            delayUT = Planetarium.GetUniversalTime();
                            SpeedupController.StoreTimeWarp();
                            SpeedupController.StopSpeedup();
                            // switch NavBall UI
                            FlightGlobals.SetSpeedMode(FlightGlobals.SpeedDisplayModes.Orbit);
                        }
                        double diffUT = Planetarium.GetUniversalTime() - delayUT;
                        //attitude.attitudeTo(q, AttitudeReference.ORBIT, this);
                        q.x = (GravityTurner.attitude.lastAct.x * 8.0f + q.x) / 9.0f;
                        if (diffUT > 10 || (GravityTurner.attitude.lastAct.x > 0.02 && diffUT > 2.0))
                        {
                            Phase = AscentPhase.InInsertion;
                            delayUT = double.NaN;
                            SpeedupController.RestoreTimeWarp();
                            SpeedupController.ApplySpeedup(2);
                        }
                    }
                    GravityTurner.attitude.attitudeTo(q, AttitudeReference.ORBIT, this);
                }
                GravityTurner.attitude.enabled = true;
                GravityTurner.attitude.Drive(s);
                CalculateLosses(vessel);
                GravityTurner.DebugMessage += "-";
            }
        }

        private float LaunchHeading(Vessel vessel)
        {
            return (float)MuUtils.HeadingForLaunchInclination(vessel.mainBody, Parameters.Inclination, vessel.latitude, inclinationHeadingCorrectionSpeed);
        }

        private float ProgradeHeading(bool surface = true)
        {
            Quaternion current;
            if (surface)
                current = Quaternion.LookRotation(vesselState.surfaceVelocity.normalized, vesselState.up) * Quaternion.Euler(0, 0, Parameters.Roll);
            else
                current = Quaternion.LookRotation(vesselState.orbitalVelocity.normalized, vesselState.up) * Quaternion.Euler(0, 0, Parameters.Roll);
            //current *= vesselState.rotationSurface.Inverse();
            return (float)Vector3d.Angle(Vector3d.Exclude(vesselState.up, vesselState.surfaceVelocity), vesselState.north);
        }


        Quaternion RollRotation()
        {
            return Quaternion.AngleAxis(Parameters.Roll, Vector3.forward);
        }

        double TWRWeightedAverage(double MinimumDeltaV, Vessel vessel)
        {
            stagestats.RequestUpdate(this);
            double TWR = 0;
            double deltav = 0;
            for (int i = stagestats.atmoStats.Length - 1; i >= 0; i--)
            {
                double stagetwr = (stagestats.atmoStats[i].StartTWR(vessel.mainBody.GeeASL) + stagestats.atmoStats[i].MaxTWR(vessel.mainBody.GeeASL)) / 2;
                if (stagetwr > 0)
                {
                    TWR += stagetwr * stagestats.atmoStats[i].deltaV;
                    deltav += stagestats.atmoStats[i].deltaV;
                    if (deltav >= MinimumDeltaV)
                        break;
                }
            }
            return TWR / deltav;
        }

        public string PreflightInfo(Vessel vessel)
        {
            string info = "";
            info += string.Format("Surface TWR:\t{0:0.00}\n", TWRWeightedAverage(2 * vessel.mainBody.GeeASL * Parameters.DestinationHeight, vessel));
            info += string.Format("Mass:\t\t{0:0.00} t\n", vesselState.mass);
            info += string.Format("Height:\t\t{0:0.0} m\n", vesselState.vesselHeight);
            info += "\n";
            info += string.Format("Drag area:\t\t{0:0.00}\n", vesselState.areaDrag);
            info += string.Format("Drag coefficient:\t{0:0.00}\n", vesselState.dragCoef);
            info += string.Format("Drag coefficient fwd:\t{0:0.00}\n", vessel.DragCubeCoefForward());
            DragRatio.value = vesselState.areaDrag / vesselState.mass;
            info += string.Format("area/mass:\t{0:0.00}\n", DragRatio.value);
            return info;
        }


        public static float MaxAngle(Vessel vessel, VesselState vesselState)
        {
            float angle = 100000 / (float)vesselState.dynamicPressure;
            float vertical = 90 + vessel.Pitch();
            angle = Mathf.Clamp(angle, 0, 35);
            if (angle > vertical)
                return vertical;
            return angle;
        }

        public static double CircularOrbitSpeed(CelestialBody body, double radius)
        {
            return Math.Sqrt(body.gravParameter / radius);
        }

        //Computes the deltaV of the burn needed to circularize an orbit.
        public static Vector3d DeltaVToCircularize(Orbit o)
        {
            double UT = Planetarium.GetUniversalTime();
            UT += o.timeToAp;

            Vector3d desiredVelocity = CircularOrbitSpeed(o.referenceBody, o.Radius(UT)) * o.Horizontal(UT);
            Vector3d actualVelocity = o.SwappedOrbitalVelocityAtUT(UT);
            return desiredVelocity - actualVelocity;
        }

        public void FixedUpdate()
        {
            if (Parameters == null)
                Parameters = GravityTurner.Parameters;
            if (Launching)
            {
                stagestats.editorBody = GravityTurner.getVessel.mainBody;
                vesselState.Update(GravityTurner.getVessel);
                GravityTurner.attitude.OnFixedUpdate();
                stagestats.OnFixedUpdate();
                stagestats.RequestUpdate(this);
            }
            else if (Parameters.EnableStats && !GravityTurner.getVessel.Landed && !GravityTurner.getVessel.IsInStableOrbit())
            {
                CalculateLosses(GravityTurner.getVessel);
                stagestats.editorBody = GravityTurner.getVessel.mainBody;
                vesselState.Update(GravityTurner.getVessel);
                GravityTurner.attitude.OnFixedUpdate();
                stagestats.OnFixedUpdate();
                stagestats.RequestUpdate(this);
            }
            else if (Parameters.EnableStats && !GravityTurner.getVessel.Landed && GravityTurner.getVessel.IsInStableOrbit())
            {
                if (VectorLoss > 0.01)
                {
                    Message = string.Format(
                        "Total Vector Loss:\t{0:0.00} m/s\n" +
                        "Total Loss:\t{1:0.00} m/s\n" +
                        "Total Burn:\t\t{2:0.0}\n\n",
                        VectorLoss,
                        TotalLoss,
                        TotalBurn
                        );
                }
                else
                    Message = "";

                Message += string.Format(
                    "Apoapsis:\t\t{0}\n" +
                    "Periapsis:\t\t{1}\n" +
                    "Inclination:\t\t{2:0.0} °\n",
                    OrbitExtensions.FormatOrbitInfo(GravityTurner.getVessel.orbit.ApA, GravityTurner.getVessel.orbit.timeToAp),
                    OrbitExtensions.FormatOrbitInfo(GravityTurner.getVessel.orbit.PeA, GravityTurner.getVessel.orbit.timeToPe),
                    GravityTurner.getVessel.orbit.inclination
                    );

            }
            else if (Parameters.EnableStats && GravityTurner.getVessel.Landed)
            {
                double diffUT = Planetarium.GetUniversalTime() - delayUT;
                if (diffUT > 1 || Double.IsNaN(delayUT))
                {
                    vesselState.Update(GravityTurner.getVessel);
                    stagestats.OnFixedUpdate();
                    stagestats.RequestUpdate(this);
                    Message = PreflightInfo(GravityTurner.getVessel);
                    delayUT = Planetarium.GetUniversalTime();
                }
            }
        }

        public void CalculateSettings(Vessel vessel, bool UseBest = false)
        {
            float baseFactor = Mathf.Round((float)vessel.mainBody.GeeASL * 100.0f) / 10.0f;
            GravityTurner.Log("Base turn speed factor {0:0.00}", baseFactor);

            // reset the settings to defaults
            if (GameSettings.MODIFIER_KEY.GetKey())
            {
                GravityTurner.launchdb.Clear();
                Parameters.TurnAngle = 10;
                Parameters.StartSpeed = baseFactor * 10.0;
                Parameters.DestinationHeight = (vessel.StableOrbitHeight() + 10000) / 1000;
                GravityTurner.Log("Reset results");
                return;
            }
            GravityTurner.Log("Min orbit height: {0}", vessel.StableOrbitHeight());

            stagestats.ForceSimunlation();
            double TWR = 0;
            for (int i = stagestats.atmoStats.Length - 1; i >= 0; i--)
            {
                double stagetwr = stagestats.atmoStats[i].StartTWR(vessel.mainBody.GeeASL);
                if (stagetwr > 0)
                {
                    if (vessel.StageHasSolidEngine(i))
                        TWR = (stagetwr + stagestats.atmoStats[i].MaxTWR(vessel.mainBody.GeeASL)) / 2.3;
                    else
                        TWR = stagetwr;
                    break;
                }
            }
            if (TWR > 1.2)
            {
                GravityTurner.Log("First guess for TWR > 1.2 {0:0.00}", TWR);
                TWR -= 1.2;
                if (!Parameters.TurnAngle.locked)
                    Parameters.TurnAngle = Mathf.Clamp((float)(10 + TWR * 5), 10, 80);
                if (!Parameters.StartSpeed.locked)
                {
                    Parameters.StartSpeed = Mathf.Clamp((float)(baseFactor * 10 - TWR * baseFactor * 3), baseFactor, baseFactor * 10);
                    if (Parameters.StartSpeed < 10)
                        Parameters.StartSpeed = 10;
                }
            }

            double guessTurn;
            double guessSpeed;
            if (UseBest && GravityTurner.launchdb.BestSettings(out guessTurn, out guessSpeed))
            {
                GravityTurner.Log("UseBest && launchdb.BestSettings");
                if (!Parameters.StartSpeed.locked)
                    Parameters.StartSpeed = guessSpeed;
                if (!Parameters.TurnAngle.locked)
                    Parameters.TurnAngle = guessTurn;
            }
            else if (GravityTurner.launchdb.GuessSettings(out guessTurn, out guessSpeed))
            {
                GravityTurner.Log("GuessSettings");
                if (!Parameters.StartSpeed.locked)
                    Parameters.StartSpeed = guessSpeed;
                if (!Parameters.TurnAngle.locked)
                    Parameters.TurnAngle = guessTurn;
            }

            if (!Parameters.APTimeStart.locked)
                Parameters.APTimeStart = 50;
            if (!Parameters.APTimeFinish.locked)
                Parameters.APTimeFinish = 50;
            if (!Parameters.Sensitivity.locked)
                Parameters.Sensitivity = 0.3;
            if (!Parameters.DestinationHeight.locked)
            {
                Parameters.DestinationHeight = vessel.StableOrbitHeight() + 10000;
                Parameters.DestinationHeight /= 1000;
            }
            if (!Parameters.Roll.locked)
                Parameters.Roll = 0;
            if (!Parameters.Inclination.locked)
                Parameters.Inclination = 0;
            if (!Parameters.PressureCutoff.locked)
                Parameters.PressureCutoff = 1200;
            Parameters.Save();
        }
        public void CalculateLosses(Vessel vessel)
        {
            if (vesselState.mass == 0)
                return;

            double fwdAcceleration = Vector3d.Dot(vessel.acceleration, vesselState.forward.normalized);
            double GravityDrag = Vector3d.Dot(vesselState.gravityForce, -vessel.obt_velocity.normalized);
            double TimeInterval = Time.time - FlyTimeInterval;
            FlyTimeInterval = Time.time;
            HorizontalDistance += Vector3d.Exclude(vesselState.up, vesselState.orbitalVelocity).magnitude * TimeInterval;
            VelocityLost += ((vesselState.thrustCurrent / vesselState.mass) - fwdAcceleration) * TimeInterval;
            DragLoss += vesselState.drag * TimeInterval;
            GravityDragLoss += GravityDrag * TimeInterval;

            double VectorDrag = vesselState.thrustCurrent - Vector3d.Dot(vesselState.thrustVectorLastFrame, vessel.obt_velocity.normalized);
            VectorDrag = VectorDrag / vesselState.mass;
            VectorLoss += VectorDrag * TimeInterval;
            TotalBurn += vesselState.thrustCurrent / vesselState.mass * TimeInterval;

            double GravityDragLossAtAp = GravityDragLoss + vessel.obt_velocity.magnitude - vessel.orbit.getOrbitalVelocityAtUT(vessel.orbit.timeToAp + Planetarium.GetUniversalTime()).magnitude;
            TotalLoss = DragLoss + GravityDragLossAtAp + VectorLoss;
            if (vessel.CriticalHeatPart().CriticalHeat() > MaxHeat)
                MaxHeat = vessel.CriticalHeatPart().CriticalHeat();

            Message = string.Format(
                "Air Drag:\t\t{0:0.00} m/s²\n" +
                "GravityDrag:\t{1:0.00} m/s²\n" +
                "Thrust Vector Drag:\t{5:0.00} m/s²\n" +
                "Air Drag Loss:\t{2:0.00} m/s\n" +
                "Gravity Drag Loss:\t{3:0.00} -> {4:0.00}m/s @AP\n\n" +
                "Total Vector Loss:\t{6:0.00} m/s\n" +
                "Total Loss:\t{7:0.00} m/s\n" +
                "Total Burn:\t\t{8:0.0}\n\n" +
                "Apoapsis:\t\t{9}\n" +
                "Periapsis:\t\t{10}\n" +
                "Inclination:\t\t{11:0.0} °\n",
                vesselState.drag,
                GravityDrag,
                DragLoss,
                GravityDragLoss, GravityDragLossAtAp,
                VectorDrag,
                VectorLoss,
                TotalLoss,
                TotalBurn,
                OrbitExtensions.FormatOrbitInfo(vessel.orbit.ApA, vessel.orbit.timeToAp),
                OrbitExtensions.FormatOrbitInfo(vessel.orbit.PeA, vessel.orbit.timeToPe),
                vessel.orbit.inclination
                );
        }

        public delegate void KillDelegate();
        public event KillDelegate Killed;
        private void OnKilled()
        {
            if (Killed != null)
                Killed();
        }

    }


}
