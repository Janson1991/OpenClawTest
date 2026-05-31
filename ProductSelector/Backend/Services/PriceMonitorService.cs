using Microsoft.EntityFrameworkCore;
using ProductSelector.Data;
using ProductSelector.Models;

namespace ProductSelector.Services;

public interface IPriceMonitorService
{
    Task<List<PriceAlert>> GetUserAlertsAsync(string userId);
    Task<PriceAlert> CreateAlertAsync(PriceAlert alert);
    Task<bool> DeleteAlertAsync(int alertId);
    Task<List<PriceAlert>> CheckAlertsAsync();
    Task RecordPriceHistoryAsync(int productId, decimal price);
    Task<List<PriceHistory>> GetPriceHistoryAsync(int productId, int days = 30);
}

public class PriceMonitorService : IPriceMonitorService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PriceMonitorService> _logger;
    
    public PriceMonitorService(AppDbContext context, ILogger<PriceMonitorService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<List<PriceAlert>> GetUserAlertsAsync(string userId)
    {
        return await _context.PriceAlerts
            .Where(a => a.UserId == userId)
            .Include(a => a.Product)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<PriceAlert> CreateAlertAsync(PriceAlert alert)
    {
        alert.CreatedAt = DateTime.UtcNow;
        alert.IsActive = true;
        
        _context.PriceAlerts.Add(alert);
        await _context.SaveChangesAsync();
        
        return alert;
    }
    
    public async Task<bool> DeleteAlertAsync(int alertId)
    {
        var alert = await _context.PriceAlerts.FindAsync(alertId);
        if (alert == null)
            return false;
        
        _context.PriceAlerts.Remove(alert);
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<List<PriceAlert>> CheckAlertsAsync()
    {
        var triggeredAlerts = new List<PriceAlert>();
        
        var activeAlerts = await _context.PriceAlerts
            .Where(a => a.IsActive)
            .Include(a => a.Product)
            .ToListAsync();
        
        foreach (var alert in activeAlerts)
        {
            if (alert.Product == null) continue;
            
            bool triggered = false;
            
            if (alert.AlertOnPriceDrop && alert.Product.Price < alert.LastCheckedPrice)
            {
                triggered = true;
                _logger.LogInformation($"价格下降提醒: {alert.Product.Name} 从 ¥{alert.LastCheckedPrice} 降到 ¥{alert.Product.Price}");
            }
            
            if (alert.TargetPrice.HasValue && alert.Product.Price <= alert.TargetPrice.Value)
            {
                triggered = true;
                _logger.LogInformation($"目标价格提醒: {alert.Product.Name} 达到目标价 ¥{alert.TargetPrice}");
            }
            
            if (triggered)
            {
                alert.LastTriggeredAt = DateTime.UtcNow;
                alert.TriggerCount++;
                triggeredAlerts.Add(alert);
            }
            
            alert.LastCheckedPrice = alert.Product.Price;
            alert.LastCheckedAt = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync();
        
        return triggeredAlerts;
    }
    
    public async Task RecordPriceHistoryAsync(int productId, decimal price)
    {
        var history = new PriceHistory
        {
            ProductId = productId,
            Price = price,
            RecordedAt = DateTime.UtcNow
        };
        
        _context.PriceHistories.Add(history);
        await _context.SaveChangesAsync();
    }
    
    public async Task<List<PriceHistory>> GetPriceHistoryAsync(int productId, int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        
        return await _context.PriceHistories
            .Where(h => h.ProductId == productId && h.RecordedAt >= startDate)
            .OrderBy(h => h.RecordedAt)
            .ToListAsync();
    }
}
