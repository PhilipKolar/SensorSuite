using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WSNUtil;

namespace DisplayServer
{
    class ErrorCalculator
    {
        private Dictionary<int, float> _Error; //int: Message number, float: error value
        private float _MinError;
        private float _MaxError;
        private float _MeanError;
        private Dictionary<int, float> _SquaredError;
        private float _SquaredMinError;
        private float _SquaredMaxError;
        private float _SquaredMeanError;
        private int _NoEstimateCount = 0;

        public ErrorCalculator()
        {
            _Error = new Dictionary<int, float>();
            _SquaredError = new Dictionary<int, float>();
            _MinError = float.MaxValue;
            _MaxError = float.MinValue;
            _SquaredMinError = float.MaxValue;
            _SquaredMaxError = float.MinValue;
        }

        public void AddNewMeasure(int MessageCount, ObjectEstimate estimateState, ObjectEstimate trueState)
        {
            if (trueState == null)
                return;
            if (estimateState == null)
            {
                _NoEstimateCount++;
                return;
            }

            float CurrError = (float)Math.Sqrt(Math.Pow(estimateState.X - trueState.X, 2) + Math.Pow(estimateState.Y - trueState.Y, 2));
            _MinError = CurrError < _MinError ? CurrError : _MinError;
            _MaxError = CurrError > _MaxError ? CurrError : _MaxError;
            float CurrMeanError = CurrError;
            foreach (KeyValuePair<int, float> kvp in _Error) //TODO: Change the mean calculation to run in O(1) time instead of O(n)
                CurrMeanError += kvp.Value;
            _MeanError = CurrMeanError / _Error.Count;
            _Error.Add(MessageCount, CurrError);

            float CurrSquaredError = (float) (Math.Pow(estimateState.X - trueState.X, 2) + Math.Pow(estimateState.Y - trueState.Y, 2));
            _SquaredMinError = CurrSquaredError < _SquaredMinError ? CurrSquaredError : _SquaredMinError;
            _SquaredMaxError = CurrSquaredError > _SquaredMaxError ? CurrSquaredError : _SquaredMaxError;
            float CurrSquaredMeanError = CurrSquaredError;
            foreach (KeyValuePair<int, float> kvp in _SquaredError) //TODO: Change the mean calculation to run in O(1) time instead of O(n)
                CurrSquaredMeanError += kvp.Value;
            _SquaredMeanError = CurrSquaredMeanError / _SquaredError.Count;
            _SquaredError.Add(MessageCount, CurrSquaredError);
        }

        public void AddNewMeasure(int MessageCount, ObjectEstimate[] estimateState, ObjectEstimate[] trueState)
        {
            float TotalX = 0f, TotalY = 0f, TotalVX = 0f, TotalVY = 0f;
            ObjectEstimate Estimate;
            if (estimateState != null && estimateState.Length != 0)
            {
                foreach (ObjectEstimate oe in estimateState)
                {
                    TotalX += oe.X;
                    TotalY += oe.Y;
                    TotalVX += oe.VelocityX;
                    TotalVY += oe.VelocityY;
                }
                Estimate = new ObjectEstimate(TotalX / estimateState.Length, TotalY / estimateState.Length, TotalVX / estimateState.Length, TotalVY / estimateState.Length);
            }
            else
                Estimate = null;

            ObjectEstimate Real;
            if (trueState != null && trueState.Length != 0)
            {
                TotalX = 0f;
                TotalY = 0f;
                TotalVX = 0f;
                TotalVY = 0f;
                foreach (ObjectEstimate oe in trueState)
                {
                    TotalX += oe.X;
                    TotalY += oe.Y;
                    TotalVX += oe.VelocityX;
                    TotalVY += oe.VelocityY;
                }
                Real = new ObjectEstimate(TotalX / trueState.Length, TotalY / trueState.Length, TotalVX / trueState.Length, TotalVY / trueState.Length);
            }
            else
                Real = null;

            AddNewMeasure(MessageCount, Estimate, Real);
        }

        public void SaveToFile(string FileName)
        {
            StreamWriter Writer = new StreamWriter(FileName);
            Writer.WriteLine("Error for results calculated at {0}", DateTime.Now);
            Writer.WriteLine("");
            Writer.WriteLine("Sample count:      {0}", _Error.Count);
            Writer.WriteLine("No estimate count: {0}", _NoEstimateCount);
            Writer.WriteLine("Real state count:  {0} (i.e. Sample count + No estimate count)", _Error.Count + _NoEstimateCount);
            Writer.WriteLine("");
            Writer.WriteLine("*** UNMODIFIED ERROR ***");
            Writer.WriteLine("Mean: {0}cm{1}Min:  {2}cm{1}Max:  {3}cm", _MeanError, Environment.NewLine, _MinError, _MaxError);
            Writer.WriteLine("*** SQUARED ERROR ***");
            Writer.WriteLine("Mean: {0}cm^2{1}Min:  {2}cm^2{1}Max:  {3}cm^2", _SquaredMeanError, Environment.NewLine, _SquaredMinError, _SquaredMaxError);
            Writer.Close();
        }
    }
}
