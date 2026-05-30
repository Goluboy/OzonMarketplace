namespace ProductService.Domain.Entities;

public class Category : IEquatable<Category>
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Path { get; private set; } = null!;
    
    private Category() { }
    
    public static Category Create(string name, string path)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category Name cannot be null or empty", nameof(name));
        }

        ValidatePath(path);

        return new Category
        {
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
        ValidatePath(newPath);
        
        Path = newPath;
    }

    public void SetId(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Id must be greater than zero");
        }
        Id = id;
    }
    
    private static void ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Category Path cannot be null or empty", nameof(path));
        }

        if (path.StartsWith('.') || path.EndsWith('.'))
        {
            throw new ArgumentException("Category Path cannot start or end with a dot", nameof(path));
        }

        if (path.Contains(".."))
        {
            throw new ArgumentException("Category Path cannot contain consecutive dots", nameof(path));
        }

        var segments = path.Split('.');
        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                throw new ArgumentException("Category Path segments cannot be empty", nameof(path));
            }
            
            if (!segment.All(c => char.IsLetterOrDigit(c) || c == '-'))
            {
                throw new ArgumentException($"Category Path segment '{segment}' contains invalid characters", nameof(path));
            }
        }
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