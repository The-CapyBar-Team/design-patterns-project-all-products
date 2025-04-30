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
        private ProductsCacheService productsCacheService;
        public StockService(RabbitMQService rabbitMQService, ProductsDBContext dbContext, ProductsCacheService productsCacheService) 
        {
            this.rabbitMQService = rabbitMQService;
            this.dbContext = dbContext;
            this.productsCacheService = productsCacheService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            rabbitMQService.DeclareQueue("DecreaseStockRequests").GetAwaiter().GetResult();
            rabbitMQService.DeclareQueue("AddProductDBRequests").GetAwaiter().GetResult();
            rabbitMQService.DeclareQueue("UpdateProductDBRequests").GetAwaiter().GetResult();
            rabbitMQService.DeclareQueue("ProductStockLists").GetAwaiter().GetResult();

            rabbitMQService.SubscribeToQueue("AddProductDBRequests", async message =>
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
                productsCacheService.InvalidateCache();

                var productStockChange = new ProductStockInfo()
                {
                    ProductId = protoProduct.Id,
                    StockChange = (uint)product.Stock,
                };

                var productStockList = new ProductStockList();
                productStockList.Products.Add(productStockChange);
                await rabbitMQService.SendMessage("ProductStockLists", productStockList.ToByteArray());
            });

            rabbitMQService.SubscribeToQueue("UpdateProductDBRequests", async message =>
            {
                var protoProduct = ProductProto.Parser.ParseFrom(message);
                var product = dbContext.Products.Find(protoProduct.Id);
                //var product = dbContext.Products.FirstOrDefault(p => p.Id == protoProduct.Id);

                if (product != null)
                {
                    product.Name = protoProduct.Name;
                    product.Description = protoProduct.Description;
                    product.Price = protoProduct.Price;

                    var stockChange = protoProduct.Stock - (uint)product.Stock;

                    product.Stock = (int)protoProduct.Stock;
                    dbContext.SaveChanges();
                    productsCacheService.InvalidateCache();

                    var productStockChange = new ProductStockInfo()
                    {
                        ProductId = protoProduct.Id,
                        StockChange = stockChange,
                    };
                    var productStockList = new ProductStockList();
                    productStockList.Products.Add(productStockChange);
                    await rabbitMQService.SendMessage("ProductStockLists", productStockList.ToByteArray());
                }
            });

            rabbitMQService.SubscribeToQueue("DecreaseStockRequests", message =>
            {
                var decreaseStockRequest = DecreaseStockRequest.Parser.ParseFrom(message);
                var product = dbContext.Products.Find(decreaseStockRequest.ProductId);
                //var product = dbContext.Products.FirstOrDefault(product => product.Id == decreaseStockRequest.ProductId);
                
                if (product != null)
                {
                    product.Stock--;
                    var newReceipt = new Receipt { ProductId = decreaseStockRequest.ProductId, UserId = decreaseStockRequest.UserId };
                    dbContext.Receipts.Add(newReceipt);
                    dbContext.SaveChanges();
                    productsCacheService.InvalidateCache();
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
