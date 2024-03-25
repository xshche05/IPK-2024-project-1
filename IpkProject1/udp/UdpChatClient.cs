using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using IpkProject1.enums;
using IpkProject1.fsm;
using IpkProject1.interfaces;

namespace IpkProject1.udp;

public class UdpChatClient : IChatClient
{
    private readonly UdpClient _client = new ();
    private bool _isConnected = false;
    private IPEndPoint endPoint;
    private int _idCounter = 0;
    private BlockingCollection<int> confirmedMessages = new ();
    private List<int> processedMessages = new ();
    
    public void Connect(string ip, int port)
    {
        // todo: check if ip is valid or hostname
        IPAddress ipAddress = Dns.GetHostAddresses(ip)[0];
        endPoint = new IPEndPoint(ipAddress, port);
        _client.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        _isConnected = true;
        Console.WriteLine("Connected to server...");
    }
    
    public void Disconnect()
    {
        _client.Close();
        _isConnected = false;
        Console.WriteLine("Disconnected from server...");
    }

    public int GetId()
    {
        _idCounter++;
        return _idCounter;
    }

    public async Task Reader()
    {
        Console.Error.WriteLine("Reader started...");
        while (_isConnected)
        {
            try
            {
                var data = await _client.ReceiveAsync();
                byte[] bytes = data.Buffer;
                MessageTypeEnum type = (MessageTypeEnum)bytes[0];
                UInt16 msg_id = BitConverter.ToUInt16(bytes[1..3]);
                if (type == MessageTypeEnum.Confirm)
                {
                    confirmedMessages.Add(msg_id);
                }
                else
                {
                    endPoint = data.RemoteEndPoint;
                    UdpPacket p = UdpPacketBuilder.build_confirm(msg_id);
                    // randomize confirm message
                    // if (new Random().Next(0, 20) < 10)
                    // {
                    //     await _client.Client.SendToAsync(p.ToBytes(), SocketFlags.None, endPoint);   
                    // }
                    await _client.Client.SendToAsync(p.ToBytes(), SocketFlags.None, endPoint);
                    UdpPacket packet = new UdpPacket(type, bytes);
                    if (!processedMessages.Contains(msg_id))
                    {
                        processedMessages.Add(msg_id);
                        packet.Print();
                        switch (ClientFsm.GetState())
                        {
                            case FsmStateEnum.Auth:
                                if (packet.GetMsgType() == MessageTypeEnum.Err)
                                {
                                    ClientFsm.SetState(FsmStateEnum.End);
                                    UdpPacket bye = UdpPacketBuilder.build_bye();
                                    await SendDataToServer(bye);
                                }
                                else if (packet.GetMsgType() == MessageTypeEnum.Reply && packet.ToBytes()[3] == 1)
                                {
                                    ClientFsm.SetState(FsmStateEnum.Open);
                                }
                                else if (packet.GetMsgType() == MessageTypeEnum.Reply && packet.ToBytes()[3] == 0)
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
                    }
                }
            }
            catch (Exception e)
            {
                break;
            }
        }
        Console.Error.WriteLine("Reader terminated...");
    }

    public async Task Printer()
    {
        return;
    }
    
    // todo add sending func, send from queue, add add to queue func
    public async Task SendDataToServer(IPacket packet) {
        byte[] data = packet.ToBytes();
        switch (ClientFsm.GetState())
        {
            case FsmStateEnum.Start:
                if (packet.GetMsgType() == MessageTypeEnum.Auth)
                    ClientFsm.SetState(FsmStateEnum.Auth);
                break;
        }
        int id = ((UdpPacket)packet).GetMsgId();
        for (int i = 0; i < 3; i++)
        {
            await _client.Client.SendToAsync(data, SocketFlags.None, endPoint);
            // wait for confirm message with timeout 250ms
            int controlId = -1;
            Task checkTask = Task.Run(() => { controlId = confirmedMessages.Take(); });
            Task delayTask = Task.Delay(250);
            Task firstCompleted = await Task.WhenAny(checkTask, delayTask);
            if (firstCompleted == checkTask && checkTask.IsCompletedSuccessfully && controlId == id)
            {
                Console.WriteLine("Message with id " + id + " confirmed");
                break;
            }
        }
    }
}