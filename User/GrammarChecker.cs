using System.Text.RegularExpressions;
using IpkProject1.SysArg;

namespace IpkProject1.User;

public static class GrammarChecker
{
    private const string UserNamePattern = @"^[A-Za-z0-9\-]{1,20}$";
    #if DEBUG
    private const string ChanelIdPattern = @"^[A-Za-z0-9\-\.]{1,20}$";
    #else
    private const string ChanelIdPattern = @"^[A-Za-z0-9\-]{1,20}$";
    #endif
    private const string SecretPattern = @"^[A-Za-z0-9\-]{1,128}$";
    private const string DisplayNamePattern = @"^[\x21-\x7E]{1,20}$";
    private const string MsgPattern = @"^[\x20-\x7E]{1,1400}$";
    
    public static bool CheckUserName(string userName)
    {
        var flag = Regex.IsMatch(userName, UserNamePattern);
        if (!flag) Io.ErrorPrintLine("ERR: Invalid username, please check your input!", ColorScheme.Warning);
        return flag;
    }
    
    public static bool CheckChanelId(string chanelId)
    {
        var flag = Regex.IsMatch(chanelId, ChanelIdPattern);
        if (!flag) Io.ErrorPrintLine("ERR: Invalid channel id, please check your input!", ColorScheme.Warning);
        return flag;
    }
    
    public static bool CheckSecret(string secret)
    {
        var flag = Regex.IsMatch(secret, SecretPattern);
        if (!flag) Io.ErrorPrintLine("ERR: Invalid secret, please check your input!", ColorScheme.Warning);
        return flag;
    }
    
    public static bool CheckDisplayName(string displayName, bool server = false)
    {
        var flag = Regex.IsMatch(displayName, DisplayNamePattern);
        if (!flag && !server) Io.ErrorPrintLine("ERR: Invalid display name, please check your input!", ColorScheme.Warning);
        return flag;
    }
    
    public static bool CheckMsg(string msg, bool server = false)
    {
        var flag = Regex.IsMatch(msg, MsgPattern);
        if (!flag && !server) Io.ErrorPrintLine("ERR: Invalid message, please check your input!", ColorScheme.Warning);
        return flag;
    }
}