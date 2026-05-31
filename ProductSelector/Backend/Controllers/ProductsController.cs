using Microsoft.AspNetCore.Mvc;
using ProductSelector.Models;
using ProductSelector.Services;

namespace ProductSelector.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IScraperService _scraperService;
    
    public ProductsController(IProductService productService, IScraperService scraperService)
    {
        _productService = productService;
        _scraperService = scraperService;
    }
    
    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetProducts(
        [FromQuery] string? platform = null,
        [FromQuery] string? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var products = await _productService.GetProductsAsync(platform, category, page, pageSize);
        return Ok(products);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
            return NotFound();
        
        return Ok(product);
    }
    
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        var created = await _productService.CreateProductAsync(product);
        return CreatedAtAction(nameof(GetProduct), new { id = created.Id }, created);
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult<Product>> UpdateProduct(int id, Product product)
    {
        if (id != product.Id)
            return BadRequest();
        
        var updated = await _productService.UpdateProductAsync(product);
        return Ok(updated);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var result = await _productService.DeleteProductAsync(id);
        if (!result)
            return NotFound();
        
        return NoContent();
    }
    
    [HttpGet("search")]
    public async Task<ActionResult<List<Product>>> SearchProducts([FromQuery] string keyword)
    {
        var products = await _productService.SearchProductsAsync(keyword);
        return Ok(products);
    }
    
    [HttpGet("trending")]
    public async Task<ActionResult<List<Product>>> GetTrendingProducts([FromQuery] int limit = 10)
    {
        var products = await _productService.GetTrendingProductsAsync(limit);
        return Ok(products);
    }
    
    [HttpPost("scrape")]
    public async Task<ActionResult<List<Product>>> ScrapeProducts([FromQuery] string keyword, [FromQuery] int page = 1)
    {
        var products = await _scraperService.Scrape1688Async(keyword, page);
        
        // 保存到数据库
        foreach (var product in products)
        {
            await _productService.CreateProductAsync(product);
        }
        
        return Ok(products);
    }
}
