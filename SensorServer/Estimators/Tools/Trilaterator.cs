using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSNUtil;

namespace SensorServer.Estimators.Tools
{
    public abstract class Trilaterator
    {
        public abstract List<ObjectEstimate> CalculateEstimates(List<Tuple<Sensor, Measurement>> Measurements);

        protected virtual bool _IsWithinVision(ObjectEstimate candidate, Sensor[] sensorList)
        {
            foreach (Sensor s in sensorList)
            {
                if (candidate.X == s.X && candidate.Y == s.Y)
                    continue;

                double candidatePhi = _ToDeg(Math.Atan((s.Y - candidate.Y) / (s.X - candidate.X)));
                if (candidate.X <= s.X && candidate.Y >= s.Y)
                {
                    candidatePhi = 180 - candidatePhi;
                }
                else if (candidate.X <= s.X && candidate.Y < s.Y)
                {
                    candidatePhi = 180 + candidatePhi;
                }
                else if (candidate.X >= s.X && candidate.Y < s.Y)
                {
                    candidatePhi = 360 - candidatePhi;
                }

                if (candidatePhi > s.Phi + s.Theta / 2 ||
                    candidatePhi < s.Phi - s.Theta / 2)
                {
                    return false;
                }
            }
            return true;
        }

        protected virtual double _ToDeg(double angle)
        {
            return angle * (180.0 / Math.PI);
        }

        protected virtual bool _HasDuplicateSolutions(double[,] testArray)
        {
            if (testArray[0, 0] == testArray[1, 0] && testArray[0, 1] == testArray[1, 1])
                return true;
            return false;
        }

        protected virtual double _DistanceBetween(ObjectEstimate point1, ObjectEstimate point2)
        {
            double PreRoot = Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2);
            return Math.Sqrt(PreRoot);
        }
    }
}
