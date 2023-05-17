using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.Store.Shared
{
    public class OrderDetail
    {
        public int OrderId { get; set; }
        public int ProductoId { get; set; }
        public int Quantity { get; set; }

        public DateTime OrderDate { get; set; }
        public DateTime DeliveryDate { get; set; }
       

    }
}
