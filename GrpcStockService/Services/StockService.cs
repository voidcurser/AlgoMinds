using DataLayer.Models;
using Grpc.Core;
using MongoDB.Driver;

namespace GrpcStockService.Services;

public class StockService : Stocks.StocksBase
{
    private readonly ILogger<StockService> _logger;
    private readonly IMongoCollection<Stock> _stocks;

    public StockService(ILogger<StockService> logger, IMongoClient client)
    {
        _logger = logger;
        var database = client.GetDatabase("AlgoMindsDatabase");
        _stocks = database.GetCollection<Stock>("Stocks");
    }
    public override async Task<ProductStock> AddProducts(ProductStock request, ServerCallContext context)
    {
        var stock = new DataLayer.Models.Stock();
        stock.NumberOfProducts = request.Quantity;
        stock.Product = new Products() { Description = request.Product.Description };


        await _stocks.InsertOneAsync(stock);

        return new ProductStock
        {
            Id = stock.Id,
            Product = new ProductsModel() { Id = stock.Product.Id,Description = stock.Product.Description },
            Quantity = stock.NumberOfProducts,
        };
    }
    public override Task<ProductStock> GetStock(ProductsModel request, ServerCallContext context)
    {
       var teste = _stocks.Find(x => x.Product.Description == request.Description).FirstOrDefaultAsync();
        return base.GetStock(request, context);
    }
    public override Task<ProductStock> IncreaseStock(ProductStock request, ServerCallContext context)
    {
        return base.IncreaseStock(request, context);
    }
    public override Task<ProductStock> DecreaseStock(ProductStock request, ServerCallContext context)
    {
        return base.DecreaseStock(request, context);
    }
}
