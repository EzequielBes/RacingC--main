public abstract class OptimizedMemoryReader : IMemoryReader
{
    private readonly ConcurrentQueue<TelemetryData> _dataQueue = new();
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private readonly Timer _flushTimer;
    
    protected OptimizedMemoryReader()
    {
        // Flush queue a cada 100ms para reduzir overhead
        _flushTimer = new Timer(FlushQueue, null, 100, 100);
    }

    protected async void OnDataReceived(TelemetryData data)
    {
        _dataQueue.Enqueue(data);
        
        // Limitar tamanho da queue para evitar memory leak
        if (_dataQueue.Count > 1000)
        {
            _dataQueue.TryDequeue(out _);
        }
    }

    private async void FlushQueue(object state)
    {
        if (!await _processingLock.WaitAsync(50)) return;
        
        try
        {
            var batch = new List<TelemetryData>();
            while (_dataQueue.TryDequeue(out var data) && batch.Count < 50)
            {
                batch.Add(data);
            }
            
            if (batch.Any())
            {
                await ProcessBatchAsync(batch);
            }
        }
        finally
        {
            _processingLock.Release();
        }
    }

    protected abstract Task ProcessBatchAsync(List<TelemetryData> batch);
}