using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace IpkProject1.tcp;

public class TcpChatClient
{
    private static TcpClient Client { get; } = new TcpClient();
    public static void Connect(string ip, int port)
    {
        Client.Connect(ip, port);
        Console.WriteLine("Connected to server...");
    }
    
    public static void Disconnect()
    {
        Client.Close();
        Console.WriteLine("Disconnected from server...");
    }
    
    // async function send data to server
    public static async Task SendDataToServer(TcpPacket packet)
    {
        byte[] buffer = packet.ToBytes();
        await Client.GetStream().WriteAsync(buffer, 0, buffer.Length);
    }
    
    public static async Task Reader(BlockingCollection<TcpPacket> gotPackets)
    {
        // read separate messages from server, every message is separated by CRLF
        string message = "";
        byte[] buffer = new byte[1];
        // write to stderr if reader is terminated
        Console.Error.WriteLine("Reader started...");
        Socket s = Client.Client;
        while (s.Connected)
        {
            try
            {
                while (await s.ReceiveAsync(buffer) != 0)
                {
                    message += Encoding.UTF8.GetString(buffer); // read stream to message
                    if (message.EndsWith("\r\n")) // check if full message is received
                    {
                        gotPackets.Add(TcpPacketParser.Parse(message.Trim()));
                        message = ""; // clear message
                    }
                }
            }
            catch (SocketException e)
            {
                Console.Error.WriteLine("Reader got socket closed");
                break;
            }
        }
        gotPackets.CompleteAdding();
        Console.Error.WriteLine("Reader terminated...");
    }
    
    public static async Task Printer(BlockingCollection<TcpPacket> gotPackets)
    {
        Console.Error.WriteLine("Printer started...");
        while (!gotPackets.IsCompleted)
        {
            TcpPacket? packet = await Task.Run(() =>
            {
                try
                {
                    TcpPacket p = gotPackets.Take(); 
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