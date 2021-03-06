﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace WSNUtil
{
    public enum SensorServerMode { Normal, MonitorOnly, ReadFromStore };
    public enum SensorClientMode { SendRealData, SendRandomData };

    public static partial class Variables
    {
        private const string _CONFIG_LOCATOR_FILE = @"../../../SensorServer/bin/Debug/ConfigFiles.ini";
        private static string _DefaultINILocation;
        private static string _DefaultCSVLocation;
        public static string DefaultINILocation {
            get
            {
                if (_DefaultINILocation == null)
                    _DefaultINILocation = ConfigFileParser.RetrieveString("INI_FILE", _CONFIG_LOCATOR_FILE);
                return _DefaultINILocation;
            }
        }
        public static string DefaultCSVLocation {
            get
            {
                if (_DefaultCSVLocation == null)
                    _DefaultCSVLocation = ConfigFileParser.RetrieveString("CSV_FILE", _CONFIG_LOCATOR_FILE);
                return _DefaultCSVLocation;
            }
        }
        public static int GetSensorServerTrilateratorLeastSquaresIterations(string iniFile)
        {
            return ConfigFileParser.RetrieveInt("TRILATERATOR_NOISY_2D_LEAST_SQUARES_ITERATIONS", iniFile);
        }

        public static float GetSensorServerTrilateratorGroupingThresholdLeastSquares(string iniFile)
        {
            return ConfigFileParser.RetrieveFloat("TRILATERATOR_NOISY_2D_LEAST_SQUARES_GROUPING_THRESHOLD", iniFile);
        }

        public static string GetSensorServerOctaveBinaryPath(string iniFile)
        {
            return ConfigFileParser.RetrieveString("SENSOR_SERVER_OCTAVE_BINARY_PATH", iniFile);
        }

        public static string GetSensorServerTrilateratorScriptPathLeastSquares(string iniFile)
        {
            return ConfigFileParser.RetrieveString("TRILATERATOR_NOISY_2D_LEAST_SQUARES_OCTAVE_SCRIPT_PATH", iniFile);
        }

        public static float GetSensorServerTrilateratorGroupingThreshhold(string iniFile)
        {
            return ConfigFileParser.RetrieveFloat("TRILATERATOR_NOISY_2D_GROUPING_THRESHOLD", iniFile);
        }

        public static int GetSensorServerTrilateratorGridDivison(string iniFile)
        {
            return ConfigFileParser.RetrieveInt("TRILATERATOR_NOISY_2D_GRID_DIVISON", iniFile);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iniFile"></param>
        /// <returns>A string of the trilaterator's class name in ALL CAPS</returns>
        public static string GetSensorServerInitialEstimatorTrilaterator(string iniFile)
        {
            return ConfigFileParser.RetrieveString("INITIAL_ESTIMATOR_TRILATERATOR", iniFile).ToUpper();
        }

        public static bool GetSensorServerUseInitialMeasurementAsState(string iniFile)
        {
            return ConfigFileParser.RetrieveBool("USE_INITIAL_MEASUREMENT_AS_STATE", iniFile);
        }

        public static int GetSensorServerMeasurementWaitLag(string iniFile)
        {
            return ConfigFileParser.RetrieveInt("SENSOR_SERVER_MEASUREMENT_WAIT_LAG", iniFile);
        }

        public static string GetSensorServerRealStateFilePath(string iniFile)
        {
            return ConfigFileParser.RetrieveString("SENSOR_SERVER_REAL_STATE", iniFile);
        }

        public static bool GetSensorServerTrilateratorNoiseless0DAveragingAnchor(string iniFile)
        {
            return ConfigFileParser.RetrieveBool("TRILATERATOR_NOISELESS_0D_AVERAGING_ANCHOR", iniFile);
        }

        public static double GetSensorServerTrilateratorNoiseless0DDistanceTolerance(string iniFile)
        {
            return ConfigFileParser.RetrieveDouble("TRILATERATOR_NOISELESS_0D_DISTANCE_TOLERANCE", iniFile);
        }

        public static bool GetDrawDisplayServerDrawXY1To1(string iniFile)
        {
            return ConfigFileParser.RetrieveBool("DRAW_XY_AXIS_1_TO_1", iniFile);
        }
        public static bool GetDisplayServerDrawReal(string iniFile)
        {
            return ConfigFileParser.RetrieveBool("DRAW_REAL", iniFile);
        }

        public static bool GetDisplayServerDrawAdditional(string iniFile)
        {
            return ConfigFileParser.RetrieveBool("DRAW_ADDITIONAL", iniFile);
        }

        public static bool GetDisplayServerDrawEsimated(string iniFile)
        {
            return ConfigFileParser.RetrieveBool("DRAW_ESTIMTAED", iniFile);
        }

        public static bool GetDisplayServerDrawTrilaterated(string iniFile)
        {
            return ConfigFileParser.RetrieveBool("DRAW_TRILATERATED", iniFile);
        }

        public static bool GetDisplayServerDrawMeasurements(string iniFile)
        {
            return ConfigFileParser.RetrieveBool("DRAW_MEASUREMENTS", iniFile);
        }

        public static bool GetDisplayServerDrawSensors(string iniFile)
        {
            return ConfigFileParser.RetrieveBool("DRAW_SENSORS", iniFile);
        }

        public static bool GetDisplayServerDrawLegend(string iniFile)
        {
            return ConfigFileParser.RetrieveBool("DRAW_LEGEND", iniFile);
        }

        /// <summary>
        /// Measured in milliseconds
        /// </summary>
        /// <param name="iniFile"></param>
        /// <returns></returns>
        public static float GetSensorClientTimeSchedulingTolerance_ms(string iniFile)
        {
            return ConfigFileParser.RetrieveFloat("SENSOR_CLIENT_TIME_SCHEDULING_TOLERANCE", iniFile);
        }

        public static string GetDisplayServerResultFolder(string iniFile)
        {
            return ConfigFileParser.RetrieveString("RESULT_OUTPUT_FOLDER", iniFile);
        }

        public static int GetSensorClientBuzzerPin(string iniFile)
        {
            return ConfigFileParser.RetrieveInt("SENSOR_CLIENT_BUZZER_PIN", iniFile);
        }

        public static decimal GetSensorClientBuzzerTimeOn_ms(string iniFile)
        {
            return (decimal)ConfigFileParser.RetrieveFloat("SENSOR_CLIENT_BUZZER_TIMEON", iniFile);
        }

        public static decimal GetSensorClientBuzzerTimeOff_ms(string iniFile)
        {
            return (decimal)ConfigFileParser.RetrieveFloat("SENSOR_CLIENT_BUZZER_TIMEOFF", iniFile);
        }

        public static string GetSensorServerMeasureStoreFilePath(string iniFile)
        {
            return ConfigFileParser.RetrieveString("SENSOR_SERVER_MEASURESTORE_FILEPATH", iniFile);
        }

        /// <summary>
        /// Entry in the INI file should be in degrees CELCIUS
        /// </summary>
        /// <param name="iniFile"></param>
        /// <returns></returns>
        public static float GetSensorClientTemperature_celcius(string iniFile)
        {
            return ConfigFileParser.RetrieveFloat("SENSOR_CLIENT_TEMPERATURE", iniFile);
        }

        public static bool GetSensorClientTimeDivison(string iniFile)
        {
            return ConfigFileParser.RetrieveBool("SENSOR_CLIENT_TIME_DIVISON", iniFile);
        }

        public static string GetSensorClientMeasureExecutable(string iniFile)
        {
            return ConfigFileParser.RetrieveString("SENSOR_CLIENT_MEASURE_EXECUTABLE", iniFile);
        }

        public static int GetSensorPort(string iniFile)
        {
            return ConfigFileParser.RetrieveInt("SENSOR_PORT", iniFile);
        }

        /// <summary>
        /// Field name: SENSOR_SERVER_MODE
        /// Valid values: Normal, MonitorOnly, ReadFromStore
        /// </summary>
        /// <param name="iniFile"></param>
        /// <returns></returns>
        public static SensorServerMode GetSensorServerMode(string iniFile)
        {
            string Result = ConfigFileParser.RetrieveString("SENSOR_SERVER_MODE", iniFile).ToUpper();
            if (Result == "NORMAL")
                return SensorServerMode.Normal;
            else if (Result == "MONITORONLY")
                return SensorServerMode.MonitorOnly;
            else if (Result == "READFROMSTORE")
                return SensorServerMode.ReadFromStore;
            else
                throw new Exception(string.Format("Error: Field for SENSOR_SERVER_MODE in file '{0}' is invalid ({1}). Must be either 'Normal', 'MonitorOnly', or 'ReadFromStore' (quotes not included).",
                    iniFile, Result));
        }

        public static SensorClientMode GetSensorClientMode(string iniFile)
        {
            string Result = ConfigFileParser.RetrieveString("SENSOR_CLIENT_MODE", iniFile).ToUpper();
            if (Result == "SENDREALDATA")
                return SensorClientMode.SendRealData;
            else if (Result == "SENDRANDOMDATA")
                return SensorClientMode.SendRandomData;
            else
                throw new Exception(string.Format("Error: Field for SENSOR_CLIENT_MODE in file '{0}' is invalid ({1}). Must be either 'SendRealData' or 'SendRandomData' (quotes not included).",
                    iniFile, Result));
        }

        public static int GetDisplayPort(string iniFile)
        {
            return ConfigFileParser.RetrieveInt("DISPLAY_PORT", iniFile);
        }

        public static IPAddress GetSensorServerIP(string iniFile)
        {
            return ConfigFileParser.RetrieveIP("SENSOR_SERVER_IP", iniFile);
        }

        public static IPAddress GetDisplayIP(string iniFile)
        {
            return ConfigFileParser.RetrieveIP("DISPLAY_SERVER_IP", iniFile);
        }

        /// <summary>
        /// Retrieves a polling delay measued in ms (milliseconds)
        /// </summary>
        /// <param name="configFile">File path of the INI-formatted confuration file containing a "POLLING_DELAY" property</param>
        /// <returns></returns>
        public static int GetPollingDelay_ms(string iniFile)
        {
            return ConfigFileParser.RetrieveInt("POLLING_DELAY", iniFile);
        }

        public static int GetSensorClientTriggerPin(string iniFile)
        {
            return ConfigFileParser.RetrieveInt("SENSOR_CLIENT_TRIGGER_PIN", iniFile);
        }

        public static int GetSensorClientEchoPin(string iniFile)
        {
            return ConfigFileParser.RetrieveInt("SENSOR_CLIENT_ECHO_PIN", iniFile);
        }

        public static float GetSensorClientMaxDistance_cm(string iniFile)
        {
            return ConfigFileParser.RetrieveFloat("SENSOR_CLIENT_MAX_DISTANCE", iniFile);
        }

        /// <summary>
        /// Read from a CSV file to retrieve sensor information. Comments allowed with '#' or ';'. Data should be in format:
        /// SensorID, Theta, Phi, X, Y
        /// </summary>
        /// <param name="csvFile">The location of the config file to read</param>
        /// <returns></returns>
        public static List<Sensor> GetSensorConfig(string csvFile)
        {
            StreamReader Reader = new StreamReader(csvFile);
            List<Sensor> ToReturn = new List<Sensor>();
            int SensorID = 1;
            while (!Reader.EndOfStream)
            {
                float Theta, Phi, X, Y, Distance;
                string CurrentLine = Reader.ReadLine();
                string[] CommentSplit = CurrentLine.Split(new char[] { '#', ';' });
                string[] CommaSplit = CommentSplit[0].Split(',');

                if (CommentSplit.Length > 1 && CommaSplit.Length == 1) // Don't display a warning if the line is just a comment
                    continue;
                if (CurrentLine.Trim() == "") //Don't display a warning for empty lines
                    continue;
                if (CommaSplit.Length != 5)
                {
                    Console.WriteLine("Warning: Invalid line detected in sensor config file: '{0}'. Too many elements (must be 5)", CurrentLine);
                    continue;
                }
                if (float.TryParse(CommaSplit[0], out Theta) == false)
                {
                    Console.WriteLine("Warning: Invalid Theta value detected in sensor config file in line: '{0}'. Must be in float format", CurrentLine);
                    continue;
                }
                if (float.TryParse(CommaSplit[1], out Phi) == false)
                {
                    Console.WriteLine("Warning: Invalid Phi value detected in sensor config file in line: '{0}'. Must be in float format", CurrentLine);
                    continue;
                }
                if (float.TryParse(CommaSplit[2], out X) == false)
                {
                    Console.WriteLine("Warning: Invalid X value detected in sensor config file in line: '{0}'. Must be in float format", CurrentLine);
                    continue;
                }
                if (float.TryParse(CommaSplit[3], out Y) == false)
                {
                    Console.WriteLine("Warning: Invalid Y value detected in sensor config file in line: '{0}'. Must be in float format", CurrentLine);
                    continue;
                }
                if (float.TryParse(CommaSplit[4], out Distance) == false)
                {
                    Console.WriteLine("Warning: Invalid Distance value detected in sensor config file in line: '{0}'. Must be in float format", CurrentLine);
                    continue;
                }

                Sensor CurrentSensor = new Sensor(SensorID, Theta, Phi, X, Y, Distance);
                ToReturn.Add(CurrentSensor);
                SensorID++;
            }
            Reader.Close();

            return ToReturn;
        }
    } // End Class
} // End Namespace
