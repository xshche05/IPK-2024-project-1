using System.Collections.Concurrent;
using System.Diagnostics;
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
    private readonly BlockingCollection<UdpPacket> _gotPacketsQueue = new ();
    private readonly List<int> _processedMessages = new ();
    private readonly BlockingCollection<UdpPacket> _sendPacketsQueue = new ();
    
    public void Connect(string host, int port)
    {
        // todo: check if ip is valid or hostname
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
    }

    public void Close()
    {
        _client.Close();
        _gotPacketsQueue.CompleteAdding();
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
                UInt16 msg_id = BitConverter.ToUInt16(bytes[1..3]);
                if (type == MessageTypeEnum.Confirm)
                {
                    _confirmedMessages.Add(msg_id);
                    Io.DebugPrintLine("Confirm received, id: " + msg_id);
                }
                else
                {
                    _endPoint = data.RemoteEndPoint;
                    UdpPacket confirm = UdpPacketBuilder.build_confirm(msg_id);
                    await _client.Client.SendToAsync(confirm.ToBytes(), SocketFlags.None, _endPoint);
                    UdpPacket packet = new UdpPacket(type, bytes);
                    if (!_processedMessages.Contains(msg_id))
                    {
                        _processedMessages.Add(msg_id);
                        _gotPacketsQueue.Add(packet);
                        FsmUpdate(packet);
                    }
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
            if (packet != null) packet.Print();
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
    
    public async Task AddPacketToSendQueue(IPacket packet)
    {
        await Task.Run(() =>
        {
            if (packet.GetMsgType() == MessageTypeEnum.Auth)
            {
                IpkProject1.AuthSem.WaitOne();
                Io.DebugPrintLine("AuthSem acquired in UdpChatClient...");
            }

            _sendPacketsQueue.Add((UdpPacket)packet);
            Io.DebugPrintLine("Packet added to send queue...");
        });
    }
    
    private async Task SendDataToServer(IPacket packet) {
        byte[] data = packet.ToBytes();
        switch (ClientFsm.State)
        {
            case FsmStateEnum.Start:
                if (packet.GetMsgType() == MessageTypeEnum.Auth)
                    ClientFsm.SetState(FsmStateEnum.Auth);
                break;
        }
        int id = ((UdpPacket)packet).GetMsgId();
        for (int i = 0; i < 1 + SysArgParser.GetAppConfig().Retries; i++)
        {
            _client.Client.SendTo(data, SocketFlags.None, _endPoint);
            // wait for confirm message with timeout 250ms
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
                Io.ErrorPrintLine("ERROR: Message with id " + id + " not confirmed, max retries reached");
                if (ClientFsm.State != FsmStateEnum.End)
                {
                    ClientFsm.SetState(FsmStateEnum.End);
                }
            }
        }
    }
}