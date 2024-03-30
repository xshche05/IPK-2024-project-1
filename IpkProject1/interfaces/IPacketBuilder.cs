namespace IpkProject1.interfaces;

public interface IPacketBuilder
{
    // Builds an auth packet with the given login, display name and secret
    public IPacket build_auth(string login, string displayName, string secret)
    {
        throw new NotImplementedException();
    }
    // Builds a message packet with the given display name and message
    public IPacket build_msg(string displayName, string msg)
    {
        throw new NotImplementedException();
    }
    // Builds an error packet with the given display name and message
    public IPacket build_error(string displayName, string msg)
    {
        throw new NotImplementedException();
    }
    // Builds a join packet with the given channel and display name
    public IPacket build_join(string channel, string displayName)
    {
        throw new NotImplementedException();
    }
    // Builds a bye packet
    public IPacket build_bye()
    {
        throw new NotImplementedException();
    }
}