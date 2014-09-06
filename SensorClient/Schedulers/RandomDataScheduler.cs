using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using WSNUtil;

namespace SensorClient.Schedulers
{
    class RandomDataScheduler : IScheduler
    {
        public string CSVFile { get; private set; } //= Variables.DefaultCSVLocation;
        public string INIFile { get; private set; } //= Variables.DefaultINILocation;
        private Mutex _ConsoleOutputMutex = new Mutex(false);
        private SensorSender _Sender;
        private bool _Shutdown = false; //Determines if threads need to stop looping and shut down.
        private Mutex _RandomJunkGeneratorMutex = new Mutex(false);
        private Random _RandomJunkGenerator = new Random();

        public RandomDataScheduler(string csvFile, string iniFile)
        {
            CSVFile = csvFile;
            INIFile = iniFile;
        }

        /// <summary>
        /// Executes a number of threads proportionate to the number of sensors to emulate (i.e. same amount as in the CSVFile)
        /// </summary>
        public void Start()
        {
            List<Sensor> SensorList = Variables.GetSensorConfig(CSVFile);
            foreach (Sensor s in SensorList)
            {
                Thread t = new Thread(new ParameterizedThreadStart(_SendRandomDataIndividual));
                t.Start(s.SensorID);
            }
        }

        private void _SendRandomDataIndividual(object sensorId)
        {
            int SensorId = (int)sensorId;
            int PollingDelay = Variables.GetPollingDelay_ms(INIFile);

            do
            {
                _ConsoleOutputMutex.WaitOne();
                Console.WriteLine("[Sensor {0}] Attempting to connect to server...", SensorId);
                _ConsoleOutputMutex.ReleaseMutex();
                _Sender = new SensorSender(SensorId, INIFile);
                _Sender.Start();
                _Sender._ConnectionEstablished.WaitOne();
            } while (!_Sender.Connected);
            Console.WriteLine("Connection established.");

            while (_Shutdown == false)
            {
                _RandomJunkGeneratorMutex.WaitOne();
                float Distance = ((float)_RandomJunkGenerator.Next(100, 500 * 100)) / 100;
                _RandomJunkGeneratorMutex.ReleaseMutex();
                DateTime TimeStamp = DateTime.Now;
                _ConsoleOutputMutex.WaitOne();
                Console.WriteLine("[Sensor {0}] Sending message (Distance = {1}, TimeStamp  = {2}) ", SensorId, Distance, TimeStamp.Ticks);
                _ConsoleOutputMutex.ReleaseMutex();
                try
                {
                    if (_Sender.CanSend)
                        _Sender.SendData(Distance, TimeStamp);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error occured while sending measurement: \nSource:\n\t{2}\nMessage:\n\t{0}\nStackTrace:\n\t{1}, ", e.Message, e.StackTrace, e.Source);
                }
                Thread.Sleep(PollingDelay);
            }
            Console.WriteLine("[Sensor {0}] Finished shutting down.", SensorId);
        }

        public void Close()
        {
            _Shutdown = true;
        }
    }
}
