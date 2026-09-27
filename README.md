# IP2C Web API

A clean, modern **.NET 9** Web API service that maps IP addresses to countries using [ip2c.org](https://ip2c.org), persists records in SQL Server via Entity Framework Core 9, maintains an in-memory LRU cache, and runs an automated hourly background renewal service.

---

## Features

- **RESTful Endpoints**: Look up IP addresses and generate aggregated country reports.
- **Multi-Tiered Lookups**: In-memory LRU Cache $\rightarrow$ Local SQL Database $\rightarrow$ Upstream [ip2c.org](https://ip2c.org).
- **In-Memory LRU Cache**: Implemented using .NET 9's generic `OrderedDictionary<string, IpInfoDTO>` with thread-safe reads, promotion on access, and bounded size eviction.
- **Background Worker**: Built with `BackgroundService`, updating database entries and cache every hour using keyset pagination and controlled parallel HTTP requests.
- **Clean Architecture & Dependency Inversion**: Loose coupling with `IIp2cRepository`, `IGeoIpService`, and `ICacheService`.
- **RFC 7807 Problem Details**: Standardized structured error responses for bad IPs, invalid country codes, and upstream outages.
- **Fully Unit Tested**: Comprehensive test suite with xUnit and Moq.

---

## API Endpoints

### 1. Lookup IP Address
```http
GET /api/ip/{ip}
```

**Example Request:**
```http
GET /api/ip/8.8.8.8
```

**Response (`200 OK`):**
```json
{
  "twoLetterCode": "US",
  "threeLetterCode": "USA",
  "countryName": "United States of America (the)"
}
```

**Error Responses:**
- `400 Bad Request`: Invalid IPv4 or IPv6 address.
- `404 Not Found`: IP not found in database or upstream service.
- `502 Bad Gateway`: Upstream service outage or rate-limiting.
- `503 Service Unavailable`: Connection failure to upstream service.

---

### 2. Country Report
```http
GET /api/ip/report
GET /api/ip/report?countryCodes=US,GR
```

Supports both repeated parameters (`?countryCodes=US&countryCodes=GR`) and comma-separated lists (`?countryCodes=US,GR`).

**Response (`200 OK`):**
```json
[
  {
    "name": "United States",
    "addressesCount": 11,
    "lastAddressUpdated": "2026-09-27T08:46:10.7074186Z"
  },
  {
    "name": "Greece",
    "addressesCount": 3,
    "lastAddressUpdated": "2026-09-27T08:46:10.5000000Z"
  }
]
```

---

## Database Setup

The database schema and seed data are in [`IP2C_WebAPI/sample_db.sql`](IP2C_WebAPI/sample_db.sql).

To initialize or reset the database in SQL Server (e.g., `.\SQLEXPRESS`):
```powershell
sqlcmd -S .\SQLEXPRESS -d master -i IP2C_WebAPI/sample_db.sql
```

---

## Running the Application & Tests

### Run Tests:
```powershell
dotnet test
```

### Run Web API:
```powershell
dotnet run --project IP2C_WebAPI/IP2C_WebAPI.csproj
```

Open Swagger UI in development: `http://localhost:5292/swagger`
