using System.Collections.Concurrent;
using System.Text.Json;

public interface IStockCache
{
    Task<int> GetStockAsync(int productId);
    Task UpdateStockAsync(int productId, int newStock);
    IEnumerable<int> GetAllProductIds();
}

public class JsonStockCache : IStockCache
{
    private readonly string _filePath;
    private ConcurrentDictionary<int, int> _stockCache;
    private readonly ILogger<JsonStockCache> _logger;
    private readonly object _fileLock = new object();

    public JsonStockCache(string filePath, ILogger<JsonStockCache> logger)
    {
        _filePath = filePath;
        _stockCache = new ConcurrentDictionary<int, int>();
        _logger = logger;
        LoadCache();
    }

    private void LoadCache()
    {
        try
        {
            _logger.LogInformation($"Trying to load cache from {_filePath}");
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var stockData = JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? new Dictionary<int, int>();
                _stockCache = new ConcurrentDictionary<int, int>(stockData);
            }
            else
            {
                _stockCache = new ConcurrentDictionary<int, int>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading cache from {_filePath}: {ex.Message}");
        }
    }

    private void SaveCache()
    {
        try
        {
            lock (_fileLock)
            {
                var json = JsonSerializer.Serialize(_stockCache);
                File.WriteAllText(_filePath, json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error saving cache to {_filePath}: {ex.Message}");
        }
    }

    public IEnumerable<int> GetAllProductIds() => _stockCache.Keys;

    public Task<int> GetStockAsync(int productId)
    {
        if (_stockCache.TryGetValue(productId, out var stock))
        {
            return Task.FromResult(stock);
        }
        return Task.FromResult(0);
    }

    public Task UpdateStockAsync(int productId, int newStock)
    {
        _stockCache[productId] = newStock;
        SaveCache();
        return Task.CompletedTask;
    }
}