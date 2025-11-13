namespace Client.TcpMessages;

public enum MessageType
{
    CONNECT = 0,
    DISCONNECT,
    WELCOME,
    UNKNOWN,
    SEARCHFILES
}