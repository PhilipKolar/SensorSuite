using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSNUtil;

namespace SensorServer.Estimators.Tools
{
    class KalmanFilter2D
    {
        public double Dt { get; private set; }
        private double _Measurement_sigma_x;
        private double _Measurement_sigma_y;
        private string _A;
        private string _H;
        private string _R;
        private string _Q;
        private string _P;
        private string _S;
        private string _Z;
        private MLApp.MLApp _Matlab;
        public bool PredictionNext { get; private set; }

        public KalmanFilter2D(MLApp.MLApp matlab_app, double dt = 1, double measurement_sigma_x = 4, double measurement_sigma_y = 4, string r = "[ measurement_sigma_x ^ 2, 0 ; 0, measurement_sigma_y ^ 2 ]",
            string q = "[ dt ^ 4 / 4, 0, dt^3/2, 0 ; 0, dt^4/4, 0, dt^3/2 ; dt^3/2, 0, dt^2, 0 ; 0, dt^3/2, 0, dt^2 ]",
            string p = "[ dt ^ 4 / 4, 0, dt^3/2, 0 ; 0, dt^4/4, 0, dt^3/2 ; dt^3/2, 0, dt^2, 0 ; 0, dt^3/2, 0, dt^2 ]", double s_pos_x = 0,
            double s_pos_y = 0, double s_vel_x = 0, double s_vel_y = 0)
        {
            _A = "[1, 0, dt, 0 ; 0, 1, 0,  dt ; 0, 0, 1, 0 ; 0, 0, 0, 1 ]";
            _H = "[1, 0, 0, 0 ; 0, 1, 0, 0 ]";

            Dt = dt;
            _Measurement_sigma_x = measurement_sigma_x;
            _Measurement_sigma_y = measurement_sigma_y;
            _R = r;
            _Q = q;
            _S = string.Format("[{0} ; {1} ; {2} ; {3}]", s_pos_x, s_pos_y, s_vel_x, s_vel_y);
            _P = p;

            PredictionNext = true;

            Type ActivationContext = Type.GetTypeFromProgID("matlab.application.single");
            _Matlab = matlab_app; //(MLApp.MLApp)Activator.CreateInstance(ActivationContext);
            _InitMatlabVariables();
        }

        private void _InitMatlabVariables()
        {
            _Matlab.Execute(string.Format("dt = {0};", Dt));
            _Matlab.Execute(string.Format("measurement_sigma_x = {0};", _Measurement_sigma_x));
            _Matlab.Execute(string.Format("measurement_sigma_y = {0};", _Measurement_sigma_y));
            _Matlab.Execute(string.Format("A = {0};", _A));
            _Matlab.Execute(string.Format("H = {0};", _H));
            _Matlab.Execute(string.Format("R = {0};", _R));
            _Matlab.Execute(string.Format("Q = {0};", _Q));
            _Matlab.Execute(string.Format("s = {0};", _S));
            _Matlab.Execute(string.Format("P = {0};", _P));
        }

        private string _PredictNextStateMean = string.Format("s = A * s;");
        private string _PredictNextCovariance = string.Format("P = A * P * A' + Q;");

        public ObjectEstimate PredictState()
        {
            if (PredictionNext == false)
                throw new InvalidOperationException("Predict state cannot be called consecutively, please use CorrectState().");
            PredictionNext = false;

            //Prediction
            _Matlab.Execute(_PredictNextStateMean);
            _Matlab.Execute(_PredictNextCovariance);

            return (GetStateVector("s"));
        }

        private string _CalculateKalmanGain = string.Format("K = P * H' / (H * P * H' + R);");
        private string _UpdateCovarianceEstimation = string.Format("P = (eye(4) - K * H) * P;");
        public ObjectEstimate CorrectState(float next_masurement_x, float next_masurement_y)
        {
            if (PredictionNext == true)
                throw new InvalidOperationException("Correct state cannot be called consecutively, please use PredictState().");
            PredictionNext = true;

            //Create measurement matrix in advance
            _Z = string.Format("[{0} ; {1}]", next_masurement_x, next_masurement_y);
            //Correction
            _Matlab.Execute(_CalculateKalmanGain);
            string UpdateStateEstimate = string.Format("s = s + K * ({0} - H * s);", _Z);
            _Matlab.Execute(UpdateStateEstimate);
            _Matlab.Execute(_UpdateCovarianceEstimation);

            return (GetStateVector("s"));
        }

        public ObjectEstimate GetStateVector(string variable_name)
        {
            //Grab array from Matlab and parse the string to retrieve values
            string EstimatedStateMatlabFormatted = _Matlab.Execute(variable_name);
            string EstimatedStateSplit1 = EstimatedStateMatlabFormatted.Split('=')[1].Trim();
            string[] EstimatedStateSplit2 = EstimatedStateSplit1.Split(' ');
            string[] EstimatedState = new string[4];
            int Curr_index = 0;
            foreach (string s in EstimatedStateSplit2)
            {
                if (s != "")
                {
                    EstimatedState[Curr_index++] = s.Trim();
                }
            }
            double Position_x = double.Parse(EstimatedState[0]);
            double Position_y = double.Parse(EstimatedState[1]);
            double Velocity_x = double.Parse(EstimatedState[2]);
            double Velocity_y = double.Parse(EstimatedState[3]);

            ObjectEstimate EstimatedStateToReturn = new ObjectEstimate((float)Position_x, (float)Position_y, (float)Velocity_x, (float)Velocity_y);
            return EstimatedStateToReturn;
        }
    }
}
