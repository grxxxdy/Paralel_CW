namespace Client;

class Program
{
    static void Main(string[] args)
    {
        // Connect the client
        TcpClient client = new TcpClient();
        Console.WriteLine("\nTrying to connect to the server.");
        client.Connect("192.168.0.175", 5000);
        
        client.Disconnect();

        Console.ReadKey();
    }
}