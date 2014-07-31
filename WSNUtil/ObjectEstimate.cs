using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSNUtil
{
    [Serializable]
    public class ObjectEstimate
    {
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float VelocityX { get; set;  }
        public float VelocityY { get; set; }

        public ObjectEstimate(float positionX, float positionY, float velocityX, float velocityY)
        {
            PositionX = positionX;
            PositionY = positionY;
            VelocityX = velocityX;
            VelocityY = velocityY;
        }

        /// <summary>
        /// Deep copy constructor
        /// </summary>
        /// <param name="toCopy">ObjectEstimate to copy into a new object</param>
        public ObjectEstimate(ObjectEstimate toCopy)
        {
            PositionX = toCopy.PositionX;
            PositionY = toCopy.PositionY;
            VelocityX = toCopy.VelocityX;
            VelocityY = toCopy.VelocityY;
        }
    }
}
