using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSNUtil;

namespace SensorServer.Estimators.Tools
{
    class TrilateratorNoiseless0D : Trilaterator
    {
        public double DistanceTolerance { get; private set; }
        public List<ObjectEstimate> CurrAdditionalInfo { get; private set; }
        public bool AveragingAnchor { get; private set; }
        public TrilateratorNoiseless0D(double distanceTolerance, bool averagingAnchor)
        {
            DistanceTolerance = distanceTolerance;
            AveragingAnchor = averagingAnchor;
        }

        public override List<ObjectEstimate> CalculateEstimates(List<Tuple<Sensor, Measurement>> Measurements)
        {
            CurrAdditionalInfo = new List<ObjectEstimate>();

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
                        else
                            CurrAdditionalInfo.Add(oe);
                    }
                }
            }

            List<ObjectEstimate> ProximityFilteredEstimates = _RunProximityFilter(MasterList);
            return ProximityFilteredEstimates;
        }


        private List<ObjectEstimate> _RunProximityFilter(List<ObjectEstimate> dataToFilter)
        {
            //TODO: Add a limit to how much each anchor can drift. Probably change Tuple to a container sub-class
            List<Tuple<ObjectEstimate, int>> Anchors = new List<Tuple<ObjectEstimate, int>>(); //Thresholds are measured with respect to these anchors, which are assigned when a new measurement is found that doesn't belong to another anchor
                                                                                               //int represents the amount of measurements an anchor represents (used for mean calculation).
            foreach (ObjectEstimate oe in dataToFilter)
            {
                Tuple<ObjectEstimate, int> CurrAnchor = _GetAnchor(oe, Anchors, DistanceTolerance);
                Tuple<ObjectEstimate, int> CurrAnchor2 = _GetAnchor(oe, Anchors, DistanceTolerance);
                if (CurrAnchor == null) //No anchors in range, assignment current candidate as a new anchor
                {
                    Anchors.Add(new Tuple<ObjectEstimate, int>(oe, 1));
                }
                else
                {
                    if (AveragingAnchor) //Average current position of anchor with new candidate
                    {
                        CurrAnchor.Item1.X = (float)UpdateAverage(CurrAnchor.Item1.X, CurrAnchor.Item2, oe.X);
                        CurrAnchor.Item1.Y = (float)UpdateAverage(CurrAnchor.Item1.Y, CurrAnchor.Item2, oe.Y);
                    }
                }
            }

            List<ObjectEstimate> AnchorsToReturn = new List<ObjectEstimate>();
            foreach (Tuple<ObjectEstimate, int> anchor in Anchors) //Convert Anchors to a list suitable for returning
            {
                AnchorsToReturn.Add(anchor.Item1);
            }
            return AnchorsToReturn;
        }

        private double UpdateAverage(double currentAverage, int previousCount, double newNumber)
        {
            if (previousCount == 0)
                return newNumber;
            else if (previousCount < 0)
                throw new ArgumentOutOfRangeException("Average count cannot be negative");

            double DivisionRemoved = currentAverage * previousCount;
            double NewNumberInserted = DivisionRemoved + newNumber;
            double DivisionReapplied = NewNumberInserted / (previousCount + 1);
            return DivisionReapplied;
        }

        private Tuple<ObjectEstimate, int> _GetAnchor(ObjectEstimate currCandidate, List<Tuple<ObjectEstimate, int>> Anchors, double distanceThreshold)
        {
            foreach (Tuple<ObjectEstimate, int> anchorTuple in Anchors)
            {
                if (_DistanceBetween(currCandidate, anchorTuple.Item1) <= distanceThreshold)
                {
                    return anchorTuple;
                }
            }
            return null;
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
