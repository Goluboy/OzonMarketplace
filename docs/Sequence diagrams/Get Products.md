``` mermaid
sequenceDiagram
    participant Frontend
    participant NGINX
    participant ProductService
    participant PostgreSQL

    Frontend->>NGINX: GET /api/products?page=1&size=20
    NGINX->>ProductService: Proxy to ProductService
    
    ProductService->>PostgreSQL: SELECT * FROM products LIMIT 20 OFFSET 0
    PostgreSQL-->>ProductService: Return 20 products + total count
    ProductService-->>NGINX: 200 OK + { items: [...], totalCount: 150, hasNext: true }
    NGINX-->>Frontend: 200 OK + JSON response
    
    Frontend->>Frontend: Render first 20 products
    Frontend->>Frontend: Enable "Load More" button (hasNext: true)
    
    Note over Frontend: User scrolls to bottom or click 'next page'
    
    Frontend->>NGINX: GET /api/products?page=2&size=20
    NGINX->>ProductService: Proxy to ProductService
    ProductService->>PostgreSQL: SELECT * FROM products LIMIT 20 OFFSET 20
    PostgreSQL-->>ProductService: Return next 20 products
    ProductService-->>Frontend: 200 OK + { items: [...], totalCount: 150, hasNext: true }
    
    Note over Frontend: Continue until hasNext: false
    
```