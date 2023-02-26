using DataLayer.Models;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcOrderService;
using GrpcStockService;
using MongoDB.Driver;

namespace GrpcOrderService.Services;

public class OrderService : Orders.OrdersBase
{
	private readonly ILogger<OrderService> _logger;
    private readonly IMongoCollection<Order> _orders;

    public OrderService(ILogger<OrderService> logger,IMongoClient client)
    {
        _logger = logger;
        var database = client.GetDatabase("AlgoMindsDatabase");
        _orders = database.GetCollection<Order>("Orders");
    }
    public override async Task<OrderModel> PlaceOrder(OrderModel request, ServerCallContext context)
    {
        //var channel = GrpcChannel.ForAddress("https://localhost:7053");
        //var client =  new Greeter.GreeterClient(channel);
        //var reply = await client.SayHelloAsync(
        //          new HelloRequest { Name = "GreeterClient" });

        var order = new DataLayer.Models.Order();
        order.Products = new List<Products>();
        foreach(var p in request.Products)
        {
            order.Products.Add(new Products()
            {
                Description = p.Description,
            });
        }

        await _orders.InsertOneAsync(order);

        return new OrderModel
        {
            Id = order.Id
        };
    }
}
