using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using ProductService.Application.Services.Categories;
using ProductService.Presentation.Mappers;
using ProductService.Presentation.Models;

namespace ProductService.Presentation.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyCollection<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        string? clientEtag = Request.Headers.IfNoneMatch;
        
        if (!string.IsNullOrEmpty(clientEtag))
        {
            clientEtag = clientEtag.Trim('"');
        }
        
        var categoriesResponse = await categoryService.GetAllAsync(clientEtag, ct);

        if (!categoriesResponse.IsModified)
        {
            return new StatusCodeResult(StatusCodes.Status304NotModified);
        }
        
        var response = categoriesResponse.Categories.Select(x => x.ToHttpResponse()).ToList();
        
        Response.Headers[HeaderNames.ETag] = $"\"{categoriesResponse.ETag}\"";
        Response.Headers[HeaderNames.CacheControl] = "no-cache";
        
        return Ok(response);
    }
    
    [HttpPost]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] UpsertCategoryRequest request, CancellationToken ct)
    {
        var categoryDto = await categoryService.CreateAsync(request.ToCreateDto(), ct);
        
        var response = categoryDto.ToHttpResponse();
        
        return Ok(response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpsertCategoryRequest request, CancellationToken ct)
    {
        var updatedCategoryDto = await categoryService.UpdateAsync(request.ToUpdateDto(id), ct);
        
        var response = updatedCategoryDto.ToHttpResponse();
        
        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await categoryService.DeleteAsync(id, ct);
        
        return NoContent();
    }
}