using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using SensorClient.Schedulers;
using WSNUtil;

namespace SensorClient
{
    class Program
    {
        static string CSVFile = Variables.DefaultCSVLocation;
        static string INIFile = Variables.DefaultINILocation;
        static Mutex ConsoleOutputMutex = new Mutex(false);

        static void Main(string[] args)
        {
            SensorClientMode Mode;
            IScheduler Scheduler;
            try
            {
                Mode = Variables.GetSensorClientMode(INIFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving mode of operation for sensor client in INI file at {0}. Exception message: '{1}'.\nShutting sensor server down.", INIFile, ex.Message);
                return;
            }

            if (Mode == SensorClientMode.SendRealData)
            {
                Console.WriteLine("Beginning in SendRealData mode");
                if (!ArgsIsValid(args))
                    return;
                int SensorID = int.Parse(args[0]);
                bool TimeDivision = Variables.GetSensorClientTimeDivison(INIFile);
                Scheduler = new RealDataScheduler(INIFile, CSVFile, SensorID, TimeDivision);
                Scheduler.SendData();
                int BuzzerPin = Variables.GetSensorClientBuzzerPin(INIFile);
                if (BuzzerPin > 0)
                {
                    PiezoBuzzer Buzzer = new PiezoBuzzer(BuzzerPin, Variables.GetSensorClientBuzzerTimeOn(INIFile),
                        Variables.GetSensorClientBuzzerTimeOff(INIFile));
                    Buzzer.Start();

                    PromptUntilExit("q");
                    Console.WriteLine("Shutting real sensor down");
                    Buzzer.Stop();
                    Buzzer.Dispose();
                }
                else
                {
                    PromptUntilExit("q");
                    Console.WriteLine("Shutting real sensor down");
                }
            }
            else if (Mode == SensorClientMode.SendRandomData)
            {
                Console.WriteLine("Beginning in SendRandomData mode");
                Scheduler = new RandomDataScheduler(CSVFile, INIFile);
                Scheduler.SendData();

                PromptUntilExit("q");
                Console.WriteLine("Shutting random sensors down");
            }
            else
                throw new Exception("Unknown SensorClientMode");

            Scheduler.Close();
            Thread.Sleep(1000);
            Environment.Exit(0); //Force exit after 1s, occurs if foreground threads take too long (e.g. blocking netcode that takes very long to timeout)
        }

        static void PromptUntilExit(string ExitSequence)
        {
            ExitSequence = ExitSequence.ToLower();
            string Input = ExitSequence + "a"; //Add anything to ExitSequence just to get a different string
            do
            {
                Console.WriteLine("Enter '{0}' to shut sensor down: ", ExitSequence);
                Input = Console.ReadLine();
            } while (Input.ToLower() != ExitSequence);
        }

        static bool ArgsIsValid(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Error: 1 argument expected. Must contain a valid SensorID.");
                return false;
            }

            int SensorID;
            if (int.TryParse(args[0], out SensorID) == false)
            {
                Console.WriteLine("Error: argument must be an integer.");
                return false;
            }
            //Check that the SensorID is contained in the CSVFile (i.e. is a valid sensor ID)
            List<Sensor> Sensors = Variables.GetSensorConfig(CSVFile);
            bool IDFound = false;
            foreach (Sensor S in Sensors)
            {
                if (S.SensorID == SensorID)
                {
                    IDFound = true;
                    break;
                }
            }
            if (!IDFound)
            {
                Console.WriteLine("Error: SensorID specified was not found in the CSV file at '{0}'", CSVFile);
                return false;
            }

            return true;
        }
    } // End class
} // End namespace