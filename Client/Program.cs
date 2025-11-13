namespace Client;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Select mode:");
        Console.WriteLine("1 - Normal client mode");
        Console.WriteLine("2 - Load the server");

        var mode = Console.ReadLine();

        while (mode != "1" && mode != "2")
        {
            Console.WriteLine("Please select the correct option.");
            mode = Console.ReadLine();
        }

        switch (mode)
        {
            case "1":
                ClientMode();
                break;
            case "2":
                LoadServer();
                break;
        }
        
        
    }

    static void ClientMode()
    {
        // Connect the client
        TcpClient client = new TcpClient();
        Console.WriteLine("\nTrying to connect to the server.");
        client.Connect("192.168.0.175", 5000);

        while (true)
        {
            Console.WriteLine("1 - Search for a word");
            Console.WriteLine("2 - Exit");
            
            var mode = Console.ReadLine();

            while (mode != "1" && mode != "2")
            {
                Console.WriteLine("Please select the correct option.");
                mode = Console.ReadLine();
            }

            switch (mode)
            {
                case "1":
                    // Make a search
                    Console.WriteLine("Write a word to search for:");
                    string? searchWord = Console.ReadLine();

                    while (searchWord == null)
                    {
                        Console.WriteLine("Please enter a valid word.");
                        searchWord = Console.ReadLine();
                    }
        
                    client.SearchFiles(searchWord);
                    break;
                case "2":
                    client.Disconnect();
                    client.Dispose();
                    return;
            }
        }
    }
    
    static void LoadServer()    // For testing server queue
    {
        Console.WriteLine("=== Load server mode ===");
        Console.Write("Enter number of clients to create: ");
        
        int clientCount;
        while (!int.TryParse(Console.ReadLine(), out clientCount) || clientCount <= 0)
        {
            Console.WriteLine("Please enter a positive integer:");
        }

        var clients = new List<TcpClient>();

        Console.WriteLine($"\nCreating {clientCount} clients and connecting them to the server...");

        for (int i = 0; i < clientCount; i++)
        {
            try
            {
                var client = new TcpClient();
                client.Connect("192.168.0.175", 5000);
                clients.Add(client);
                Console.WriteLine($"[Load] Client #{i + 1} connected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Load] Failed to connect client #{i + 1}: {ex.Message}");
            }
        }
        
        Console.WriteLine("\nAll load-clients created. Press any button to disconnect them.");
        Console.ReadKey();
        
        Console.WriteLine("[Load] Disconnecting load clients...");
        
        foreach (var client in clients)
        {
            client.Disconnect();
            client.Dispose();
        }

        Console.WriteLine("[Load] All load clients disconnected. Press any key to exit.");
        Console.ReadKey();
    }
}