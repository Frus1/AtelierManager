using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtelierManager.Models
{
    public class Material
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Quantity { get; set; }
        public string Unit { get; set; } = "шт";
        public double Price { get; set; }
    }
}
