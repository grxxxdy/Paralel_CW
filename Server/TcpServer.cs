using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Server.InvertedIndexStructure;
using Server.TcpMessages;

namespace Server;

public class TcpServer : IDisposable
{
    private Socket? _socket;
    private InvertedIndex _invertedIndex;

    private ThreadPool _threadPool;
    
    private int _connectedClients = 0;
    
    private Mutex _consoleMutex;

    public TcpServer(InvertedIndex invertedIndex, ThreadPool threadPool)
    {
        _invertedIndex = invertedIndex;
        _threadPool = threadPool;
        _consoleMutex = new Mutex();
    }
    public void StartTcp(int port)
    {
        string ip = GetLocalIpAddress();
        
        // Socket creation
        var tcpEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        // socket bining
        _socket.Bind(tcpEndpoint);
        _socket.Listen(10000);
        
        Console.WriteLine($"\n[Server] Server running on {ip}:{port}.");
        
        // accept connections
        while (true)
        {
            Socket client;
            try
            {
                client = _socket.Accept();
            }
            catch (Exception ex)
            {
                SafeConsoleWrite($"[Server] Accept error: {ex.Message}");
                continue;
            }

            int now = Interlocked.Increment(ref _connectedClients);
            // To make things quicker
            //SafeConsoleWrite($"\n[Server] Client connected. Total clients: {now}");
            
            _threadPool.EnqueueTask(() => HandleClient(client));
        }
    }

    private void HandleClient(Socket clientSocket)
    {
        try
        {
            using NetworkStream stream = new NetworkStream(clientSocket, ownsSocket: false);
            long clientId = clientSocket.Handle.ToInt64();
            
            try
            {
                MessageManager.SendMessage(stream, MessageType.WELCOME,
                    "[Server] You've been added to the queue. Please await.");
            }
            catch
            {
                SafeConsoleWrite($"[Server] Client {clientId} disconnected before WELCOME.");
                return;
            }
            
            while (true)
            {
                var (type, payload) = MessageManager.ReadMessage(stream);
                SafeConsoleWrite($"[Server] Received a message of type {type} from client {clientId}: {payload}");

                MessageType typeToSend = MessageType.UNKNOWN;
                string payloadToSend = "";

                switch (type)
                {
                    case MessageType.CONNECT:
                        typeToSend = MessageType.CONNECT;
                        payloadToSend = "[Server] Connected successfully!";
                        break;
                    case MessageType.DISCONNECT:
                        SafeConsoleWrite($"[Server] Client {clientId} requested disconnect.");
                        return;
                    case MessageType.SEARCHFILES:
                        string word = payload;
                        List<string> searchResults = _invertedIndex.Search(word);
                        payloadToSend = searchResults.Count > 0 ? string.Join("\n", searchResults) : string.Empty;
                        typeToSend = MessageType.SEARCHFILES;
                        break;
                }

                MessageManager.SendMessage(stream, typeToSend, payloadToSend);
                SafeConsoleWrite($"[Server] Sent a message of type {typeToSend} to client {clientId}.");
                //Console.WriteLine($"[Server] Sent a message of type {typeToSend} to client {clientId}: \"{payloadToSend}\"");
            }
        }
        catch (Exception ex)
        {
            SafeConsoleWrite($"[Server] Server error: {ex.Message}");
        }
        finally
        {
            try { clientSocket.Shutdown(SocketShutdown.Both); } catch { }
            try { clientSocket.Close(); } catch { }
            try { clientSocket.Dispose(); } catch { }
            
            int now = Interlocked.Decrement(ref _connectedClients);
            SafeConsoleWrite($"[Server] Client disconnected. Total clients: {now}");
        }
    }
    
    private static string GetLocalIpAddress()
    {
        foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (netInterface.OperationalStatus != OperationalStatus.Up ||
                netInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                netInterface.NetworkInterfaceType == NetworkInterfaceType.Tunnel ||
                netInterface.Description.Contains("VPN") ||
                netInterface.Description.Contains("Virtual"))
            {
                continue;
            }

            foreach (UnicastIPAddressInformation ip in netInterface.GetIPProperties().UnicastAddresses)
            {
                if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.Address.ToString();
                }
            }
        }

        throw new Exception("No active network adapters with a valid IPv4 address found!");
    }
    
    private void SafeConsoleWrite(string message)
    {
        _consoleMutex.WaitOne();
        Console.WriteLine(message);
        _consoleMutex.ReleaseMutex();
    }
    
    public void Dispose()
    {
        _socket?.Shutdown(SocketShutdown.Both);
        _socket?.Close();
        _socket?.Dispose();
    }

}