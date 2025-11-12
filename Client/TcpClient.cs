using System.Net;
using System.Net.Sockets;
using Client.TcpMessages;

namespace Client;

public class TcpClient : IDisposable
{
    private Socket? _socket;
    
    public void Connect(string serverIp, int port)
    {
        // Connect socket
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.Connect(new IPEndPoint(IPAddress.Parse(serverIp), port));
        
        // Send message
        using NetworkStream stream = new NetworkStream(_socket);
        
        MessageManager.SendMessage(stream, MessageType.CONNECT, "Hello from client");
        
        // Read response
        var (type, payload) = MessageManager.ReadMessage(stream);
        Console.WriteLine($"Server response of type \"{type}\": {payload}\n");
    }
    
    public void Disconnect()
    {
        if (_socket == null || !_socket.IsBound)
            return;
        
        // Send message
        using NetworkStream stream = new NetworkStream(_socket);
        
        MessageManager.SendMessage(stream, MessageType.DISCONNECT, "");
    }

    public void Dispose()
    {
        _socket?.Shutdown(SocketShutdown.Both);
        _socket?.Close();
        _socket?.Dispose();
    }
}