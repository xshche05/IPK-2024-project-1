using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using IpkProject1.enums;
using IpkProject1.fsm;

namespace IpkProject1.tcp;

public class TcpChatClient
{
    private readonly TcpClient _client = new ();
    private readonly BlockingCollection<TcpPacket> _gotPackets = new ();
    public void Connect(string ip, int port)
    {
        _client.Connect(ip, port);
        Console.WriteLine("Connected to server...");
    }
    
    public void Disconnect()
    {
        _client.Close();
        Console.WriteLine("Disconnected from server...");
    }
    
    // async function send data to server
    public async Task SendDataToServer(TcpPacket packet)
    {
        byte[] buffer = packet.ToBytes();
        await _client.GetStream().WriteAsync(buffer, 0, buffer.Length);
    }
    
    public async Task Reader()
    {
        // read separate messages from server, every message is separated by CRLF
        string message = "";
        byte[] buffer = new byte[1];
        // write to stderr if reader is terminated
        Console.Error.WriteLine("Reader started...");
        Socket s = _client.Client;
        while (s.Connected)
        {
            try
            {
                while (await s.ReceiveAsync(buffer) != 0)
                {
                    message += Encoding.UTF8.GetString(buffer); // read stream to message
                    if (message.EndsWith("\r\n")) // check if full message is received
                    {
                        TcpPacket p = TcpPacketParser.Parse(message.Trim());
                        _gotPackets.Add(p);
                        switch (ClientFsm.GetState())
                        {
                            case FsmStateEnum.Auth:
                                if (p.GetMsgType() == MessageTypeEnum.Err)
                                {
                                    ClientFsm.SetState(FsmStateEnum.End);
                                    TcpPacket bye = TcpPacketBuilder.build_bye();
                                    await SendDataToServer(bye);
                                } else if (p.GetMsgType() == MessageTypeEnum.Reply)
                                {
                                    ClientFsm.SetState(FsmStateEnum.Open);
                                } else if (p.GetMsgType() == MessageTypeEnum.NotReply)
                                {
                                    ClientFsm.SetState(FsmStateEnum.Auth);
                                }
                                break;
                            case FsmStateEnum.Open:
                                if (p.GetMsgType() == MessageTypeEnum.Err)
                                {
                                    ClientFsm.SetState(FsmStateEnum.End);
                                    TcpPacket bye = TcpPacketBuilder.build_bye();
                                    await SendDataToServer(bye);
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
            catch (SocketException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
        _gotPackets.CompleteAdding();
        Console.Error.WriteLine("Reader terminated...");
    }
    
    public async Task Printer()
    {
        Console.Error.WriteLine("Printer started...");
        while (!_gotPackets.IsCompleted)
        {
            TcpPacket? packet = await Task.Run(() =>
            {
                try
                {
                    TcpPacket p = _gotPackets.Take(); 
                    return p;
                } catch (InvalidOperationException)
                {
                    return null;
                }
            });
            if (packet != null) packet.Print();
        }
        Console.Error.WriteLine("Printer terminated...");
    }
}