using Microsoft.AspNetCore.Mvc;
using ProductSelector.Models;
using ProductSelector.Services;

namespace ProductSelector.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PriceMonitorController : ControllerBase
{
    private readonly IPriceMonitorService _priceMonitorService;
    
    public PriceMonitorController(IPriceMonitorService priceMonitorService)
    {
        _priceMonitorService = priceMonitorService;
    }
    
    [HttpGet("alerts/{userId}")]
    public async Task<ActionResult<List<PriceAlert>>> GetUserAlerts(string userId)
    {
        var alerts = await _priceMonitorService.GetUserAlertsAsync(userId);
        return Ok(alerts);
    }
    
    [HttpPost("alerts")]
    public async Task<ActionResult<PriceAlert>> CreateAlert(PriceAlert alert)
    {
        var created = await _priceMonitorService.CreateAlertAsync(alert);
        return CreatedAtAction(nameof(GetUserAlerts), new { userId = created.UserId }, created);
    }
    
    [HttpDelete("alerts/{alertId}")]
    public async Task<IActionResult> DeleteAlert(int alertId)
    {
        var result = await _priceMonitorService.DeleteAlertAsync(alertId);
        if (!result)
            return NotFound();
        
        return NoContent();
    }
    
    [HttpPost("check-alerts")]
    public async Task<ActionResult<List<PriceAlert>>> CheckAlerts()
    {
        var triggeredAlerts = await _priceMonitorService.CheckAlertsAsync();
        return Ok(triggeredAlerts);
    }
    
    [HttpGet("history/{productId}")]
    public async Task<ActionResult<List<PriceHistory>>> GetPriceHistory(
        int productId, 
        [FromQuery] int days = 30)
    {
        var history = await _priceMonitorService.GetPriceHistoryAsync(productId, days);
        return Ok(history);
    }
    
    [HttpPost("record-price")]
    public async Task<IActionResult> RecordPrice([FromQuery] int productId, [FromQuery] decimal price)
    {
        await _priceMonitorService.RecordPriceHistoryAsync(productId, price);
        return Ok();
    }
}
