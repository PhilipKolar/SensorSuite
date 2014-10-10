using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WSNUtil;
using LibSharpTave;

namespace SensorServer.Estimators.Tools
{
    class TrilateratorNoisy2DLeastSquares : Trilaterator
    {
        public string ScriptPath { get; set; }
        public uint Iterations
        {
            get { return _Iterations; }
            private set { _Iterations = value; }
        }
        public float GroupingThreshold { get; private set; }
        public override List<ObjectEstimate> CurrAdditionalInfo
        {
            get { return new List<ObjectEstimate>(); }
            protected set { throw new NotImplementedException("This property is currently not in use for this class as there is no additional information to provide."); }
        }

        private static Octave _Oct = null; //TODO: Remove static, but make sure octave process isn't restarted every time a trilateration is done
        private bool _CustomIterations; //TODO: Get rid of this variable, made redundant because the constructor now forces a custom iteration
        private uint _Iterations;

        public TrilateratorNoisy2DLeastSquares(string octaveBinaryPath, bool displayWindow, string trilaterationScriptPath, float groupingThreshold)
            : this(octaveBinaryPath, displayWindow, trilaterationScriptPath, 8, groupingThreshold)
        {
        }

        public TrilateratorNoisy2DLeastSquares(string octaveBinaryPath, bool displayWindow, string trilaterationScriptPath, uint iterations, float groupingThreshold)
        {
            if (_Oct == null)
            {
                _Oct = new Octave(octaveBinaryPath, false);
                _Oct.ExecuteCommand(@"pkg load optim");
                _Oct.ExecuteCommand(@"warning(""off"", ""Octave:broadcast"");");
            }
            ScriptPath = trilaterationScriptPath;
            _CustomIterations = true;
            SetLeastSquareIterations(iterations);
            GroupingThreshold = groupingThreshold;
        }

        /// <summary>
        /// Set the amount of times Octave should repeat leasqr() with different initial guesses. Higher values increase accuracy on average
        /// but increase processing drastically. Recommended values are between 3 and 15.
        /// WARNING: Computational complexity scales at O(iterations^2), setting it much higher than the recommended values may cause extremely long processing times.
        /// </summary>
        /// <param name="iterations">How to divide the grid around the trilaterated sensors. e.g, a value of 10 will divide into a 10x10 grid where each cell will be processed</param>
        public void SetLeastSquareIterations(uint iterations)
        {
            _CustomIterations = true;
            _Iterations = iterations;
        }

        public override List<ObjectEstimate> CalculateEstimates(List<Tuple<Sensor, Measurement>> Measurements)
        {
            //Group Measurements
            List<List<Tuple<Sensor, Measurement>>> GroupsToTrilaterate = _GroupMeasurements(Measurements, GroupingThreshold);

            // Trilaterate each group
            List<ObjectEstimate> ToReturn = new List<ObjectEstimate>();
            foreach (List<Tuple<Sensor, Measurement>> group in GroupsToTrilaterate)
            {
                if (group.Count < 2)
                    continue;
                ObjectEstimate Result = _RunTrilateration(group);
                if (Result != null)
                    ToReturn.Add(Result);
            }
            return ToReturn;
        }

        private List<List<Tuple<Sensor, Measurement>>> _GroupMeasurements(List<Tuple<Sensor, Measurement>> Measurements, float Threshold)
        {
            List<bool> Marks = new List<bool>(Measurements.Count); //Mirrors Measurements to let us keep track of which Measurements have already been grouped (faster than searching the list every time)
            for (int i = 0; i < Measurements.Count; i++)
                Marks.Add(false);
            List<List<Tuple<Sensor, Measurement>>> GroupedMeasurements = new List<List<Tuple<Sensor, Measurement>>>();
            for (int i = 0; i < Measurements.Count; i++)
            {
                if (Marks[i] == true)
                    continue;
                List<Tuple<Sensor, Measurement>> NewGroup = new List<Tuple<Sensor, Measurement>>();
                NewGroup.Add(Measurements[i]);
                bool ChangeMade;
                do //This loop catches the case where order of execution is important to catch all nearby measurements
                {
                    ChangeMade = false;
                    //Iterate through all remaining measurements to test if they are in range
                    for (int j = i + 1; j < Measurements.Count; j++)
                    {
                        if (Marks[j] == true)
                            continue;
                        if (_ProximityCheck(NewGroup, Measurements[j], Threshold))
                        {
                            ChangeMade = true;
                            Marks[j] = true;
                            NewGroup.Add(Measurements[j]);
                        }
                    }
                } while (ChangeMade == true);
                if (NewGroup.Count > 1)
                    GroupedMeasurements.Add(NewGroup);
            }
            return GroupedMeasurements;
        }

        private bool _ProximityCheck(List<Tuple<Sensor, Measurement>> currGroup, Tuple<Sensor, Measurement> newSensor, float MaxProximity)
        {
            float NewX = (float)(newSensor.Item1.X + newSensor.Item2.Distance * Math.Cos(_ToRad(newSensor.Item1.Phi)));
            float NewY = (float)(newSensor.Item1.Y + newSensor.Item2.Distance * Math.Sin(_ToRad(newSensor.Item1.Phi)));
            foreach (Tuple<Sensor, Measurement> measure in currGroup)
            {
                float CurrX = (float)(measure.Item1.X + measure.Item2.Distance * Math.Cos(_ToRad(measure.Item1.Phi)));
                float CurrY = (float)(measure.Item1.Y + measure.Item2.Distance * Math.Sin(_ToRad(measure.Item1.Phi)));
                float Distance = (float)Math.Sqrt(Math.Pow(NewX - CurrX, 2) + Math.Pow(NewY - CurrY, 2));
                if (Distance <= MaxProximity)
                    return true;
            }
            return false;
        }

        private float _ToRad(float degrees)
        {
            return degrees * (float)Math.PI / 180f;
        }

        private ObjectEstimate _RunTrilateration(List<Tuple<Sensor, Measurement>> measurements)
        {
            Tuple<string, string> OctaveStrings = _SetSensorMeasurements(measurements);
            double[][] BestAnswers = _ExecuteScript(OctaveStrings.Item1, OctaveStrings.Item2);
            double[] BestAnswer = _BestSensorCoverage(BestAnswers, measurements);
            return new ObjectEstimate((float)BestAnswer[0], (float)BestAnswer[1], 0f, 0f);
        }

        private double[] _BestSensorCoverage(double[][] Points, List<Tuple<Sensor, Measurement>> sensorInfo)
        {
            double[] BestPoint = null;
            int BestVision = -1;
            foreach (double[] point in Points)
            {
                int VisionCount = 0;
                foreach (Tuple<Sensor, Measurement> sensors in sensorInfo)
                    if (_IsWithinVision(point, new Sensor[] { sensors.Item1 }))
                        VisionCount++;
                if (VisionCount > BestVision)
                {
                    BestPoint = point;
                    BestVision = VisionCount;
                }
            }

            if (BestVision == -1)
                throw new Exception("Error occured when finding the point with best sensor coverage");
            return BestPoint;
        }

        /// <summary>
        /// Creates an octave-formatted matrix of measurements.
        /// </summary>
        /// <param name="measurements"></param>
        /// <returns>Tuple.Item1: x, the sensor positions. Tuple.Item2: y, the sensor measurement distances</returns>
        private Tuple<string, string> _SetSensorMeasurements(List<Tuple<Sensor, Measurement>> measurements)
        {
            StringBuilder SensorPositionsBuilder = new StringBuilder("x = [");
            StringBuilder MeasurementRadiusesBuilder = new StringBuilder("y = [");
            foreach (Tuple<Sensor, Measurement> SensorMeasureTuple in measurements)
            {
                SensorPositionsBuilder.AppendFormat(" {0}, {1} ;", SensorMeasureTuple.Item1.X, SensorMeasureTuple.Item1.Y);
                MeasurementRadiusesBuilder.AppendFormat(" {0} ;", SensorMeasureTuple.Item2.Distance);
            }
            SensorPositionsBuilder.Remove(SensorPositionsBuilder.Length - 1, 1);
            MeasurementRadiusesBuilder.Remove(MeasurementRadiusesBuilder.Length - 1, 1);
            SensorPositionsBuilder.Append("];");
            MeasurementRadiusesBuilder.Append("];");

            return new Tuple<string, string>(SensorPositionsBuilder.ToString(), MeasurementRadiusesBuilder.ToString());
        }

        private double[][] _ExecuteScript(string sensorPositions, string measurementRadiuses)
        {
            bool IterationsEncountered = false; //Used for when CustomIterations is enabled
            bool SensorPositionsEncountered = false;
            bool MeasurementRadiusesEncountered = false;

            StreamReader Reader = new StreamReader(ScriptPath);
            //StringBuilder ScriptBuilder = new StringBuilder(5000);
            while (Reader.EndOfStream == false)
            {
                string CurrLine = Reader.ReadLine().Split('%')[0].Trim();
                if (CurrLine == "")
                    continue;

                //Check for replacable values
                if (_CustomIterations && IterationsEncountered == false)
                    if (CurrLine.Length > 13 && CurrLine.Substring(0, 14).ToLower() == "num_iterations")
                    {
                        CurrLine = string.Format("num_iterations = {0};", _Iterations);
                        IterationsEncountered = true;
                    }
                if (SensorPositionsEncountered == false)
                    if (CurrLine.Length > 0 && CurrLine.Substring(0, 1).ToLower() == "x")
                    {
                        CurrLine = sensorPositions;
                        SensorPositionsEncountered = true;
                    }
                if (MeasurementRadiusesEncountered == false)
                    if (CurrLine.Length > 0 && CurrLine.Substring(0, 1).ToLower() == "y")
                    {
                        CurrLine = measurementRadiuses;
                        MeasurementRadiusesEncountered = true;
                    }

                string ToExecute;
                if (CurrLine[CurrLine.Length - 1] != ';')
                {
                    ToExecute = string.Format("{0}; ", CurrLine);
                    //ScriptBuilder.AppendFormat("{0}; ", CurrLine);
                }
                else
                {
                    ToExecute = string.Format("{0} ", CurrLine);
                    //ScriptBuilder.AppendFormat("{0} ", CurrLine);
                }
                _Oct.ExecuteCommand(ToExecute);
            }
            Reader.Close();
            //_Oct.ExecuteCommand(ScriptBuilder.ToString());

            double[][] Result = _Oct.GetMatrix("all_best_answers");
            if (Result.Length == 0)
            {
                Result = new double[1][];
                Result[0] = _Oct.GetVector("all_best_answers");
            }
            return Result;
        }
    }
}
