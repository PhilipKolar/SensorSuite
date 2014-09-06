using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using WSNUtil;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace DisplayServer //TODO: Change console.WriteLine()'s to something in the form
{
    public delegate void MessageReceievedDelegate(List<ObjectEstimate> RawData, List<ObjectEstimate> StateEstimate, List<ObjectEstimate> AdditionalStateInfo, List<ObjectEstimate> RealState);
    class DisplayReceiver
    {
        private IPAddress _ServerIP;
        private int _PortNo;
        private TcpListener _ListenerConnection;
        private Dictionary<TcpClient, NetworkStream> _ClientStreams;
        private Semaphore _ConcurrentConnectionLimit = new Semaphore(1, 1);
        private TextBox _OutputTextbox;
        private Semaphore _ConnectionEstablishedSemaphore = new Semaphore(0, 1);

        public void WaitForConnection()
        {
            _ConnectionEstablishedSemaphore.WaitOne();
            _ConnectionEstablishedSemaphore.Release();
        }

        public string ClientIP { get; private set; }
        
        public event MessageReceievedDelegate OnMessageReceived;

        /// <summary>
        /// Creates a DisplayReceiver that listens for incoming connections and records all incoming data.
        /// </summary>
        /// <param name="configFile">The location of the .ini file containing the Server IP/Port configuration</param>
        public DisplayReceiver(string configFile, TextBox outputLabel)
        {
            _ServerIP = Variables.GetDisplayIP(configFile);
            _PortNo = Variables.GetDisplayPort(configFile);
            _ClientStreams = new Dictionary<TcpClient, NetworkStream>();
            _OutputTextbox = outputLabel;
        }

        public void Start()
        {
            Thread t = new Thread(new ThreadStart(_StartListener));
            t.Start();
        }

        /// <summary>
        /// Creates a new TcpClient object for _ClientConnection and attempts to connect to it.
        /// </summary>
        private void _StartListener()
        {
            //Start listening for client requests
            _ListenerConnection = new TcpListener(_ServerIP, _PortNo);
            _ListenerConnection.Start();

            //Listening loop
            while (true)
            {
                _ConcurrentConnectionLimit.WaitOne();
                TcpClient Client = _ListenerConnection.AcceptTcpClient();
                ClientIP = _GetIP(Client).ToString();
                NetworkStream ListenerStream = Client.GetStream();
                _ClientStreams.Add(Client, ListenerStream);
                _ConnectionEstablishedSemaphore.Release();

                Thread NewMonitoringStream = new Thread(new ParameterizedThreadStart(_MonitorStream));
                NewMonitoringStream.Start(new Tuple<TcpClient, NetworkStream> (Client, ListenerStream));
            }
        }

        private void _MonitorStream(object clientAndStreamTuple)
        {
            Tuple<TcpClient, NetworkStream> TupleArgument = (Tuple<TcpClient, NetworkStream>) clientAndStreamTuple;
            NetworkStream ListenerStream = TupleArgument.Item2;
            TcpClient Client = TupleArgument.Item1;

            while (true)
            {
                //Retrieve the raw data from the network stream
                byte[] RawDataSizeBuffer = new byte[4];
                _WaitAndRead(ListenerStream, RawDataSizeBuffer, RawDataSizeBuffer.Length);
                int RawDataSize = BitConverter.ToInt32(RawDataSizeBuffer, 0);
                byte[] RawDataBuffer = new byte[RawDataSize];
                _WaitAndRead(ListenerStream, RawDataBuffer, RawDataSize);
                //Retrieve the state estimation data from the network stream
                byte[] StateSizeBuffer = new byte[4];
                _WaitAndRead(ListenerStream, StateSizeBuffer, StateSizeBuffer.Length);
                int StateSize = BitConverter.ToInt32(StateSizeBuffer, 0);
                byte[] StateBuffer = new byte[StateSize];
                _WaitAndRead(ListenerStream, StateBuffer, StateSize);
                //Retrieve the additional state estimation data from the network stream
                byte[] AdditionalStateSizeBuffer = new byte[4];
                _WaitAndRead(ListenerStream, AdditionalStateSizeBuffer, AdditionalStateSizeBuffer.Length);
                int AdditionalStateSize = BitConverter.ToInt32(AdditionalStateSizeBuffer, 0);
                byte[] AdditionalStateBuffer = new byte[AdditionalStateSize];
                _WaitAndRead(ListenerStream, AdditionalStateBuffer, AdditionalStateSize);
                //Retrieve the real state data from the network stream
                byte[] RealStateSizeBuffer = new byte[4];
                _WaitAndRead(ListenerStream, RealStateSizeBuffer, RealStateSizeBuffer.Length);
                int RealStateSize = BitConverter.ToInt32(RealStateSizeBuffer, 0);
                byte[] RealStateBuffer = new byte[RealStateSize];
                _WaitAndRead(ListenerStream, RealStateBuffer, RealStateSize);


                //Check for an end sequence //TODO: FIX!
                //if (RawDataSize == 330 && StateSize == 330) //An empty list of ObjectEstimates will be 330 bytes
                //{
                //    _OutputTextbox.Invoke((MethodInvoker)(() => _OutputTextbox.Text += string.Format("Received end sequence from client {0}, no longer listening to this client\n", _GetIP(Client).ToString())));
                //    ListenerStream.Close();
                //    Client.Close();
                //    _ClientStreams.Remove(Client);
                //    _ConcurrentConnectionLimit.Release();
                //    return;
                //}

                BinaryFormatter Formatter = new BinaryFormatter();

                //Deserialise the raw data into its original List<ObjectEsimate> form
                MemoryStream RawDataStream = new MemoryStream();
                RawDataStream.Write(RawDataBuffer, 0, RawDataBuffer.Length);
                RawDataStream.Position = 0;
                List<ObjectEstimate> RawDataList = (List<ObjectEstimate>)Formatter.Deserialize(RawDataStream);
                //Deserialise the state estimate data into its original List<ObjectEsimate> form
                MemoryStream StateStream = new MemoryStream();
                StateStream.Write(StateBuffer, 0, StateBuffer.Length);
                StateStream.Position = 0;
                List<ObjectEstimate> StateList = (List<ObjectEstimate>)Formatter.Deserialize(StateStream);
                //Deserialise the additional state estimation data into its original List<ObjectEsimate> form
                MemoryStream AdditionalStateStream = new MemoryStream();
                AdditionalStateStream.Write(AdditionalStateBuffer, 0, AdditionalStateBuffer.Length);
                AdditionalStateStream.Position = 0;
                List<ObjectEstimate> AdditionalStateList = (List<ObjectEstimate>)Formatter.Deserialize(AdditionalStateStream);
                //Deserialise the real state data into its original List<ObjectEsimate> form
                MemoryStream RealStateStream = new MemoryStream();
                RealStateStream.Write(RealStateBuffer, 0, RealStateBuffer.Length);
                RealStateStream.Position = 0;
                List<ObjectEstimate> RealStateList = (List<ObjectEstimate>)Formatter.Deserialize(RealStateStream);

                OnMessageReceived(RawDataList, StateList, AdditionalStateList, RealStateList);
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
