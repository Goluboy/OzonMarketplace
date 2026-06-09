using System.Data;
using ProductService.Application.DTO.Category;
using ProductService.Application.Exceptions;
using ProductService.Application.Mappers;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions;
using ProductService.Infrastructure.Abstractions.UnitOfWork.Abstractions;

namespace ProductService.Application.Services.Categories;

public class CategoryService(IUnitOfWork unitOfWork, ICategoryRepository categoryRepository) : ICategoryService
{
    public async Task<IReadOnlyCollection<CategoryDto>> GetAllAsync(CancellationToken ct)
    {
        // TODO: Двухфазное кэширование и E-Tag (паттерн Декоратор 'CachingCategoryService'):
        // 1. ФАЗА 1: Получить текущий E-Tag из Redis. Если он совпадает с присланным клиентом If-None-Match — вернуть маркер 304 (Not Modified) без вычитки данных.
        // 2. ФАЗА 2: Если тег не совпал, попробовать получить сериализованный список категорий из Redis. Если он есть — вернуть его и обновить E-Tag на клиенте.
        
        // Важно: Не забываем передавать CancellationToken во все асинхронные вызовы БД
        var categories = await categoryRepository.GetAllAsync(ct);
        
        // TODO: ФАЗА 3 (Cache Miss): Если данных в Redis не было, сгенерировать новый E-Tag,
        // а затем записать полученный список DTO и новый E-Tag в Redis с TTL.
        
        return categories
            .Select(c => c.ToDto())
            .ToList();
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
            
            // TODO: Инвалидация кэша: Сбросить/удалить ключи данных и E-Tag категорий из Redis.
            
            return category.ToDto();
        }
        catch
        {
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
            
            // TODO: Инвалидация кэша: Сбросить/удалить ключи данных и E-Tag из Redis.
            
            category.ClearDomainEvents();
            
            return category.ToDto();
        }
        catch
        {
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
            
            // TODO: Инвалидация кэша: Сбросить/удалить ключи данных и E-Tag из Redis.
        }
        catch 
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public Task<string> GetVersionETagAsync(CancellationToken ct)
    {
        //TODO Redis кеширование версии категорий
        return Task.FromResult("1");
    }
}