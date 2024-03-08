namespace IpkProject1.tcp;

using System;
using System.Text.RegularExpressions;

public class GrammarChecker
{
    private const string IdPattern = @"[a-zA-Z0-9\-]{1,20}";
    private const string SecretPattern = @"[a-zA-Z0-9\-]{1,128}";
    private const string ContentPattern = @"[\x20-\x7E]{1,1400}";
    private const string DnamePattern = @"[\x21-\x7E]{1,20}";
    private const string SpPattern = " ";
    private const string CrLfPattern = "\r\n";
    private const string IsPattern = SpPattern + "IS" + SpPattern;
    private const string AsPattern = SpPattern + "AS" + SpPattern;
    private const string UsingPattern = SpPattern + "USING" + SpPattern;

    private const string JoinPattern = $"JOIN{SpPattern}{IdPattern}{AsPattern}{DnamePattern}";
    private const string AuthPattern = $"AUTH{SpPattern}{IdPattern}{AsPattern}{DnamePattern}{UsingPattern}{SecretPattern}";
    private const string MsgFromPattern = $"MSG FROM{SpPattern}{DnamePattern}{IsPattern}{ContentPattern}";
    private const string ErrFromPattern = $"ERR FROM{SpPattern}{DnamePattern}{IsPattern}{ContentPattern}";
    private const string ReplyPattern = $"REPLY{SpPattern}(OK|NOK){IsPattern}{ContentPattern}";
    private const string ByePattern = "BYE";
    
    private const string PacketContentPattern = $"({JoinPattern})|({AuthPattern})|({MsgFromPattern})|({ErrFromPattern})|({ReplyPattern})|({ByePattern})";
    private const string PacketPattern = $"{PacketContentPattern}{CrLfPattern}";
        
    private static string FullMatchPattern(string pattern)
    {
        return $"^{pattern}$";
    }
    
    public static bool CheckId(string id)
    {
        return Regex.IsMatch(id, FullMatchPattern(IdPattern));
    }
    
    public static bool CheckSecret(string secret)
    {
        return Regex.IsMatch(secret, FullMatchPattern(SecretPattern));
    }
    
    public static bool CheckContent(string content)
    {
        return Regex.IsMatch(content, FullMatchPattern(ContentPattern));
    }
    
    public static bool CheckDname(string dname)
    {
        return Regex.IsMatch(dname, FullMatchPattern(DnamePattern));
    }
    
    private static bool IsValidPacket(string packet)
    {
        return Regex.IsMatch(packet, FullMatchPattern(PacketPattern));
    }
    
    private static TcpPacketType GetPacketType(string packet)
    {
        if (Regex.IsMatch(packet, FullMatchPattern(JoinPattern)))
        {
            return TcpPacketType.Join;
        }
        if (Regex.IsMatch(packet, FullMatchPattern(AuthPattern)))
        {
            return TcpPacketType.Auth;
        }
        if (Regex.IsMatch(packet, FullMatchPattern(MsgFromPattern)))
        {
            return TcpPacketType.MsgFrom;
        }
        if (Regex.IsMatch(packet, FullMatchPattern(ErrFromPattern)))
        {
            return TcpPacketType.ErrFrom;
        }
        if (Regex.IsMatch(packet, FullMatchPattern(ReplyPattern)))
        {
            return TcpPacketType.Reply;
        }
        return TcpPacketType.Bye;
    }
    
    public static TcpPacket GetTcpPacket(string packet)
    {
        if (!IsValidPacket(packet))
        {
            throw new ArgumentException("Invalid packet");
        }
        
        var trimmedPacket = packet.Trim();
        
        var packetType = GetPacketType(trimmedPacket);
        var tcpPacket = new TcpPacket(packetType);
        
        switch (packetType)
        {
            case TcpPacketType.Join:
                // TODO
                break;
            case TcpPacketType.Auth:
                // TODO
                break;
            case TcpPacketType.MsgFrom: // Done
                var packetPartsMsgFrom = trimmedPacket.Split(" ", 5);
                tcpPacket.SetDname(packetPartsMsgFrom[2]);
                tcpPacket.SetContent(packetPartsMsgFrom[4]);
                break;
            case TcpPacketType.ErrFrom: 
                var packetPartsErrFrom = trimmedPacket.Split(" ", 5);
                tcpPacket.SetDname(packetPartsErrFrom[2]);
                tcpPacket.SetContent(packetPartsErrFrom[4]);
                break;
            case TcpPacketType.Reply: // Done
                var packetPartsReply = trimmedPacket.Split(" ", 4);
                tcpPacket.SetReply(packetPartsReply[1]);
                tcpPacket.SetContent(packetPartsReply[3]);
                break;
        }
        return tcpPacket;
    }
    
    
}