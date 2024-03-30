namespace IpkProject1.enums;

// Enum for all possible message types according to the assignment,
// and their respective byte values for UDP communication
public enum MessageTypeEnum
{
    Confirm = 0x00,
    Reply = 0x01,
    Auth = 0x02,
    Join = 0x03,
    Msg = 0x04,
    Err = 0xFE,
    Bye = 0xFF,
    None = 0xAA,
}