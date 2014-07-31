using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSNUtil;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace SensorServer
{
    class MeasurementStore
    {
        private Queue<_StampedMeasurement> _Data;
        private DateTime _FirstTime;
        private DateTime _PreviousTime;
        public event MessageReceievedDelegate OnMessageReceived;

        public MeasurementStore()
        {
            _Data = new Queue<_StampedMeasurement>();
        }

        public MeasurementStore(string csvFile)
        {
            StreamReader Reader = new StreamReader(csvFile);
            bool LastLine = false;
            if (Reader.EndOfStream)
                throw new ArgumentException("Error: CSV file is empty");

            long FirstTimeTicks;
            while (!Reader.EndOfStream)
            {
                string CurrentLine = _ProcessCSVLine(Reader.ReadLine());

                if (long.TryParse(CurrentLine, out FirstTimeTicks) == true)
                {
                    _FirstTime = new DateTime(FirstTimeTicks);
                    break;
                }
                if (Reader.EndOfStream)
                    LastLine = true;
            }
            if (LastLine)
                throw new ArgumentException("Error: CSV File does not contain all data (could not find _FirstTime ticks)");

            long PreviousTimeTicks;
            while (!Reader.EndOfStream)
            {
                string CurrentLine = _ProcessCSVLine(Reader.ReadLine());

                if (long.TryParse(CurrentLine, out PreviousTimeTicks) == true)
                {
                    _PreviousTime = new DateTime(PreviousTimeTicks);
                    break;
                }
                if (Reader.EndOfStream)
                    LastLine = true;
            }
            if (LastLine)
                throw new ArgumentException("Error: CSV File does not contain all data (could not find _PreviousTime ticks)");

            _Data = new Queue<_StampedMeasurement>();
            while (!Reader.EndOfStream)
            {
                string CurrentLine = _ProcessCSVLine(Reader.ReadLine());
                string[] Components = CurrentLine.Split(',');
                if (Components.Length != 5)
                    continue;

                float Distance;
                if (float.TryParse(Components[0], out Distance) == false)
                    continue;

                long TimeStampTicks;
                if (long.TryParse(Components[1], out TimeStampTicks) == false)
                    continue;
                DateTime TimeStamp = new DateTime(TimeStampTicks);

                int SensorID;
                if (int.TryParse(Components[2], out SensorID) == false)
                    continue;

                int TimeStage;
                if (int.TryParse(Components[3], out TimeStage) == false)
                    continue;

                int TimeReceived;
                if (int.TryParse(Components[4], out TimeReceived) == false)
                    continue;

                Measurement NewMeasurement = new Measurement(Distance, TimeStamp, SensorID, TimeStage);
                _StampedMeasurement NewStampedMeasurement = new _StampedMeasurement(NewMeasurement, TimeReceived);
                _Data.Enqueue(NewStampedMeasurement);
            }

            Reader.Close();
        }

        private string _ProcessCSVLine(string currentLine)
        {
            return currentLine.Split(new char[] { '#', ';' })[0].Trim();
        }

        /// <summary>
        /// Multi-threaded method that will begin triggering OnMessageReceived events in the order that they were Enqueued.
        /// The first measurement will always trigger an event nearly instantly.
        /// Calling this method without defining an event handler for OnMessageReceived will throw an Exception.
        /// </summary>
        public void Start()
        {
            if (OnMessageReceived == null)
                throw new Exception("Error: No OnMessageReceived event handler defined for MeasurementStore.");

            Thread t = new Thread(new ThreadStart(_SendAllMessages));
            t.Start();
        }

        private void _SendAllMessages()
        {
            while (_Data.Count != 0)
            {
                _StampedMeasurement Current = _Data.Dequeue();
                if (Current.TimeReceived > 0)
                    Thread.Sleep(Current.TimeReceived);
                OnMessageReceived(Current.Measure.SensorID, Current.Measure.Distance, Current.Measure.TimeStamp);
            }
        }

        private Semaphore _EnqueueSemaphore = new Semaphore(1, 1); //Need a semaphore instead of a Mutex since we will be accessing it from
                                                                  // (potentially) different threads, as the class is designed to be thread safe
        /// <summary>
        /// Adds a measurement to the MeasurementStore.
        /// This method is thread safe.
        /// </summary>
        /// <param name="measureToStore"></param>
        /// <param name="timeReceived"></param>
        public void Enqueue(Measurement measureToStore, DateTime timeReceived)
        {
            _EnqueueSemaphore.WaitOne();
            if (_Data.Count == 0)
            {
                _FirstTime = measureToStore.TimeStamp;
                _PreviousTime = timeReceived; //Make the initial StampedMeasurement occur at t = 0
            }
            _Data.Enqueue(new _StampedMeasurement(measureToStore, (int)((timeReceived.Ticks - _PreviousTime.Ticks) / TimeSpan.TicksPerMillisecond)));
            _PreviousTime = timeReceived;
            _EnqueueSemaphore.Release();
        }

        /// <summary>
        /// Saves the object in CSV form to the specified file. Throw an exception if a file with that name already exists.
        /// This method does NOT save any event handlers assigned to OnMessageReceived.
        /// This method is thread safe.
        /// </summary>
        /// <param name="csvFilePath"></param>
        public void Save(string csvFilePath) // You can't serialize a Queue or any LinkedList-esque data structure in .NET, only a "normal" List.
        {                                    // So we implement our own serialisation, this method uses a CSV format with comments starting with '#' or ';'.
            _EnqueueSemaphore.WaitOne(); //Don't let the queue change while we're saving our data
            FileStream SaveFile = new FileStream(csvFilePath, FileMode.CreateNew); //Throw an exception if overwriting is attempted. We don't want to accidently lose our data.
            StreamWriter Writer = new StreamWriter(SaveFile);

            Writer.WriteLine("# This MeasurementStore object was saved on {0}{1}", DateTime.Now.ToString(), Environment.NewLine);

            Writer.WriteLine("# _FirstTime.Ticks");
            Writer.WriteLine(_FirstTime.Ticks);
            Writer.WriteLine("# _PreviousTime.Ticks");
            Writer.WriteLine(_PreviousTime.Ticks);
            Writer.WriteLine("# _Data");
            Writer.WriteLine("# Measure.Distance, Measure.TimeStamp.Ticks, Measure.SensorID, Measure.TimeStage, Curr.TimeReceived (time since last measurement in ms)");
            Queue<_StampedMeasurement> DataBackup = new Queue<_StampedMeasurement>(_Data);
            while (_Data.Count != 0)
            {
                _StampedMeasurement Curr = _Data.Dequeue();
                Writer.WriteLine("{0},{1},{2},{3},{4}", Curr.Measure.Distance, Curr.Measure.TimeStamp.Ticks, Curr.Measure.SensorID, Curr.Measure.TimeStage, Curr.TimeReceived);
            }
            _Data = DataBackup;

            Writer.Close();
            _EnqueueSemaphore.Release();
        }

        private class _StampedMeasurement
        {
            public Measurement Measure { get; private set; }
            public int TimeReceived { get; private set; } //Time since the last measurement that the current measurement was received. Measured in ms.

            public _StampedMeasurement(Measurement measure, int timeReceived)
            {
                Measure = measure;
                TimeReceived = timeReceived;
            }
        } //End class (StampedMeasurement)
    } //End class (MeasurementStore)
} //End Namespace
