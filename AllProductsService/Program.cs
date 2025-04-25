using AllProductsService.Data;
using AllProductsService.Services;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
namespace AllProductsService
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Env.Load();

            using IHost host = CreateHostBuilder(args).Build();

            //host.Services.GetRequiredService<GetAllProductsService>();

            // Migration
            await ApplyMigrationsAsync(host.Services);

            // Main Logic
            //await RunApplicationAsync(host.Services);

            await host.RunAsync();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    // Register DbContext
                    
                    var connectionString = Env.GetString("CONNECTION_STRING");
                    Console.WriteLine(connectionString);
                    services.AddDbContext<ProductsDBContext>(options =>
                        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

                    // Register RabbitMQ service
                    services.AddSingleton<RabbitMQService>(_ =>
                    {
                        var rabbitMQHost = Env.GetString("RABBITMQ_HOST");
                        var rabbitMQPort = Env.GetString("RABBITMQ_PORT");
                        var rabbitMQUser = Env.GetString("RABBITMQ_USER");
                        var rabbitMQPassword = Env.GetString("RABBITMQ_PASSWORD");

                        return new RabbitMQService(rabbitMQHost, rabbitMQPort, rabbitMQUser, rabbitMQPassword);
                    });

                    services.AddHostedService<GetAllProductsService>();

                    services.AddHostedService<StockService>();

                    //Register other services
                    // services.AddSingleton<IYourService, YourService>();
                });

        static async Task ApplyMigrationsAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var dbContext = services.GetRequiredService<ProductsDBContext>();
                await dbContext.Database.MigrateAsync();
                Console.WriteLine("Migration success");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration error: {ex.Message}");
                throw;
            }
        }

        //static async Task RunApplicationAsync(IServiceProvider serviceProvider)
        //{
        //    using var scope = serviceProvider.CreateScope();
        //    var services = scope.ServiceProvider;

        //    try
        //    {
        //        var rabbitMQService = services.GetRequiredService<RabbitMQService>();

        //        // Subscribe to RabbitMQ
        //        rabbitMQService.SubscribeToQueue("your_queue_name", message =>
        //        {
        //            // get message
        //            Console.WriteLine($"Got message: {message}");

        //            // send message
        //            //rabbitMQService.SendMessage("response_queue", $"Answer for: {message}");
        //        });

        //        Console.WriteLine("Service is working, press any key to close");
        //        Console.ReadKey();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error: {ex.Message}");
        //        throw;
        //    }
        //}
    }
}

