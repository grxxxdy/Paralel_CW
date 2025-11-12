namespace Server;

class Program
{
    static void Main(string[] args)
    {
        TcpServer server = new TcpServer();
        server.StartTcp(5000);
    }
}