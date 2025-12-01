using DAL.Models;

public class Feedback
{
    public int Id { get; set; }
    public int CustomerAccountId { get; set; }
    public int OrderId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Account Customer { get; set; }
    public virtual Order Order { get; set; }
}
