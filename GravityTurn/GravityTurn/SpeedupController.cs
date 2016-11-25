using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GravityTurn
{
    public static class SpeedupController
    {
        static int previousTimeWarp = 0;
        static public void StoreTimeWarp()
        {
            previousTimeWarp = TimeWarp.CurrentRateIndex;
        }

        static public void RestoreTimeWarp()
        {
            if (previousTimeWarp != 0)
            {
                TimeWarp.fetch.Mode = TimeWarp.Modes.LOW;
                TimeWarp.SetRate(previousTimeWarp, false);
            }
            previousTimeWarp = 0;
        }

        static public void ApplySpeedup(int rate)
        {
            if (GravityTurner.Parameters.EnableSpeedup)
            {
                TimeWarp.fetch.Mode = TimeWarp.Modes.LOW;
                TimeWarp.SetRate(previousTimeWarp < rate ? rate : previousTimeWarp, false);
            }
        }

        static public void StopSpeedup()
        {
            TimeWarp.SetRate(0, false);
        }
    }
}
