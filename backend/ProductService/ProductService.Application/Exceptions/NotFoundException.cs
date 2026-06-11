namespace ProductService.Application.Exceptions;

public class NotFoundException(string entityName, object id)
    : Exception($"{entityName} with id '{id}' was not found.");