using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using WSNUtil;

namespace SensorClient.Schedulers
{
    class RealDataScheduler : IScheduler
    {
        public string INIFile { get; private set; }
        public string CSVFile { get; private set; }
        public int SensorID { get; private set; }
        public bool TimeDivison { get; private set; }

        private delegate void _MeasurementObtainedHandler(float measurement, DateTime timeStamp);
        private event _MeasurementObtainedHandler _MeasurementObtained;
        private SensorSender _Sender;
        private bool _Shutdown = false; //Determines if threads need to stop looping and shut down.
        private int _PollingDelay;
        private float _TimeoutTime; //Time a HCSR04 measurement takes before timeing out (i.e. maximum time a measurement can take). Determined by max distance in INI file.
        private int _SensorCount;

        public RealDataScheduler(string iniFile, string csvFile, int sensorID, bool timeDivision)
        {
            INIFile = iniFile;
            SensorID = sensorID;
            TimeDivison = timeDivision;
            _PollingDelay = Variables.GetPollingDelay(iniFile);

            _TimeoutTime = 2 * Variables.GetSensorClientMaxDistance(iniFile) / HCSR04Sensor.SPEED_OF_SOUND;
            if (timeDivision)
            {
                _SensorCount = Variables.GetSensorConfig(csvFile).Count;
                if (_TimeoutTime > _PollingDelay / _SensorCount)
                {
                    throw new ArgumentOutOfRangeException("Error: Incompatible Sensor MaxDistance and Polling Frequency, polling frequency can't be acheived with time divison. Try increasing POLLING_DELAY or reducing SENSOR_CLIENT_MAX_DISTANCE. Turning off Time Division will also solve this error.");
                }
            }
        }

        public void SendData()
        {
            Thread T = new Thread(new ThreadStart(_SendDataWorker));
            T.Start();
        }

        private void _SendDataWorker()
        {
            _MeasurementObtained += _GotMeasurement;
            do
            {
                if (_Shutdown)
                {
                    Console.WriteLine("Close() called before connection could be established. Aborting connection attempt.");
                    return;
                }
                Console.WriteLine("Attempting to connect to server at {0}...", Variables.GetSensorServerIP(INIFile));
                _Sender = new SensorSender(SensorID, INIFile);
                _Sender.Start();
                _Sender.ConnectionEstablished.WaitOne();
            } while (!_Sender.Connected);
            Console.WriteLine("Connected to SensorServer. Now sending data.");
            if (TimeDivison)
                _GetMeasurementsTimeDivision();
            else
                _GetMeasurements();
        }

        private void _GetMeasurementsTimeDivision()
        {
            float MaximumDistance = Variables.GetSensorClientMaxDistance(INIFile);
            int EchoPinNumber = Variables.GetSensorClientEchoPin(INIFile);
            int TriggerPinNumber = Variables.GetSensorClientTriggerPin(INIFile);

            HCSR04Sensor Sensor = new HCSR04Sensor(EchoPinNumber, TriggerPinNumber, MaximumDistance);
            Sensor.GetMeasurement(); //First measurement tends to be wildly inaccurate, so do not use it
            Thread.Sleep(_PollingDelay);
            float TimeSliceLength = (float)_PollingDelay / (float)_SensorCount;
            float StartTime = (SensorID - 1) * TimeSliceLength;
            float EndTime = SensorID * TimeSliceLength;
            Console.WriteLine("Measurements:");
            while (_Shutdown == false)
            {
                long CurrentMilliseconds = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) % _PollingDelay;
                if (CurrentMilliseconds >= StartTime && CurrentMilliseconds < EndTime)
                {
                    float Distance = Sensor.GetMeasurement();
                    _MeasurementObtained(Distance, DateTime.Now);
                    Console.WriteLine("{0}", Distance);
                    Thread.Sleep((int)(TimeSliceLength));
                }
                else
                    continue; //Busy loop while waiting for time slot
            }
            Console.WriteLine("The sensor scheduler has succesfully shutdown.");
            Sensor.Dispose();
        }

        private void _GetMeasurements()
        {
            float MaximumDistance = Variables.GetSensorClientMaxDistance(INIFile);
            int EchoPinNumber = Variables.GetSensorClientEchoPin(INIFile);
            int TriggerPinNumber = Variables.GetSensorClientTriggerPin(INIFile);

            HCSR04Sensor Sensor = new HCSR04Sensor(EchoPinNumber, TriggerPinNumber, MaximumDistance);
            Sensor.GetMeasurement(); //First measurement tends to be wildly inaccurate, so do not use it
            Thread.Sleep(_PollingDelay);
            while (_Shutdown == false)
            {
                DateTime Start = DateTime.Now;
                float Distance = Sensor.GetMeasurement();
                Console.WriteLine("{0}", Distance);
                DateTime End = DateTime.Now;
                _MeasurementObtained(Distance, End);

                double TimeTaken = (End - Start).TotalMilliseconds; //In millisecondsy
                Thread.Sleep(TimeTaken > _PollingDelay ? 0 : _PollingDelay - (int)TimeTaken); //Subtract the time taking the measurement took from the polling delay
            }
            Console.WriteLine("The sensor scheduler has succesfully shutdown.");
            Sensor.Dispose();
        }

        private void _GotMeasurement(float measurement, DateTime timeStamp)
        {
            try
            {
                if (_Sender.CanSend)
                    _Sender.SendData(measurement, timeStamp);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured while sending measurement: \nSource:\n\t{2}\nMessage:\n\t{0}\nStackTrace:\n\t{1}, ", e.Message, e.StackTrace, e.Source);
            }
        }

        public void Close()
        {
            _Shutdown = true;
        }
    }
}
