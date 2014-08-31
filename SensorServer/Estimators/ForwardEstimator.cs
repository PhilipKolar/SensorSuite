using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSNUtil;

namespace SensorServer.Estimators
{
    /// <summary>
    /// Extremely basic estimator that places measurements directly in front of a sensor and performs no measurement merging
    /// Useful for displaying raw measurement data
    /// </summary>
    public class ForwardEstimator : IEstimator
    {
        public List<ObjectEstimate> CurrEsimate { get; private set; }
        public List<ObjectEstimate> CurrAdditionalInfo { get { throw new NotImplementedException("ForwardEstimator does not provide additional information"); } }

        private List<Tuple<Sensor, Measurement>> CurrentStageMeasurements;

        public ForwardEstimator()
        {
            CurrentStageMeasurements = new List<Tuple<Sensor, Measurement>>();
        }

        public void AddMeasurement(Sensor source, Measurement measurement)
        {
            CurrentStageMeasurements.Add(new Tuple<Sensor, Measurement>(source, measurement));
        }

        public List<ObjectEstimate> ComputeEstimate()
        {
            List<ObjectEstimate> NextEstimate = new List<ObjectEstimate>();
            foreach (Tuple<Sensor, Measurement> measurementPair in CurrentStageMeasurements)
            {
                float EstimatedXPosition = measurementPair.Item1.X + (float)Math.Cos(measurementPair.Item1.Phi * Math.PI / 180) * measurementPair.Item2.Distance;
                float EstimatedYPosition = measurementPair.Item1.Y + (float)Math.Sin(measurementPair.Item1.Phi * Math.PI / 180) * measurementPair.Item2.Distance;

                ObjectEstimate CurrEstimate = new ObjectEstimate(EstimatedXPosition, EstimatedYPosition, 0f, 0f);
                NextEstimate.Add(CurrEstimate);
            }

            CurrEsimate = NextEstimate;
            CurrentStageMeasurements = new List<Tuple<Sensor, Measurement>>();
            return NextEstimate;
        }
    }
}
