using Server.InvertedIndexStructure;

namespace Server;

class Program
{
    static void Main(string[] args)
    {
        // launch tcp server
        TcpServer server = new TcpServer();
        server.StartTcp(5000);
        
        // launch inverted index
        InvertedIndex invertedIndex = new InvertedIndex(4, 1000, 10);
    }
}