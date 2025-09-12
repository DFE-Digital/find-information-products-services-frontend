namespace FipsFrontend.Models;

public class CacheInfoViewModel
{
    public List<CacheItemInfo> CacheEntries { get; set; } = new();
    public int TotalEntries { get; set; }
    public int ActiveEntries { get; set; }
    public int ExpiredEntries { get; set; }
    public int TotalHits { get; set; }
}
