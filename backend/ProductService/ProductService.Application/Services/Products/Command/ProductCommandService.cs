using System.ComponentModel.DataAnnotations;
using Core.Minio.Service;
using Microsoft.Extensions.Logging;
using ProductService.Application.DTO.Category;
using ProductService.Application.DTO.Product;
using ProductService.Application.Exceptions;
using ProductService.Application.Helpers;
using ProductService.Application.Mappers;
using ProductService.Domain.Entities;
using ProductService.Domain.Events;
using ProductService.Domain.ValueObjects;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;
using ProductService.Infrastructure.Abstractions.UnitOfWork.Abstractions;

namespace ProductService.Application.Services.Products.Command;

public class ProductCommandService(IUnitOfWork unitOfWork, IProductRepository productRepository,
    ICategoryRepository categoryRepository, IProductImageUrlHelper urlHelper, IS3StorageService storageService,
    ICurrentUserHelper userHelper, ILogger<ProductCommandService> logger) : IProductCommandService
{
    public async Task<ProductDetailsDto> CreateProductAsync(CreateProductDto dto, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var sellerId = userHelper.UserId;
        
        var categoryDto = await EnsureCategoryExistsAsync(dto.CategoryId, ct);
        
        var price = new Money(dto.Price.Amount, dto.Price.Currency);
        
        var storedImages = urlHelper.ToStoredImages(dto.ImagesUrl);
        
        var product = Product.Create(dto.Sku, dto.Name, dto.Description, dto.CategoryId, sellerId, price, storedImages);

        await unitOfWork.BeginTransactionAsync(ct); //TODO BeginOutboxTransactionAsync для outbox
        try
        {
            ct.ThrowIfCancellationRequested();
            
            await productRepository.AddAsync(product);
            
            await unitOfWork.CommitAsync();
            
            // TODO Kafka - Опубликовать событие ProductCreatedEvent в брокер сообщений
            
            product.ClearDomainEvents();
            
            logger.LogInformation("Product created successfully. ProductId: {ProductId}, SKU: {Sku}, SellerId: {SellerId}", 
                product.Id, product.Sku, sellerId);
            
            return ToDtoWithAbsoluteUrls(product, categoryDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create product. SKU: {Sku}, SellerId: {SellerId}. Transaction rolled back.", 
                dto.Sku, sellerId);
            
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<ProductDetailsDto> UpdateProductAsync(UpdateProductDto dto, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        
        var categoryDto = await EnsureCategoryExistsAsync(dto.CategoryId, ct);
        
        var product = await productRepository.GetAsync(dto.ProductId)
            ?? throw new NotFoundException(nameof(Product), dto.ProductId);

        EnsureProductOwnership(product);
        
        if (!string.Equals(product.Name, dto.Name, StringComparison.Ordinal) ||
            !string.Equals(product.Description, dto.Description, StringComparison.Ordinal) ||
            product.CategoryId != dto.CategoryId)
        {
            product.UpdateDetails(dto.Name, dto.Description, dto.CategoryId);
        }
        
        var newPrice = new Money(dto.Price.Amount, dto.Price.Currency);
        if (product.Price != newPrice)
        {
            product.ChangePrice(newPrice);
        }
        
        var storedImages = urlHelper.ToStoredImages(dto.ImagesUrl);
        
        product.UpdateImages(storedImages);
        
        product.IncrementVersion();
        
        if (product.DomainEvents.Count == 0)
        {
            return ToDtoWithAbsoluteUrls(product, categoryDto);
        }
        
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            ct.ThrowIfCancellationRequested();
            
            var imagesUpdateEvent = product.DomainEvents
                .OfType<ProductImagesUpdatedEvent>()
                .FirstOrDefault();
            
            await productRepository.UpdateAsync(product);
            await unitOfWork.CommitAsync();
            
            // TODO Kafka - Опубликовать событие ProductUpdatedEvent в брокер сообщений
            
            if (imagesUpdateEvent != null && imagesUpdateEvent.RemovedUrls.Count != 0)
            {
                DeleteImagesAsync(imagesUpdateEvent.RemovedUrls);
            }
            
            product.ClearDomainEvents();

            logger.LogInformation("Product updated successfully in database. ProductId: {ProductId}, UserId: {UserId}",
                product.Id, userHelper.UserId);
            
            return ToDtoWithAbsoluteUrls(product, categoryDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update product. ProductId: {ProductId}, UserId: {UserId}. Transaction rolled back.", 
                dto.ProductId, userHelper.UserId);
            
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        
        var product = await productRepository.GetAsync(id)
                      ?? throw new NotFoundException(nameof(Product), id);

        EnsureProductOwnership(product);
        
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            ct.ThrowIfCancellationRequested();
            
            var imageUrlsToRemove = product.Images.Select(img => img.Url).ToList(); 
            
            await productRepository.DeleteAsync(id);
            await unitOfWork.CommitAsync();
            
            // TODO Kafka - Опубликовать событие ProductDeletedEvent в брокер сообщений

            if (imageUrlsToRemove.Count != 0)
            {
                DeleteImagesAsync(imageUrlsToRemove);
            }
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    private void EnsureProductOwnership(Product product)
    {
        if (userHelper.IsAdmin)
        {
            logger.LogWarning("Admin {AdminId} bypassed ownership check for Product {ProductId}", userHelper.UserId, product.Id);
            return;
        }
        
        if (!product.IsOwnedBy(userHelper.UserId))
        {
            logger.LogWarning("Unauthorized access attempt. User {UserId} tried to modify Product {ProductId} owned by {OwnerId}", 
                userHelper.UserId, product.Id, product.SellerId);
            throw new ForbiddenException();
        }
    }
    
    private void DeleteImagesAsync(List<string> imageUrlsToRemove)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await storageService.DeleteFilesAsync(imageUrlsToRemove, CancellationToken.None);
                logger.LogInformation("Background S3 file deletion completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during S3 file deletion. Error: {Message}", ex.Message);
            }
        }, CancellationToken.None);
    }
    
    private ProductDetailsDto ToDtoWithAbsoluteUrls(Product product, CategoryDto categoryDto)
    {
        var detailsDto = product.ToDto(categoryDto);
        return detailsDto with
        {
            Images = urlHelper.ToAbsoluteImageDtos(detailsDto.Images)
        };
    }
    
    private async Task<CategoryDto> EnsureCategoryExistsAsync(int categoryId, CancellationToken ct)
    {
        var category = await categoryRepository.GetAsync(categoryId);

        if (category == null)
        {
            throw new ValidationException($"Category with id {categoryId} does not exist.");
        }
        
        return category.ToDto();
    }
}