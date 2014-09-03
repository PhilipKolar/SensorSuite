using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSNUtil;
using SensorServer.Estimators;
using System.Threading;
using System.IO;

namespace SensorServer
{
    class Program
    {
        static Dictionary<int, List<Measurement>> RawData = new Dictionary<int, List<Measurement>>(); //Sensor ID, list of measurements containing that sensor ID
        static string CSVFile = Variables.DefaultCSVLocation;
        static string INIFile = Variables.DefaultINILocation;
        static DateTime StartTime = DateTime.MinValue; //The tick count associated with time stage t = 0
        static DateTime LocalStartTime = DateTime.MinValue; //Similar to above but represents the current machines time and not the recorded time
        static int PollingDelay = Variables.GetPollingDelay_ms(INIFile);
        static Dictionary<int, Sensor> SensorList = GetSensorDictionary(); //TODO: Convert to SortedDictionary for better performance
        static Mutex RawDataMutex = new Mutex(false);
        static MeasurementStore MeasureStore;
        static readonly string MEASUREMENTSTORE_CSV_FILE = Variables.GetSensorServerMeasureStoreFilePath(INIFile);
        static SensorServerMode Mode;

        static Dictionary<int, Sensor> GetSensorDictionary()
        {
             List<Sensor> SensorList = Variables.GetSensorConfig(CSVFile);
             Dictionary<int, Sensor> ToReturn = new Dictionary<int, Sensor>(SensorList.Count);
             foreach (Sensor s in SensorList)
                 ToReturn.Add(s.SensorID, s);
             return ToReturn;
        }

        static void MeasurementReceivedNormal(int sensorID, float distance, DateTime timeStamp)
        {
            List<Measurement> CurrentList;
            RawDataMutex.WaitOne();
            if (RawData.Count == 0)
            {
                StartTime = new DateTime(timeStamp.Ticks); //Initialise StartTimeTicks when the first message is received
                LocalStartTime = DateTime.Now;
            }
            if (RawData.TryGetValue(sensorID, out CurrentList) == false)
            { //If the dictionary has no entry for the currenty sensorID, make a list for it
                CurrentList = new List<Measurement>();
                RawData.Add(sensorID, CurrentList);
            }
            RawDataMutex.ReleaseMutex();
            int TimeDifferenceMilliseconds = (int)((timeStamp.Ticks - StartTime.Ticks) / TimeSpan.TicksPerMillisecond); // Ticks represent 100 us (nanoseconds), divide by 10,000 to get ms (the same as PollingDelay).
            int TimeStage = (int)Math.Round((double)TimeDifferenceMilliseconds / PollingDelay, 0, MidpointRounding.AwayFromZero);
            Measurement ReceivedMeasurement = new Measurement(distance, timeStamp, sensorID, TimeStage);
            RawDataMutex.WaitOne();
            CurrentList.Add(ReceivedMeasurement);
            RawDataMutex.ReleaseMutex();
            Console.WriteLine("Data Received:\n\tSensor ID: '{3}'\n\tTime Stage: '{4}'\n\tDistance: '{0}'\n\tTicks: '{1}', DateTime: '{2}'\n", distance, timeStamp.Ticks, timeStamp.ToString(), sensorID, TimeStage); //TODO: Delete this when no longer needed for testing
            //EstimatorMutex.WaitOne();
            //Estimator.AddMeasurement(SensorList[ReceivedMeasurement.SensorID], ReceivedMeasurement);
            //EstimatorMutex.ReleaseMutex();
        }

        static void MeasurementReceivedReadFromStore(int sensorID, float distance, DateTime timeStamp)
        {
            MeasurementReceivedNormal(sensorID, distance, timeStamp);
        }

        static void MeasurementReceivedMonitorOnly(int sensorID, float distance, DateTime timeStamp)
        {
            DateTime TimeReceived = DateTime.Now; //Get Datetime.Now before any calculation is done to minimise error
            int TimeDifferenceMilliseconds = (int)((timeStamp.Ticks - StartTime.Ticks) / TimeSpan.TicksPerMillisecond);
            int TimeStage = (int)Math.Round((double)TimeDifferenceMilliseconds / PollingDelay, 0, MidpointRounding.AwayFromZero);
            Measurement ReceivedMeasurement = new Measurement(distance, timeStamp, sensorID, TimeStage);
            MeasureStore.Enqueue(ReceivedMeasurement, TimeReceived);
        }

        static List<Measurement> GetTimeStageMeasurements(int TimeStage)
        {
            List<Measurement> ToReturn = new List<Measurement>();
            RawDataMutex.WaitOne();
            foreach (KeyValuePair<int, List<Measurement>> dataListPair in RawData)
            {
                Measurement SearchResult = dataListPair.Value.FirstOrDefault(measurement => measurement.TimeStage == TimeStage);
                if (SearchResult == null)
                    continue;
                else
                    ToReturn.Add(SearchResult);
            }
            RawDataMutex.ReleaseMutex();
            return ToReturn;
        }

        /// <summary>
        /// Removes all measurements from stated time stage and below. NOT guaranteed to catch all, breaks once a higer time stage is found, for performance (removing absolutely every measurement right away is not essential)
        /// </summary>
        /// <param name="timestage"></param>
        static void RemoveTimeStageMeasurements(int timestage)
        {
            RawDataMutex.WaitOne();
            foreach (KeyValuePair<int, List<Measurement>> sensorMeasurements in RawData)
            {
                while (sensorMeasurements.Value.Count != 0)
                {
                    if (sensorMeasurements.Value[0].TimeStage > timestage)
                        break;
                    sensorMeasurements.Value.RemoveAt(0); //TODO: Change this operation, RemoveAt(0) is O(n) complexity as it shifts data elements down the array
                }
            }
            RawDataMutex.ReleaseMutex();
        }

        static void Main()
        {
            try
            {
                Mode = Variables.GetSensorServerMode(INIFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving mode of operation for sensor server in INI file at {0}. Exception message: '{1}'.\nShutting sensor server down.", INIFile, ex.Message);
                return;
            }

            if (Mode == SensorServerMode.Normal)
                MainNormal();
            else if (Mode == SensorServerMode.MonitorOnly)
                MainMonitorOnly();
            else if (Mode == SensorServerMode.ReadFromStore)
                MainReadFromStore();
        }

        static void MainNormal()
        {
            SensorReceiver Receiver = new SensorReceiver(INIFile);
            Receiver.OnMessageReceived += MeasurementReceivedNormal;
            Receiver.Start();

            SetupDisplaySender();
        }

        static void MainMonitorOnly()
        {
            MeasureStore = new MeasurementStore();
            SensorReceiver Receiver = new SensorReceiver(INIFile);
            Receiver.OnMessageReceived += MeasurementReceivedMonitorOnly;
            Receiver.Start();

            const int SAVE_FREQUENCY_MS = 5000; //How often to save the MeasureStore data
            if (File.Exists(MEASUREMENTSTORE_CSV_FILE))
            {
                Console.WriteLine("ERROR: Attempt to overwrite file {0} denied. Please make sure to use a different file name for the MeasureStore.",
                        MEASUREMENTSTORE_CSV_FILE);
                return;
            }
            while (true)
            {
                Thread.Sleep(SAVE_FREQUENCY_MS);
                Console.Write("Saving MeasureStore... ");
                File.Delete(MEASUREMENTSTORE_CSV_FILE + ".temp");
                MeasureStore.Save(MEASUREMENTSTORE_CSV_FILE + ".temp"); //Save into a temp file first to prevent data loss during abrubt closure
                if (File.Exists(MEASUREMENTSTORE_CSV_FILE))
                {
                    File.Delete(MEASUREMENTSTORE_CSV_FILE + ".backup");
                    try
                    {
                        File.Move(MEASUREMENTSTORE_CSV_FILE, MEASUREMENTSTORE_CSV_FILE + ".backup"); //Move the old save file to a .backup extension
                    }
                    catch (IOException ex)
                    { } //Dropbox can recreate a file sometimes
                }
                File.Delete(MEASUREMENTSTORE_CSV_FILE);
                File.Move(MEASUREMENTSTORE_CSV_FILE + ".temp", MEASUREMENTSTORE_CSV_FILE); //Finally move in the new save file with the proper file name
                File.Delete(MEASUREMENTSTORE_CSV_FILE + ".temp");
                Console.WriteLine("Done");
            }
        }

        static void MainReadFromStore()
        {
            SensorReceiver Receiver = new SensorReceiver(INIFile);
            //StartTimeTicks = DateTime.Now.Ticks;
            try
            {
                MeasureStore = new MeasurementStore(MEASUREMENTSTORE_CSV_FILE);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: No measure store CSV file with name '{0}' found. Shutting sensor server down. Exception Message: {1}", MEASUREMENTSTORE_CSV_FILE, e.Message);
                return;
            }
            MeasureStore.OnMessageReceived += MeasurementReceivedReadFromStore;
            MeasureStore.Start();

            SetupDisplaySender();
        }

        static void SetupDisplaySender()
        {
            DisplaySender Sender = new DisplaySender(Variables.DefaultINILocation);
            Sender.Start();
            const int TOLERANCE_LAG = 500; //Time to wait for sensor measurements before finalising an estimate. Measured in ms.
            Thread.Sleep(TOLERANCE_LAG); // Lag behind the sensors to wait for any slow communication.
            Sender.ConnectionEstablished.WaitOne();
            Sender.ConnectionEstablished.Release();
            int CurrTimeStage = 0;

            IEstimator RawMeasurementEstimator = new ForwardEstimator();
            IEstimator ObjectCandidateEstimator = new InitialEstimator(INIFile);

            RealStateParser RealParser = null;
            if (Mode == SensorServerMode.ReadFromStore)
                RealParser = new RealStateParser(Variables.GetSensorServerRealStateFilePath(INIFile));
            while (true)
            {
                List<Measurement> CurrStageMeasurements = GetTimeStageMeasurements(CurrTimeStage);

                if (CurrStageMeasurements.Count != 0)
                {
                    foreach (Measurement CurrMeasurement in CurrStageMeasurements)
                    {
                        RawMeasurementEstimator.AddMeasurement( SensorList[CurrMeasurement.SensorID], CurrMeasurement); //TODO: Remove used measurements from RawData to prevent memory "leaking"
                        ObjectCandidateEstimator.AddMeasurement(SensorList[CurrMeasurement.SensorID], CurrMeasurement);
                    }
                    List<ObjectEstimate> RawMeasurementEstimate = RawMeasurementEstimator.ComputeEstimate();
                    List<ObjectEstimate> ObjectCandidateEstimate = ObjectCandidateEstimator.ComputeEstimate();
                    List<ObjectEstimate> RealState = new List<ObjectEstimate>();
                    if (Mode == SensorServerMode.ReadFromStore)
                    {
                        ObjectEstimate RealStateObject = RealParser.GetState(StartTime + new TimeSpan(0, 0, 0, 0, PollingDelay * CurrTimeStage));
                        if (RealStateObject != null)
                            RealState.Add(RealStateObject);
                    }
                    Sender.SendData(RawMeasurementEstimate, ObjectCandidateEstimate, ObjectCandidateEstimator.CurrAdditionalInfo, RealState);
                    Console.WriteLine("Data for time stage {0} sent to display server", CurrTimeStage);
                }

                RemoveTimeStageMeasurements(CurrTimeStage);
                // Wait a delay equal to the PollingDelay to give sensors time to take and send their measurements
                if (StartTime != DateTime.MinValue)
                {
                    DateTime ExpectedTime = LocalStartTime.AddMilliseconds(TOLERANCE_LAG + PollingDelay * (CurrTimeStage + 1));
                    int SleepTime = DateTime.Now > ExpectedTime ? 0 : PollingDelay;
                    Thread.Sleep(SleepTime); //Don't wait for the polling delay if we are behind schedule.
                    CurrTimeStage++;
                }
                else
                    Thread.Sleep(PollingDelay);
            }
        }
    } // End class
} // End namespace