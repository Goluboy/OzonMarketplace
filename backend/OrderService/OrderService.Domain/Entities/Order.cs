using OrderService.Domain.Events;
using OrderService.Domain.Interfaces.Domain;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Entities;

public class Order : IAuditable, IVersioned, ICloneable, IEquatable<Order>
{
    public OrderId Id { get; init; }
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = null!;
    public Email CustomerEmail { get; private set; } = null!;
    public DeliveryAddress? DeliveryAddress { get; private set; }

    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    public int Version { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    private readonly List<OrderItem> _items = new();

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear(); 

    public bool CanBeModified() =>
        Status is OrderStatus.Created or OrderStatus.Paid;
    public override string ToString() =>
        $"Order(Id={Id}, Status={Status}, Total={TotalAmount}, Items={Items.Count})";

    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> AllowedTransitions = new Dictionary<OrderStatus, HashSet<OrderStatus>>
    {
        [OrderStatus.Created] = new() { OrderStatus.Paid, OrderStatus.Cancelled },
        [OrderStatus.Paid] = new() { OrderStatus.Assembling, OrderStatus.Cancelled },
        [OrderStatus.Assembling] = new() { OrderStatus.Shipping, OrderStatus.Cancelled },
        [OrderStatus.Shipping] = new() { OrderStatus.Delivered, OrderStatus.Cancelled },
    };

    private Order() { }

    public static Order Create(
        Guid customerId,
        string customerName,
        string customerEmail,
        string? deliveryAddress,
        IEnumerable<OrderItem> items)
    {
        ArgumentNullException.ThrowIfNull(customerName);
        
        var itemsList = items.ToList();
        if (itemsList.Count == 0)
        {
            throw new ArgumentException("Order must contain at least one item", nameof(itemsList));
        }

        var order = new Order
        {
            Id = new OrderId(Guid.NewGuid()),
            CustomerId = customerId,
            CustomerName = customerName,
            CustomerEmail = new Email(customerEmail),
            DeliveryAddress = DeliveryAddress.Create(deliveryAddress),
            Status = OrderStatus.Created,
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };

        foreach (var item in itemsList)
        {
            order._items.Add(item);
            item.SetOrderId(order.Id);
        }

        order.RecalculateTotal();

        order._domainEvents.Add(new OrderCreatedEvent(
            order.Id,
            order.CustomerId,
            order.CustomerName,
            order.CustomerEmail,
            order.TotalAmount,
            order.DeliveryAddress,
            order.Items.Select(i => i.ToSnapshot()).ToList(), 
            order.CreatedAt));



        return order;
    }

    
    private void RecalculateTotal()
    {
        TotalAmount = new Money(Math.Round(_items.Sum(i => i.Subtotal.Value), 2, MidpointRounding.AwayFromZero));
    }

    public void AddItem(OrderItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!CanBeModified())
        {
            throw new InvalidOperationException($"Cannot add items to order with status '{Status}'");
        }

        _items.Add(item);
        item.SetOrderId(Id);
        RecalculateTotal();
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
        _domainEvents.Add(new OrderItemAddedEvent(
            Id,
            item.ToSnapshot(),
            TotalAmount,
            UpdatedAt.Value));

    }

    public void RemoveItem(Guid orderItemId)
    {
        if (!CanBeModified())
        {
            throw new InvalidOperationException($"Cannot remove items from order with status '{Status}'");
        }

        var item = _items.FirstOrDefault(i => i.Id == orderItemId);
        if (item == null)
        {
            throw new InvalidOperationException($"Item '{orderItemId}' not found in order");
        }

        _items.Remove(item);
        RecalculateTotal();
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
        _domainEvents.Add(new OrderItemRemovedEvent(
            Id,
            orderItemId,
            item.ProductId,
            item.Quantity,
            item.Subtotal,
            TotalAmount,
            UpdatedAt.Value));
    }

    public void Cancel(Guid? cancelledBy = null, string? reason = null)
    {
        if (Status == OrderStatus.Cancelled)
        {
            return;
        }

        ChangeStatus(OrderStatus.Cancelled, cancelledBy, reason); 
    }

    public OrderStatusHistory ChangeStatus(
        OrderStatus newStatus,
        Guid? changedBy = null,
        string? comment = null)
    {
        var oldStatus = Status;

        ValidateStatusTransition(Status, newStatus);

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        if (newStatus == OrderStatus.Cancelled && CancelledAt == null)
        {
            CancelledAt = DateTime.UtcNow;
        }

        _domainEvents.Add(new OrderStatusChangedEvent(
            Id,
            oldStatus,
            newStatus,
            changedBy,
            comment,
            DateTime.UtcNow));

        IncrementVersion();

        return OrderStatusHistory.Create(Id, oldStatus, newStatus, changedBy, comment);
    }

    private static void ValidateStatusTransition(OrderStatus current, OrderStatus next)
    {
        if (current == next)
        {
            return;
        }
        if (!AllowedTransitions.TryGetValue(current, out var targets) || !targets.Contains(next))
        {
            throw new InvalidOperationException(
                $"Invalid status transition: '{current}' to '{next}'");
        }
    }

    public bool IsOwnedBy(Guid customerId) => CustomerId == customerId;

    public void IncrementVersion()
    {
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }

    public object Clone()
    {
        return CloneOrder();
    }

    public Order CloneOrder()
    {
        var clone = new Order
        {
            Id = Id,
            CustomerId = CustomerId,
            CustomerName = CustomerName,
            CustomerEmail = CustomerEmail,
            DeliveryAddress = DeliveryAddress,
            Status = Status,
            TotalAmount = TotalAmount,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            CancelledAt = CancelledAt,
            Version = Version
        };

        foreach (var item in _items)
        {
            clone._items.Add(item.CloneItem());
        }

        return clone;
    }

    public bool Equals(Order? other)
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

    public override bool Equals(object? obj) => Equals(obj as Order);

    public override int GetHashCode() => Id.GetHashCode();
}