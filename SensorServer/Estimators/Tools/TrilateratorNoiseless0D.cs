using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSNUtil;

namespace SensorServer.Estimators.Tools
{
    class TrilateratorNoiseless0D : Trilaterator
    {
        public TrilateratorNoiseless0D()
        { }

        public override List<ObjectEstimate> CalculateEstimates(List<Tuple<Sensor, Measurement>> Measurements)
        {
            List<ObjectEstimate> MasterList = new List<ObjectEstimate>();
            for (int Sensor1Index = 0; Sensor1Index < Measurements.Count; Sensor1Index++)
            {
                for (int Sensor2Index = Sensor1Index + 1; Sensor2Index < Measurements.Count; Sensor2Index++)
                {
                    ObjectEstimate[] Intercepts = _GetIntercepts(Measurements[Sensor1Index], Measurements[Sensor2Index]);
                    foreach (ObjectEstimate oe in Intercepts)
                    {
                        if (_IsWithinVision(oe, new Sensor[] { Measurements[Sensor1Index].Item1, Measurements[Sensor2Index].Item1 }))
                            MasterList.Add(oe);
                    }
                }
            }

            List<ObjectEstimate> ProximityFilteredEstimates = _RunProximityFilter(MasterList, 10); //TODO: Add threshold to INI file
            return ProximityFilteredEstimates;
        }


        private List<ObjectEstimate> _RunProximityFilter(List<ObjectEstimate> dataToFilter, double distanceThreshold)
        {
            List<ObjectEstimate> Anchors = new List<ObjectEstimate>(); //Thresholds are measured with respect to these anchors, which are assigned when a new measurement is found that doesn't belong to another anchor
            foreach (ObjectEstimate oe in dataToFilter)
            {
                if (_IsNewAnchor(oe, Anchors, distanceThreshold))
                {
                    Anchors.Add(oe);
                }
            }

            return Anchors;
        }

        private bool _IsNewAnchor(ObjectEstimate currCandidate, List<ObjectEstimate> Anchors, double distanceThreshold)
        {
            foreach (ObjectEstimate anchor in Anchors)
            {
                if (_DistanceBetween(currCandidate, anchor) <= distanceThreshold)
                {
                    return false;
                }
            }
            return true;
        }


        private ObjectEstimate[] _GetIntercepts(Tuple<Sensor, Measurement> measurementPair1, Tuple<Sensor, Measurement> measurementPair2)
        {
            Measurement measurement1 = measurementPair1.Item2;
            Measurement measurement2 = measurementPair2.Item2;
            Sensor sensor1 = measurementPair1.Item1;
            Sensor sensor2 = measurementPair2.Item1;

            ObjectEstimate[] Results;
            //Return nothing if either sensor timed out
            if (measurement1.Distance < 0 || measurement2.Distance < 0)
                return Results = new ObjectEstimate[0];

            //TODO: Error checking for divide by 0
            double c = Math.Pow(measurement1.Distance, 2) - Math.Pow(measurement2.Distance, 2) - Math.Pow(sensor1.X, 2) + Math.Pow(sensor2.X, 2) - Math.Pow(sensor1.Y, 2) + Math.Pow(sensor2.Y, 2);
            double d = 2 * (sensor1.Y - sensor2.Y);
            double e = -2 * (sensor1.X - sensor2.X);
            double f = Math.Pow(measurement1.Distance, 2) - (Math.Pow(c, 2) / Math.Pow(e, 2)) + (2 * sensor1.X * c / e) - Math.Pow(sensor1.X, 2) - Math.Pow(sensor1.Y, 2);
            double g = (Math.Pow(e, 2) + Math.Pow(d, 2)) / Math.Pow(e, 2);
            double h = (2 * c * d - 2 * sensor1.X * d * e - 2 * sensor1.Y * Math.Pow(e, 2)) / Math.Pow(e, 2);

            int currIndex = 0;
            double[,] solutions = new double[2, 2]; //Up to 2 real solutions with index 0 = x and index 1 = y

            solutions[currIndex, 1] = (-h + Math.Sqrt(Math.Pow(h, 2) + 4 * g * f)) / (2 * g);
            solutions[currIndex, 0] = (c + d * solutions[currIndex, 1]) / e;
            if (double.IsNaN(solutions[currIndex, 0]) == false)
                currIndex++;

            solutions[currIndex, 1] = (-h - Math.Sqrt(Math.Pow(h, 2) + 4 * g * f)) / (2 * g);
            solutions[currIndex, 0] = (c + d * solutions[currIndex, 1]) / e;
            if (double.IsNaN(solutions[currIndex, 0]) == false)
                currIndex++;

            if (currIndex == 0)
                return new ObjectEstimate[0];
            if (currIndex == 1 || _HasDuplicateSolutions(solutions)) //Return an array of length 1 if there's only one real intercept.
                return new ObjectEstimate[1] { new ObjectEstimate((float)solutions[0, 0], (float)solutions[0, 1], 0f, 0f) };
            return new ObjectEstimate[2] { new ObjectEstimate((float)solutions[0, 0], (float)solutions[0, 1], 0f, 0f),
                                           new ObjectEstimate((float)solutions[1, 0], (float)solutions[1, 1], 0f, 0f) };
        }
    }
}
