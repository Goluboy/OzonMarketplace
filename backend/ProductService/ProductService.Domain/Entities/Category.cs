namespace ProductService.Domain.Entities;

public class Category : IEquatable<Category>
{
    public Guid Id { get; init; }
    public string Name { get; private set; } = null!;
    public string Path { get; private set; } = null!;
    
    private Category() { }
    
    public static Category Create(string name, string path)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category Name cannot be null or empty", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Category Path cannot be null or empty", nameof(path));
        }

        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Path = path
        };
    }
    
    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("Category Name cannot be null or empty", nameof(newName));
        }
        
        Name = newName;
    }
    
    public void MoveTo(string newPath)
    {
        if (string.IsNullOrWhiteSpace(newPath))
        {
            throw new ArgumentException("Category Path cannot be null or empty", nameof(newPath));
        }
        
        Path = newPath;
    }

    public bool Equals(Category? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as Category);

    public override int GetHashCode() => Id.GetHashCode();
    
    public static bool operator ==(Category? left, Category? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    public static bool operator !=(Category? left, Category? right) => !(left == right);
}