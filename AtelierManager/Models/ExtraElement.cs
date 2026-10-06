using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtelierManager.Models
{
    public class ExtraElement
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public string Name { get; set; } = "";
        public double DefaultPrice { get; set; }
        public bool IsActive { get; set; }

        public Service? Service { get; set; }
    }

}
