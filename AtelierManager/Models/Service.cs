using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtelierManager.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string ServiceType { get; set; } = "";
        public double BasePrice { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }

        public ICollection<ExtraElement> ExtraElements { get; set; } = new List<ExtraElement>();
    }

}
