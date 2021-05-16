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
    /// <summary>
    /// Class to instantiate a client to communicate with TCP server.
    /// </summary>
    public class ServerClient : IDisposable
    {
        /// <summary>
        /// The applicable device data streams for the E4 server.
        /// </summary>
        public enum DeviceStreams 
        { 
            /// <summary>
            /// Accelerometer data stream.
            /// </summary>
            ACC, 
            /// <summary>
            /// Blood volume pulse data stream.
            /// </summary>
            BVP, 
            /// <summary>
            /// Galvanic skin response data stream.
            /// </summary>
            GSR, 
            /// <summary>
            /// Inter-beat interval data stream.
            /// </summary>
            IBI, 
            /// <summary>
            /// Heart rate data stream.
            /// </summary>
            HR, 
            /// <summary>
            /// Temperature data stream.
            /// </summary>
            TMP, 
            /// <summary>
            /// Battery data stream.
            /// </summary>
            BAT, 
            /// <summary>
            /// Tags data stream. This is the onboard E4 button.
            /// </summary>
            TAG 
        }
        /// <summary>
        /// Connection to be used in communications
        /// </summary>
        public Socket SocketConnection;
        /// <summary>
        /// Name of the device for connection
        /// </summary>
        public string DeviceName = string.Empty;
        /// <summary>
        /// List of subscribed streams for synchronizing readings
        /// </summary>
        public List<DeviceStreams> SubscribedStreams = new List<DeviceStreams>();
        /// <summary>
        /// List for 3D accelerometer readings.
        /// </summary>
        public List<double> ACCReadings3D = new List<double>();
        /// <summary>
        /// List for X accelerometer readings.
        /// </summary>
        public List<double> ACCReadingsX = new List<double>();
        /// <summary>
        /// List for Y accelerometer readings.
        /// </summary>
        public List<double> ACCReadingsY = new List<double>();
        /// <summary>
        /// List for Z accelerometer readings.
        /// </summary>
        public List<double> ACCReadingsZ = new List<double>();
        /// <summary>
        /// List for accelerometer timestamps.
        /// </summary>
        public List<double> ACCTimestamps = new List<double>();
        /// <summary>
        /// List for blood volume pulse readings.
        /// </summary>
        public List<double> BVPReadings = new List<double>();
        /// <summary>
        /// List for blood volume pulse timestamps.
        /// </summary>
        public List<double> BVPTimestamps = new List<double>();
        /// <summary>
        /// List for galvanic skin response readings.
        /// </summary>
        public List<double> GSRReadings = new List<double>();
        /// <summary>
        /// List for galvanic skin response timestamps.
        /// </summary>
        public List<double> GSRTimestamps = new List<double>();
        /// <summary>
        /// List for inter-beat interval readings.
        /// </summary>
        public List<double> IBIReadings = new List<double>();
        /// <summary>
        /// List for inter-beat interval timestamps.
        /// </summary>
        public List<double> IBITimestamps = new List<double>();
        /// <summary>
        /// List for heart rate readings.
        /// </summary>
        public List<double> HRReadings = new List<double>();
        /// <summary>
        /// List for heart rate timestamps.
        /// </summary>
        public List<double> HRTimestamps = new List<double>();
        /// <summary>
        /// List for temperature readings.
        /// </summary>
        public List<double> TMPReadings = new List<double>();
        /// <summary>
        /// List for temperature timestamps.
        /// </summary>
        public List<double> TMPTimestamps = new List<double>();
        /// <summary>
        /// List for battery readings.
        /// </summary>
        public List<double> BATReadings = new List<double>();
        /// <summary>
        /// List for battery timestamps.
        /// </summary>
        public List<double> BATTimestamps = new List<double>();
        /// <summary>
        /// List for tag readings.
        /// </summary>
        public List<double> TAGReadings = new List<double>();
        /// <summary>
        /// List for tag timestamps.
        /// </summary>
        public List<double> TAGTimestamps = new List<double>();
        /// <summary>
        /// Size of receive buffer.
        /// </summary>
        public const int BufferSize = 4096;
        /// <summary>
        /// Receive buffer.
        /// </summary>
        public readonly byte[] Buffer = new byte[BufferSize];
        /// <summary>
        /// Received data string.
        /// </summary>
        public readonly StringBuilder Sb = new StringBuilder();
        // The port number for the remote device.
        private const string ServerAddress = "127.0.0.1";
        private const int ServerPort = 28000;
        /// <summary>
        /// ManualResetEvent instances signal completion.
        /// </summary>
        public readonly ManualResetEvent ConnectDone = new ManualResetEvent(false);
        /// <summary>
        /// ManualResetEvent signal completion of send.
        /// </summary>
        public readonly ManualResetEvent SendDone = new ManualResetEvent(false);
        /// <summary>
        /// ManualResetEvent signal completion of receive.
        /// </summary>
        public readonly ManualResetEvent ReceiveDone = new ManualResetEvent(false);
        // The response from the remote device.
        private static String _response = String.Empty;
        /// <summary>  
        /// Starts the TCP client.
        /// </summary>
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
        /// The callback for connect events to the TCP server.
        /// </summary>
        /// <param name="ar">The async result.</param>
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
        /// Starts the receive process.
        /// </summary>
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
        /// TCP receive callback.
        /// </summary>
        /// <param name="ar">Async result.</param>
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
        /// Send data to TCP server.
        /// </summary>
        /// <param name="client">The TCP client.</param>
        /// <param name="data">The data to send.</param>
        public void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, client);
        }
        /// <summary>
        /// The callback for send completion.
        /// </summary>
        /// <param name="ar">The async result.</param>
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
        /// Handles the response from the E4 TCP server.
        /// </summary>
        /// <param name="response">The response from the server.</param>
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
        /// Takes the TCP data and places it into the corresponding list.
        /// </summary>
        /// <param name="DataPacket">The data packet from TCP server.</param>
        /// <param name="response">Unused.</param>
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
        /// Handles the received error codes.
        /// </summary>
        /// <param name="ErrorResponse">TCP error response to be parsed.</param>
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
                    throw new DeviceNotConnectedException("The device is not connected over BTLE.");
                case "The device has not been discovered yet":
                    throw new DeviceNotAvailableException("The device has not been discovered yet.");
                case "turned off via button":
                    throw new DeviceTurnedOffException(DeviceName + ": Device turned off via button.");
            }
        }
        /// <summary>
        /// Handles the device discovered response.
        /// </summary>
        /// <param name="responses">The responses from the TCP server.</param>
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
        /// <summary>
        /// Handles the discover device from BTLE connection.
        /// </summary>
        /// <param name="responses">The responses from the TCP server.</param>
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
        /// <summary>
        /// Adds tag event to tag list.
        /// </summary>
        public void TagStressEvent()
        {
            TAGReadings.Add(1.0f);
            TAGTimestamps.Add(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }
        /// <summary>
        /// Closes the TCP client.
        /// </summary>
        public void Dispose()
        {
            SocketConnection.Close();
        }
    }
    /// <summary>
    /// Utilities for sending commands to TCP server.
    /// </summary>
    public class Utilities
    {
        /// <summary>
        /// Delegate to alert that windowed data is ready. Triggered by stopwatch.
        /// </summary>
        public delegate void WindowedDataReady();
        /// <summary>
        /// Event to alert that windowed data is ready.
        /// </summary>
        public static event WindowedDataReady WindowedDataReadyEvent;
        /// <summary>
        /// Starts the E4 server GUI with a process call.
        /// </summary>
        /// <param name="ServerPath">The path to the server exe.</param>
        public static void StartE4ServerGUI(string ServerPath)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                FileName = ServerPath //@"{absolute path}\EmpaticaBLEServer.exe"
            };
            process.StartInfo = startInfo;
            process.Start();
        }
        /// <summary> 
        /// List of available devices on TCP server.
        /// </summary>
        public static List<string> AvailableDevices = new List<string>();
        /// <summary>
        /// Starts the E4 server.
        /// </summary>
        /// <param name="ServerPath">The path to the exe.</param>
        /// <param name="APIKey">The API key to complete connection.</param>
        /// <param name="IPaddress">The IP address to the TCP server. Defaults to 127.0.0.1</param>
        /// <param name="Port">The port to connect through. Defaults to 8000.</param>
        public static void StartE4Server(string ServerPath, string APIKey, string IPaddress = "127.0.0.1", string Port = "8000")
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                FileName = ServerPath, //@"{absolute path}\EmpaticaBLEServer.exe"
                Arguments = APIKey + " " + IPaddress + " " + Port
            };
            process.StartInfo = startInfo;
            process.Start();
        }
        /// <summary>
        /// Starts the windowed data reading timer.
        /// </summary>
        /// <param name="Seconds">The number of seconds for the timer.</param>
        /// <returns>Returns the timer instance.</returns>
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
        /// <summary>
        /// Grab data window.
        /// </summary>
        /// <param name="E4Object">The TCP client handling the responses.</param>
        /// <param name="Filepath">The filepath to save out the data. Defaults to null.</param>
        /// <returns>List of List of data from TCP server.</returns>
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
        /// <summary>
        /// Clears the data lists.
        /// </summary>
        /// <param name="E4Object">The TCP client handling the responses.</param>
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
        /// <summary>
        /// Saves the readings from the lists of data to file.
        /// </summary>
        /// <param name="E4Object">The TCP client handling the responses.</param>
        /// <param name="Filepath">The filepath to the file to save data to.</param>
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
        /// Lists the discovered device on the TCP server to the AvailableDevices list.
        /// </summary>
        /// <param name="E4Object">The TCP client handling resposnes.</param>
        public static void ListDiscoveredDevices(ServerClient E4Object)
        {
            E4Object.Send(E4Object.SocketConnection, "device_list" + Environment.NewLine);
            E4Object.SendDone.WaitOne();
            E4Object.Receive();
            E4Object.ReceiveDone.WaitOne();
        }
        /// <summary>
        /// Connect device from TCP server.
        /// </summary>
        /// <param name="E4Object">The TCP client handling resposnes.</param>
        public static void ConnectDevice(ServerClient E4Object)
        {
            E4Object.Send(E4Object.SocketConnection, "device_connect " + E4Object.DeviceName + Environment.NewLine);
            E4Object.SendDone.WaitOne();
            E4Object.Receive();
            E4Object.ReceiveDone.WaitOne();
        }
        /// <summary>
        /// Disconnect device from TCP server.
        /// </summary>
        /// <param name="E4Object">The TCP client handling resposnes.</param>
        public static void DisconnectDevice(ServerClient E4Object)
        {
            E4Object.Send(E4Object.SocketConnection, "device_disconnect" + Environment.NewLine);
            E4Object.SendDone.WaitOne();
            E4Object.Receive();
            E4Object.ReceiveDone.WaitOne();
        }
        /// <summary>
        /// List devices discovered over BTLE.
        /// </summary>
        /// <param name="E4Object">The TCP client handling resposnes.</param>
        public static void ListDiscoveredDevicesBTLE(ServerClient E4Object)
        {
            E4Object.Send(E4Object.SocketConnection, "device_discover_list" + Environment.NewLine);
            E4Object.SendDone.WaitOne();
            E4Object.Receive();
            E4Object.ReceiveDone.WaitOne();
        }
        /// <summary>
        /// Connect device over BTLE.
        /// </summary>
        /// <param name="E4Object">The TCP client handling resposnes.</param>
        /// <param name="Timeout">The timeout for the client. Defaults to -1.</param>
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
        /// Disconnect device connected over BTLE.
        /// </summary>
        /// <param name="E4Object">The TCP client handling resposnes.</param>
        public static void DisconnectDeviceBTLE(ServerClient E4Object)
        {
            E4Object.Send(E4Object.SocketConnection, "device_disconnect_btle " + E4Object.DeviceName + Environment.NewLine);
            E4Object.SendDone.WaitOne();
            E4Object.Receive();
            E4Object.ReceiveDone.WaitOne();
        }
        /// <summary>
        /// Subscribes device to data stream.
        /// </summary>
        /// <param name="E4Object">The TCP client handling resposnes.</param>
        /// <param name="Stream">The stream to unsubscribe from.</param>
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
        /// Unsubscribes device from data streams.
        /// </summary>
        /// <param name="E4Object">The TCP client handling resposnes.</param>
        /// <param name="Stream">The stream to unsubscribe from.</param>
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
        /// Suspends the data streaming on the E4 device.
        /// </summary>
        /// <param name="E4Object">The TCP client handling resposnes.</param>
        public static void SuspendStreaming(ServerClient E4Object)
        {
            E4Object.Send(E4Object.SocketConnection, "pause ON" + Environment.NewLine);
            E4Object.SendDone.WaitOne();
            E4Object.Receive();
            E4Object.ReceiveDone.WaitOne();
        }
        /// <summary>
        /// Starts the data streaming on the E4 device.
        /// </summary>
        /// <param name="E4Object">The TCP client handling resposnes.</param>
        public static void StartStreaming(ServerClient E4Object)
        {
            E4Object.Send(E4Object.SocketConnection, "pause OFF" + Environment.NewLine);
            E4Object.SendDone.WaitOne();
            E4Object.Receive();
            E4Object.ReceiveDone.WaitOne();
        }
    }
}