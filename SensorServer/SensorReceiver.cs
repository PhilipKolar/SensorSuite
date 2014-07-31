using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using WSNUtil;

namespace SensorServer
{
    public delegate void MessageReceievedDelegate(int sensorID, float distance, DateTime timeStamp);
    public class SensorReceiver
    {
        private IPAddress _ServerIP;
        private int _PortNo;
        private TcpListener _ListenerConnection;
        private Dictionary<TcpClient, NetworkStream> _ClientStreams;
        
        public event MessageReceievedDelegate OnMessageReceived;

        /// <summary>
        /// Creates a SensorReceiver that listens for incoming connections and records all incoming data.
        /// </summary>
        /// <param name="configFile">The location of the .ini file containing the Server IP/Port configuration</param>
        public SensorReceiver(string configFile)
        {
            _ServerIP = Variables.GetSensorServerIP(configFile);
            _PortNo = Variables.GetSensorPort(configFile);
            _ClientStreams = new Dictionary<TcpClient, NetworkStream>();
        }

        public void Start()
        {
            Thread t = new Thread(new ThreadStart(_StartListener));
            t.Start();
        }

        /// <summary>
        /// Creates a new TcpClient object for _ClientConnection and attempts to connect to it.
        /// </summary>
        private void _StartListener() //Following documentation: http://msdn.microsoft.com/en-us/library/vstudio/system.net.sockets.tcplistener
        {
            //Start listening for client requests
            _ListenerConnection = new TcpListener(_ServerIP, _PortNo);
            _ListenerConnection.Start();
            Console.WriteLine("Waiting for connection...");

            //Listening loop
            try
            {
                while (true)
                {
                    TcpClient Client = _ListenerConnection.AcceptTcpClient();
                    Console.WriteLine("{1}\nNew client ('{0}') connected\n{1}", _GetIP(Client).ToString(), new string('*', 40));
                    NetworkStream ListenerStream = Client.GetStream();
                    _ClientStreams.Add(Client, ListenerStream);

                    Thread NewMonitoringStream = new Thread(new ParameterizedThreadStart(_MonitorStream));
                    NewMonitoringStream.Start(new Tuple<TcpClient, NetworkStream> (Client, ListenerStream));
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void _MonitorStream(object clientAndStreamTuple)
        {
            Tuple<TcpClient, NetworkStream> TupleArgument = (Tuple<TcpClient, NetworkStream>) clientAndStreamTuple;
            NetworkStream ListenerStream = TupleArgument.Item2;
            TcpClient Client = TupleArgument.Item1;

            while (true)
            {
                byte[] buffer = new byte[4 + 4 + 8]; //int (sensorID), float (distance), long (timeStampTicks)
                try
                {
                    _WaitAndRead(ListenerStream, buffer, buffer.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading from NetworkStream for client '{0}'. No longer monitoring this client. Exception message: '{1}'",
                        _GetIP(Client).ToString(), ex.Message);
                    return;
                }
                int sensorID = BitConverter.ToInt32(buffer, 0);
                float distance = BitConverter.ToSingle(buffer, 4);
                long ticks = BitConverter.ToInt64(buffer, 4 + 4);
                if (distance == 0.0f && ticks == 0)
                {
                    Console.WriteLine("Received end sequence from client {0}, no longer listening to this client", _GetIP(Client).ToString());
                    ListenerStream.Close();
                    Client.Close();
                    _ClientStreams.Remove(Client);
                    return;
                }
                DateTime dt = new DateTime(ticks);
                OnMessageReceived(sensorID, distance, dt);
            }
        }

        private void _WaitAndRead(NetworkStream stream, byte[] buffer, int byteCount)
        {
            int TotalBytesRead = 0;
            while (TotalBytesRead != byteCount)
                TotalBytesRead += stream.Read(buffer, TotalBytesRead, byteCount - TotalBytesRead);
        }

        private IPAddress _GetIP(TcpClient client)
        {
            IPEndPoint temp = client.Client.RemoteEndPoint as IPEndPoint;
            IPAddress ip = temp.Address;
            return ip;
        }
    } // End class
} // End namespace
