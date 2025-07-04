
![image](https://github.com/user-attachments/assets/4f479358-6ad0-458e-8ad5-94254e71f782)

# Trading System

## Project Structure

### Domain  
Contains the core business logic:

- **Entities**: Order, Trade, etc.
- **Enumerations**: OrderSide, OrderType, etc.
- **Matching Engine**: A class or service that maintains an in-memory order book and implements matching logic.

### Infrastructure  
Handles data access and messaging:

- **Data Persistence**: EF Core for SQL, NoSQL, or simple in-memory storage.
- **Messaging Components**: For event publishing (e.g., RabbitMQ, Kafka).

### Web API
Hosts the ASP.NET Core API:

- **Order Controllers**: For submitting and managing orders.
- **Program Startup Configuration**: Sets up the API endpoints and middleware.

### Client
A lightweight React front end for submitting orders and viewing the order book
and trade history.

---

## Key Features

### Domain Model  

#### Entities & Enums  
Define core entities like `Order` and `Trade`, along with enumerations for order attributes.

#### Matching Engine  (Deprecated)
Implements the in-memory order book and matching logic, processing both limit and market orders.
### Infrastructure  
Handles data access and messaging:
- **Data Persistence**: EF Core for SQL, NoSQL, or simple in-memory storage.
- **Messaging Components**: For event publishing (e.g., RabbitMQ, Kafka).
#### Consumer
 Handles order consumption and processing
 
---

## API Endpoints  

### Submit Orders  
- `POST /api/orders/limit` — Submit a limit order.  
- `POST /api/orders/market` — Submit a market order.  

### Order Book & Trades (Optional)  
- `GET /api/orderbook/{instrumentId}` — Retrieve the top levels of the order book.  
- `GET /api/trades/{instrumentId}` — List recent trades.  

### Order Management  
- `GET /api/orders/{orderId}` — Retrieve an order by its ID.  
- `POST /api/orders/{orderId}/cancel` — Cancel an existing order.  

---

## Persistence Layer  

### In-Memory Storage  
- Begin with a simple in-memory store for orders and trades.

### Future Enhancements  
- Plan to integrate EF Core or alternative storage solutions (SQL, NoSQL) for production use.

---

## Testing & Validation  

### Unit Testing  
- Thoroughly test order matching scenarios to validate business logic before API integration.

### Multi-Instrument Support  
- Instead of a single global order book, manage a collection (e.g., a dictionary) mapping each `instrumentId` to its own order book.

---

## Optional Message Bus Integration  

### Event Publishing  
- Publish trade events using RabbitMQ, Kafka, or another messaging system for analytics or external processing.

---

## Front-End / Dashboard  

### Visualization  
- Develop a simple web UI or a real-time dashboard (using SignalR and Blazor) to monitor the order book and trade activity.

---

## Advanced Extensions  

### Risk Checks  
- Add pre-trade checks to ensure users have sufficient capital and do not exceed position limits.

### Additional Order Types  
- Expand support to include market, limit, and stop orders.

### Concurrency & Multi-Threading  
- Enhance performance by allowing multiple threads to feed orders into a thread-safe matching engine.

### Replay / Backtesting  
- Create tools to replay historical orders and trades for performance evaluation and algorithm tuning.

### Algorithmic Orders  
- Implement advanced logic such as Iceberg Orders, VWAP, or other algorithmic strategies.

---

## Getting Started  

### Set Up the Solution  
Create a `.NET 9` solution with the following projects:

1. **Domain**: Define your entities, enums, and matching engine.  
2. **Infrastructure**: Set up data access and messaging.  
3. **WebApi**: Develop the API with controllers and startup configuration.  

### Configure the Domain  
- Implement your in-memory order book and matching logic.

### Build the API  
- Create endpoints for order submission, retrieval, and cancellation.

### Establish Persistence  
- Start with in-memory storage, then plan for integration with EF Core or another database for production.

### Test Thoroughly  
- Write unit tests to cover all order matching scenarios before exposing the API.

### Enhance as Needed  
- Consider integrating a message bus, dashboard, and advanced order types as your system evolves.

---

## Conclusion  

This order matching system is designed to be **modular, scalable, and extendable**. It provides a solid foundation for building a **full-featured trading platform**—from a simple in-memory prototype to a production-ready system with real-time analytics and advanced trading features.


## Future Improvements

- **Stock Information**: Fetch and display stock price data for different instruments.  
- **Login System**: Implement user authentication to track and view trade history.  
- **Trading Dashboard**: Enhance the UI for real-time visualization of trades and order book updates.
- **Scalability, Automation, Logging**: in case that this is widely used, ha.

## Running the Client UI

1. Start the API:
   ```bash
   dotnet run --project src/WebApi
   ```
   The API listens on `http://localhost:5000` by default.

2. Open the client:
   - Serve the static files in `src/Client` using any web server, e.g.
     ```bash
     npx serve src/Client
     ```
   - Alternatively, open `src/Client/index.html` directly in your browser.

Use the forms on the page to submit limit or market orders and to view the
current order book and trade history.
