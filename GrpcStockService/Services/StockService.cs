using DataLayer.Models;
using Grpc.Core;
using MongoDB.Driver;
using System.Diagnostics.Metrics;

namespace GrpcStockService.Services;

public class StockService : Stocks.StocksBase
{
    private readonly ILogger<StockService> _logger;
    private readonly IMongoCollection<Stock> _stocks;
    private readonly IMongoCollection<Sequence> _counters;
    private readonly IMongoDatabase _database;

    public StockService(ILogger<StockService> logger, IMongoClient client)
    {
        _logger = logger;
        _database = client.GetDatabase("AlgoMindsDatabase");
        _stocks = _database.GetCollection<Stock>("Stocks");
        _counters = _database.GetCollection<Sequence>("counters");
        var hasSequence = _counters.Find(x => x.Name.Equals("products")).FirstOrDefault();
        if (hasSequence is null)
        {
            var counter = new Sequence
            {
                Name = "products",
                Value = 1
            };

            _counters.InsertOne(counter);
        }

    }
    public override async Task<Response> AddProducts(ProductStock request, ServerCallContext context)
    {
        try
        {
            var stock = new DataLayer.Models.Stock();
            stock.NumberOfProducts = request.Quantity;
            stock.Product = new Products() { Description = request.Product.Description };
            var sequence = _counters.FindOneAndUpdate(
                                    Builders<Sequence>.Filter.Eq(x => x.Name, "products"),
                                    Builders<Sequence>.Update.Inc(x => x.Value, 1));

            var nextId = sequence.Value;
            var products = _stocks;
            stock.Id = nextId;
            stock.Product.Id = nextId;
            await _stocks.InsertOneAsync(stock);
            _logger.LogInformation($"Product {stock.Product.Description} added successfully");
            return new Response
            {
                Message = $"Product {stock.Product.Description} added successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"There was an error adding the product!:{ex.Message}");
            return new Response
            {
                Message = $"There was an error adding the product!"
            };
        }
    }
    public override async Task<Response> IncreaseStock(ProductStock request, ServerCallContext context)
    {
        try
        {
            var filter = Builders<Stock>.Filter.Eq(x => x.Product.Id, request.Product.Id);
            var update = Builders<Stock>.Update.Inc(x => x.NumberOfProducts, request.Quantity);
            await _stocks.UpdateOneAsync(filter, update);
            _logger.LogError($"Product updated successfully");
            return new Response
            {
                Message = $"Product updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"There was an error updating the product!: {ex.Message}");
            return new Response
            {
                Message = $"There was an error updating the product!"
            };
        }
    }
    public override async Task<Response> DecreaseStock(ProductStock request, ServerCallContext context)
    {
        try
        {
            var stock = new DataLayer.Models.Stock();
            var filter = Builders<Stock>.Filter.Eq(x => x.Product.Id, request.Product.Id);
            var update = Builders<Stock>.Update.Inc(x => x.NumberOfProducts, -request.Quantity);
            await _stocks.UpdateOneAsync(filter, update);
            _logger.LogError($"Product updated successfully");
            return new Response
            {
                Message = $"Product updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"There was an error updating the product!:{ex.Message}");
            return new Response
            {
                Message = $"There was an error updating the product!"
            };
        }
    }
    public override async Task<ResponseInt> GetStock(ProductsModel request, ServerCallContext context)
    {
        try
        {
            var stockCurrentProduct = await _stocks.Find(x => x.Product.Description == request.Description).FirstOrDefaultAsync();
            _logger.LogError($"GetStock executed successfully");
            return new ResponseInt
            {
                Result = stockCurrentProduct is not null ? stockCurrentProduct.NumberOfProducts : -1,
                Message = $"GetStock executed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"There was an error updating the product!:{ex.Message}");
            return new ResponseInt
            {
                Result = -1,
                Message = $"There was an error updating the product!"
            };
        }
    }
    public override async Task<ResponseBool> IsStockAvailableForTheOrder(ResponseProductCollection request, ServerCallContext context)
    {
        try
        {
            var result = true;
            foreach (var p in request.AllProducts)
            {
                var stockCurrentProduct = await _stocks.Find(x => x.Product.Id == p.Product.Id).FirstOrDefaultAsync();
                if (stockCurrentProduct is not null)
                {
                    if (stockCurrentProduct.NumberOfProducts < p.Quantity)
                    {
                        result = false;
                    }
                }
            }
            if (result)
            {
                _logger.LogError($"IsStockAvailableForTheOrder executed successfully");
                return new ResponseBool
                {
                    Result = true,
                    Message = $"GetStock executed successfully"
                };
            }
            else
            {
                _logger.LogError($"There is not enough stock for your order");
                return new ResponseBool
                {
                    Result = false,
                    Message = $"There is not enough stock for your order"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"There was an error updating the product!:{ex.Message}");
            return new ResponseBool
            {
                Result = false,
                Message = $"There was an error updating the product!"
            };
        }
    }
    public override async Task<ResponseProductCollection> GetAllProducts(EmptyObject request, ServerCallContext context)
    {
        var result = new ResponseProductCollection();
        try
        {
            var allStocks = await _stocks.Find(x => true).ToListAsync();
            foreach (var st in allStocks)
            {
                var toAdd = new ProductStock
                {
                    Id = st.Id,
                    Product = new ProductsModel
                    {
                        Id = st.Product.Id,
                        Description = st.Product.Description,
                    },
                    Quantity = st.NumberOfProducts
                };

                result.AllProducts.Add(toAdd);
            }
            _logger.LogError($"GetAllProducts executed without error!");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"There was an error returning the list of products!:{ex.Message} ");
            return result;
        }


    }
}
