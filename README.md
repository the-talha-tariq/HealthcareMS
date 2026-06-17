# HealthcareMS — Microservices Healthcare Management Platform

HealthcareMS is a healthcare management system built on **.NET 8**, designed around a microservices architecture. Patient management, appointments, identity, and notifications are each handled by independently deployable services that communicate through an API Gateway and asynchronous messaging — rather than as a single monolithic application.

This project was built to apply real-world distributed systems patterns: database-per-service, event-driven communication, centralized authentication at the gateway, and dead-letter handling for failed background processing.

---

## Architecture Overview

```
                        ┌─────────────────────┐
                        │   Blazor Server Web   │
                        │   (HealthcareMS.Web)  │
                        └──────────┬───────────┘
                                   │ HTTP (Bearer token attached by
                                   │ custom message handler)
                                   ▼
                        ┌──────────────────────┐
                        │     API Gateway       │
                        │       (Ocelot)        │
                        │  - JWT validation     │
                        │  - Request routing    │
                        └──────────┬───────────┘
              ┌────────────────────┼────────────────────┐
              ▼                    ▼                     ▼
     ┌─────────────────┐ ┌──────────────────┐  ┌──────────────────┐
     │ IdentityService  │ │  PatientService   │  │ AppointmentService│
     │  - Auth/Login     │ │  - Patient CRUD    │  │  - Booking logic   │
     │  - JWT issuance   │ │  - Soft delete     │  │  - Calls Patient   │
     │  - BCrypt hashing │ │                    │  │    Service to      │
     └────────┬─────────┘ └─────────┬──────────┘  │    validate patient │
              │                     │              │  - Publishes events │
              ▼                     ▼              └─────────┬──────────┘
     ┌─────────────────┐ ┌──────────────────┐                │
     │ HealthcareMS_     │ │ HealthcareMS_      │                │ appointment.booked
     │ IdentityDB        │ │ PatientDB          │                ▼
     └─────────────────┘ └──────────────────┘       ┌──────────────────┐
                                                       │     RabbitMQ      │
                                                       └─────────┬────────┘
                                                                 │ consumes
                                                                 ▼
                                                       ┌──────────────────┐
                                                       │ NotificationService│
                                                       │  - Sends email     │
                                                       │  - Retry queue     │
                                                       │  - Dead-letter queue│
                                                       └──────────────────┘
```

Each core service owns its own SQL Server database, following the **database-per-service** pattern — there is no shared schema between services.

---

## Services

| Service | Responsibility |
|---|---|
| **HealthcareMS.Web** | Blazor Server frontend. Handles login, patient management, and appointment UI. Talks only to the API Gateway, never directly to backend services. |
| **ApiGateway** | Ocelot-based reverse proxy and BFF layer. Validates JWTs (issuer, audience, expiry, signature) before forwarding requests downstream. Provides one public surface for the frontend. |
| **IdentityService** | Handles registration, login, BCrypt password hashing, and JWT issuance with user claims (name, email, role, expiry). |
| **PatientService** | CRUD and soft-delete for patient records. Owns `HealthcareMS_PatientDB`. |
| **AppointmentService** | Booking and cancellation logic. Validates patient existence via PatientService, owns `HealthcareMS_AppointmentDB`, and publishes `appointment.booked` events to RabbitMQ. |
| **NotificationService** | Background consumer. Listens for booking events and sends confirmation emails. Implements retry and dead-letter queue handling for failed deliveries. |
| **Shared.Contracts** | Shared DTOs and event contracts used across services. |

---

## Key Design Decisions

**API Gateway as single entry point.** The frontend never calls a backend service directly — every request flows through Ocelot, which centralizes JWT validation and hides internal service URLs. This keeps the frontend simple (it only needs to know the gateway address) and keeps auth logic in one place instead of duplicated across services.

**Hybrid authentication.** The frontend uses cookie-based sessions (`ASP.NET Core Cookie Authentication`) for a normal Blazor login experience, while backend calls use stateless JWT Bearer tokens. A custom `HttpClient` message handler reads the JWT from the authenticated user's claims and attaches it as a Bearer token on outgoing API calls — so the rest of the app doesn't need to think about token plumbing.

**Asynchronous notification flow.** When an appointment is booked, `AppointmentService` doesn't wait for an email to send. It persists the appointment, publishes an `appointment.booked` event to RabbitMQ, and returns immediately. `NotificationService` consumes the event independently, with retry and dead-letter queues so a failed email doesn't block or lose the underlying appointment.

**Database-per-service.** `IdentityService`, `PatientService`, and `AppointmentService` each own a separate SQL Server database. This avoids cross-service schema coupling and allows each service to evolve its data model independently.

---

## Tech Stack

**Backend:** .NET 8 Web API, ASP.NET Core, Entity Framework Core, SQL Server, JWT Bearer Authentication, Ocelot API Gateway, RabbitMQ, Background Services, Swagger/OpenAPI

**Frontend:** Blazor Server, Razor Components, Interactive Server Rendering, ASP.NET Core Cookie Authentication

**Patterns:** Repository Pattern, Service Layer Pattern, DTO-based contracts, Dependency Injection, layered architecture (Controller → Service → Repository → Data Access)

**Security:** JWT authentication, BCrypt password hashing, role-based authorization, gateway-level token validation

---

## Project Structure

```
HealthcareMS/
├── HealthcareMS.Web/         # Blazor Server frontend
├── ApiGateway/                # Ocelot API Gateway
├── IdentityService/            # Auth, JWT issuance
├── PatientService/             # Patient CRUD
├── AppointmentService/         # Booking + event publishing
├── NotificationService/        # Event consumer + email
└── Shared.Contracts/           # Shared DTOs/events
```

---

## Roadmap

The following are planned but not yet implemented:

- Docker containerization for all services + Docker Compose for local orchestration
- Azure deployment (Container Apps, Azure SQL, Key Vault, Application Insights)
- Centralized logging and distributed tracing
- Refresh token support and service-to-service authentication
- SignalR for real-time notifications
- Unit and integration test coverage
- CI/CD pipeline (GitHub Actions / Azure DevOps)
- Health check endpoints and API versioning

---

## Getting Started

> Local setup instructions — update with your actual connection strings, RabbitMQ config, and run order once finalized.

1. Clone the repository
2. Restore each service: `dotnet restore`
3. Update connection strings in each service's `appsettings.json`
4. Apply EF Core migrations per service: `dotnet ef database update`
5. Ensure RabbitMQ is running locally (or update connection settings)
6. Run services in order: IdentityService → PatientService → AppointmentService → NotificationService → ApiGateway → HealthcareMS.Web

---

## License

MIT
