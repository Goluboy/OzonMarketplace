``` mermaid
sequenceDiagram
    participant Frontend
    participant NGINX
    participant OrderService
    participant ProductService
    participant PriceService
    participant Kafka
    participant Saga_Timer

    Frontend->>NGINX: POST /orders (JWT token)
    NGINX->>OrderService: Forward request (validated JWT)
    
    OrderService->>OrderService: Create Order (Draft status)
    OrderService->>Saga_Timer: Start 30s timeout for OrderId
    OrderService->>Kafka: Publish OrderCreated (with MessageId)
    
    Kafka->>ProductService: Consume OrderCreated
    ProductService->>ProductService: Check if already processed MessageId
    alt Already Processed
        ProductService->>ProductService: Ignore duplicate
    else New Message
        ProductService->>ProductService: Reserve Stock
        alt Stock Available
            ProductService->>Kafka: Publish StockReserved (with same MessageId)
        else Insufficient Stock
            ProductService->>Kafka: Publish StockReservationFailed (with same MessageId)
        end
    end
    
    Kafka->>PriceService: Consume StockReserved  
    PriceService->>PriceService: Check if already processed MessageId
    alt Already Processed
        PriceService->>PriceService: Ignore duplicate
    else New Message
        PriceService->>PriceService: Calculate Final Price
        PriceService->>Kafka: Publish PriceCalculated (with same MessageId)
    end
    
    Kafka->>OrderService: Consume StockReserved
    OrderService->>OrderService: Check order status
    alt Order still Draft
        OrderService->>OrderService: Mark stock confirmed
        OrderService->>OrderService: Check if all services responded
        alt All responses received
            OrderService->>OrderService: Confirm Order (Confirmed status)
            OrderService->>Saga_Timer: Cancel timeout
        end
    end
    
    Kafka->>OrderService: Consume StockReservationFailed
    OrderService->>OrderService: Check order status
    alt Order still Draft
        OrderService->>OrderService: Cancel Order (Failed status)
        OrderService->>Kafka: Publish OrderCancelled (compensation)
        OrderService->>Saga_Timer: Cancel timeout
    end
    
    Kafka->>OrderService: Consume PriceCalculated
    OrderService->>OrderService: Store price info (non-blocking)
    
    Saga_Timer->>OrderService: Timeout (30s elapsed)
    OrderService->>OrderService: Check order status
    alt Order still Draft
        OrderService->>OrderService: Cancel Order (Timeout status)
        OrderService->>Kafka: Publish OrderCancelled (compensation)
    end
    
    Kafka->>ProductService: Consume OrderCancelled
    ProductService->>ProductService: Release reserved stock (compensation)
    
    OrderService->>NGINX: Return final response
    NGINX->>Frontend: Order result
```
