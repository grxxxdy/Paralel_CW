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
        MessageType type = MessageType.UNKNOWN;
        string payload = "";
        while (type != MessageType.CONNECT)
        {
            (type, payload) = MessageManager.ReadMessage(stream);
            Console.WriteLine($"Server response of type \"{type}\": {payload}\n");
        }
    }
    
    public void Disconnect()
    {
        if (_socket == null || !_socket.IsBound)
            return;
        
        // Send message
        using NetworkStream stream = new NetworkStream(_socket);
        
        MessageManager.SendMessage(stream, MessageType.DISCONNECT, "");
    }

    public void SearchFiles(string word)
    {
        try
        {
            using NetworkStream stream = new NetworkStream(_socket, ownsSocket: false);
            
            // Send message
            MessageManager.SendMessage(stream, MessageType.SEARCHFILES, word);
            Console.WriteLine($"[Client] Sent search request for word: \"{word}\"");
            
            // Get response
            var (responseType, responsePayload) = MessageManager.ReadMessage(stream);
            
            // Check response
            if (responseType == MessageType.SEARCHFILES)
            {
                if (string.IsNullOrWhiteSpace(responsePayload))
                    Console.WriteLine("[Client] No files found for this word.\n");
                else
                {
                    var files = responsePayload.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    Console.WriteLine($"[Client] Files containing \"{word}\":");
                    foreach (var file in files)
                        Console.WriteLine($"  - {file}");
                    Console.WriteLine("\n");
                }
            }
            else
            {
                Console.WriteLine($"[Client] Unexpected response type: {responseType}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Client] Search failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _socket?.Shutdown(SocketShutdown.Both);
        _socket?.Close();
        _socket?.Dispose();
    }
}