namespace WebApp.Domain.Entites;

public class DedupRecord
{
    public string Key { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
