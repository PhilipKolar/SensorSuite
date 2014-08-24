using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSNUtil
{
    [Serializable]
    public class ObjectEstimate
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float VelocityX { get; set;  }
        public float VelocityY { get; set; }

        public ObjectEstimate(float positionX, float positionY, float velocityX, float velocityY)
        {
            X = positionX;
            Y = positionY;
            VelocityX = velocityX;
            VelocityY = velocityY;
        }

        /// <summary>
        /// Deep copy constructor
        /// </summary>
        /// <param name="toCopy">ObjectEstimate to copy into a new object</param>
        public ObjectEstimate(ObjectEstimate toCopy)
        {
            X = toCopy.X;
            Y = toCopy.Y;
            VelocityX = toCopy.VelocityX;
            VelocityY = toCopy.VelocityY;
        }
    }
}
