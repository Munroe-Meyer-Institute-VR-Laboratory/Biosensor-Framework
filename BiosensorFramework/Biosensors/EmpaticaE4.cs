using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;

using MMIVR.BiosensorFramework.Exceptions;

namespace MMIVR.BiosensorFramework.Biosensors.EmpaticaE4
{
    public class ServerClient : IDisposable
    {
        public enum DeviceStreams { ACC, BVP, GSR, IBI, HR, TMP, BAT, TAG }
        // Connection to be used in communications
        public Socket SocketConnection;
        // Name of the device for connection
        public string DeviceName = string.Empty;
        // List of subscribed streams for synchronizing readings
        public List<DeviceStreams> SubscribedStreams = new List<DeviceStreams>();
        // Lists for readings
        public List<double> ACCReadings3D = new List<double>();
        public List<double> ACCReadingsX = new List<double>();
        public List<double> ACCReadingsY = new List<double>();
        public List<double> ACCReadingsZ = new List<double>();
        public List<double> ACCTimestamps = new List<double>();

        public List<double> BVPReadings = new List<double>();
        public List<double> BVPTimestamps = new List<double>();

        public List<double> GSRReadings = new List<double>();
        public List<double> GSRTimestamps = new List<double>();

        public List<double> IBIReadings = new List<double>();
        public List<double> IBITimestamps = new List<double>();

        public List<double> HRReadings = new List<double>();
        public List<double> HRTimestamps = new List<double>();

        public List<double> TMPReadings = new List<double>();
        public List<double> TMPTimestamps = new List<double>();

        public List<double> BATReadings = new List<double>();
        public List<double> BATTimestamps = new List<double>();

        public List<double> TAGReadings = new List<double>();
        public List<double> TAGTimestamps = new List<double>();
        // Size of receive buffer.
        public const int BufferSize = 4096;
        // Receive buffer.
        public readonly byte[] Buffer = new byte[BufferSize];
        // Received data string.
        public readonly StringBuilder Sb = new StringBuilder();
        // The port number for the remote device.
        private const string ServerAddress = "127.0.0.1";
        private const int ServerPort = 28000;
        // ManualResetEvent instances signal completion.
        public readonly ManualResetEvent ConnectDone = new ManualResetEvent(false);
        public readonly ManualResetEvent SendDone = new ManualResetEvent(false);
        public readonly ManualResetEvent ReceiveDone = new ManualResetEvent(false);
        // The response from the remote device.
        private static String _response = String.Empty;
        /// <summary>  </summary>
        public void StartClient()
        {
            // Connect to a remote device.
            try
            {
                // Establish the remote endpoint for the socket.
                var ipHostInfo = new IPHostEntry { AddressList = new[] { IPAddress.Parse(ServerAddress) } };
                var ipAddress = ipHostInfo.AddressList[0];
                var remoteEp = new IPEndPoint(ipAddress, ServerPort);

                // Create a TCP/IP socket.
                SocketConnection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.
                SocketConnection.BeginConnect(remoteEp, ConnectCallback, SocketConnection);
            }
            catch (Exception e)
            {
                throw new ClientConnectionException("Exception encountered when starting TCP client: " + e.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ar"></param>
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Complete the connection.
                SocketConnection.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}", SocketConnection.RemoteEndPoint);

                // Signal that the connection has been made.
                ConnectDone.Set();
            }
            catch (Exception e)
            {
                throw new ConnectCallbackException("Exception encountered when completing socket connection: " + e.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="E4Client"></param>
        public void Receive()
        {
            try
            {
                // Begin receiving the data from the remote device.
                SocketConnection.BeginReceive(Buffer, 0, BufferSize, 0, ReceiveCallback, this);
            }
            catch (Exception e)
            {
                throw new ReceiveException("Exception encountered when receiving data: " + e.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                var E4Object = (ServerClient)ar.AsyncState;
                var Client = E4Object.SocketConnection;

                // Read data from the remote device.
                var bytesRead = Client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    //Sb.Append(" " + Encoding.ASCII.GetString(Buffer, 0, bytesRead);
                    try
                    {
                        _response = Encoding.ASCII.GetString(Buffer, 0, bytesRead);
                    }
                    catch
                    {
                        //Console.WriteLine("\nFailed String Builder");
                    }

                    HandleResponseFromEmpaticaBLEServer(_response);

                    Sb.Clear();

                    ReceiveDone.Set();

                    // Get the rest of the data.
                    Client.BeginReceive(Buffer, 0, BufferSize, 0, ReceiveCallback, this);
                }
                else
                {
                    // All the data has arrived; put it in response.
                    if (Sb.Length > 1)
                    {
                        _response = Sb.ToString();
                    }
                    // Signal that all bytes have been received.
                    ReceiveDone.Set();
                }
            }
            catch (Exception e)
            {
                throw new ReceiveCallbackException("Exception encountered when completing receive callback: " + e.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        public void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, client);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ar"></param>
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                var client = (Socket)ar.AsyncState;
                // Complete sending the data to the remote device.
                client.EndSend(ar);
                // Signal that all bytes have been sent.
                SendDone.Set();
            }
            catch (Exception e)
            {
                throw new SendCallbackException("Exception encountered when performing send callback: " + e.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        private void HandleResponseFromEmpaticaBLEServer(string response)
        {
            List<string[]> AllResponses = new List<string[]>();
            string[] responses = response.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            //for (int i = 0; i < responses.Length; i++) responses[i] = responses[i].Trim(new Char[] { '\r' });
            foreach (string res in responses) AllResponses.Add(res.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            try
            {
                foreach (string[] res in AllResponses)
                {
                    if (res.Length < 2)
                    {
                        continue;
                    }
                    if (res[0] == "R")
                    {
                        if (String.Equals("OK", res[2]))
                        {
                            continue;
                        }
                        else if (res.ToList().Contains("ERR"))
                        {
                            HandleErrorCodes(responses);
                        }
                        else
                        {
                            switch (res[1])
                            {
                                case "device_discover_list":
                                    HandleDiscoverResponseBTLE(AllResponses[0]);
                                    break;
                                case "device_connect_btle":
                                    break;
                                case "device_disconnect_btle":
                                    break;
                                case "device_list":
                                    HandleDiscoverResponse(AllResponses[0]);
                                    break;
                                case "device_connect":
                                    break;
                                case "device_disconnect":
                                    break;
                                case "device_subscribe":
                                    break;
                                case "pause":
                                    break;
                                case "connection":
                                    HandleErrorCodes(res);
                                    break;
                                case "device":
                                    HandleErrorCodes(res);
                                    break;
                            }
                        }
                    }
                    else if (res[0].Substring(0, 2) == "E4")
                    {
                        HandleDataStream(res, response);
                    }
                }
            }
            catch (Exception e)
            {
                throw new HandleResponseException("Exception encountered when handling response, {0}: {1}", response, e.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="DataPacket"></param>
        private void HandleDataStream(string[] DataPacket, string response)
        {
            // Get the data type from the first packet string
            try
            {
                string DataType = DataPacket[0].Substring(3, DataPacket[0].Length - 3);
                switch (DataType)
                {
                    case "Acc":
                        ACCReadings3D.Add(double.Parse(DataPacket[2]));
                        ACCReadings3D.Add(double.Parse(DataPacket[3]));
                        ACCReadings3D.Add(double.Parse(DataPacket[4]));
                        ACCReadingsX.Add(double.Parse(DataPacket[2]));
                        ACCReadingsY.Add(double.Parse(DataPacket[3]));
                        ACCReadingsZ.Add(double.Parse(DataPacket[4]));
                        ACCTimestamps.Add(double.Parse(DataPacket[1]));
                        break;
                    case "Bvp":
                        BVPReadings.Add(double.Parse(DataPacket[2]));
                        BVPTimestamps.Add(double.Parse(DataPacket[1]));
                        break;
                    case "Gsr":
                        GSRReadings.Add(double.Parse(DataPacket[2]));
                        GSRTimestamps.Add(double.Parse(DataPacket[1]));
                        break;
                    case "Temperature":
                        TMPReadings.Add(double.Parse(DataPacket[2]));
                        TMPTimestamps.Add(double.Parse(DataPacket[1]));
                        break;
                    case "Ibi":
                        IBIReadings.Add(double.Parse(DataPacket[2]));
                        IBITimestamps.Add(double.Parse(DataPacket[1]));
                        break;
                    case "Hr":
                        HRReadings.Add(double.Parse(DataPacket[2]));
                        HRTimestamps.Add(double.Parse(DataPacket[1]));
                        break;
                    case "Battery":
                        BATReadings.Add(double.Parse(DataPacket[2]));
                        BATTimestamps.Add(double.Parse(DataPacket[1]));
                        break;
                    case "Tag":
                        TAGReadings.Add(1.0);
                        TAGTimestamps.Add(double.Parse(DataPacket[1]));
                        break;
                    default:
                        throw new DataNotSupportedException();
                }
            }
            catch (Exception e)
            {
                throw new DataStreamHandlerException("Exception encountered when handling data packet: {0}", e.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ErrorResponse"></param>
        private void HandleErrorCodes(string[] ErrorResponse)
        {
            // Check error index for device_subscribe command
            int ErrorIndex = 2;
            if (ErrorResponse[1] == "device_subscribe" || ErrorResponse[1] == "device")
            {
                ErrorIndex = 3;
            }
            if (ErrorResponse[1] == "connection")
            {
                ErrorIndex = 1;
            }
            // Convert ErrorResponse to list to remove starting commands
            List<string> ErrorMessage = ErrorResponse.ToList();
            ErrorMessage.RemoveRange(0, ErrorIndex + 1);
            // Combine into a full message for switch statement
            string EMessage = ErrorMessage.ToArray().Aggregate((partial, error) => $"{partial} {error}");
            // Check if this is a reconnection event
            if (EMessage.CompareTo("connection re-established to device " + DeviceName) == 0)
            {
                throw new BTLEReconnectionException("connection re-established to device " + DeviceName);
            }
            // Switch for the message
            switch (EMessage)
            {
                case "You are not connected to any device":
                    throw new DeviceNotConnectedException("You are not connected to any device.");
                case "No connected device.":
                    throw new DeviceNotConnectedException("No connected device.");
                case "The device requested for connection is not available.":
                    throw new DeviceNotAvailableException("The device requested for connection is not available.");
                case "The device is not connected over btle":
                    throw new DeviceNotConnectedException("The device is not connected over BTLE");
                case "The device has not been discovered yet":
                    throw new DeviceNotAvailableException("The device has not been discovered yet");
                case "turned off via button":
                    throw new DeviceTurnedOffException(DeviceName + ": Device turned off via button");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="responses"></param>
        private void HandleDiscoverResponse(string[] responses)
        {
            int DevicesFound = int.Parse(responses[2]);
            if (DevicesFound == 0)
            {
                throw new DeviceNotAvailableException("No devices found connected to server.");
            }
            else
            {
                for (int i = 4; i < responses.Length; i+=2)
                {
                    Utilities.AvailableDevices.Add(responses[i]);
                }
            }
        }
        private void HandleDiscoverResponseBTLE(string[] responses)
        {
            int DevicesFound = int.Parse(responses[2]);
            if (DevicesFound == 0)
            {
                throw new DeviceNotAvailableException("No devices found connected to server.");
            }
            else
            {
                for (int i = 4; i < responses.Length; i += 3)
                {
                    if (responses[i + 2] == "allowed")
                    {
                        Utilities.AvailableDevices.Add(responses[i]);
                    }
                }
            }
        }
        public void TagStressEvent()
        {
            TAGReadings.Add(1.0f);
            TAGTimestamps.Add(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }
        public void Dispose()
        {
            SocketConnection.Close();
        }
    }
    public class Utilities
    {
        public delegate void WindowedDataReady();
        public static event WindowedDataReady WindowedDataReadyEvent;
        public static void StartE4ServerGUI()
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                FileName = @"C:\Program Files (x86)\Empatica\EmpaticaBLEServer\EmpaticaBLEServer.exe"
            };
            process.StartInfo = startInfo;
            process.Start();
        }
        /// <summary>  </summary>
        public static List<string> AvailableDevices = new List<string>();
        public static void StartE4Server(string APIKey, string IPaddress = "127.0.0.1", string Port = "8000")
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                FileName = @"C:\Program Files (x86)\Empatica\EmpaticaBLEServer\EmpaticaBLEServer.exe",
                Arguments = APIKey + " " + IPaddress + " " + Port
            };
            process.StartInfo = startInfo;
            process.Start();
        }
        public static System.Timers.Timer StartTimer(float Seconds)
        {
            System.Timers.Timer WindowTimer = new System.Timers.Timer(Seconds * 1000);
            WindowTimer.Elapsed += DataReady;
            WindowTimer.AutoReset = true;
            WindowTimer.Enabled = true;
            return WindowTimer;
        }
        private static void DataReady(object source, ElapsedEventArgs e)
        {
            WindowedDataReadyEvent?.Invoke();
        }
        public static List<List<double>> GrabWindow(ServerClient E4Object, string Filepath = null)
        {
            List<List<double>> ReturnList = new List<List<double>>
            {
                new List<double>(E4Object.ACCReadings3D),
                new List<double>(E4Object.ACCReadingsX),
                new List<double>(E4Object.ACCReadingsY),
                new List<double>(E4Object.ACCReadingsZ),
                new List<double>(E4Object.BVPReadings),
                new List<double>(E4Object.GSRReadings),
                new List<double>(E4Object.TMPReadings),
                new List<double>(E4Object.TAGReadings),
                new List<double>(E4Object.IBIReadings),
                new List<double>(E4Object.HRReadings),
                new List<double>(E4Object.BATReadings)
            };
            if (Filepath != null)
                SaveReadings(E4Object, Filepath);
            ClearReadings(E4Object);
            return ReturnList;
        }
        public static void ClearReadings(ServerClient E4Object)
        {
            E4Object.ACCReadings3D.Clear();
            E4Object.ACCReadingsX.Clear();
            E4Object.ACCReadingsY.Clear();
            E4Object.ACCReadingsZ.Clear();
            E4Object.ACCTimestamps.Clear();

            E4Object.BVPReadings.Clear();
            E4Object.BVPTimestamps.Clear();

            E4Object.GSRReadings.Clear();
            E4Object.GSRTimestamps.Clear();

            E4Object.HRReadings.Clear();
            E4Object.HRTimestamps.Clear();

            E4Object.IBIReadings.Clear();
            E4Object.IBITimestamps.Clear();

            E4Object.BATReadings.Clear();
            E4Object.BATTimestamps.Clear();

            E4Object.TAGReadings.Clear();
            E4Object.TAGTimestamps.Clear();

            E4Object.TMPReadings.Clear();
            E4Object.TMPTimestamps.Clear();
        }
        public static void SaveReadings(ServerClient E4Object, string Filepath)
        {
            string Lines = 
                string.Join(",", E4Object.ACCReadings3D) + Environment.NewLine +
                string.Join(",", E4Object.ACCReadingsX) + Environment.NewLine + 
                string.Join(",", E4Object.ACCReadingsY) + Environment.NewLine + 
                string.Join(",", E4Object.ACCReadingsZ) + Environment.NewLine + 
                string.Join(",", E4Object.ACCTimestamps) + Environment.NewLine + 
                string.Join(",", E4Object.BVPReadings) + Environment.NewLine + 
                string.Join(",", E4Object.BVPTimestamps) + Environment.NewLine + 
                string.Join(",", E4Object.GSRReadings) + Environment.NewLine + 
                string.Join(",", E4Object.GSRTimestamps) + Environment.NewLine + 
                string.Join(",", E4Object.TMPReadings) + Environment.NewLine + 
                string.Join(",", E4Object.TMPTimestamps) + Environment.NewLine + 
                string.Join(",", E4Object.TAGReadings) + Environment.NewLine + 
                string.Join(",", E4Object.TAGTimestamps) + Environment.NewLine + 
                string.Join(",", E4Object.IBIReadings) + Environment.NewLine +
                string.Join(",", E4Object.IBITimestamps) + Environment.NewLine +
                string.Join(",", E4Object.HRReadings) + Environment.NewLine + 
                string.Join(",", E4Object.HRTimestamps) + Environment.NewLine + 
                string.Join(",", E4Object.BATReadings) + Environment.NewLine + 
                string.Join(",", E4Object.BATTimestamps) + Environment.NewLine;
            File.AppendAllText(Filepath, Lines);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="E4Client"></param>
        public static void ListDiscoveredDevices(ServerClient E4Object)
        {
            E4Object.Send(E4Object.SocketConnection, "device_list" + Environment.NewLine);
            E4Object.SendDone.WaitOne();
            E4Object.Receive();
            E4Object.ReceiveDone.WaitOne();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ConnectedDevice"></param>
        /// <param name="E4Client"></param>
        public static void ConnectDevice(ServerClient E4Object)
        {
            E4Object.Send(E4Object.SocketConnection, "device_connect " + E4Object.DeviceName + Environment.NewLine);
            E4Object.SendDone.WaitOne();
            E4Object.Receive();
            E4Object.ReceiveDone.WaitOne();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="E4Client"></param>
        public static void DisconnectDevice(ServerClient E4Object)
        {
            E4Object.Send(E4Object.SocketConnection, "device_disconnect" + Environment.NewLine);
            E4Object.SendDone.WaitOne();
            E4Object.Receive();
            E4Object.ReceiveDone.WaitOne();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="E4Client"></param>
        public static void ListDiscoveredDevicesBTLE(ServerClient E4Object)
        {
            E4Object.Send(E4Object.SocketConnection, "device_discover_list" + Environment.NewLine);
            E4Object.SendDone.WaitOne();
            E4Object.Receive();
            E4Object.ReceiveDone.WaitOne();
        }
        /// <summary>
        /// 
        /// </summary>
        public static void ConnectDeviceBTLE(ServerClient E4Object, int Timeout = -1)
        {
            if (Timeout != -1 && Timeout < 254 && Timeout > -1)
                E4Object.Send(E4Object.SocketConnection, "device_connect_btle " + E4Object.DeviceName + " " + Timeout.ToString() + Environment.NewLine);
            else
                E4Object.Send(E4Object.SocketConnection, "device_connect_btle " + E4Object.DeviceName + Environment.NewLine);
            E4Object.SendDone.WaitOne();
            E4Object.Receive();
            E4Object.ReceiveDone.WaitOne();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="E4Client"></param>
        /// <param name="ConnectedDevice"></param>
        public static void DisconnectDeviceBTLE(ServerClient E4Object)
        {
            E4Object.Send(E4Object.SocketConnection, "device_disconnect_btle " + E4Object.DeviceName + Environment.NewLine);
            E4Object.SendDone.WaitOne();
            E4Object.Receive();
            E4Object.ReceiveDone.WaitOne();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="E4Client"></param>
        /// <param name="Stream"></param>
        public static void SubscribeToStream(ServerClient E4Object, ServerClient.DeviceStreams Stream)
        {
            if (!E4Object.SubscribedStreams.Contains(Stream))
            {
                string Message = "";
                switch (Stream)
                {
                    case ServerClient.DeviceStreams.ACC:
                        Message = "device_subscribe acc ON";
                        break;
                    case ServerClient.DeviceStreams.BAT:
                        Message = "device_subscribe bat ON";
                        break;
                    case ServerClient.DeviceStreams.BVP:
                        Message = "device_subscribe bvp ON";
                        break;
                    case ServerClient.DeviceStreams.GSR:
                        Message = "device_subscribe gsr ON";
                        break;
                    case ServerClient.DeviceStreams.IBI:
                        Message = "device_subscribe ibi ON";
                        break;
                    case ServerClient.DeviceStreams.TAG:
                        Message = "device_subscribe tag ON";
                        break;
                    case ServerClient.DeviceStreams.TMP:
                        Message = "device_subscribe tmp ON";
                        break;
                }
                E4Object.SubscribedStreams.Add(Stream);
                E4Object.Send(E4Object.SocketConnection, Message + Environment.NewLine);
                E4Object.SendDone.WaitOne();
                E4Object.Receive();
                E4Object.ReceiveDone.WaitOne();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="E4Client"></param>
        /// <param name="ConnectedDevice"></param>
        /// <param name="Stream"></param>
        public static void UnsubscribeToStream(ServerClient E4Object, ServerClient.DeviceStreams Stream)
        {
            if (E4Object.SubscribedStreams.Contains(Stream))
            {
                string Message = "";
                switch (Stream)
                {
                    case ServerClient.DeviceStreams.ACC:
                        Message = "device_subscribe acc OFF";
                        break;
                    case ServerClient.DeviceStreams.BAT:
                        Message = "device_subscribe bat OFF";
                        break;
                    case ServerClient.DeviceStreams.BVP:
                        Message = "device_subscribe bvp OFF";
                        break;
                    case ServerClient.DeviceStreams.GSR:
                        Message = "device_subscribe gsr OFF";
                        break;
                    case ServerClient.DeviceStreams.IBI:
                        Message = "device_subscribe ibi OFF";
                        break;
                    case ServerClient.DeviceStreams.TAG:
                        Message = "device_subscribe tag OFF";
                        break;
                    case ServerClient.DeviceStreams.TMP:
                        Message = "device_subscribe tmp OFF";
                        break;
                }
                E4Object.SubscribedStreams.Remove(Stream);
                E4Object.Send(E4Object.SocketConnection, Message + Environment.NewLine);
                E4Object.SendDone.WaitOne();
                E4Object.Receive();
                E4Object.ReceiveDone.WaitOne();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="E4Client"></param>
        public static void SuspendStreaming(ServerClient E4Object)
        {
            E4Object.Send(E4Object.SocketConnection, "pause ON" + Environment.NewLine);
            E4Object.SendDone.WaitOne();
            E4Object.Receive();
            E4Object.ReceiveDone.WaitOne();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="E4Client"></param>
        public static void StartStreaming(ServerClient E4Object)
        {
            E4Object.Send(E4Object.SocketConnection, "pause OFF" + Environment.NewLine);
            E4Object.SendDone.WaitOne();
            E4Object.Receive();
            E4Object.ReceiveDone.WaitOne();
        }
    }
}