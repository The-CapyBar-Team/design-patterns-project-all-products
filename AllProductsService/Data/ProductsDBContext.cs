using AllProductsService.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllProductsService.Data
{
    public class ProductsDBContext : DbContext
    {
        private DbSet<Product> Products { get; set; }

        public ProductsDBContext(DbContextOptions<ProductsDBContext> options) : base(options) { }


    }
}
