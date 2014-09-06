using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using WSNUtil;
using System.IO; //For IOException

namespace SensorClient
{
    public class SensorSender
    {
        private IPAddress _ServerIP;
        private int _PortNo;
        private TcpClient _ClientConnection;
        private NetworkStream _ListenerStream;
        private Semaphore _ConnectionEstablished = new Semaphore(0, 1);
        public int SensorID { get; private set; }

        public void WaitForConnection()
        {
            _ConnectionEstablished.WaitOne();
            _ConnectionEstablished.Release();
        }

        public bool Connected
        {
            get { return _ClientConnection == null ? false : _ClientConnection.Connected; }
        }
        public bool CanSend { get; private set; }

        /// <summary>
        /// Creates a DataSender that sends data to the specified ipAddress and portNo. Attemps to connect to the server on a seperate thread,
        /// use DataSender.Connected to check for connection status
        /// </summary>
        /// <param name="ipAddress">IP Address of listener to connect to</param>
        /// <param name="configFile">The location of the .ini file containing the Server IP/Port configuration</param>
        public SensorSender(int sensorID, string configFile)
        {
            _ServerIP = Variables.GetSensorServerIP(configFile);
            _PortNo = Variables.GetSensorPort(configFile);
            SensorID = sensorID;
            CanSend = false;
        }

        public void Start()
        {
            Thread t = new Thread(new ThreadStart(_InitConnection));
            t.Start();
        }

        private bool _AttemptingConnection = false; //Only allow one thread to access the connection code. TODO: Check for and fix race conditions
        /// <summary>
        /// Creates a new TcpClient object for _ClientConnection and attempts to connect to it.
        /// </summary>
        private void _InitConnection()
        {
            if (!Connected && !_AttemptingConnection)
            {
                _AttemptingConnection = true;
                if (_ClientConnection != null) //Reconnecting, clean up before continuing
                {
                    _Reset();
                    Console.WriteLine("Lost connection, attempting to reconnect to {0}...", _ServerIP.ToString());
                }
                while (_ClientConnection == null)
                {
                    try
                    {
                        _ClientConnection = new TcpClient(_ServerIP.ToString(), _PortNo);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(string.Format("Failed to connect to {0}. Exception message: {1}", _ServerIP.ToString(), ex.Message));
                        Console.WriteLine("Retrying connection to {0}...", _ServerIP.ToString());
                    }
                }
                _AttemptingConnection = false;
                CanSend = true;
                _ConnectionEstablished.Release();
            }
            else
                Console.WriteLine("Already connected or connecting, _InitConnection() aborting");
        }

        private void _Reset()
        {
            _ClientConnection.Close();
            _ClientConnection = null;
            _ListenerStream = null;
            CanSend = false;
            _ConnectionEstablished = new Semaphore(0, 1);
        }

        public void SendData(float distance, DateTime timeStamp)
        {
            if (!Connected || _AttemptingConnection)
                throw new Exception("Could not send message to listener, not currently connected.");

            byte[] DataToSend = new byte[4 + 4 + 8]; //4 bytes for an int32 (sensorID), 4 bytes for a float (distance), 8 bytes for a long (timeStamp in ticks form).
            Array.Copy(BitConverter.GetBytes(SensorID), DataToSend, 4); // Copy in the sensor ID
            Array.Copy(BitConverter.GetBytes(distance), 0, DataToSend, 4, 4); //Copy in float distance.
            Array.Copy(BitConverter.GetBytes(timeStamp.Ticks), 0, DataToSend, 8, 8); //Copy in timeStamp in the form of tick count.

            if (_ListenerStream == null) //Runs the first time only
                _ListenerStream = _ClientConnection.GetStream();

            try
            {
                _ListenerStream.Write(DataToSend, 0, DataToSend.Length);
            }
            catch (Exception e) //Sensor server shuts socket down
            {
                _InitConnection();
                Console.WriteLine("Connection established");
            }
        }

        public void SendData(Tuple<float, DateTime> dataToSend)
        {
            SendData(dataToSend.Item1, dataToSend.Item2);
        }


        public void Close()
        {
            try
            { SendData(0.0f, new DateTime(0)); } //Send an end sequence to the server before finishing
            catch (Exception ex)
            { Console.WriteLine("Warning: Could not set end sequence to server. Exception message: '{0}'", ex.Message); }

            if (_ListenerStream != null)
                _ListenerStream.Close();
            if (_ClientConnection != null)
                _ClientConnection.Close();
        }
    } // End class
} // End namespace
