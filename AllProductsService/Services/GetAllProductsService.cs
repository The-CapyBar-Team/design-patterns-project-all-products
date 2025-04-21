using AllProductsService.Data;
using AllProductsService.Protos;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllProductsService.Services
{
    internal class GetAllProductsService
    {
        private RabbitMQService rabbitMQService;
        private ProductsDBContext dbContext;
        public GetAllProductsService(RabbitMQService rabbitMQService, ProductsDBContext dbContext)
        { 
            this.rabbitMQService = rabbitMQService;
            this.dbContext = dbContext;
            SetupQueues();
        }

        private async Task SetupQueues()
        {
            await rabbitMQService.DeclareQueue("AllproductsRequests");
            await rabbitMQService.DeclareQueue("AllproductsResponses");

            rabbitMQService.SubscribeToQueue("AllproductsRequests", async response =>
            {
                AllProductsRequest request = AllProductsRequest.Parser.ParseFrom(response);
                ProductListProto productList = new ProductListProto();
                foreach (var product in dbContext.Products)
                {
                    productList.Products.Add(new ProductProto
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Description = product.Description,
                        Price = product.Price,
                        Stock = product.Stock
                    });
                }
                await rabbitMQService.SendMessage("AllproductsResponses", productList.ToByteArray());
            });
                
        }
    }
}
