using Applicate.Domain.Entities;

namespace Applicate.Domain;

public static class TypeRegistry
{
    // En simpel ordbog der mapper Resource Navn -> C# Type
    private static readonly Dictionary<string, Type> _types = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Booking", typeof(BookingEntity) },
        { "Customer", typeof(CustomerEntity) },
        { "House", typeof (HouseEntity) },
        { "User", typeof (UserEntity) },
        { "Log", typeof(LogEntity) }
    };

    public static Type? GetType(string resourceName)
    {
        _types.TryGetValue(resourceName, out var type);
        return type;
    }
}