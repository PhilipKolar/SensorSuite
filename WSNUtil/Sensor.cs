using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSNUtil
{
    public class Sensor
    {
        public int SensorID { get; private set; }
        /// <summary>
        /// Angle in degrees determing the field of view of the sensor
        /// </summary>
        public float Theta { get; private set; }
        /// <summary>
        /// Angle in degrees determining the angle the sensor is facing with respect to a global "X axis", anti-clockwise.
        /// </summary>
        public float Phi { get; private set; }
        /// <summary>
        /// Distance in the X axis from a global (0,0) point.
        /// </summary>
        public float X { get; set; }
        /// <summary>
        /// Distance in the Y axis from a global (0,0) point.
        /// </summary>
        public float Y { get; set; }
        public float Distance { get; set; }

        public Sensor(int sensorID, float theta, float phi, float x, float y, float distance)
        {
            SensorID = sensorID;
            Theta = theta;
            Phi = phi;
            X = x;
            Y = y;
            Distance = distance;
        }

        /// <summary>
        /// Deep copy constructor
        /// </summary>
        /// <param name="toCopy">ObjectEstimate to copy into a new object</param>
        public Sensor(Sensor toCopy)
        {
            SensorID = toCopy.SensorID;
            Theta = toCopy.Theta;
            Phi = toCopy.Phi;
            X = toCopy.X;
            Y = toCopy.Y;
            Distance = toCopy.Distance;
        }
    } // End class
} // End namespace
