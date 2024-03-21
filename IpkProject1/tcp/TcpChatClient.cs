using System.Net.Sockets;
using System.Text;

namespace IpkProject1.tcp;

public class TcpChatClient
{
    public static TcpClient Client { get; } = new TcpClient();
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
    public static async Task SendDataToServer(string msg)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(msg);
        await Client.GetStream().WriteAsync(buffer, 0, buffer.Length);
    }
    
    public static async Task Reader(Queue<TcpPacket> gotPackets)
    {
        // read separate messages from server, every message is separated by CRLF
        string message = "";
        byte[] buffer = new byte[1];
        // write to stderr if reader is terminated
        Console.Error.WriteLine("Reader started...");
        while (Client.Connected)
        {
            // if no data available, continue
            // Client connection check for exclude any exceptions in the future
            if (Client.Connected && !Client.GetStream().DataAvailable)
            {
                continue; // if no data available, continue
            }
            while (Client.Connected && (await Client.GetStream().ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                message += Encoding.UTF8.GetString(buffer); // read stream to message
                if (message.EndsWith("\r\n")) // check if full message is received
                {
                    // Todo parse msg to packet
                    // add packet to queue
                    Console.WriteLine(message.Trim());
                    message = ""; // clear message
                }
            }
        }
        Console.Error.WriteLine("Reader terminated...");
    }
}