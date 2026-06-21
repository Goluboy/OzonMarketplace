using System.Data;
using Microsoft.Extensions.Logging;
using ProductService.Application.DTO.Category;
using ProductService.Application.Exceptions;
using ProductService.Application.Mappers;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Abstractions.Caching.Abstractions;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions;
using ProductService.Infrastructure.Abstractions.UnitOfWork.Abstractions;

namespace ProductService.Application.Services.Categories;

public class CategoryService(IUnitOfWork unitOfWork, ICategoryRepository categoryRepository, 
    ICategoryVersionProvider versionProvider, ILogger<CategoryService> logger) : ICategoryService
{
    public async Task<CategoriesResponse> GetAllAsync(string? eTag, CancellationToken ct)
    {
        var actualEtag = await versionProvider.GetVersionETagAsync(ct);

        if (!string.IsNullOrEmpty(actualEtag) && actualEtag == eTag)
        {
            return new CategoriesResponse([], actualEtag, IsModified: false);
        }
        
        var categories = await categoryRepository.GetAllAsync(ct);
       
        var dtos = categories
            .Select(c => c.ToDto())
            .ToList();
        
        return new CategoriesResponse(dtos, actualEtag, IsModified: true);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct)
    {
        var category = Category.Create(dto.Name, dto.Path);

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var id = await categoryRepository.AddAsync(category);
            category.SetId(id);
            
            // TODO: Опубликовать события домена в CAP Outbox перед коммитом транзакции:
            
            await unitOfWork.CommitAsync();
            
            logger.LogInformation("Category created successfully. CategoryId: {CategoryId}, Name: {CategoryName}", id, dto.Name);
            
            return category.ToDto();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create category. Name: {CategoryName}. Transaction rolled back.", dto.Name);
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<CategoryDto> UpdateAsync(UpdateCategoryDto dto, CancellationToken ct)
    {
        var category = await categoryRepository.GetAsync(dto.Id) 
                       ?? throw new NotFoundException(nameof(Category), dto.Id);

        if (!string.Equals(category.Name, dto.Name, StringComparison.Ordinal))
        {
            category.Rename(dto.Name);
        }
        
        if (!string.Equals(category.Path, dto.Path, StringComparison.OrdinalIgnoreCase))
        {
            category.MoveTo(dto.Path);
        }
        
        if (category.DomainEvents.Count == 0)
        {
            return category.ToDto();
        }
        
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var success = await categoryRepository.UpdateAsync(category);
            
            if (!success)
            {
                throw new DBConcurrencyException($"Category with ID {category.Id} was modified by another process.");
            }
            
            // TODO: События: Опубликовать события изменения категории в брокер (когда появится инфраструктура)
            
            await unitOfWork.CommitAsync();
            
            category.ClearDomainEvents();
            
            logger.LogInformation("Category updated successfully. CategoryId: {CategoryId}", category.Id);
            
            return category.ToDto();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update category. CategoryId: {CategoryId}. Transaction rolled back.", dto.Id);
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var category = await categoryRepository.GetAsync(id) 
                       ?? throw new NotFoundException(nameof(Category), id);
        
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await categoryRepository.DeleteAsync(id);
            
            // TODO: События: Опубликовать события изменения категории в брокер (когда появится инфраструктура)
            
            await unitOfWork.CommitAsync();
            
            logger.LogInformation("Category deleted successfully. CategoryId: {CategoryId}", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete category. CategoryId: {CategoryId}. Transaction rolled back.", id);
            await unitOfWork.RollbackAsync();
            throw;
        }
    }
}