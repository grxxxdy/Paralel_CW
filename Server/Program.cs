using Server.InvertedIndexStructure;

namespace Server;

class Program
{
    static void Main(string[] args)
    {
        // Create a thread pool
        int threadCount = 6, queueCapacity = 1000;
        ThreadPool threadPool = new ThreadPool(threadCount, queueCapacity);
        
        // launch inverted index
        InvertedIndex invertedIndex = new InvertedIndex(threadPool, 100);
        invertedIndex.BuildIndex();
        //invertedIndex.StartScheduler(10);
        
        // launch tcp server
        TcpServer server = new TcpServer(invertedIndex, threadPool);
        server.StartTcp(5000);
    }
}