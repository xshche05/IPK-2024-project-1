using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using IpkProject1.enums;
using IpkProject1.fsm;
using IpkProject1.interfaces;
using IpkProject1.sysarg;
using IpkProject1.user;

namespace IpkProject1.udp;

public class UdpChatClient : IChatClient
{
    private readonly UdpClient _client = new ();
    public bool _isSenderTerminated { get; set; } = false;
    private IPEndPoint? _endPoint;
    private readonly BlockingCollection<int> _confirmedMessages = new ();
    public BlockingCollection<UdpPacket> GotPacketsQueue { get; } = new();
    private readonly List<int> _processedMessages = new ();
    private readonly BlockingCollection<UdpPacket> _sendPacketsQueue = new ();
    
    public void Connect(string host, int port)
    {
        // todo: check if ip is valid or hostname
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
        _endPoint = endPoint;
        _client.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        _isSenderTerminated = false;
        Io.DebugPrintLine("Connected to server...");
    }
    
    public void Disconnect()
    {
        _sendPacketsQueue.CompleteAdding();
        Io.DebugPrintLine("Disconnected from server...");
        Task? last = IpkProject1.GetLastSendTask();
        if (last != null)
        {
            last.Wait();
        }
        GotPacketsQueue.CompleteAdding();
    }

    public void Close()
    {
        _client.Close();
    }
    
    private void FsmUpdate(IPacket packet)
    {
        switch (ClientFsm.State)
        {
            case FsmStateEnum.Auth:
                if (packet.GetMsgType() == MessageTypeEnum.Err)
                {
                    ClientFsm.SetState(FsmStateEnum.End);
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
                if (packet.GetMsgType() == MessageTypeEnum.Err)
                {
                    ClientFsm.SetState(FsmStateEnum.End);
                }
                else if (packet.GetMsgType() == MessageTypeEnum.Bye)
                {
                    ClientFsm.SetState(FsmStateEnum.End);
                }
                break;
            default:
                Io.DebugPrintLine("Unknown state...");
                break;
        }
    }

    public async Task Reader()
    {
        Io.DebugPrintLine("Reader started...");
        while (!_isSenderTerminated)
        {
            try
            {
                var data = await _client.ReceiveAsync();
                byte[] bytes = data.Buffer;
                MessageTypeEnum type = (MessageTypeEnum)bytes[0];
                Io.DebugPrintLine("Message received, type: " + type);
                UInt16 msg_id = BitConverter.ToUInt16(bytes[1..3]);
                if (type == MessageTypeEnum.Confirm)
                {
                    _confirmedMessages.Add(msg_id);
                    Io.DebugPrintLine("Confirm received, id: " + msg_id);
                }
                else if (type == MessageTypeEnum.Msg || type == MessageTypeEnum.Err || type == MessageTypeEnum.Reply)
                {
                    _endPoint = data.RemoteEndPoint;
                    UdpPacket confirm = UdpPacketBuilder.build_confirm(msg_id);
                    _client.Client.SendTo(confirm.ToBytes(), SocketFlags.None, _endPoint);
                    Io.DebugPrintLine($"Confirm sent for message {type} {msg_id}");
                    UdpPacket packet = new UdpPacket(type, bytes);
                    if (!_processedMessages.Contains(msg_id))
                    {
                        Io.DebugPrintLine($"Processing message {type} {msg_id}");
                        _processedMessages.Add(msg_id);
                        GotPacketsQueue.Add(packet);
                        FsmUpdate(packet);
                    }
                    else
                    {
                        Io.DebugPrintLine($"Message already processed... {type} {msg_id}");
                    }
                }
                else
                {
                    Io.ErrorPrintLine("ERR: Unknown message type received...");
                    var err = UdpPacketBuilder.build_error(InputProcessor.DisplayName, "Unknown message type received");
                    if (_endPoint == null)
                    {
                        Io.ErrorPrintLine("ERR: EndPoint is null...");
                        ClientFsm.SetState(FsmStateEnum.End);
                        return;
                    }
                    _client.Client.SendTo(err.ToBytes(), SocketFlags.None, _endPoint);
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
        while (!GotPacketsQueue.IsCompleted)
        {
            UdpPacket? packet = await Task.Run(() =>
            {
                try
                {
                    return GotPacketsQueue.Take();
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
            Io.DebugPrintLine($"Got packet from queue {p?.GetMsgType()}");
            if (p != null)
            {
                var last = SendDataToServer(p);
                IpkProject1.SetLastSendTask(last);
                await last;
                Io.DebugPrintLine($"Send packet to server {p.GetMsgType()}...");
            }
        }
        Io.DebugPrintLine("Sender terminated...");
    }
    
    public void AddPacketToSendQueue(IPacket packet)
    {
        if (packet.GetMsgType() == MessageTypeEnum.Auth)
        {
            IpkProject1.AuthSem.WaitOne();
            Io.DebugPrintLine("AuthSem acquired in UdpChatClient...");
        }
        _sendPacketsQueue.Add((UdpPacket)packet);
        Io.DebugPrintLine("Packet added to send queue...");
    }
    
    private async Task SendDataToServer(IPacket packet) {
        byte[] data = packet.ToBytes();
        if (packet.GetMsgType() == MessageTypeEnum.Err)
        {
            ClientFsm.SetState(FsmStateEnum.Error);
        }
        int id = ((UdpPacket)packet).GetMsgId();
        for (int i = 0; i < 1 + SysArgParser.GetAppConfig().Retries; i++)
        {
            if (_endPoint == null)
            {
                Io.ErrorPrintLine("ERR: EndPoint is null...");
                ClientFsm.SetState(FsmStateEnum.End);
                return;
            }
            _client.Client.SendTo(data, SocketFlags.None, _endPoint);
            int controlId = -1;
            Task checkTask = Task.Run(() => { controlId = _confirmedMessages.Take(); });
            Task delayTask = Task.Delay(SysArgParser.GetAppConfig().Timeout);
            Task firstCompleted = await Task.WhenAny(checkTask, delayTask);
            if (firstCompleted == checkTask && checkTask.IsCompletedSuccessfully && controlId == id)
            {
                Io.DebugPrintLine("Message with id " + id + " confirmed");
                break;
            }
            if (i == SysArgParser.GetAppConfig().Retries)
            {
                Io.ErrorPrintLine("ERR: Message with id " + id + " not confirmed, max retries reached");
                if (ClientFsm.State != FsmStateEnum.End)
                {
                    ClientFsm.SetState(FsmStateEnum.End);
                }
            }
        }
    }
}