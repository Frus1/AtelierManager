using System.Collections.Generic;

namespace AtelierManager.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Login { get; set; } = "";
        public string Password { get; set; } = "";
        public int RoleId { get; set; }
        public bool IsActive { get; set; } = true;

        public Role? Role { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
