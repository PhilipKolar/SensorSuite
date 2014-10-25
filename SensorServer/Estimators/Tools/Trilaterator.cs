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
        public abstract List<ObjectEstimate> CurrAdditionalInfo { get; protected set; }

        protected virtual bool _IsWithinVision(ObjectEstimate candidate, Sensor[] sensorList)
        {
            double[] arg1 = new double[] {candidate.X, candidate.Y};
            return _IsWithinVision(arg1, sensorList);
        }

        protected virtual int _VisibleSensorCount(ObjectEstimate candidate, Sensor[] sensorList)
        {
            int SensorCount = 0;
            foreach (Sensor s in sensorList)
            {
                if (candidate.X == s.X && candidate.Y == s.Y)
                    continue;

                double candidatePhi = Math.Abs(_ToDeg(Math.Atan((s.Y - candidate.Y) / (s.X - candidate.X))));
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

                float SensorAngle1 = ((s.Phi + s.Theta / 2) + 360) % 360;
                float SensorAngle2 = ((s.Phi - s.Theta / 2) + 360) % 360;
                if (candidatePhi >= 0 && candidatePhi <= 90)
                {
                    candidatePhi += 90;
                    SensorAngle1 += 90;
                    SensorAngle2 += 90;
                    SensorAngle1 %= 360;
                    SensorAngle2 %= 360;
                }
                else if (candidatePhi >= 270 && candidatePhi <= 360)
                {
                    candidatePhi -= 90;
                    SensorAngle1 -= 90;
                    SensorAngle2 -= 90;
                    SensorAngle1 = (SensorAngle1 + 360) % 360;
                    SensorAngle2 = (SensorAngle2 + 360) % 360;
                }
                bool Pass = false;
                if (candidatePhi <= SensorAngle1 && candidatePhi >= SensorAngle2)
                    Pass = true;
                //else if (candidatePhi <= SensorAngle1 + 360 && candidatePhi >= SensorAngle2)
                //    Pass = true;
                //else if (candidatePhi <= SensorAngle1 && candidatePhi >= SensorAngle2 + 360)
                //    Pass = true;
                //else if (candidatePhi <= SensorAngle1 + 360 && candidatePhi >= SensorAngle2 + 360)
                //    Pass = true;
                if (Pass)
                    SensorCount++;
            }
            return SensorCount;
        }

        protected virtual bool _IsWithinVision(double[] candidate, Sensor[] sensorList)
        {
            if (candidate.Length != 2)
                throw new ArgumentOutOfRangeException("candidate must be of length 2, [x, y]");

            foreach (Sensor s in sensorList)
            {
                if (candidate[0] == s.X && candidate[1] == s.Y)
                    continue;

                double candidatePhi = Math.Abs(_ToDeg(Math.Atan((s.Y - candidate[1]) / (s.X - candidate[0]))));
                if (candidate[0] <= s.X && candidate[1] >= s.Y)
                {
                    candidatePhi = 180 - candidatePhi;
                }
                else if (candidate[0] <= s.X && candidate[1] < s.Y)
                {
                    candidatePhi = 180 + candidatePhi;
                }
                else if (candidate[0] >= s.X && candidate[1] < s.Y)
                {
                    candidatePhi = 360 - candidatePhi;
                }

                float SensorAngle1 = ((s.Phi - s.Theta / 2) + 360) % 360;
                float SensorAngle2 = ((s.Phi + s.Theta / 2) + 360) % 360;
                if (candidatePhi >= 0 && candidatePhi <= 90)
                {
                    candidatePhi += 90;
                    SensorAngle1 += 90;
                    SensorAngle2 += 90;
                    SensorAngle1 %= 360;
                    SensorAngle2 %= 360;
                }
                else if (candidatePhi >= 270 && candidatePhi <= 360)
                {
                    candidatePhi -= 90;
                    SensorAngle1 -= 90;
                    SensorAngle2 -= 90;
                    SensorAngle1 = (SensorAngle1 + 360) % 360;
                    SensorAngle2 = (SensorAngle2 + 360) % 360;
                }
                bool Pass = false;
                if (candidatePhi <= SensorAngle1 || candidatePhi >= SensorAngle2)
                    Pass = true;
                if (Pass == true)
                    return false;
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
