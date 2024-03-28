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
    public bool _isSenderTerminated { get; set; } = false;
    
    public void Connect(string host, int port)
    {
        IPEndPoint endPoint;
        if (IPAddress.TryParse(host, out IPAddress ip))
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
        Io.DebugPrintLine("TCP client is shutting down...");
        _client.Client.Shutdown(SocketShutdown.Both);
        Io.DebugPrintLine("TCP client is closing...");
    }
    public void Close()
    {
        _client.Close();
    }
    private async Task SendDataToServer(IPacket packet)
    {
        byte[] data = packet.ToBytes();
        switch(ClientFsm.State)
        {
            case FsmStateEnum.Auth:
                if (packet.GetMsgType() == MessageTypeEnum.Auth)
                    ClientFsm.SetState(FsmStateEnum.Auth);
                break;
        }
        await _client.Client.SendAsync(data, SocketFlags.None);
    }
    public async Task Reader()
    {
        // read separate messages from server, every message is separated by CRLF
        string message = ""; // message buffer
        byte[] buffer = new byte[1]; // read byte by byte
        Io.DebugPrintLine("Reader started...");
        while (true)
        {
            try
            {
                while (await _client.Client.ReceiveAsync(buffer) != 0)
                {
                    message += Encoding.UTF8.GetString(buffer); // read stream to message
                    if (message.EndsWith("\r\n")) // check if full message is received
                    {
                        TcpPacket p = TcpPacketParser.Parse(message.Trim());
                        _gotPacketsQueue.Add(p);
                        // todo make separate method for below
                        switch (ClientFsm.State)
                        {
                            case FsmStateEnum.Auth:
                                if (p.GetMsgType() == MessageTypeEnum.Err)
                                {
                                    ClientFsm.SetState(FsmStateEnum.End);
                                    TcpPacket bye = TcpPacketBuilder.build_bye();
                                    await AddPacketToSendQueue(bye);
                                } else if (p.GetMsgType() == MessageTypeEnum.Reply && p.GetMsgData().ToUpper().StartsWith("REPLY OK"))
                                {
                                    ClientFsm.SetState(FsmStateEnum.Open);
                                } else if (p.GetMsgType() == MessageTypeEnum.Reply && p.GetMsgData().ToUpper().Contains("REPLY NOK"))
                                {
                                    ClientFsm.SetState(FsmStateEnum.Auth);
                                }
                                break;
                            case FsmStateEnum.Open:
                                if (p.GetMsgType() == MessageTypeEnum.Err)
                                {
                                    ClientFsm.SetState(FsmStateEnum.End);
                                }
                                else if (p.GetMsgType() == MessageTypeEnum.Bye)
                                {
                                    ClientFsm.SetState(FsmStateEnum.End);
                                }
                                break;
                        }
                        message = ""; // clear message
                    }
                }
            }
            catch (SocketException) // client is closed
            {
                break;
            }
            catch (ObjectDisposedException) // client is closed
            {
                break;
            }
        }
        _gotPacketsQueue.CompleteAdding(); // signal that no more packets will be added
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
            Io.DebugPrintLine($"Got packet from queue {p?.GetMsgType()}");
            if (p != null)
            {
                var last = SendDataToServer(p);
                IpkProject1.SetLastSendTask(last);
                await last;
                Io.DebugPrintLine($"Send packet to server {p.GetMsgType()}...");
            }
        }
    }
    public async Task AddPacketToSendQueue(IPacket packet)
    {
        await Task.Run(() =>
        {
            if (packet.GetMsgType() == MessageTypeEnum.Auth)
            {
                IpkProject1.AuthSem.WaitOne();
                Io.DebugPrintLine("AuthSem acquired in TcpChatClient...");
            }
            _sendPacketsQueue.Add((TcpPacket) packet);
            Io.DebugPrintLine("Packet added to send queue...");
        });
    }
}