using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using IpkProject1.enums;
using IpkProject1.fsm;
using IpkProject1.interfaces;
using IpkProject1.user;

namespace IpkProject1.tcp;

public class TcpChatClient : IChatClient
{
    private readonly TcpClient _client = new ();
    private readonly BlockingCollection<TcpPacket> _gotPacketsQueue = new ();
    private readonly BlockingCollection<TcpPacket> _sendPacketsQueue = new ();
    
    public void Connect(string host, int port)
    {
        IPEndPoint endPoint;
        if (IPAddress.TryParse(host, out var ip))
        {
            endPoint = new IPEndPoint(ip, port);
        }
        else
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(host);
            endPoint = new IPEndPoint(hostEntry.AddressList[0], port);
        }
        _client.Connect(endPoint);
        Io.DebugPrintLine("Connected to server...");
    }
    public void Disconnect()
    {
        _sendPacketsQueue.CompleteAdding();
        Io.DebugPrintLine("TCP client is shutting down...");
        _gotPacketsQueue.CompleteAdding();
    }
    public void Close()
    {
        IpkProject1.GetLastSendTask()?.Wait();
        try
        {
            _client.Client.Shutdown(SocketShutdown.Both);
        }
        finally
        {
            _client.Client.Close();
        }
        Io.DebugPrintLine("TCP client is closing...");
        _client.Client.Close();
    }
    private async Task SendDataToServer(IPacket packet)
    {
        byte[] data = packet.ToBytes();
        if (packet.Type == MessageTypeEnum.Err)
        {
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
            int read = await _client.Client.ReceiveAsync(buffer);
            message += Encoding.ASCII.GetString(buffer, 0, read);
            if (message.Contains("\r\n"))
            {
                string[] fullMessages;
                string[] messages = message.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                if (message.EndsWith("\r\n"))
                {
                    message = ""; // clear buffer
                    fullMessages = messages;
                }
                else
                {
                    message = messages[^1]; // save unfinished message
                    fullMessages = messages[..^1];
                }
                foreach (var msg in fullMessages)
                {
                    TcpPacket packet = TcpPacketParser.Parse(msg+"\r\n");
                    _gotPacketsQueue.Add(packet);
                    ClientFsm.FsmUpdate(packet);
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
            await getTask;
            Io.DebugPrintLine($"Got packet from queue: {p?.Type}");
            if (p != null)
            {
                var last = SendDataToServer(p);
                IpkProject1.SetLastSendTask(last);
                await last;
                Io.DebugPrintLine($"Send packet to server {p.Type}...");
            }
        }
    }
    public void AddPacketToSendQueue(IPacket packet)
    {
        if (packet.Type == MessageTypeEnum.Auth)
        {
            IpkProject1.AuthSem.WaitOne();
            Io.DebugPrintLine("AuthSem acquired in TcpChatClient...");
        }
        _sendPacketsQueue.Add((TcpPacket) packet);
        Io.DebugPrintLine("Packet added to send queue...");
    }
}