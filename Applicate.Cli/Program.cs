using Applicate.Cli;
using Applicate.Domain.Data;
using Applicate.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Applicate.Domain.Entities;

// 1. Setup Host
var builder = Host.CreateApplicationBuilder(args);

// 2. Registrer Services
// ResourceService skal være Singleton, så den holder på dataen i hukommelsen
builder.Services.AddSingleton<ResourceService>();
builder.Services.AddSingleton<CliApp>();
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddScoped<ActionService>();
builder.Services.AddScoped<QueryExecutor>();
builder.Services.AddScoped<DataService>();

using IHost host = builder.Build();

// 3. Kør Appen
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Sikrer at databasen er oprettet (hvis migration fejlede tidligere)
    db.Database.EnsureCreated();

    // 1. AFSLØR STIEN
    var dbPath = Path.Combine(AppContext.BaseDirectory, "applicate.db");
    Console.WriteLine($"[DEBUG] Databasen ligger her: {dbPath}");

    // 2. TJEK DATA ANTAL
    var count = db.Bookings.Count();
    Console.WriteLine($"[DEBUG] Antal bookinger i basen før seed: {count}");

    // Tjek om der er bookinger
    if (!db.Bookings.Any())
    {
        Console.WriteLine("Seeding database with dummy bookings...");

        // 1. Opret Kunder
        var customer1 = new CustomerEntity
        {
            Id = Guid.NewGuid(),
            Name = "Anders Andersen",
            Email = "anders@example.com"
        };

        var customer2 = new CustomerEntity
        {
            Id = Guid.NewGuid(),
            Name = "Bente Bentzon",
            Email = "bente@example.com"
        };

        var house1 = new HouseEntity 
        {
            Id= Guid.NewGuid(),
            Name = "Almosen",
            Address = "almosevej 12"
        };

        var house2 = new HouseEntity
        {
            Id = Guid.NewGuid(),
            Name = "Villa",
            Address = "Strandvej 4"
        };

        // Opret Brugere
        var adminUser = new Applicate.Domain.Entities.UserEntity
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Password = "123", // Super hemmeligt
            Roles = "Admin"
        };

        var customerUser = new Applicate.Domain.Entities.UserEntity
        {
            Id = Guid.NewGuid(),
            Username = "kunde",
            Password = "123",
            Roles = "Customer"
        };

        db.Users.AddRange(adminUser, customerUser);
        db.Customers.AddRange(customer1, customer2);
        db.Houses.AddRange(house1, house2);


        db.Bookings.AddRange(
            new BookingEntity
            {
                Id = Guid.NewGuid(),
                CustomerId = customer1.Id,
                HouseId = house1.Id,
                StartDate = DateTime.Now.AddDays(10),
                EndDate = DateTime.Now.AddDays(17),
                TotalPrice = 4500.00m,
                Status = "Confirmed"
            },
            new BookingEntity
            {
                Id = Guid.NewGuid(),
                CustomerId = customer2.Id,
                HouseId = house2.Id,
                StartDate = DateTime.Now.AddDays(30),
                EndDate = DateTime.Now.AddDays(32),
                TotalPrice = 1200.50m,
                Status = "Pending"
            }
        );

        db.SaveChanges(); // Send SQL til databasen
        Console.WriteLine("Seeding færdig!");
    }
    else
    {
        Console.WriteLine("[DEBUG] Databasen var ikke tom, så vi hoppede over seed.");
    }

    var app = scope.ServiceProvider.GetRequiredService<CliApp>();

    try
    {
        await app.RunAsync(args);
    }
    catch (Exception ex)
    {
        // Global fejlhåndtering
        Console.WriteLine($"CRITICAL ERROR: {ex.Message}");
    }
}