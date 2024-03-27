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
        return Regex.IsMatch(userName, _userNamePattern);
    }
    
    public static bool CheckChanelId(string chanelId)
    {
        return Regex.IsMatch(chanelId, _chanelIdPattern);
    }
    
    public static bool CheckSecret(string secret)
    {
        return Regex.IsMatch(secret, _secretPattern);
    }
    
    public static bool CheckDisplayName(string displayName)
    {
        return Regex.IsMatch(displayName, _displayNamePattern);
    }
    
    public static bool CheckMsg(string msg)
    {
        return Regex.IsMatch(msg, _msgPattern);
    }
}