using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSNUtil
{
    public class Measurement
    {
        public float Distance { get; private set; }
        public DateTime TimeStamp { get; private set; }
        public int SensorID { get; private set; }
        public int TimeStage { get; set; }

        public Measurement(float distance, DateTime timeStamp, int sensorID)
        {
            Distance = distance;
            TimeStamp = timeStamp;
            SensorID = sensorID;
            TimeStage = -1;
        }

        public Measurement(float distance, DateTime timeStamp, int sensorID, int timeStage) : this(distance, timeStamp, sensorID)
        {
            TimeStage = timeStage;
        }
    } // End class
} // End namespace
