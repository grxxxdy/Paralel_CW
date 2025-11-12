using Server.InvertedIndexStructure;

namespace Server;

class Program
{
    static void Main(string[] args)
    {
        // launch inverted index
        InvertedIndex invertedIndex = new InvertedIndex(6, 1000);
        invertedIndex.BuildIndex();
        invertedIndex.StartScheduler(10);
        
        // launch tcp server
        TcpServer server = new TcpServer(invertedIndex);
        server.StartTcp(5000);
    }
}