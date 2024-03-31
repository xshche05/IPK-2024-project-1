namespace IpkProject1.Interfaces;

public interface IChatClient
{
    // Connects to the server
    public void Connect(string host, int port);
    // Disconnects from the server
    public void Disconnect();
    // Adds a packet to the send queue
    public void AddPacketToSendQueue(IPacket packet);
    // Starts the reader task
    public Task Reader();
    // Starts the printer task
    public Task Printer();
    // Starts the sender task
    public Task Sender();
    // Closes the client
    public void Close();
}