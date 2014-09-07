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
        private KalmanFilter2D _Kalman;
        private MLApp.MLApp _MatlabApp;

        public InitialEstimator(string iniFile, MLApp.MLApp matlab_app)
        {
            CurrentStageMeasurements = new List<Tuple<Sensor, Measurement>>();
            PreviousStagesMeasurements = new List<List<Tuple<Sensor, Measurement>>>();
            _INIFile = iniFile;
            _MatlabApp = matlab_app;
        }

        public void AddMeasurement(Sensor source, Measurement measurement)
        {
            CurrentStageMeasurements.Add(new Tuple<Sensor, Measurement>(source, measurement));
        }

        public List<ObjectEstimate> ComputeEstimate()
        {
            TrilateratorNoiseless0D Trileration = new TrilateratorNoiseless0D(Variables.GetSensorServerTrilateratorNoiseless0DDistanceTolerance(_INIFile),
                                                                              Variables.GetSensorServerTrilateratorNoiseless0DAveragingAnchor(_INIFile) );
            List<ObjectEstimate> TrilateratedData = Trileration.CalculateEstimates(CurrentStageMeasurements);
            
            if (TrilateratedData.Count != 0 && _Kalman == null)
            {
                ObjectEstimate AverageTrilateration = GetAverageEstimate(TrilateratedData);
                _Kalman = new KalmanFilter2D(_MatlabApp);
                //_Kalman = new KalmanFilter2D(_MatlabApp, s_pos_x: AverageTrilateration.X, s_pos_y: AverageTrilateration.Y, s_vel_x: 0, s_vel_y: 0);
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
            

            CurrEsimate = TrilateratedData;
            CurrAdditionalInfo = Trileration.CurrAdditionalInfo;
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
