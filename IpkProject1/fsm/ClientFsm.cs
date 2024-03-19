using System.Runtime.InteropServices.JavaScript;
using IpkProject1.Messages;

namespace IpkProject1.fsm;

public class ClientFsm
{
    private static FsmState _state = FsmState.Start;
    
    private static MessageType _lastClientMessage = MessageType.None;
    private static MessageType _lastServerMessage = MessageType.None;
    
    public static bool IsAllowedServerMessage(MessageType messageType)
    {
        _lastServerMessage = messageType;
        switch (_state)
        {
            case FsmState.Auth:
                return messageType == MessageType.NotReply
                       || messageType == MessageType.Reply
                       || messageType == MessageType.Err;
            case FsmState.Open:
                return true;
            case FsmState.Error:
                return false;
        }
        return false;
    }
    
    public static bool IsAllowedClientMessage(MessageType messageType)
    {
        _lastServerMessage = messageType;
        switch (_state)
        {
            case FsmState.Start:
                return messageType == MessageType.Auth;
            case FsmState.Auth:
                return messageType == MessageType.Auth
                       || messageType == MessageType.Bye;
            case FsmState.Open:
                return messageType == MessageType.Msg
                       || messageType == MessageType.Join
                       || messageType == MessageType.Bye
                       || messageType == MessageType.Err;
            case FsmState.Error:
                return messageType == MessageType.Bye;
        }
        return false;
    }

    public static bool MakeTransition()
    {
        // var server, client combination
        Tuple<MessageType, MessageType> pair = new Tuple<MessageType, MessageType>(
            _lastServerMessage, _lastClientMessage);
        _lastClientMessage = MessageType.None;
        _lastServerMessage = MessageType.None;
        switch (_state)
        {
            case FsmState.Start:
                if (pair.Item1 == MessageType.None && pair.Item2 == MessageType.Auth)
                {
                    _state = FsmState.Auth;
                    return true;
                }

                break;
            case FsmState.Auth:
                if (pair.Item1 == MessageType.NotReply && pair.Item2 == MessageType.Auth)
                {
                    _state = FsmState.Auth;
                    return true;
                }
                if (pair.Item1 == MessageType.Reply && pair.Item2 == MessageType.None)
                {
                    _state = FsmState.Open;
                    return true;
                }
                if ((pair.Item1 == MessageType.Err || pair.Item1 == MessageType.None) &&
                         pair.Item2 == MessageType.Bye)
                {
                    _state = FsmState.End;
                    return true;
                }
                break;
            case FsmState.Open:
                if ((pair.Item1 == MessageType.Msg || pair.Item1 == MessageType.NotReply ||
                     pair.Item1 == MessageType.Reply)
                    && pair.Item2 == MessageType.None)
                {
                    _state = FsmState.Open;
                    return true;
                }
                if (pair.Item1 == MessageType.None && 
                    (pair.Item2 == MessageType.Join || pair.Item2 == MessageType.Msg))
                {
                    _state = FsmState.Open;
                    return true;
                }
                if (pair.Item1 == MessageType.Err && pair.Item2 == MessageType.Bye)
                {
                    _state = FsmState.End;
                    return true;
                }
                if (pair.Item1 == MessageType.Bye && pair.Item2 == MessageType.None)
                {
                    _state = FsmState.End;
                    return true;
                }
                if (pair.Item2 == MessageType.Err)
                {
                    _state = FsmState.Error;
                    return true;
                }
                break;
            case FsmState.Error:
                if (pair.Item1 == MessageType.None && pair.Item2 == MessageType.Bye)
                {
                    _state = FsmState.End;
                    return true;
                }
                break;
            case FsmState.End:
                if (pair.Item2 == MessageType.None) return true;
                break;
        }
        return false;
    }
}   