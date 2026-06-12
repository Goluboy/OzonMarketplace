namespace Redis;

public class RedisOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string? InstancePrefix { get; set; }
    public int ConnectionPoolSize { get; set; } = 1;
    public int AsyncTimeout { get; set; } = 5000;
    public int SyncTimeout { get; set; } = 5000;
    public int KeepAlive { get; set; } = 60;
    public bool AbortOnConnectFail { get; set; } = false;
}