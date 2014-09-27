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
        private int _NoEstimateCount;

        private Dictionary<int, float> _ErrorOnlyTri;
        private float _MinErrorOnlyTri;
        private float _MaxErrorOnlyTri;
        private float _MeanErrorOnlyTri;
        private Dictionary<int, float> _SquaredErrorOnlyTri;
        private float _SquaredMinErrorOnlyTri;
        private float _SquaredMaxErrorOnlyTri;
        private float _SquaredMeanErrorOnlyTri;
        private int _NoEstimateCountOnlyTri;

        public ErrorCalculator()
        {
            _Error = new Dictionary<int, float>();
            _SquaredError = new Dictionary<int, float>();
            _MinError = float.MaxValue;
            _MaxError = float.MinValue;
            _SquaredMinError = float.MaxValue;
            _SquaredMaxError = float.MinValue;
            _NoEstimateCount = 0;

            _ErrorOnlyTri = new Dictionary<int, float>();
            _SquaredErrorOnlyTri = new Dictionary<int, float>();
            _MinErrorOnlyTri = float.MaxValue;
            _MaxErrorOnlyTri = float.MinValue;
            _SquaredMinErrorOnlyTri = float.MaxValue;
            _SquaredMaxErrorOnlyTri = float.MinValue;
            _NoEstimateCountOnlyTri = 0;
        }

        public void AddNewMeasure(int MessageCount, ObjectEstimate trilateration, ObjectEstimate estimateState, ObjectEstimate trueState)
        {
            if (trueState == null)
                return;
            if (estimateState == null)
            {
                _NoEstimateCount++;
                if (trilateration != null)
                    _NoEstimateCountOnlyTri++;
                return;
            }

            //Unmodified error
            float CurrError = (float)Math.Sqrt(Math.Pow(estimateState.X - trueState.X, 2) + Math.Pow(estimateState.Y - trueState.Y, 2));
            _MinError = CurrError < _MinError ? CurrError : _MinError;
            _MaxError = CurrError > _MaxError ? CurrError : _MaxError;
            float CurrMeanError = CurrError;
            foreach (KeyValuePair<int, float> kvp in _Error) //TODO: Change the mean calculation to run in O(1) time instead of O(n)
                CurrMeanError += kvp.Value;
            _MeanError = CurrMeanError / (_Error.Count + 1);
            _Error.Add(MessageCount, CurrError);

            //Unmodified error - only trilateration
            if (trilateration != null)
            {
                float CurrErrorOnlyTri = CurrError;
                _MinErrorOnlyTri = CurrErrorOnlyTri < _MinErrorOnlyTri ? CurrErrorOnlyTri : _MinErrorOnlyTri;
                _MaxErrorOnlyTri = CurrErrorOnlyTri > _MaxErrorOnlyTri ? CurrErrorOnlyTri : _MaxErrorOnlyTri;
                float CurrMeanErrorOnlyTri = CurrErrorOnlyTri;
                foreach (KeyValuePair<int, float> kvp in _ErrorOnlyTri) //TODO: Change the mean calculation to run in O(1) time instead of O(n)
                    CurrMeanErrorOnlyTri += kvp.Value;
                _MeanErrorOnlyTri = CurrMeanErrorOnlyTri / (_ErrorOnlyTri.Count + 1);
                _ErrorOnlyTri.Add(MessageCount, CurrErrorOnlyTri);
            }

            //RMSE
            float CurrSquaredError = (float) (Math.Pow(estimateState.X - trueState.X, 2) + Math.Pow(estimateState.Y - trueState.Y, 2));
            _SquaredMinError = CurrSquaredError < _SquaredMinError ? CurrSquaredError : _SquaredMinError;
            _SquaredMaxError = CurrSquaredError > _SquaredMaxError ? CurrSquaredError : _SquaredMaxError;
            float CurrSquaredMeanError = CurrSquaredError;
            foreach (KeyValuePair<int, float> kvp in _SquaredError) //TODO: Change the mean calculation to run in O(1) time instead of O(n)
                CurrSquaredMeanError += kvp.Value;
            _SquaredMeanError = CurrSquaredMeanError / (_SquaredError.Count + 1);
            _SquaredError.Add(MessageCount, CurrSquaredError);

            //RMSE - only trilateration
            if (trilateration != null)
            {
                float CurrSquaredErrorOnlyTri = CurrSquaredError;
                _SquaredMinErrorOnlyTri = CurrSquaredErrorOnlyTri < _SquaredMinErrorOnlyTri ? CurrSquaredErrorOnlyTri : _SquaredMinErrorOnlyTri;
                _SquaredMaxErrorOnlyTri = CurrSquaredErrorOnlyTri > _SquaredMaxErrorOnlyTri ? CurrSquaredErrorOnlyTri : _SquaredMaxErrorOnlyTri;
                float CurrSquaredMeanErrorOnlyTri = CurrSquaredErrorOnlyTri;
                foreach (KeyValuePair<int, float> kvp in _SquaredErrorOnlyTri) //TODO: Change the mean calculation to run in O(1) time instead of O(n)
                    CurrSquaredMeanErrorOnlyTri += kvp.Value;
                _SquaredMeanErrorOnlyTri = CurrSquaredMeanErrorOnlyTri / (_SquaredErrorOnlyTri.Count + 1);
                _SquaredErrorOnlyTri.Add(MessageCount, CurrSquaredErrorOnlyTri);
            }

        }

        public void AddNewMeasure(int MessageCount, ObjectEstimate[] trilateration, ObjectEstimate[] estimateState, ObjectEstimate[] trueState) //This method averages each array and calls the single objectestimate AddNewMeasurement() instead
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

            ObjectEstimate Trilateration;
            if (trilateration != null && trilateration.Length != 0)
            {
                TotalX = 0f;
                TotalY = 0f;
                TotalVX = 0f;
                TotalVY = 0f;
                foreach (ObjectEstimate oe in trilateration)
                {
                    TotalX += oe.X;
                    TotalY += oe.Y;
                    TotalVX += oe.VelocityX;
                    TotalVY += oe.VelocityY;
                }
                Trilateration = new ObjectEstimate(TotalX / trilateration.Length, TotalY / trilateration.Length, TotalVX / trilateration.Length, TotalVY / trilateration.Length);
            }
            else
                Trilateration = null;

            AddNewMeasure(MessageCount, Trilateration, Estimate, Real);
        }

        public void SaveToFile(string FileName)
        {
            StreamWriter Writer = new StreamWriter(FileName);
            Writer.WriteLine("Error for results calculated at {0}", DateTime.Now);
            Writer.WriteLine();
            Writer.WriteLine("Sample count:      {0}", _Error.Count);
            Writer.WriteLine("No estimate count: {0}", _NoEstimateCount);
            Writer.WriteLine("Real state count:  {0} (i.e. Sample count + No estimate count)", _Error.Count + _NoEstimateCount);
            Writer.WriteLine();
            Writer.WriteLine("*** UNMODIFIED ERROR ***");
            Writer.WriteLine("Mean: {0}cm{1}Min:  {2}cm{1}Max:  {3}cm", _MeanError, Environment.NewLine, _MinError, _MaxError);
            Writer.WriteLine("*** RMSE ***");
            Writer.WriteLine("Mean: {0}cm{1}Min:  {2}cm{1}Max:  {3}cm", Math.Sqrt(_SquaredMeanError), Environment.NewLine, Math.Sqrt(_SquaredMinError), Math.Sqrt(_SquaredMaxError));
            Writer.WriteLine();
            Writer.WriteLine("======================================");
            Writer.WriteLine();
            Writer.WriteLine("Results for when only considering estimates when a trilateration is available");
            Writer.WriteLine();
            Writer.WriteLine("Sample count:      {0}", _ErrorOnlyTri.Count);
            Writer.WriteLine("No estimate count: {0}", _NoEstimateCountOnlyTri);
            Writer.WriteLine("Real state count:  {0} (i.e. Sample count + No estimate count)", _ErrorOnlyTri.Count + _NoEstimateCountOnlyTri);
            Writer.WriteLine();
            Writer.WriteLine("*** UNMODIFIED ERROR ***");
            Writer.WriteLine("Mean: {0}cm{1}Min:  {2}cm{1}Max:  {3}cm", _MeanErrorOnlyTri, Environment.NewLine, _MinErrorOnlyTri, _MaxErrorOnlyTri);
            Writer.WriteLine("*** RMSE ***");
            Writer.WriteLine("Mean: {0}cm{1}Min:  {2}cm{1}Max:  {3}cm", Math.Sqrt(_SquaredMeanErrorOnlyTri), Environment.NewLine, Math.Sqrt(_SquaredMinErrorOnlyTri), Math.Sqrt(_SquaredMaxErrorOnlyTri));
            Writer.Close();
        }
    }
}
