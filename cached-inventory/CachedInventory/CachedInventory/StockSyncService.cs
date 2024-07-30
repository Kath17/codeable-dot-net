using CachedInventory;

public class StockSyncService : IHostedService, IDisposable
{
    private readonly ILogger<StockSyncService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private Timer _timer;

    public StockSyncService(ILogger<StockSyncService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stock Sync Service starting.");

        _timer = new Timer(SyncStock, null, TimeSpan.Zero, TimeSpan.FromSeconds(2.5));

        return Task.CompletedTask;
    }

    private async void SyncStock(object state)
    {
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var stockClient = scope.ServiceProvider.GetRequiredService<IWarehouseStockSystemClient>();
                var stockCache = scope.ServiceProvider.GetRequiredService<IStockCache>();

                var productIds = stockCache.GetAllProductIds();
                _logger.LogInformation($"Product IDs found: {string.Join(", ", productIds)}");

                foreach (var productId in productIds)
                {
                    var currentStock = await stockCache.GetStockAsync(productId);
                    _logger.LogInformation($"Fetched current stock from cache for Product ID {productId}: {currentStock}");

                    // Actualizando stock en el warehouse system con el stockCache
                    await stockClient.UpdateStock(productId, currentStock);
                    _logger.LogInformation($"Updated stock in warehouse system for Product ID {productId} with stock {currentStock}");

                    // Verificando si el stock se actualizó correctamente en el sistema de almacén
                    var updatedStock = await stockClient.GetStock(productId);
                    _logger.LogInformation($"Verified updated stock in warehouse system for Product ID {productId}: {updatedStock}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during stock sync: {ex.Message}");
        }
    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stock Sync Service stopping.");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}