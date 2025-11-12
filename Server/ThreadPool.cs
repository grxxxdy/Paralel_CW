namespace Server;

public class ThreadPool : IDisposable
{
    private Queue<Action> _taskQueue;
    private List<Thread> _workers;

    private object _queueLock = new object();
    private Mutex _consoleMutex;

    private readonly int _queueCapacity;

    private bool _shouldInterrupt;
    
    // debug stuff-----------
    public int tasksEnqueued = 0;
    public int tasksExecuted = 0;
    //-----------------------
    
    public ThreadPool(int workerThreadCount, int queueCapacity)
    {
        _shouldInterrupt = false;
        _queueCapacity = queueCapacity;
        _taskQueue = new Queue<Action>(queueCapacity);
        _workers = new List<Thread>();
        _consoleMutex = new Mutex();
        
        //Create worker threads
        for (int i = 0; i < workerThreadCount; i++)
        {
            int threadId = i;
            var worker = new Thread(() => WorkerLoop())
            {
                Name = $"Worker-{threadId}"
            };
            
            _workers.Add(worker);
            worker.Start();
        }
    }

    public void EnqueueTask(Action task)
    {
        if (task == null) throw new ArgumentNullException();

        lock (_queueLock)       // lock queue
        {
            while (_taskQueue.Count >= _queueCapacity && !_shouldInterrupt)     // If full
            {
                Monitor.Wait(_queueLock);
            }
            
            if (_shouldInterrupt)
                return;
            
            _taskQueue.Enqueue(task);   // enqueue task
            tasksEnqueued++;
            
            Monitor.PulseAll(_queueLock);
        }
    }

    private void WorkerLoop()
    {
        while (!_shouldInterrupt)
        {
            Action taskToDo = null;
            
            lock (_queueLock)       // lock queue
            {
                while (_taskQueue.Count == 0 && !_shouldInterrupt)
                {
                    Monitor.Wait(_queueLock);
                }
                
                if (_shouldInterrupt)
                    return;
                
                taskToDo = _taskQueue.Dequeue();    // dequeue task
                Monitor.PulseAll(_queueLock);
            }
            
            taskToDo.Invoke();      // start task
            
            Interlocked.Increment(ref tasksExecuted);
            // SafeConsoleWrite($"[ThreadPool] Executed {tasksExecuted} tasks");
        }
            
    }
    
    private void SafeConsoleWrite(string message)
    {
        _consoleMutex.WaitOne();
        Console.WriteLine(message);
        _consoleMutex.ReleaseMutex();
    }
    
    public void Dispose()
    {
        _shouldInterrupt = true;

        lock (_queueLock)
        {
            Monitor.PulseAll(_queueLock);
        }

        foreach (var thread in _workers)
            thread.Join();
        
        _consoleMutex.Dispose();
    }
}