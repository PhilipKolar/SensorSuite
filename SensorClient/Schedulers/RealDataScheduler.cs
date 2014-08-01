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
        private float _PollingDelay_ms;
        private float _TimeoutTime_s; //Time a HCSR04 measurement takes before timeing out (i.e. maximum time a measurement can take). Determined by max distance in INI file.
        private int _SensorCount;
        private float _TimeSchedulingTolerance_ms; //Time that must be allowed for busy waiting
        private float _PollingDelay_s
        {
            get { return _PollingDelay_ms / 1000; }
            set { _PollingDelay_ms = value * 1000; }
        }

        public RealDataScheduler(string iniFile, string csvFile, int sensorID, bool timeDivision)
        {
            INIFile = iniFile;
            SensorID = sensorID;
            TimeDivison = timeDivision;
            _PollingDelay_ms = Variables.GetPollingDelay_ms(iniFile);

            _TimeoutTime_s = 2 * 4 / HCSR04Sensor.SPEED_OF_SOUND; //About 0.0235 seconds or 23.5 milliseconds
            if (timeDivision)
            {
                _TimeSchedulingTolerance_ms = Variables.GetSensorClientTimeSchedulingTolerance_ms(iniFile);
                _SensorCount = Variables.GetSensorConfig(csvFile).Count;
                if (_TimeoutTime_s + (_TimeSchedulingTolerance_ms / 1000) > _PollingDelay_s / _SensorCount)
                {
                    throw new ArgumentOutOfRangeException("Error: Incompatible Sensor count and Polling Frequency, this polling frequency can't be acheived with time divison. Try increasing POLLING_DELAY or reducing the amount of sensors in the network. Turning off Time Division will also solve this error.");
                }
                if (_TimeSchedulingTolerance_ms == 0)
                    throw new ArgumentOutOfRangeException("Errpr: Time Scheduling tolerance is set to 0ms, no measurements will ever occur. This value should be high enough to let a busy wait consistenty catch the time slot and low enough such that the polling frequency isn't forced to be set too low (tolerance does not directly alter polling frequency)");
            }
        }

        public void Start()
        {
            Thread T = new Thread(new ThreadStart(_SendDataWorker));
            T.Start();
        }

        /// <summary>
        /// Connects to server and then calls a method to start obtaining and sending data
        /// </summary>
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
            float MaximumDistance = Variables.GetSensorClientMaxDistance_cm(INIFile);
            int EchoPinNumber = Variables.GetSensorClientEchoPin(INIFile);
            int TriggerPinNumber = Variables.GetSensorClientTriggerPin(INIFile);

            HCSR04Sensor Sensor = new HCSR04Sensor(EchoPinNumber, TriggerPinNumber, MaximumDistance);
            Sensor.GetMeasurement(); //First measurement tends to be wildly inaccurate, so do not use it
            Thread.Sleep((int)_PollingDelay_ms);
            float TimeSliceLength_ms = _TimeSchedulingTolerance_ms + (float)_PollingDelay_ms / (float)_SensorCount;
            float StartTime = (SensorID - 1) * TimeSliceLength_ms;
            float EndTime = (SensorID * TimeSliceLength_ms) - (_TimeoutTime_s * 1000); //A measurement cannot begin after the sensor timeout period has begun since it will overlap with the next timeslot
            if (EndTime <= StartTime)
                throw new Exception(string.Format("ERROR: End time for sensor scheduling occurs before or during start time - Start: {0}, End: {1}.", StartTime, EndTime));
            Console.WriteLine("Measurements:");
            while (_Shutdown == false)
            {
                long CurrentMilliseconds = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) % (int)_PollingDelay_ms;
                if (CurrentMilliseconds >= StartTime && CurrentMilliseconds < EndTime)
                {
                    float Distance = Sensor.GetMeasurement();
                    _MeasurementObtained(Distance, DateTime.Now);
                    Console.WriteLine("{0}", Distance);
                    Thread.Sleep((int)(TimeSliceLength_ms));
                }
                else
                    continue; //Busy loop while waiting for time slot
            }
            Console.WriteLine("The sensor scheduler has succesfully shutdown.");
            Sensor.Dispose();
        }

        private void _GetMeasurements()
        {
            float MaximumDistance = Variables.GetSensorClientMaxDistance_cm(INIFile);
            int EchoPinNumber = Variables.GetSensorClientEchoPin(INIFile);
            int TriggerPinNumber = Variables.GetSensorClientTriggerPin(INIFile);

            HCSR04Sensor Sensor = new HCSR04Sensor(EchoPinNumber, TriggerPinNumber, MaximumDistance);
            Sensor.GetMeasurement(); //First measurement tends to be wildly inaccurate, so do not use it
            Thread.Sleep((int)_PollingDelay_ms);
            while (_Shutdown == false)
            {
                DateTime Start = DateTime.Now;
                float Distance = Sensor.GetMeasurement();
                Console.WriteLine("{0}", Distance);
                DateTime End = DateTime.Now;
                _MeasurementObtained(Distance, End);

                double TimeTaken = (End - Start).TotalMilliseconds; //In millisecondsy
                Thread.Sleep(TimeTaken > (int)_PollingDelay_ms ? 0 : (int)(_PollingDelay_ms - TimeTaken)); //Subtract the time taking the measurement took from the polling delay
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
