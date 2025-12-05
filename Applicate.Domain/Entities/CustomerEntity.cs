namespace Applicate.Domain.Entities;

public class CustomerEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
}