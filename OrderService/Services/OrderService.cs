using DataLayer.Models;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcStockService;
using MongoDB.Driver;
using System.Diagnostics.Metrics;

namespace GrpcOrderService.Services;

public class OrderService : Orders.OrdersBase
{
	private readonly ILogger<OrderService> _logger;
    private readonly IMongoCollection<Order> _orders;
    private readonly IMongoCollection<Sequence> _counters;
    private readonly IMongoDatabase _database;

    public OrderService(ILogger<OrderService> logger,IMongoClient client)
    {
        _logger = logger;
        _database = client.GetDatabase("AlgoMindsDatabase");
        _orders = _database.GetCollection<Order>("Orders");
        _counters = _database.GetCollection<Sequence>("counters");
        var hasSequence = _counters.Find(x => x.Name.Equals("orders")).FirstOrDefault();
        if (hasSequence is null)
        {
            var counter = new Sequence
            {
                Name = "orders",
                Value = 1
            };

            _counters.InsertOne(counter);
        }
    }
    public override async Task<Response> PlaceOrder(OrderModel request, ServerCallContext context)
    {
        using var channel = GrpcChannel.ForAddress("https://localhost:7053");
        var client = new Stocks.StocksClient(channel);
        var toVerify = new ResponseProductCollection();
        foreach(var p in request.Products)
        {
            toVerify.AllProducts.Add(new ProductStock
            {
                Product = new ProductsModel { Id = p.Id, Description = p.Description},
                Quantity = p.Quantity
            });
        }

        var hasStock = await client.IsStockAvailableForTheOrderAsync(toVerify);
        if (hasStock is not null && hasStock.Result)
        {
            var order = new Order();
            order.Products = new List<Products>();
            foreach (var p in request.Products)
            {
                order.Products.Add(new Products()
                {
                    Id = p.Id,
                    Description = p.Description,
                });
            }
            var sequence = _counters.FindOneAndUpdate(
                                   Builders<Sequence>.Filter.Eq(x => x.Name, "orders"),
                                   Builders<Sequence>.Update.Inc(x => x.Value, 1));

            var nextId = sequence.Value;
            order.Id = nextId;
            await _orders.InsertOneAsync(order);
            foreach (var p in request.Products)
            {
                await client.DecreaseStockAsync(new ProductStock
                {
                    Quantity = p.Quantity,
                    Product = new ProductsModel()
                    {
                        Id = p.Id
                    }
                });
            }
            
            return new Response
            {
                Message ="Order executed with success!"
            };
        }
        else
        {
            return new Response
            {
                Message="There is not enough stock to fullfill the order"
            };
        }
    }
}
