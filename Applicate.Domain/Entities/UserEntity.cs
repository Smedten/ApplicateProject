namespace Applicate.Domain.Entities;

public class UserEntity
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; } // I prod skal dette være Hashed!
    public required string Roles { get; set; }
}