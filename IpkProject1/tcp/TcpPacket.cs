using System.Text;
using IpkProject1.Messages;

namespace IpkProject1.tcp;

public class TcpPacket
{
    private MessageType _type;
    private string _data;
    
    const string CRLF = "\r\n";
        
    public void build_auth(string login, string dname, string secret)
    {
        _type = MessageType.Auth;
        _data = $"AUTH {login} AS {dname} USING {secret}{CRLF}"; 
    }
    
    public void build_msg(string dname, string msg)
    {
        _type = MessageType.Msg;
        _data = $"MSG FORM {dname} IS {msg}{CRLF}";
    }
    
    public void build_error(string dname, string msg)
    {
        _type = MessageType.Err;
        _data = $"ERROR FORM {dname} IS {msg}{CRLF}";
    }
}