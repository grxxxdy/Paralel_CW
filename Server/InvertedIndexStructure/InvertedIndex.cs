using System.Collections.Concurrent;
using System.Diagnostics;

namespace Server.InvertedIndexStructure;

public class InvertedIndex : IDisposable
{
    private ConcurrentDictionary<string, HashSet<string>> _index;       // inverted index 
    private ConcurrentDictionary<string, DateTime> _fileProcessedTimes = new();     // when were files last changed
    private string _dataDirectory;
    private ThreadPool _threadPool;
    private object _taskLock = new object();
    private int _pendingTasks;
    private HashSet<string> _indexedFiles = new HashSet<string>();
    private bool _isSchedulerRunning = false;
    private Thread? _schedulerThread;
    
    // quick lauch option for debug
    // -1 - no limits, anuthing >0 — sets the amount of files to index
    private readonly int _maxFilesToIndex;

    public InvertedIndex(ThreadPool threadPool, int maxFilesToIndex = -1)
    {
        _index = new ConcurrentDictionary<string, HashSet<string>>();
        _dataDirectory = "../../../InvertedIndexStructure/Data";
        _threadPool = threadPool;
        _maxFilesToIndex = maxFilesToIndex;
    }

    public void BuildIndex()
    {
        var stopwatch = Stopwatch.StartNew();
        
        var files = Directory.GetFiles(_dataDirectory, "*.txt", SearchOption.AllDirectories);
        Console.WriteLine($"[InvertedIndex] Found {files.Length} files.");
        
        if (_maxFilesToIndex > 0 && files.Length > _maxFilesToIndex)
        {
            Console.WriteLine($"[InvertedIndex] Found {files.Length} files. Going to index {_maxFilesToIndex} of them.");
            files = files.Take(_maxFilesToIndex).ToArray();
        }

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
        Console.WriteLine($"[InvertedIndex] Total files processed: {files.Length}");
        Console.WriteLine($"[InvertedIndex] Tasks enqueued: {_threadPool.tasksEnqueued}");
        Console.WriteLine($"[InvertedIndex] Tasks executed: {_threadPool.tasksExecuted}");
        Console.WriteLine($"[InvertedIndex] Build time: {stopwatch.ElapsedMilliseconds / 1000.0} s");
    }

    private void ProcessFile(string filePath)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            
            var content = File.ReadAllText(filePath);
            var words = content
                .ToLower()
                .Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '-', '"', '(', ')', '[', ']', '{', '}' },
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                _index.AddOrUpdate(
                    word,
                    _ => new HashSet<string> { fileName },
                    (_, set) =>
                    {
                        lock (set) set.Add(fileName);
                        return set;
                    });
            }
            
            // Save last time the file was processed
            _fileProcessedTimes[fileName] = File.GetLastWriteTimeUtc(filePath);
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

    public void StartScheduler(int intervalSeconds)
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
                        var fileName = Path.GetFileName(file);
                        var currentWrite = File.GetLastWriteTimeUtc(file);      // time when file was last edited
                        
                        // Check if the file is new
                        bool isNew;
                        lock (_indexedFiles)
                        {
                            isNew = !_indexedFiles.Contains(file);
                            if (isNew) _indexedFiles.Add(file);
                        }
                        
                        // if new, add it to index
                        if (isNew)
                        {
                            Console.WriteLine($"[Scheduler] New file detected: {file}");
                            lock (_taskLock) { _pendingTasks++; }
                            var pathNew = file;
                            _threadPool.EnqueueTask(() => ProcessFile(pathNew));
                            continue;
                        }

                        // if edited                                     
                        var lastProcessed = _fileProcessedTimes.GetOrAdd(fileName, currentWrite);
                        
                        if (currentWrite > lastProcessed)
                        {
                            Console.WriteLine($"[Scheduler] Changed file detected: {file}");   
                            _fileProcessedTimes[fileName] = currentWrite;
                            
                            lock (_taskLock) { _pendingTasks++; }                                   
                            var pathChanged = file;                                                 
                            _threadPool.EnqueueTask(() => ProcessFile(pathChanged));                
                        }
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
        StopScheduler();
        _threadPool.Dispose();
    }
}