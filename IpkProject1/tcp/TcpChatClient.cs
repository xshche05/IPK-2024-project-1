using System.Collections.Concurrent;
using System.Diagnostics;
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
    
    /* public async Task SendDataToServer(IPacket packet)
    {
        if (packet is TcpPacket tcpPacket)
        {
            Io.DebugPrintLine($"Sending TCP packet: {tcpPacket.GetMsgType()}");
            if (tcpPacket.GetMsgType() == MessageTypeEnum.Auth && ClientFsm.GetState() == FsmStateEnum.Start)
                ClientFsm.SetState(FsmStateEnum.Auth);
            await _client.Client.SendAsync(tcpPacket.ToBytes());
        }
        else
        {
            Io.DebugPrintLine("Invalid packet type...");
            // todo terminate the program
        }
    } */
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
                        switch (ClientFsm.GetState())
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
    
    public async Task Sender(){ }
    
    public async Task AddPacketToSendQueue(IPacket packet)
    {
        if (packet is TcpPacket tcpPacket)
        {
            Io.DebugPrintLine($"Adding TCP packet to send queue: {tcpPacket.GetMsgType()}");
            _sendPacketsQueue.Add(tcpPacket);
        }
        else
        {
            Io.DebugPrintLine("Invalid packet type...");
            // todo terminate the program
        }
    }
}