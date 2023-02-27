using System.Threading.Tasks;
using AlgoShop;
using Grpc.Net.Client;

// The port number must match the port of the gRPC server.
using var channel = GrpcChannel.ForAddress("https://localhost:7053");
using var channelOrder = GrpcChannel.ForAddress("https://localhost:7174");
var client = new Stocks.StocksClient(channel);
var clientOrder = new Orders.OrdersClient(channelOrder);

int choice = 0;
while (choice != 6)
{
    Console.WriteLine("Menu:");
    Console.WriteLine("1. Add Products");
    Console.WriteLine("2. Increase Stock");
    Console.WriteLine("3. Decrease Stock");
    Console.WriteLine("4. Verify Stock");
    Console.WriteLine("5. Make a Order");
    Console.WriteLine("6. Quit");

    Console.Write("Enter your choice: ");
    if (int.TryParse(Console.ReadLine(), out choice))
    {
        var name = string.Empty;
        int quantity = 0;
        int id = -1;
        bool isInt = false;
        var allProducts = new ResponseProductCollection();
        switch (choice)
        {
            case 1:
                Console.Write("Enter product Name: ");
                name = Console.ReadLine();
                Console.Write("Enter product quantity: ");
                quantity = 0;
                isInt = int.TryParse(Console.ReadLine(), out quantity);
                if (isInt)
                {
                    var reply = await client.AddProductsAsync(
                  new ProductStock
                  {
                      Quantity = quantity,
                      Product = new ProductsModel()
                      {
                          Description = name
                      }
                  });
                    Console.WriteLine($"{reply.Message}");
                }
                else
                {
                    Console.WriteLine("Incorrect quantity format, it must be a integer number");
                }
                break;
            case 2:
                allProducts = await client.GetAllProductsAsync(new EmptyObject());
                if (allProducts is not null)
                {
                    Console.WriteLine("Products:");
                    foreach (var p in allProducts.AllProducts)
                    {
                        Console.WriteLine(p.Product.Id + " - " + p.Product.Description + " | Quantity: " + p.Quantity);
                    }
                }
                Console.Write("Select the number of product: ");
                int.TryParse(Console.ReadLine(), out id);
                Console.Write("Enter quantity to increase: ");
                quantity = 0;
                isInt = int.TryParse(Console.ReadLine(), out quantity);
                if (isInt)
                {
                    var reply = await client.IncreaseStockAsync(
                  new ProductStock
                  {
                      Quantity = quantity,
                      Product = new ProductsModel()
                      {
                          Id = id
                      }
                  });
                    Console.WriteLine($"{reply.Message}");
                }
                else
                {
                    Console.WriteLine("Incorrect quantity format, it must be a integer number");
                }
                break;
            case 3:
                allProducts = await client.GetAllProductsAsync(new EmptyObject());
                if (allProducts is not null)
                {
                    Console.WriteLine("Products:");
                    foreach (var p in allProducts.AllProducts)
                    {
                        Console.WriteLine(p.Product.Id + " - " + p.Product.Description + " | Quantity: " + p.Quantity);
                    }
                }
                Console.Write("Select the number of product: ");
                int.TryParse(Console.ReadLine(), out id);
                Console.Write("Enter quantity to increase: ");
                quantity = 0;
                isInt = int.TryParse(Console.ReadLine(), out quantity);
                if (isInt)
                {
                    var reply = await client.DecreaseStockAsync(
                                new ProductStock
                                {
                                    Quantity = quantity,
                                    Product = new ProductsModel()
                                    {
                                        Id = id
                                    }
                                });
                    Console.WriteLine($"{reply.Message}");
                }
                else
                {
                    Console.WriteLine("Incorrect quantity format, it must be a integer number");
                }
                break;
            case 4:
                allProducts = await client.GetAllProductsAsync(new EmptyObject());
                if (allProducts is not null)
                {
                    Console.WriteLine("Products:");
                    foreach (var p in allProducts.AllProducts)
                    {
                        Console.WriteLine(p.Product.Id + " - " + p.Product.Description + " | Quantity: " + p.Quantity);
                    }
                }
                break;
            case 5:
                allProducts = await client.GetAllProductsAsync(new EmptyObject());
                if (allProducts is not null)
                {
                    Console.WriteLine("Products:");
                    foreach (var p in allProducts.AllProducts)
                    {
                        Console.WriteLine(p.Product.Id + " - " + p.Product.Description + " | Quantity: " + p.Quantity);
                    }
                }
                Console.WriteLine("Select the product and how much of each separated by a comma ',': ");
                Console.WriteLine("To finish filling the cart write Done!");
                var input = string.Empty;
                var shoppingCart = new List<ProductStock>();
                while (input.ToLower() != "done")
                {
                    var aux = Console.ReadLine();
                    var value = aux.Split(',');
                    var isProduct = int.TryParse(value[0], out id);
                    if (!isProduct)
                    {
                        var isOver = aux;
                        if (isOver.ToLower().Equals("done"))
                        {
                            Console.WriteLine("Shopping cart:");
                            var orderModel = new OrderModel();
                            foreach (var a in shoppingCart)
                            {
                                Console.WriteLine(a.Product.Description + " " + a.Quantity);
                                orderModel.Products.Add(new ProductsOrderModel
                                {
                                    Id = a.Id,
                                    Description = a.Product.Description,
                                    Quantity = a.Quantity
                                });
                            }

                            var reply = await clientOrder.PlaceOrderAsync(orderModel);

                            Console.WriteLine($"{reply.Message}");
                            break;
                        }
                    }
                    else
                    {
                        if (shoppingCart.Any(x => x.Id == id))
                        {
                            var current = shoppingCart.FirstOrDefault(x => x.Id == id);
                            current.Quantity += int.Parse(value[1]);
                        }
                        else
                        {
                            shoppingCart.Add(new ProductStock
                            {
                                Id = id,
                                Product = allProducts?.AllProducts.FirstOrDefault(x => x.Id == id)?.Product,
                                Quantity = int.Parse(value[1])
                            });
                        }

                    }
                }
                break;
            case 6:
                Console.WriteLine("Bye!");
                break;
            default:
                Console.WriteLine("Invalid choice");
                break;
        }
    }
    else
    {
        Console.WriteLine("Invalid choice");
    }
}
Console.ReadKey();