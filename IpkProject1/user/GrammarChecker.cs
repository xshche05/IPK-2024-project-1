using System.Diagnostics;
using System.Text.RegularExpressions;

namespace IpkProject1.user;

public static class GrammarChecker
{
    private const string _userNamePattern = @"^[A-Za-z0-9\-]{1,20}$";
    #if DEBUG
    private const string _chanelIdPattern = @"^[A-Za-z0-9\-\.]{1,20}$";
    #else
    private const string _chanelIdPattern = @"^[A-Za-z0-9\-]{1,20}$";
    #endif
    private const string _secretPattern = @"^[A-Za-z0-9\-]{1,128}$";
    private const string _displayNamePattern = @"^[\x21-\x7E]{1,20}$";
    private const string _msgPattern = @"^[\x20-\x7E]{1,1400}$";
    
    public static bool CheckUserName(string userName)
    {
        var flag = Regex.IsMatch(userName, _userNamePattern);
        if (!flag) Io.ErrorPrintLine("ERR: Invalid username, please check your input!");
        return flag;
    }
    
    public static bool CheckChanelId(string chanelId)
    {
        var flag = Regex.IsMatch(chanelId, _chanelIdPattern);
        if (!flag) Io.ErrorPrintLine("ERR: Invalid channel id, please check your input!");
        return flag;
    }
    
    public static bool CheckSecret(string secret)
    {
        var flag = Regex.IsMatch(secret, _secretPattern);
        if (!flag) Io.ErrorPrintLine("ERR: Invalid secret, please check your input!");
        return flag;
    }
    
    public static bool CheckDisplayName(string displayName)
    {
        var flag = Regex.IsMatch(displayName, _displayNamePattern);
        if (!flag) Io.ErrorPrintLine("ERR: Invalid display name, please check your input!");
        return flag;
    }
    
    public static bool CheckMsg(string msg)
    {
        var flag = Regex.IsMatch(msg, _msgPattern);
        if (!flag) Io.ErrorPrintLine("ERR: Invalid message, please check your input!");
        return flag;
    }
}