using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtelierManager.Models
{
    public class Person
    {
        public int Id { get; set; }
        public int ClientId { get; set; }

        public string DisplayName { get; set; } = "Персона";
        public string? Gender { get; set; }

        public Client? Client { get; set; }
        public ICollection<MeasurementSet> MeasurementSets { get; set; } = new List<MeasurementSet>();
    }

}
