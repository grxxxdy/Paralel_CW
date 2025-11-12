using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Server.TcpMessages;

namespace Server;

public class TcpServer : IDisposable
{
    private Socket? _socket;

    public void StartTcp(int port)
    {
        string ip = GetLocalIpAddress();
        
        // Socket creation
        var tcpEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        // socket bining
        _socket.Bind(tcpEndpoint);
        _socket.Listen(10);
        
        Console.WriteLine($"\nServer running on {ip}:{port}.");
        
        // accept connections
        while (true)
        {
            var client = _socket.Accept();
            
            Thread clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }

    private void HandleClient(Socket clientSocket)
    {
        try
        {
            using NetworkStream stream = new NetworkStream(clientSocket);
            long clientId = clientSocket.Handle.ToInt64();
            Task<List<List<int>>?>? res = null;

            while (true)
            {
                var (type, payload) = MessageManager.ReadMessage(stream);
                Console.WriteLine($"\nReceived a message of type {type} from client {clientId}: {payload}");

                MessageType typeToSend = MessageType.UNKNOWN;
                string payloadToSend = "";
                
                switch (type)
                {
                    case MessageType.CONNECT:
                        typeToSend = MessageType.CONNECT;
                        payloadToSend = "Connected successfully!";
                        break;
                    case MessageType.DISCONNECT:
                        // Do some stuff with client data here 
                        clientSocket.Shutdown(SocketShutdown.Both);
                        clientSocket.Close();
                        return;
                }
                
                MessageManager.SendMessage(stream, typeToSend, payloadToSend);
                Console.WriteLine($"Sent a message of type {typeToSend} to client {clientId}: \"{payloadToSend}\"");
            }
        }
        catch (Exception ex)
        {
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
            Console.WriteLine($"Server error: {ex.Message}");
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
    
    public void Dispose()
    {
        _socket?.Shutdown(SocketShutdown.Both);
        _socket?.Close();
        _socket?.Dispose();
    }

}