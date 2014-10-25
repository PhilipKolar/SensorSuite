using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSNUtil;
using LibSharpTave;

namespace SensorServer.Estimators.Tools
{
    class KalmanFilter2DOctave
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
        private Octave _Oct;
        //private MLApp.MLApp _Matlab;
        public bool PredictionNext { get; private set; }

        public KalmanFilter2DOctave(Octave oct, double dt = 1, double measurement_sigma_x = 4, double measurement_sigma_y = 4, string r = "[ measurement_sigma_x ^ 2, 0 ; 0, measurement_sigma_y ^ 2 ]",
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
            _Oct = oct;
            _InitMatlabVariables();
        }

        private void _InitMatlabVariables()
        {

            _Oct.ExecuteCommand(string.Format("dt = {0};", Dt));
            _Oct.ExecuteCommand(string.Format("measurement_sigma_x = {0};", _Measurement_sigma_x));
            _Oct.ExecuteCommand(string.Format("measurement_sigma_y = {0};", _Measurement_sigma_y));
            _Oct.ExecuteCommand(string.Format("A = {0};", _A));
            _Oct.ExecuteCommand(string.Format("H = {0};", _H));
            _Oct.ExecuteCommand(string.Format("R = {0};", _R));
            _Oct.ExecuteCommand(string.Format("Q = {0};", _Q));
            _Oct.ExecuteCommand(string.Format("s = {0};", _S));
            _Oct.ExecuteCommand(string.Format("P = {0};", _P));
        }

        private string _PredictNextStateMean = string.Format("s = A * s;");
        private string _PredictNextCovariance = string.Format("P = A * P * A' + Q;");

        public ObjectEstimate PredictState()
        {
            if (PredictionNext == false)
                throw new InvalidOperationException("Predict state cannot be called consecutively, please use CorrectState().");
            PredictionNext = false;

            //Prediction
            _Oct.ExecuteCommand(_PredictNextStateMean);
            _Oct.ExecuteCommand(_PredictNextCovariance);

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
            _Oct.ExecuteCommand(_CalculateKalmanGain);
            string UpdateStateEstimate = string.Format("s = s + K * ({0} - H * s);", _Z);
            _Oct.ExecuteCommand(UpdateStateEstimate);
            _Oct.ExecuteCommand(_UpdateCovarianceEstimation);

            return (GetStateVector("s"));
        }

        public ObjectEstimate GetStateVector(string variable_name)
        {
            _Oct.ExecuteCommand(string.Format("{0} = {0}';", variable_name));
            double[] Variable = _Oct.GetVector(variable_name);
            _Oct.ExecuteCommand(string.Format("{0} = {0}';", variable_name));

            ObjectEstimate EstimatedStateToReturn = new ObjectEstimate((float)Variable[0], (float)Variable[1], (float)Variable[2], (float)Variable[3]);
            return EstimatedStateToReturn;
        }
    }
}
