using System.Net.Sockets;
using System.Text;

namespace Server.TcpMessages;

public class MessageManager
{
    public static void SendMessage(NetworkStream stream, MessageType type, string message)
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);
        byte[] typeBytes = BitConverter.GetBytes((int)type);
        
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(typeBytes);
            Array.Reverse(lengthBytes);
        }
        
        stream.Write(typeBytes, 0, 4);
        stream.Write(lengthBytes, 0, 4);
        stream.Write(messageBytes, 0, messageBytes.Length);
    }
    
    public static (MessageType type, string payload) ReadMessage(NetworkStream stream)
    {
        byte[] typeBytes = new byte[4];
        byte[] lengthBytes = new byte[4];

        stream.Read(typeBytes, 0, 4);
        stream.Read(lengthBytes, 0, 4);
        
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(typeBytes);
            Array.Reverse(lengthBytes);
        }

        int length = BitConverter.ToInt32(lengthBytes, 0);
        byte[] messageBytes = new byte[length];

        int totalRead = 0;

        while (totalRead < length)
        {
            int bytesRead = stream.Read(messageBytes, totalRead, length - totalRead);
            totalRead += bytesRead;
        }

        return new((MessageType)BitConverter.ToInt32(typeBytes), Encoding.UTF8.GetString(messageBytes));
    }
}