namespace ProductService.Application.Exceptions;

public class ForbiddenException()
    : Exception("Access denied. You do not have permission to perform this action.");  