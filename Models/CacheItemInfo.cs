namespace FipsFrontend.Models;

public class CacheItemInfo
{
    public string Endpoint { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastAccessed { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public TimeSpan Duration { get; set; }
    public int HitCount { get; set; }
    public string Size { get; set; } = string.Empty;
    
    public bool IsExpired => ExpiresAt <= DateTimeOffset.UtcNow;
    
    public TimeSpan TimeUntilExpiry => ExpiresAt - DateTimeOffset.UtcNow;
    
    public string Status => IsExpired ? "Expired" : "Active";
}
