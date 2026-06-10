using System.ComponentModel.DataAnnotations;
using Core.Minio.Helpers;
using ProductService.Application.DTO.Category;
using ProductService.Application.DTO.Product;
using ProductService.Application.Exceptions;
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
    ICategoryRepository categoryRepository, IS3UrlFormatter urlFormatter) : IProductCommandService
{
    public async Task<ProductDetailsDto> CreateProductAsync(CreateProductDto dto, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        
        var sellerId = Guid.NewGuid(); //TODO Авторизация на уровне ролей
        
        var categoryDto = await EnsureCategoryExistsAsync(dto.CategoryId, ct);
        
        var price = new Money(dto.Price.Amount, dto.Price.Currency);
        var relativeImages = dto.ImagesUrl
            .Select(url => new ProductImage(urlFormatter.ToObjectKey(url)))
            .ToList();
        
        var product = Product.Create(dto.Sku, dto.Name, dto.Description, dto.CategoryId, sellerId, price, relativeImages);

        await unitOfWork.BeginTransactionAsync(ct); //TODO BeginOutboxTransactionAsync для outbox
        try
        {
            ct.ThrowIfCancellationRequested();
            
            await productRepository.AddAsync(product);
            
            await unitOfWork.CommitAsync();
            
            // TODO Invalidation (Redis) - Инвалидировать/удалить кэш каталога и списков, так как появился новый товар
            // TODO Kafka - Опубликовать событие ProductCreatedEvent в брокер сообщений
            
            product.ClearDomainEvents();
            
            var absoluteImages = product.Images
                .Select(img => urlFormatter.ToAbsoluteUrl(img.Url))
                .ToList();
            
            return product.ToDto(categoryDto, absoluteImages);
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<ProductDetailsDto> UpdateProductAsync(UpdateProductDto dto, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        
        var categoryDto = await EnsureCategoryExistsAsync(dto.CategoryId, ct);
        
        //TODO Кеширование
        var product = await productRepository.GetAsync(dto.ProductId)
            ?? throw new NotFoundException(nameof(Product), dto.ProductId);

        //TODO product.IsOwnedBy(userId);
        
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
        
        var relativeImagesUrls = dto.ImagesUrl
            .Select(urlFormatter.ToObjectKey)
            .ToList(); 
        
        product.UpdateImages(relativeImagesUrls);
        
        product.IncrementVersion();
        
        if (product.DomainEvents.Count == 0)
        {
            var currentAbsoluteImages = product.Images
                .Select(img => urlFormatter.ToAbsoluteUrl(img.Url))
                .ToList();
            
            return product.ToDto(categoryDto, currentAbsoluteImages);
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
            
            // TODO BackgroundWorker для удаления файлов из S3, urls лежат в imagesUpdateEvent.RemovedUrls
            // TODO Invalidation (Redis) - Инвалидировать/удалить кэш каталога и списков, так как появился новый товар
            // TODO Kafka - Опубликовать событие ProductUpdatedEvent в брокер сообщений
            
            product.ClearDomainEvents();
            
            var absoluteImageUrls = product.Images
                .Select(img => urlFormatter.ToAbsoluteUrl(img.Url))
                .ToList();
            
            return product.ToDto(categoryDto, absoluteImageUrls);
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        
        //TODO Кеширование
        var product = await productRepository.GetAsync(id)
                      ?? throw new NotFoundException(nameof(Product), id);

        //TODO product.IsOwnedBy(userId);
        
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            ct.ThrowIfCancellationRequested();
            
            await productRepository.DeleteAsync(id);
            await unitOfWork.CommitAsync();
            
            // TODO: Invalidation (Redis) - Сбросить кэш детальной карточки "products:details:{id}",
            // легкой карточки "products:card:{id}" и сбросить кэш каталога.
            // TODO Kafka - Опубликовать событие ProductDeletedEvent в брокер сообщений
            // TODO BackgroundWorker для удаления файлов из S3
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }
    
    private async Task<CategoryDto> EnsureCategoryExistsAsync(int categoryId, CancellationToken ct)
    {
        //TODO Redis кеширование
        var category = await categoryRepository.GetAsync(categoryId);

        if (category == null)
        {
            throw new ValidationException($"Category with id {categoryId} does not exist.");
        }
        
        return category.ToDto();
    }
}