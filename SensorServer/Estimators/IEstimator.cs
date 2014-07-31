using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSNUtil;

namespace SensorServer.Estimators
{
    public interface IEstimator
    {
        void AddMeasurement(Sensor source, Measurement measurement);
        List<ObjectEstimate> ComputeEstimate();
    }
}
