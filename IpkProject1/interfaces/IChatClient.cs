namespace IpkProject1.interfaces;

public interface IChatClient
{
    public void Connect(string host, int port);
    public void Disconnect();
    public Task SendDataToServer(IPacket packet);
    public Task Reader();
    public Task Printer();
}