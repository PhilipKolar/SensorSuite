using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSNUtil;

namespace SensorServer.Estimators
{
    public interface IEstimator
    {
        List<ObjectEstimate> CurrEsimate { get; }
        List<ObjectEstimate> TrilateratedEstimates { get; }
        List<ObjectEstimate> CurrAdditionalInfo { get; }
        //List<ObjectEstimate> AdditionalInfo;
        void AddMeasurement(Sensor source, Measurement measurement);
        List<ObjectEstimate> ComputeEstimate();
    }
}
