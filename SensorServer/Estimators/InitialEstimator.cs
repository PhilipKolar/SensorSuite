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
        public List<ObjectEstimate> TrilateratedEstimates { get; private set; }
        private string _INIFile;

        private List<Tuple<Sensor, Measurement>> CurrentStageMeasurements;
        private List<List<Tuple<Sensor, Measurement>>> PreviousStagesMeasurements;
        private KalmanFilter2D _Kalman;
        private MLApp.MLApp _MatlabApp;
        private bool _UseInitialMeasurementAsState;

        public InitialEstimator(string iniFile, MLApp.MLApp matlab_app)
        {
            CurrentStageMeasurements = new List<Tuple<Sensor, Measurement>>();
            PreviousStagesMeasurements = new List<List<Tuple<Sensor, Measurement>>>();
            _INIFile = iniFile;
            _MatlabApp = matlab_app;
            _UseInitialMeasurementAsState = Variables.GetSensorServerUseInitialMeasurementAsState(iniFile);
        }

        public void AddMeasurement(Sensor source, Measurement measurement)
        {
            CurrentStageMeasurements.Add(new Tuple<Sensor, Measurement>(source, measurement));
        }

        public List<ObjectEstimate> ComputeEstimate()
        {

            Trilaterator Trileration = null;
            string Mode = Variables.GetSensorServerInitialEstimatorTrilaterator(_INIFile);
            if (Mode == "TRILATERATOR_NOISELESS_0D")
            {
                Trileration = new TrilateratorNoiseless0D(Variables.GetSensorServerTrilateratorNoiseless0DDistanceTolerance(_INIFile),
                                                                                   Variables.GetSensorServerTrilateratorNoiseless0DAveragingAnchor(_INIFile));
            }
            else if (Mode == "TRILATERATOR_NOISY_2D")
            {
                Trileration = new TrilateratorNoisy2D(Variables.GetSensorServerTrilateratorGroupingThreshhold(_INIFile),
                                                      Variables.GetSensorServerTrilateratorGridDivison(_INIFile));
            }
            List<ObjectEstimate> TrilateratedData = Trileration.CalculateEstimates(CurrentStageMeasurements);
            
            if (TrilateratedData.Count != 0 && _Kalman == null)
            {
                ObjectEstimate AverageTrilateration = GetAverageEstimate(TrilateratedData); // TODO: Add mode
                if (_UseInitialMeasurementAsState)
                    _Kalman = new KalmanFilter2D(_MatlabApp, s_pos_x: AverageTrilateration.X, s_pos_y: AverageTrilateration.Y, s_vel_x: 0, s_vel_y: 0);
                else 
                    _Kalman = new KalmanFilter2D(_MatlabApp);
            }
            List<ObjectEstimate> KFilteredData = new List<ObjectEstimate>();
            if (TrilateratedData.Count != 0)
            {
                _Kalman.PredictState();
                ObjectEstimate AverageTrilateration = GetAverageEstimate(TrilateratedData);
                KFilteredData.Add(_Kalman.CorrectState(AverageTrilateration.X, AverageTrilateration.Y));
            }
            else if (TrilateratedData.Count == 0 && _Kalman != null)
            {
                //No data, so we use our prediction instead
                ObjectEstimate Prediction = _Kalman.PredictState();
                KFilteredData.Add(_Kalman.CorrectState(Prediction.X, Prediction.Y));
            }

            PreviousStagesMeasurements.Add(CurrentStageMeasurements);
            CurrentStageMeasurements = new List<Tuple<Sensor, Measurement>>();


            CurrEsimate = KFilteredData;
            CurrAdditionalInfo = Trileration.CurrAdditionalInfo;
            TrilateratedEstimates = TrilateratedData;
            return KFilteredData;
        }

        private ObjectEstimate GetAverageEstimate(List<ObjectEstimate> objects)
        {
            double TotalX = 0;
            double TotalY = 0;
            double TotalVX = 0;
            double TotalVY = 0;
            foreach (ObjectEstimate oe in objects)
            {
                TotalX += oe.X;
                TotalY += oe.Y;
                TotalVX += oe.VelocityX;
                TotalVY += oe.VelocityY;
            }
            TotalX /= objects.Count;
            TotalY /= objects.Count;
            TotalVX /= objects.Count;
            TotalVY /= objects.Count;

            return new ObjectEstimate((float)TotalX, (float)TotalY, (float)TotalVX, (float)TotalVY);
        }
    }
}
