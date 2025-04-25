using AllProductsService.Data;
using AllProductsService.Models.Entities;
using AllProductsService.Protos;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllProductsService.Services
{
    internal class StockService : IHostedService
    {
        private RabbitMQService rabbitMQService;
        private ProductsDBContext dbContext;
        public StockService(RabbitMQService rabbitMQService, ProductsDBContext dbContext) 
        {
            this.rabbitMQService = rabbitMQService;
            this.dbContext = dbContext;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            rabbitMQService.DeclareQueue("DecreaseStockRequests").GetAwaiter().GetResult();
            rabbitMQService.DeclareQueue("AddProductDBRequests").GetAwaiter().GetResult();
            rabbitMQService.DeclareQueue("UpdateProductDBRequests").GetAwaiter().GetResult();

            rabbitMQService.SubscribeToQueue("AddProductDBRequests", message =>
            {
                var protoProduct = ProductProto.Parser.ParseFrom(message);
                Product product = new Product()
                {
                    Name = protoProduct.Name,
                    Description = protoProduct.Description,
                    Price = protoProduct.Price,
                    Stock = (int)protoProduct.Stock,
                };
                dbContext.Products.Add(product);
                dbContext.SaveChanges();
            });

            rabbitMQService.SubscribeToQueue("UpdateProductDBRequests", async message =>
            {
                var protoProduct = ProductProto.Parser.ParseFrom(message);

                var product = dbContext.Products.FirstOrDefault(p => p.Id == protoProduct.Id);

                if (product != null)
                {
                    product.Name = protoProduct.Name;
                    product.Description = protoProduct.Description;
                    product.Price = protoProduct.Price;

                    var stockChange = protoProduct.Stock - (uint)product.Stock;

                    product.Stock = (int)protoProduct.Stock;
                    dbContext.SaveChanges();

                    var productStockChange = new ProductStockInfo()
                    {
                        ProductId = protoProduct.Id,
                        StockChange = stockChange,
                    };
                    await rabbitMQService.SendMessage("ProductStockInfos", productStockChange.ToByteArray());
                }
            });

            rabbitMQService.SubscribeToQueue("DecreaseStockRequests", message =>
            {
                var decreaseStockRequest = DecreaseStockRequest.Parser.ParseFrom(message);
                var product = dbContext.Products.FirstOrDefault(product => product.Id == decreaseStockRequest.ProductId);
                if (product != null)
                {
                    product.Stock--;
                    dbContext.SaveChanges();
                }
            });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
