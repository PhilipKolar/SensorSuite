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
        public List<ObjectEstimate> CurrEsimate { get; private set; }
        public List<ObjectEstimate> CurrAdditionalInfo { get; private set; }
        private string _INIFile;

        private List<Tuple<Sensor, Measurement>> CurrentStageMeasurements;
        private List<List<Tuple<Sensor, Measurement>>> PreviousStagesMeasurements;

        public InitialEstimator(string iniFile)
        {
            CurrentStageMeasurements = new List<Tuple<Sensor, Measurement>>();
            PreviousStagesMeasurements = new List<List<Tuple<Sensor, Measurement>>>();
            _INIFile = iniFile;
        }

        public void AddMeasurement(Sensor source, Measurement measurement)
        {
            CurrentStageMeasurements.Add(new Tuple<Sensor, Measurement>(source, measurement));
        }

        public List<ObjectEstimate> ComputeEstimate()
        {
            TrilateratorNoiseless0D Trileration = new TrilateratorNoiseless0D(Variables.GetSensorServerTrilateratorNoiseless0DDistanceTolerance(_INIFile));
            List<ObjectEstimate> TrilateratedData = Trileration.CalculateEstimates(CurrentStageMeasurements);

            PreviousStagesMeasurements.Add(CurrentStageMeasurements);
            CurrentStageMeasurements = new List<Tuple<Sensor, Measurement>>();
            //TODO: Track/filter/etc

            CurrEsimate = TrilateratedData;
            CurrAdditionalInfo = Trileration.CurrAdditionalInfo;
            return TrilateratedData;
        }
    }
}
