using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Applicate.Domain.Entities;

public class BookingEntity
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid HouseId { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public required string Status { get; set; }

    public CustomerEntity? Customer { get; set; }
}
