using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSNUtil;

namespace SensorServer.Estimators.Tools
{
    class TrilateratorNoisy2D : Trilaterator
    {
        public float GroupingThreshold { get; private set; }
        public int GridDivision { get; private set; }
        public override List<ObjectEstimate> CurrAdditionalInfo
        {
            get
            {
                return new List<ObjectEstimate>();
            }
            protected set
            {
                throw new NotImplementedException("This property is currently not in use for this class as there is no additional information to provide.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="groupingThreshold">The maximum distance that two measurement arcs can be before not being considered the same measurement</param>
        /// <param name="gridDivison"></param>
        public TrilateratorNoisy2D(float groupingThreshold, int gridDivison)
        {
            GroupingThreshold = groupingThreshold;
            GridDivision = gridDivison;
        }

        public override List<ObjectEstimate> CalculateEstimates(List<Tuple<Sensor, Measurement>> Measurements)
        {
            List<List<Tuple<Sensor, Measurement>>> Groups = _GroupMeasurements(Measurements, GroupingThreshold);

            List<ObjectEstimate> ToReturn = new List<ObjectEstimate>();
            foreach (List<Tuple<Sensor, Measurement>> group in Groups)
            {
                ObjectEstimate Result = _TrilaterateGroup(group);
                if (Result != null)
                    ToReturn.Add(Result);
            }
            return ToReturn;
        }

        private ObjectEstimate _TrilaterateGroup(List<Tuple<Sensor, Measurement>> group)
        {
            float StartX = float.MaxValue;
            float StartY = float.MaxValue;
            float EndX = float.MinValue;
            float EndY = float.MinValue;
            float LargestMaxDistance = float.MinValue;
            foreach (Tuple<Sensor, Measurement> measurePair in group)
            {
                StartX = measurePair.Item1.X < StartX ? measurePair.Item1.X : StartX;
                StartY = measurePair.Item1.Y < StartY ? measurePair.Item1.Y : StartY;
                EndX = measurePair.Item1.X > EndX ? measurePair.Item1.X : EndX;
                EndY = measurePair.Item1.Y > EndY ? measurePair.Item1.Y : EndY;
                LargestMaxDistance = measurePair.Item1.Distance > LargestMaxDistance ? measurePair.Item1.Distance : LargestMaxDistance;
            }
            StartX -= LargestMaxDistance;
            StartY -= LargestMaxDistance;
            EndX += LargestMaxDistance;
            EndY += LargestMaxDistance;


            float CurrX = StartX;
            float CurrY = StartY;
            float IncremeentX = (EndX - StartX) / GridDivision;
            float IncremeentY = (EndY - StartY) / GridDivision;
            ObjectEstimate BestCandidate = null;
            float BestCandidateError = float.MaxValue;
            for (int i = 0; i < GridDivision; i++)
            {
                for (int j = 0; j < GridDivision; j++)
                {
                    ObjectEstimate CurrPos = new ObjectEstimate(CurrX, CurrY, 0, 0);
                    if (_GroupHasVision(group, CurrPos) == false)
                    {
                        CurrX += IncremeentX;
                        continue;
                    }
                    float SquareError = _GetSquaredError(group, CurrPos);
                    if (SquareError < BestCandidateError)
                    {
                        BestCandidate = CurrPos;
                        BestCandidateError = SquareError;
                    }
                    CurrX += IncremeentX;
                }
                CurrY += IncremeentY;
                CurrX = StartX;
            }

            return BestCandidate;
        }

        private bool _GroupHasVision(List<Tuple<Sensor, Measurement>> group, ObjectEstimate candidate) //TODO: Delete?
        {
            foreach (Tuple<Sensor, Measurement> measure in group)
            {
                if (_IsWithinVision(candidate, new Sensor[] { measure.Item1 }) == false)
                    return false;
            }
            return true;
        }

        private float _GetSquaredError(List<Tuple<Sensor, Measurement>> group, ObjectEstimate candidate)
        {
            float ToReturn = 0f;
            foreach (Tuple<Sensor, Measurement> measure in group)
            {
                float TotalDist = (float)Math.Sqrt(Math.Pow(candidate.X - measure.Item1.X, 2) + Math.Pow(candidate.Y - measure.Item1.Y, 2));
                float Error = TotalDist - measure.Item2.Distance;
                ToReturn += (float)Math.Pow(Error, 2);
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
    }
}
