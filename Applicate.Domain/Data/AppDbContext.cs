using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Applicate.Domain.Entities;

namespace Applicate.Domain.Data;

public class AppDbContext : DbContext
{
    public DbSet<BookingEntity> Bookings { get; set; }
    public DbSet<CustomerEntity> Customers { get; set; }
    public DbSet<HouseEntity> Houses { get; set; }
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<LogEntity> Logs { get; set; }


    // Konfiguration af stien til database-filen
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Vi gemmer filen 'applicate.db' lokalt
        optionsBuilder.UseSqlite(@"Data Source=C:\Users\MMS\Code\ApplicateProject\applicate.db");
    }
}
