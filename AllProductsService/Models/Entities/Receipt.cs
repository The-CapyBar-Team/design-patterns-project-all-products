using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllProductsService.Models.Entities
{
    public class Receipt
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int ProductId { get; set; }
    }
}
