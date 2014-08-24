using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSNUtil;
using SensorServer.Estimators.Tools;

namespace SensorServer.Estimators
{
    /// <summary>
    /// First/Initial attempt at an estimator
    /// </summary>
    class InitialEstimator : IEstimator
    {
        private List<Tuple<Sensor, Measurement>> CurrentStageMeasurements;
        private List<List<Tuple<Sensor, Measurement>>> PreviousStagesMeasurements;

        public InitialEstimator()
        {
            CurrentStageMeasurements = new List<Tuple<Sensor, Measurement>>();
            PreviousStagesMeasurements = new List<List<Tuple<Sensor, Measurement>>>();
        }

        public void AddMeasurement(Sensor source, Measurement measurement)
        {
            CurrentStageMeasurements.Add(new Tuple<Sensor, Measurement>(source, measurement));
        }

        public List<ObjectEstimate> ComputeEstimate()
        {
            TrilateratorNoiseless0D Trileration = new TrilateratorNoiseless0D();
            List<ObjectEstimate> TrilateratedData = Trileration.CalculateEstimates(CurrentStageMeasurements);

            PreviousStagesMeasurements.Add(CurrentStageMeasurements);
            CurrentStageMeasurements= new List<Tuple<Sensor, Measurement>>();

            //TODO: Track/filter/etc

            return TrilateratedData;
        }
    }
}
