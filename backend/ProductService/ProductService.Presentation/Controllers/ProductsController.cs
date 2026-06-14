using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.Services.Media;
using ProductService.Application.Services.Products.Command;
using ProductService.Application.Services.Products.Query;
using ProductService.Presentation.Mappers;
using ProductService.Presentation.Models;

namespace ProductService.Presentation.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Policy = "SellerOrAdmin")]
public class ProductsController(IProductCommandService commandService, IProductQueryService queryService, IMediaService mediaService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductCursorPagedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCatalog([FromQuery] ProductSearchFilterRequest request, CancellationToken ct)
    {
        var cursorResult = await queryService.GetCatalogAsync(request.ToDto(), ct);
        
        var response = cursorResult.ToHttpResponse();
        
        return Ok(response);
    }
    
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var productDetailsDto = await queryService.GetProductAsync(id, ct);

        var response = productDetailsDto.ToHttpResponse();
        
        return Ok(response);
    }
    
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var productDetailsDto = await commandService.CreateProductAsync(request.ToDto(), ct);
        
        var response = productDetailsDto.ToHttpResponse();
        
        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateProductRequest request,
        CancellationToken ct)
    {
        var productDetailsDto = await commandService.UpdateProductAsync(request.ToDto(id), ct);
        
        var response = productDetailsDto.ToHttpResponse();
        
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        await commandService.DeleteProductAsync(id, ct);
        
        return NoContent();
    }

    [HttpPost("upload-urls")]
    [ProducesResponseType(typeof(UploadFilesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult UploadFiles([FromBody] UploadFilesRequest request, CancellationToken ct)
    {
        var urls = mediaService.PrepareBatchUpload(request.ToDto(), ct);
        
        return Ok(urls.ToHttpResponse());
    }
}