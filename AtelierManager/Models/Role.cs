using System.Collections.Generic;

namespace AtelierManager.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
