# ZenGear

> E-commerce platform for computer parts and gaming gear

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17+-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)

---

## 🚀 Tech Stack

- **Backend:** .NET 10, C# 14, Clean Architecture + CQRS
- **Database:** PostgreSQL 17+, Entity Framework Core 10
- **Cache:** Redis
- **Authentication:** JWT + Google OAuth

---

## 📋 Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 17+](https://www.postgresql.org/download/)
- [Redis](https://redis.io/download) (optional)

---

## 📁 Project Structure

```
ZenGear/
├── src/
│   ├── ZenGear.Domain/           # Entities, Value Objects, Events
│   ├── ZenGear.Application/      # Use Cases, DTOs, CQRS
│   ├── ZenGear.Infrastructure/   # EF Core, Repositories
│   └── ZenGear.Api/              # Controllers, Middleware
├── tests/
│   ├── ZenGear.Domain.Tests/
│   ├── ZenGear.Application.Tests/
│   └── ZenGear.Infrastructure.Tests/
├── Directory.Build.props         # Common build properties
├── Directory.Packages.props      # Centralized package versions
├── .gitignore
└── README.md
```

---

## 📚 Key libraries

### Architecture & Design Patterns
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) - Robert C. Martin
- [Domain-Driven Design](https://www.domainlanguage.com/ddd/) - Eric Evans
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html) - Martin Fowler

### .NET & EF Core
- [.NET 10 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [MediatR](https://github.com/jbogard/MediatR) - CQRS implementation
- [FluentValidation](https://docs.fluentvalidation.net/)
- [AutoMapper](https://docs.automapper.org/)

### Database
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Npgsql - .NET PostgreSQL](https://www.npgsql.org/doc/)

### Libraries
- [NanoId](https://github.com/codeyu/nanoid-net) - Secure ID generation
- [Serilog](https://serilog.net/) - Structured logging

---

<div align="center">

**Clean Architecture & .NET 10**

</div>