using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using IpkProject1.Enums;
using IpkProject1.Fsm;
using IpkProject1.Interfaces;
using IpkProject1.SysArg;
using IpkProject1.User;

namespace IpkProject1.Udp;

public class UdpChatClient : IChatClient
{
    // Server end point, to send data to
    private IPEndPoint? _serverEndPoint;
    // Flag to determine if the sender task is terminated
    private bool _isSenderTerminated = false;
    // UdpClient to send and receive data
    private readonly UdpClient _client = new ();
    // List of processed messages' ids to prevent duplicate processing
    private readonly List<int> _processedMessages = new ();
    // Blocking collection to store confirmed messages' ids
    private readonly BlockingCollection<int> _confirmedMessages = new ();
    // Blocking collection to store received packets
    private readonly BlockingCollection<UdpPacket> _gotPacketsQueue = new();
    // Blocking collection to store packets to send
    private readonly BlockingCollection<UdpPacket> _sendPacketsQueue = new ();
    
    private int _authRefId = -1;
    
    public void Connect(string host, int port)
    {
        if (IPAddress.TryParse(host, out var ip))
        {
            // id host is an IP address
            _serverEndPoint = new IPEndPoint(ip, port);
        }
        else
        {
            // if host is a domain name
            IPAddress[] hostAddresses = Dns.GetHostAddresses(host);
            IPAddress? IPv4 = Array.Find(hostAddresses, ipTest => ipTest.AddressFamily == AddressFamily.InterNetwork);
            if (IPv4 == null)
            {
                Io.ErrorPrintLine("ERR: Invalid host address, cant find an IPv4 according to hostname! Exiting.", ColorScheme.Error);
                Environment.Exit(1);
            }
            _serverEndPoint = new IPEndPoint(IPv4, port);
        }
        // Bind client to any available port
        _client.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        // Set sender task flag to false
        _isSenderTerminated = false;
        Io.DebugPrintLine("Connected to server...");
    }
    
    public void Disconnect()
    {
        // Mark Send Queue as completed
        _sendPacketsQueue.CompleteAdding();
        Io.DebugPrintLine("Disconnected from server...");
        // Wait for the last send task to finish
        Task? last = Program.GetLastSendTask();
        if (last != null)
        {
            last.Wait();
        }
        // Mark Got Packets Queue as completed
        _gotPacketsQueue.CompleteAdding();
    }

    public void Close()
    {
        // Close connection
        _client.Close();
    }

    public async Task Reader()
    {
        Io.DebugPrintLine("Reader started...");
        while (!_isSenderTerminated)
        {
            try
            {
                var data = await _client.ReceiveAsync(); // Read one packet
                byte[] bytes = data.Buffer;
                if (bytes.Length < 3)
                {
                    IPacket err = new UdpPacketBuilder().build_error(InputProcessor.DisplayName, "Too small paket");
                    AddPacketToSendQueue(err);
                    Io.ErrorPrintLine("ERR: got too small packet", ColorScheme.Error);
                    continue;
                }
                MessageTypeEnum type = (MessageTypeEnum)bytes[0]; // Get message type
                Io.DebugPrintLine("Message received, type: " + type);
                UInt16 msgId = BitConverter.ToUInt16(bytes[1..3]); // Get got message ref id
                if (type == MessageTypeEnum.Confirm)
                {
                    // Mark message with id as confirmed
                    _confirmedMessages.Add(msgId);
                    Io.DebugPrintLine("Confirm received, id: " + msgId);
                }
                else if (type == MessageTypeEnum.Msg || type == MessageTypeEnum.Err || type == MessageTypeEnum.Reply)
                {
                    // update server end point
                    _serverEndPoint = data.RemoteEndPoint;
                    // Send confirm message
                    UdpPacket confirm = new UdpPacketBuilder().build_confirm(msgId);
                    _client.Client.SendTo(confirm.ToBytes(), SocketFlags.None, _serverEndPoint);
                    Io.DebugPrintLine($"Confirm sent for message {type} {msgId}");
                    // Convert data to UdpPacket
                    UdpPacket packet = UdpPacketParser.Parse(bytes);
                    if (type == MessageTypeEnum.Reply && ClientFsm.State == FsmStateEnum.Auth)
                    {
                        var replyRefId = BitConverter.ToUInt16(packet.ToBytes()[4..6]);
                        if (replyRefId != _authRefId)
                        {
                            _authRefId = -1;
                            continue;
                        }
                    }
                    if (!_processedMessages.Contains(msgId)) // Check if message was already processed
                    {
                        Io.DebugPrintLine($"Processing message {type} {msgId}");
                        // Add message to processed messages
                        _processedMessages.Add(msgId);
                        // Add message to got packets queue
                        _gotPacketsQueue.Add(packet);
                        // Update client's FSM
                        ClientFsm.FsmUpdate(packet);
                    }
                    else
                    {
                        Io.DebugPrintLine($"Message already processed... {type} {msgId}");
                    }
                }
                else
                {
                    // Got unsupported message type
                    Io.ErrorPrintLine("ERR: Unknown message type received...", ColorScheme.Error);
                    var err = new UdpPacketBuilder().build_error(InputProcessor.DisplayName, "Unknown message type received");
                    if (_serverEndPoint == null)
                    {
                        Io.ErrorPrintLine("ERR: EndPoint is null...", ColorScheme.Error);
                        ClientFsm.SetState(FsmStateEnum.End);
                        return;
                    }
                    // Send error message to server
                    _client.Client.SendTo(err.ToBytes(), SocketFlags.None, _serverEndPoint);
                    // Change FSM state to error
                    ClientFsm.SetState(FsmStateEnum.Error);
                }
            }
            catch (Exception e)
            {
                Io.DebugPrintLine("Reader exception: " + e.Message + " " + e.GetType());
                break;
            }
        }
        Io.DebugPrintLine("Reader terminated...");
    }

    public async Task Printer()
    {
        Io.DebugPrintLine("Printer started...");
        while (!_gotPacketsQueue.IsCompleted)
        {
            // Wait for packet to be added to the queue
            UdpPacket? packet = await Task.Run(() =>
            {
                try
                {
                    return _gotPacketsQueue.Take();
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            });
            // Print packet
            if (packet != null) packet.Print();
            else break;
        }
        Io.DebugPrintLine("Printer terminated...");
    }

    public async Task Sender()
    {
        Io.DebugPrintLine("Sender started...");
        while (!_sendPacketsQueue.IsCompleted)
        {
            // Wait for packet to be added to the queue
            UdpPacket? p = null;
            var getTask = Task.Run(() =>
            {
                try
                {
                    p = _sendPacketsQueue.Take();
                }
                catch (InvalidOperationException) 
                {
                    p = null;
                }
            });
            await getTask; 
            Io.DebugPrintLine($"Got packet from queue {p?.Type}");
            if (p != null)
            {
                // Send packet to server
                var last = SendDataToServer(p);
                // Update last send task
                Program.SetLastSendTask(last);
                await last; // Wait for the task to finish
                Io.DebugPrintLine($"Send packet to server {p.Type}...");
            }
        }
        Io.DebugPrintLine("Sender terminated...");
    }
    
    public void AddPacketToSendQueue(IPacket packet)
    {
        if (packet.Type == MessageTypeEnum.Auth)
        {
            // Lock the semaphore to prevent sending packets before authentication
            Program.AuthSem.WaitOne();
            _authRefId = ((UdpPacket)packet).GetMsgId();
            Io.DebugPrintLine("AuthSem acquired in UdpChatClient...");
        }
        // Add packet to send queue
        _sendPacketsQueue.Add((UdpPacket)packet);
        Io.DebugPrintLine("Packet added to send queue...");
    }
    
    private async Task SendDataToServer(IPacket packet) {
        byte[] data = packet.ToBytes();
        if (packet.Type == MessageTypeEnum.Err)
        {
            // Change FSM state to error if error message is sent
            ClientFsm.SetState(FsmStateEnum.Error);
        }
        int id = ((UdpPacket)packet).GetMsgId();
        for (int i = 0; i < 1 + SysArgParser.Config.Retries; i++) // Try to send message with retries
        {
            if (_serverEndPoint == null)
            {
                Io.ErrorPrintLine("ERR: EndPoint is null...", ColorScheme.Error);
                ClientFsm.SetState(FsmStateEnum.End);
                return;
            }
            _client.Client.SendTo(data, SocketFlags.None, _serverEndPoint);
            int controlId = -1;
            Task checkTask = Task.Run(() => { controlId = _confirmedMessages.Take(); });  // Confirmation waiter
            Task delayTask = Task.Delay(SysArgParser.Config.Timeout); // Timeout timer
            Task firstCompleted = await Task.WhenAny(checkTask, delayTask); // Wait for confirm message or timeout
            if (firstCompleted == checkTask && checkTask.IsCompletedSuccessfully && controlId == id)
            {
                // Message confirmed
                Io.DebugPrintLine("Message with id " + id + " confirmed");
                break;
            }
            if (i == SysArgParser.Config.Retries)
            {
                // Message not confirmed, max retries reached
                Io.ErrorPrintLine("ERR: Message with id " + id + " not confirmed, max retries reached", ColorScheme.Error);
                if (ClientFsm.State != FsmStateEnum.End)
                {
                    Program.ExitCode = 1;
                    ClientFsm.NeedByeSendFlag = false;
                    ClientFsm.SetState(FsmStateEnum.End);
                }
            }
        }
    }
}