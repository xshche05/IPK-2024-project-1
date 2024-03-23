namespace IpkProject1.enums;

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