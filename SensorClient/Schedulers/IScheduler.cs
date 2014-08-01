using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SensorClient.Schedulers
{
    interface IScheduler
    {
        string CSVFile { get; }
        string INIFile { get; }
        void Start();
        void Close();
    }
}
