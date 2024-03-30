using IpkProject1.enums;

namespace IpkProject1.interfaces;

public interface IPacket
{
    // Property for the packet type
    public MessageTypeEnum Type { get; }
    // Prints the packet to the console
    public void Print();
    // Converts the packet to a byte array
    public byte[] ToBytes();
    // Returns a reply state of reply packets, null in case of other packets
    public bool? ReplyState();
}