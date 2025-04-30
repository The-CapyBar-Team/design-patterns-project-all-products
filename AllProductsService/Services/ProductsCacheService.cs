using AllProductsService.Data;
using AllProductsService.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllProductsService.Services
{
    internal class ProductsCacheService
    {
        private ProductsDBContext dbContext;
        private bool CacheIsValid { get; set; }
        private List<Product> cachedProducts = null;
        public List<Product> Products 
        {
            get
            {
                if(!CacheIsValid || cachedProducts == null)
                {
                    CacheIsValid = true;
                    cachedProducts = dbContext.Products.ToList();
                }
                return cachedProducts;
            }
        }

        public ProductsCacheService(ProductsDBContext dbContext) 
        {
            this.dbContext = dbContext;
        }

        public void InvalidateCache()
        {
            CacheIsValid = false;
        }
    }
}
