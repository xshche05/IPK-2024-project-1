namespace IpkProject1.interfaces;

public interface IPacketBuilder
{
    public static IPacket build_auth(string login, string dname, string secret)
    {
        throw new NotImplementedException();
    }

    public static IPacket build_msg(string dname, string msg)
    {
        throw new NotImplementedException();
    }
    
    public static IPacket build_error(string dname, string msg)
    {
        throw new NotImplementedException();
    }
    
    public static IPacket build_join(string channel, string dname)
    {
        throw new NotImplementedException();
    }
    
    public static IPacket build_bye()
    {
        throw new NotImplementedException();
    }
}