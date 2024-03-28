namespace IpkProject1.interfaces;

public interface IChatClient
{
    public bool _isSenderTerminated { get; set; }
    public void Connect(string host, int port);
    public void Disconnect();
    public Task AddPacketToSendQueue(IPacket packet);
    public Task Reader();
    public Task Printer();
    public Task Sender();
    public void Close();
}