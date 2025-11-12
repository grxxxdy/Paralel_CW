using System.Collections.Concurrent;

namespace Server.InvertedIndex;

public class InvertedIndex : IDisposable
{
    private ConcurrentDictionary<string, HashSet<string>> _index;
    private string _dataDirectory;
    private ThreadPool _threadPool;
    private object _taskLock = new object();
    private int _pendingTasks;
    
    public InvertedIndex(int threadCount = 4, int queueCapacity = 1000)
    {
        _index = new ConcurrentDictionary<string, HashSet<string>>();
        _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        _threadPool = new ThreadPool(threadCount, queueCapacity);
    }

    public void BuildIndex()
    {
        var files = Directory.GetFiles(_dataDirectory, "*.txt", SearchOption.AllDirectories);
        Console.WriteLine($"[InvertedIndex] Found {files.Length} files.");
        
        _pendingTasks = files.Length;
        
        foreach (var file in files)
        {
            var path = file;
            _threadPool.EnqueueTask(() => ProcessFile(path));
        }
        
        lock (_taskLock)
        {
            while (_pendingTasks > 0)
                Monitor.Wait(_taskLock);
        }
        
        Console.WriteLine("[InvertedIndex] Index built successfully.");
    }

    private void ProcessFile(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            var words = content
                .ToLower()
                .Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '-', '"', '(', ')', '[', ']', '{', '}' },
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                _index.AddOrUpdate(
                    word,
                    _ => new HashSet<string> { filePath },
                    (_, set) =>
                    {
                        lock (set) set.Add(filePath);
                        return set;
                    });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InvertedIndex] Failed to process {filePath}: {ex.Message}");
        }
        finally
        {
            lock (_taskLock)
            {
                _pendingTasks--;
                if (_pendingTasks == 0)
                    Monitor.PulseAll(_taskLock);
            }
        }
    }
    
    public List<string> Search(string word)
    {
        word = word.ToLower();
        if (_index.TryGetValue(word, out var files))
            return files.ToList();
        return new List<string>();
    }

    public void Dispose()
    {
        _threadPool.Dispose();
    }
}