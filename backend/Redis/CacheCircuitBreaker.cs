namespace Redis;

internal enum BreakerState { Closed, Open, HalfOpen }

internal class CacheCircuitBreaker
{
    private readonly TimeSpan _resetTimeout = TimeSpan.FromSeconds(10);
    private const int FailureThreshold = 3;

    private int _failureCount;
    private DateTime _lastStateChange = DateTime.MinValue;
    private BreakerState _state = BreakerState.Closed;
    private readonly Lock _lock = new();

    public bool IsAllowed()
    {
        lock (_lock)
        {
            if (_state == BreakerState.Closed)
            {
                return true;
            }

            if (_state == BreakerState.Open)
            {
                if (DateTime.UtcNow - _lastStateChange > _resetTimeout)
                {
                    _state = BreakerState.HalfOpen;
                    _lastStateChange = DateTime.UtcNow;
                    return true;
                }
                return false;
            }

            return true;
        }
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            if (_state != BreakerState.Closed)
            {
                _state = BreakerState.Closed;
                _lastStateChange = DateTime.UtcNow;
            }
        }
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            if (_failureCount >= FailureThreshold && _state != BreakerState.Open)
            {
                _state = BreakerState.Open;
                _lastStateChange = DateTime.UtcNow;
            }
        }
    }
}