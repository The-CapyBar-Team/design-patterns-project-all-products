using AllProductsService.Data;
using AllProductsService.Models.Entities;
using AllProductsService.Protos;
using Google.Protobuf;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllProductsService.Services
{
    internal class GetAllProductsService : IHostedService
    {
        private RabbitMQService rabbitMQService;
        private ProductsDBContext dbContext;
        private ProductsCacheService productsCacheService;

        public bool CacheIsValid {  get; private set; }
        public GetAllProductsService(RabbitMQService rabbitMQService, ProductsDBContext dbContext, ProductsCacheService productsCacheService)
        { 
            this.rabbitMQService = rabbitMQService;
            this.dbContext = dbContext;
            this.productsCacheService = productsCacheService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            rabbitMQService.DeclareQueue("AllproductsRequests").GetAwaiter().GetResult();
            rabbitMQService.DeclareQueue("AllproductsResponses").GetAwaiter().GetResult();
            rabbitMQService.DeclareQueue("ProductStockLists").GetAwaiter().GetResult();

            var productStockList = new ProductStockList();
            productStockList.Products.AddRange(dbContext.Products.Select(product => 
                new ProductStockInfo 
                { 
                    ProductId = product.Id, 
                    StockChange = (uint)product.Stock
                }));

            rabbitMQService.SendMessage("ProductStockLists", productStockList.ToByteArray()).GetAwaiter().GetResult();

            rabbitMQService.SubscribeToQueue("AllproductsRequests", async response =>
            {
                AllProductsRequest request = AllProductsRequest.Parser.ParseFrom(response);
                var id = request.RequestId;
                var productList = new AllProductsResponse();
                var products = productsCacheService.Products;
                foreach (var product in products)
                {
                    productList.Products.Add(new ProductProto
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Description = product.Description,
                        Price = product.Price,
                        Stock = (uint)product.Stock
                    });
                }
                productList.RequestId = id;
                await rabbitMQService.SendMessage("AllproductsResponses", productList.ToByteArray());
            });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
