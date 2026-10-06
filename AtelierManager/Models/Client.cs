using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtelierManager.Models
{
    public class Client
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<Person> Persons { get; set; } = new List<Person>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
