namespace FPSRoguelike.Core;

/// <summary>
/// Simple logging interface for game messages
/// </summary>
public interface ILogger
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogDebug(string message);
}

/// <summary>
/// Console-based logger implementation
/// </summary>
public class ConsoleLogger : ILogger
{
    private bool debugEnabled;
    
    public ConsoleLogger(bool enableDebug = false)
    {
        debugEnabled = enableDebug;
    }
    
    public void LogInfo(string message)
    {
        Console.WriteLine($"[INFO] {message}");
    }
    
    public void LogWarning(string message)
    {
        Console.WriteLine($"[WARN] {message}");
    }
    
    public void LogError(string message)
    {
        Console.WriteLine($"[ERROR] {message}");
    }
    
    public void LogDebug(string message)
    {
        if (debugEnabled)
            Console.WriteLine($"[DEBUG] {message}");
    }
}

/// <summary>
/// Null logger that discards all messages (for production)
/// </summary>
public class NullLogger : ILogger
{
    public void LogInfo(string message) { }
    public void LogWarning(string message) { }
    public void LogError(string message) { }
    public void LogDebug(string message) { }
}