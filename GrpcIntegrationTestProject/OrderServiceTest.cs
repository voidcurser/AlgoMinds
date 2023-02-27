using AlgoShop;
using DataLayer.Models;
using Grpc.Net.Client;
using GrpcOrderService.Services;
using MongoDB.Driver;
using System.Xml.Linq;

namespace GrpcIntegrationTestProject
{
    [TestFixture]
    public class OrderServiceTest
    {
        private MongoClient _client;
        private IMongoDatabase _database;
        private Stocks.StocksClient _stocksClient;
        private Orders.OrdersClient _ordersClient;

        [SetUp]
        public void Setup()
        {
            _client = new MongoClient("mongodb://localhost:27017");
            _database = _client.GetDatabase("AlgoMindsDatabase");

            var channel = GrpcChannel.ForAddress("https://localhost:7053");
            _stocksClient = new Stocks.StocksClient(channel);
            var channelOrder = GrpcChannel.ForAddress("https://localhost:7174");
            _ordersClient = new Orders.OrdersClient(channelOrder);
            _stocksClient.AddProducts(
                new ProductStock
                {
                    Quantity = 100,
                    Product = new ProductsModel()
                    {
                        Description = "Banana"
                    }
                });
            _stocksClient.AddProducts(
                new ProductStock
                {
                    Quantity = 100,
                    Product = new ProductsModel()
                    {
                        Description = "Potato"
                    }
                });

        }

        [TearDown]
        public void Cleanup()
        {
            _client.DropDatabase("AlgoMindsDatabase");
        }

        [Test]
        public async Task PlaceOrder_WithEnoughStock_ShouldReturnSuccess()
        {
            var orderModel = new OrderModel();
            orderModel.Products.Add(new ProductsOrderModel
            {
                Id = 1,
                Description = "Banana",
                Quantity = 1
            });
            orderModel.Products.Add(new ProductsOrderModel
            {
                Id = 2,
                Description = "Potato",
                Quantity = 1
            });

            var response = await _ordersClient.PlaceOrderAsync(orderModel);

            Assert.AreEqual("Order executed with success!", response.Message);


            //Verify that a new order was inserted into the database.
            var orders = await _database.GetCollection<Order>("Orders").FindAsync(x => true);
            var orderList = await orders.ToListAsync();
            Assert.AreEqual(1, orderList.Count);
            var order = orderList[0];
            Assert.AreEqual(1, order.Id);
            Assert.AreEqual(orderModel.Products.Count, order.Products.Count);
            for (int i = 0; i < orderModel.Products.Count; i++)
            {
                Assert.AreEqual(orderModel.Products[i].Id, order.Products[i].Id);
                Assert.AreEqual(orderModel.Products[i].Description, order.Products[i].Description);
            }
        }
    }
}
