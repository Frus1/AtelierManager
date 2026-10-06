using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtelierManager.Models
{
    public class OrderService
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ServiceId { get; set; }

        public int PersonId { get; set; }
        public int? MeasurementSetId { get; set; }

        public double BasePrice { get; set; }
        public double LineTotal { get; set; }

        public Order? Order { get; set; }
        public Service? Service { get; set; }

        public Person? Person { get; set; }
        public MeasurementSet? MeasurementSet { get; set; }

        public ICollection<OrderServiceExtra> Extras { get; set; } = new List<OrderServiceExtra>();
    }

}
