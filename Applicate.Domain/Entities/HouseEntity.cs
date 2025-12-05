using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Applicate.Domain.Entities
{
    public class HouseEntity
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Address { get; set; }
    }
}
