using System.Collections.Concurrent;

namespace Server.InvertedIndexStructure;

public class InvertedIndex : IDisposable
{
    private ConcurrentDictionary<string, HashSet<string>> _index;
    private string _dataDirectory;
    private ThreadPool _threadPool;
    private object _taskLock = new object();
    private int _pendingTasks;
    private HashSet<string> _indexedFiles = new HashSet<string>();
    private bool _isSchedulerRunning = false;
    private Thread? _schedulerThread;

    public InvertedIndex(int threadCount, int queueCapacity, int updatesMonitorInverval)
    {
        _index = new ConcurrentDictionary<string, HashSet<string>>();
        _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        _threadPool = new ThreadPool(threadCount, queueCapacity);
        
        if(updatesMonitorInverval > 0)
            StartScheduler(updatesMonitorInverval);
    }

    public void BuildIndex()
    {
        var files = Directory.GetFiles(_dataDirectory, "*.txt", SearchOption.AllDirectories);
        Console.WriteLine($"[InvertedIndex] Found {files.Length} files.");

        _pendingTasks = files.Length;

        foreach (var file in files)
        {
            var path = file;

            lock (_indexedFiles)
                _indexedFiles.Add(path);

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

    private void StartScheduler(int intervalSeconds)
    {
        if (_isSchedulerRunning) return;

        _isSchedulerRunning = true;

        _schedulerThread = new Thread(() =>
        {
            while (_isSchedulerRunning)
            {
                try
                {
                    var files = Directory.GetFiles(_dataDirectory, "*.txt", SearchOption.AllDirectories);

                    foreach (var file in files)
                    {
                        lock (_indexedFiles)
                        {
                            if (_indexedFiles.Contains(file))
                                continue;
                            _indexedFiles.Add(file);
                        }

                        Console.WriteLine($"[Scheduler] New file detected: {file}");

                        lock (_taskLock)
                        {
                            _pendingTasks++;
                        }

                        var path = file;
                        _threadPool.EnqueueTask(() => ProcessFile(path));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Scheduler] Error while scanning: {ex.Message}");
                }

                Thread.Sleep(intervalSeconds * 1000);
            }
        });

        _schedulerThread.IsBackground = true;
        _schedulerThread.Start();

        Console.WriteLine("[Scheduler] Started periodic file scanning.");
    }
    
    public void StopScheduler()
    {
        _isSchedulerRunning = false;
        _schedulerThread?.Join();
        Console.WriteLine("[Scheduler] Stopped.");
    }

    public void Dispose()
    {
        _threadPool.Dispose();
    }
}