using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using IpkProject1.interfaces;

namespace IpkProject1.udp;

public class UdpChatClient : IChatClient
{
    private readonly UdpClient _client = new ();
    private readonly BlockingCollection<UdpPacket> _gotPackets = new ();
    private int _idCounter = 0;
    private List<int> _responseIds = new ();
    
    public UdpChatClient()
    {
        throw new NotImplementedException();
    }
    
    public void Connect(string ip, int port)
    {
        _client.Connect(ip, port);
    }
    
    private void ChangePort(int port)
    {
        var endPoint = _client.Client.RemoteEndPoint as IPEndPoint;
        _client.Close();
        if (endPoint == null) return;
        _client.Connect(endPoint.Address.ToString(), port);
    }
    public void Disconnect()
    {
        try
        {
            _client.Client.Shutdown(SocketShutdown.Both);
        }
        finally
        {
            _client.Client.Close();
        }
        Console.WriteLine("Disconnected from server...");
    }

    public int GetId()
    {
        _idCounter++;
        return _idCounter;
    }
    
    public async Task Reader()
    {
        throw new NotImplementedException();
    }
    
    public async Task Printer()
    {
        throw new NotImplementedException();
    }
    
    public async Task SendDataToServer(IPacket packet)
    {
        throw new NotImplementedException();
    }
}