using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using WSNUtil;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace SensorServer
{
    class DisplaySender
    {
        private IPAddress _ServerIP;
        private int _PortNo;
        private TcpClient _ClientConnection;
        private NetworkStream _ListenerStream;
        public Semaphore ConnectionEstablished = new Semaphore(0, 1);

        public bool Connected
        {
            get { return _ClientConnection == null ? false : _ClientConnection.Connected; }
        }

        public DisplaySender(string configFile)
        {
            _ServerIP = Variables.GetDisplayIP(configFile);
            _PortNo = Variables.GetDisplayPort(configFile);
        }

        public void Start()
        {
            Thread t = new Thread(new ThreadStart(_InitConnection));
            t.Start();
        }

        private void _InitConnection()
        {
            try
            {
                _ClientConnection = new TcpClient(_ServerIP.ToString(), _PortNo);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Failed to connect to {0}. Exception message: {1}", _ServerIP.ToString(), ex.Message));
            }
            ConnectionEstablished.Release();
        }

        public void SendData(List<ObjectEstimate> rawDataToSend, List<ObjectEstimate> stateToSend, List<ObjectEstimate> additionalInfoToSend)
        {
            if (!Connected)
                throw new Exception("Could not send message to listener, not currently connected.");

            BinaryFormatter Formatter = new BinaryFormatter();
            MemoryStream RawDataStream = new MemoryStream();
            MemoryStream StateStream = new MemoryStream();
            MemoryStream AdditionalInfoStream = new MemoryStream();
            Formatter.Serialize(RawDataStream, rawDataToSend);
            Formatter.Serialize(StateStream, stateToSend);
            Formatter.Serialize(AdditionalInfoStream, additionalInfoToSend);
            byte[] DataToSend = new byte[4 + RawDataStream.Length + 4 + StateStream.Length + 4 + AdditionalInfoStream.Length]; //Each 4 bytes is for an int representing size of stream (e.g. RawDataStream)
            RawDataStream.Position = 0;
            RawDataStream.Read(DataToSend, 4, (int)RawDataStream.Length);
            StateStream.Position = 0;
            StateStream.Read(DataToSend, 4 + (int)RawDataStream.Length + 4, (int)StateStream.Length);
            AdditionalInfoStream.Position = 0;
            AdditionalInfoStream.Read(DataToSend, 4 + (int)RawDataStream.Length + 4 + (int)StateStream.Length + 4, (int)AdditionalInfoStream.Length);

            Array.Copy(BitConverter.GetBytes((int)RawDataStream.Length), 0, DataToSend, 0, 4); //Copy in the length of rawDataToSend
            Array.Copy(BitConverter.GetBytes((int)StateStream.Length), 0, DataToSend, 4 + (int)RawDataStream.Length, 4); //Copy in the length of statetoSend
            Array.Copy(BitConverter.GetBytes((int)AdditionalInfoStream.Length), 0, DataToSend, 4 + (int)RawDataStream.Length + 4 + (int)StateStream.Length, 4); //Copy in the length of additionalInfoToSend

            if (_ListenerStream == null) //Runs the first time only
                _ListenerStream = _ClientConnection.GetStream();

            _ListenerStream.Write(DataToSend, 0, DataToSend.Length);
        }

        public void Close()
        {
            try
            {
                SendData(new List<ObjectEstimate>(), new List<ObjectEstimate>(), new List<ObjectEstimate>()); //Send an end sequence to the server before finishing
            }
            catch (Exception ex)
            {
                Console.WriteLine("Warning: Could not set end sequence to server. Exception message: '{0}'", ex.Message);
            }

            if (_ListenerStream != null)
                _ListenerStream.Close();
            if (_ClientConnection != null)
                _ClientConnection.Close();
        }
    } // End class
} // End namespace
