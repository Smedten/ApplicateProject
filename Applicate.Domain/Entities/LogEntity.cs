namespace Applicate.Domain.Entities;

public class LogEntity
{
    public Guid Id { get; set; }
    public required string ResourceName { get; set; } // F.eks. "Booking"
    public required string ActionName { get; set; }   // F.eks. "SignContract"
    public required string Username { get; set; }     // F.eks. "kunde"
    public required string Details { get; set; }      // F.eks. "Executed on ID: ..."
    public DateTime Timestamp { get; set; }
}