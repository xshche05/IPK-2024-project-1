using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using IpkProject1.enums;
using IpkProject1.fsm;
using IpkProject1.interfaces;

namespace IpkProject1.tcp;

public class TcpChatClient : IChatClient
{
    private readonly TcpClient _client = new ();
    private readonly BlockingCollection<TcpPacket> _gotPackets = new ();
    public void Connect(string ip, int port)
    {
        _client.Connect(ip, port);
        Debug.WriteLine("Connected to server...");
    }
    public void Disconnect()
    {
        try
        {
            Debug.WriteLine("TCP client is shutting down...");
            _client.Client.Shutdown(SocketShutdown.Both);
        }
        finally
        {
            Debug.WriteLine("TCP client is closing...");
            _client.Client.Close();
        }
    }
    public async Task SendDataToServer(IPacket packet)
    {
        if (packet is TcpPacket tcpPacket)
        {
            Debug.WriteLine($"Sending TCP packet: {tcpPacket.GetMsgType()}");
            if (tcpPacket.GetMsgType() == MessageTypeEnum.Auth && ClientFsm.GetState() == FsmStateEnum.Start)
                ClientFsm.SetState(FsmStateEnum.Auth);
            await _client.Client.SendAsync(tcpPacket.ToBytes());
        }
        else
        {
            Debug.WriteLine("Invalid packet type...");
            // todo terminate the program
        }
    }
    public async Task Reader()
    {
        // read separate messages from server, every message is separated by CRLF
        string message = ""; // message buffer
        byte[] buffer = new byte[1]; // read byte by byte
        Debug.WriteLine("Reader started...");
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
                        _gotPackets.Add(p);
                        // todo make separate method for below
                        switch (ClientFsm.GetState())
                        {
                            case FsmStateEnum.Auth:
                                if (p.GetMsgType() == MessageTypeEnum.Err)
                                {
                                    ClientFsm.SetState(FsmStateEnum.End);
                                    TcpPacket bye = TcpPacketBuilder.build_bye();
                                    await SendDataToServer(bye);
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
        _gotPackets.CompleteAdding(); // signal that no more packets will be added
        Debug.WriteLine("Reader terminated...");
    }
    public async Task Printer()
    {
        Debug.WriteLine("Printer started...");
        while (!_gotPackets.IsCompleted)
        {
            TcpPacket? packet = await Task.Run(() =>
            {
                try
                {
                    return _gotPackets.Take();
                } catch (InvalidOperationException)
                {
                    return null;
                }
            });
            if (packet != null) packet.Print();
        }
        Debug.WriteLine("Printer terminated...");
    }
}