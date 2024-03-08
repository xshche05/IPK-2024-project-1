using System.Dynamic;

namespace IpkProject1.tcp;

public class TcpPacket
{
    private TcpPacketType Type { get; set; }
    private string? Id { get; set; }
    private string? Secret { get; set; }
    private string? Dname { get; set; }
    private string? Content { get; set; }
    private string? Reply { get; set; }
    
    public TcpPacket(TcpPacketType type)
    {
        Type = type;
        Id = null;
        Secret = null;
        Dname = null;
        Content = null;
        Reply = null;
    }
    
    public void SetId(string id)
    {
        Id = id;
    }
    
    public void SetSecret(string secret)
    {
        Secret = secret;
    }
    
    public void SetDname(string dname)
    {
        Dname = dname;
    }
    
    public void SetContent(string content)
    {
        Content = content;
    }
    
    public void SetReply(string reply)
    {
        Reply = reply;
    }

    public void Print()
    {
        switch (Type)
        {
            case TcpPacketType.MsgFrom:
                Console.Write($"{Dname}: {Content}\n");
                break;
            case TcpPacketType.ErrFrom:
                // write to stderr
                Console.Error.Write($"ERR FROM {Dname}: {Content}\n");
                break;
            case TcpPacketType.Reply:
                if (Reply == "OK")
                {
                    Console.Error.Write($"Success: {Content}\n");
                }
                else
                {
                    Console.Error.Write($"Failure: {Content}\n");
                }
                break;
            default:
                throw new Exception("Invalid packet type to print");
        }
    }
}