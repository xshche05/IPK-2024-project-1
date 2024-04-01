using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using IpkProject1.Enums;
using IpkProject1.Fsm;
using IpkProject1.Interfaces;
using IpkProject1.SysArg;
using IpkProject1.User;

namespace IpkProject1.Tcp;

public class TcpChatClient : IChatClient
{
    // TcpClient to send and receive data
    private readonly TcpClient _client = new ();
    // Blocking collection to store received packets
    private readonly BlockingCollection<TcpPacket> _gotPacketsQueue = new ();
    // Blocking collection to store packets to send
    private readonly BlockingCollection<TcpPacket> _sendPacketsQueue = new ();
    
    public void Connect(string host, int port)
    {
        IPEndPoint endPoint;
        if (IPAddress.TryParse(host, out var ip))
        {
            // if host is an IP address
            endPoint = new IPEndPoint(ip, port);
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
            endPoint = new IPEndPoint(IPv4, port);
        }
        _client.Connect(endPoint);
        Io.DebugPrintLine("Connected to server...");
    }
    
    public void Disconnect()
    {
        // mark send queue as completed
        _sendPacketsQueue.CompleteAdding();
        Io.DebugPrintLine("TCP client is shutting down...");
        // mark got queue as completed
        _gotPacketsQueue.CompleteAdding();
    }
    
    public void Close()
    {
        // wait for the last send task to finish
        IpkProject1.GetLastSendTask()?.Wait();
        try
        {
            // shutdown the client, gracefully close the connection
            _client.Client.Shutdown(SocketShutdown.Both);
        }
        finally
        {
            // close the client
            _client.Client.Close();
        }
        Io.DebugPrintLine("TCP client is closed...");
    }
    
    private async Task SendDataToServer(IPacket packet)
    {
        byte[] data = packet.ToBytes();
        if (packet.Type == MessageTypeEnum.Err)
        {
            // Change state to error if error packet is sent
            ClientFsm.SetState(FsmStateEnum.Error);
        }
        await _client.Client.SendAsync(data, SocketFlags.None);
    }
    
    public async Task Reader()
    {
        // read separate messages from server, every message is separated by CRLF
        string message = ""; // message buffer
        byte[] buffer = new byte[2048]; // read byte by byte
        Io.DebugPrintLine("Reader started...");
        while (!_gotPacketsQueue.IsCompleted)
        {
            int read = await _client.Client.ReceiveAsync(buffer); // read from stream
            message += Encoding.ASCII.GetString(buffer, 0, read);
            if (message.Contains("\r\n")) // if got min one message
            {
                string[] fullMessages;
                // split messages by CRLF
                string[] messages = message.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                if (message.EndsWith("\r\n")) // if last message is finished
                {
                    message = ""; // clear buffer
                    // all messages are finished, save them
                    fullMessages = messages;
                }
                else
                {
                    message = messages[^1]; // save unfinished message
                    // save all messages except last one
                    fullMessages = messages[..^1];
                }
                foreach (var msg in fullMessages) // process all full messages
                {
                    // parse message to packet
                    TcpPacket packet = TcpPacketParser.Parse(msg+"\r\n"); 
                    _gotPacketsQueue.Add(packet);
                    ClientFsm.FsmUpdate(packet); // update FSM according to the packet
                    Io.DebugPrintLine($"Packet added to got queue {packet.Type}");
                }
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
            TcpPacket? packet = await Task.Run(() =>
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
            TcpPacket? p = null;
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
            // Wait for packet to be added to the queue
            await getTask;
            Io.DebugPrintLine($"Got packet from queue: {p?.Type}");
            if (p != null)
            {
                // Send packet to server
                var last = SendDataToServer(p);
                // Remember the last send task
                IpkProject1.SetLastSendTask(last);
                await last; // Wait for the task to finish
                Io.DebugPrintLine($"Send packet to server {p.Type}...");
            }
        }
    }
    
    public void AddPacketToSendQueue(IPacket packet)
    {
        if (packet.Type == MessageTypeEnum.Auth)
        {
            // lock input if auth request is sent
            IpkProject1.AuthSem.WaitOne();
            Io.DebugPrintLine("AuthSem acquired in TcpChatClient...");
        }
        // Add packet to the send queue
        _sendPacketsQueue.Add((TcpPacket) packet);
        Io.DebugPrintLine("Packet added to send queue...");
    }
}