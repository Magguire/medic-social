# MedConnect Kenya - System Architecture

## High-Level Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        External Clients                          │
│          (Web: Next.js, Mobile, Admin UI - Blazor)              │
└────────────────────┬────────────────────────────────────────────┘
                     │ HTTPS
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                     API Gateway (Kong/nginx)                     │
│              Rate Limiting, SSL Termination, Routing             │
└────────────────────┬────────────────────────────────────────────┘
                     │
        ┌────────────┼────────────┬─────────────┬───────────────┐
        ▼            ▼            ▼             ▼               ▼
    ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────────┐
    │Identity│  │Prof.   │  │Employer│  │Job     │  │Verification│
    │Service │  │Service │  │Service │  │Service │  │Service     │
    └────────┘  └────────┘  └────────┘  └────────┘  └────────────┘
        │            │            │          │            │
        └────────────┴────────────┴──────────┴────────────┘
                     │ HTTP/gRPC
        ┌────────────┴──────────────────┐
        ▼                               ▼
    ┌──────────────┐          ┌──────────────────┐
    │Matching Svc  │          │Subscription Svc  │
    └──────────────┘          └──────────────────┘
        │
    ┌───┴──────┬────────────┬─────────────┬────────────┐
    ▼          ▼            ▼             ▼            ▼
┌─────────┐┌────────┐┌──────────┐┌────────────┐┌────────┐
│Messaging││Notif.  ││Audit Svc ││File Svc    ││Analytics│
│Service  ││Service ││          ││            ││Service   │
└─────────┘└────────┘└──────────┘└────────────┘└────────┘
    │         │           │           │            │
    └─────────┴───────────┴───────────┴────────────┘
                     │
                     │ events
        ┌────────────┴────────────┐
        ▼                         ▼
    ┌──────────────┐      ┌──────────────┐
    │  RabbitMQ    │      │    Redis     │
    │(Event Bus)   │      │ (Cache+Rate  │
    └──────────────┘      │  Limiting)   │
                          └──────────────┘
        │
    ┌───┴────────────────┐
    ▼                    ▼
┌──────────────────────────────────────┐
│    Shared Database Layer (EF Core)   │
│  - Provider: SQL Server/PG/MySQL     │
│  - Tenant Isolation (TenantId filter)│
│  - Audit Tables (partitioned)        │
└──────────────────────────────────────┘
    │
    ├─ Identity DB       │
    ├─ Professional DB   │
    ├─ Employer DB       │
    ├─ Job DB            │
    ├─ Audit DB          │
    └─ Shared Svc DB     │

```

## Service Responsibilities

| Service | Key Functions |
|---------|---------------|
| **Identity** | User auth, JWT, tenant admin, RBAC |
| **Professional** | Onboarding, document uploads, verification status |
| **Employer** | Company profile, KRA validation, subscription mgmt |
| **Job** | Job posting, filter criteria, auto-shortlisting |
| **Matching** | Resume parsing, scoring, candidate ranking |
| **Verification** | External board APIs, async workflows |
| **Subscription** | Plans, feature gating, billing readiness |
| **Messaging** | Real-time chat, notifications, scheduling |
| **Notification** | Email, SMS, push delivery |
| **Audit** | Log aggregation, querying, compliance |
| **File** | Document storage, signed URL generation |
| **Analytics** | Dashboards, reports, metrics |

## Authentication & Authorization Flow

```
┌──────────────┐
│ Client Login │
└──────┬───────┘
       │ POST /auth/login { email, password }
       ▼
┌──────────────────────────────────┐
│ Identity Service - LoginHandler  │
│  1. Validate user credentials    │
│  2. Verify tenant access         │
│  3. Generate JWT (15 min exp)    │
│  4. Generate Refresh Token       │
│  5. Store refresh token (hashed) │
└──────┬───────────────────────────┘
       │ Return: { accessToken, refreshToken }
       ▼
┌──────────────────────┐
│ Client stores tokens │
│ - accessToken: mem   │
│ - refreshToken: HTTP-only cookie
└──────────────────────┘

When accessToken expires:
┌─────────────────────────────┐
│ POST /auth/refresh-token    │
│ { refreshToken }            │
└──────┬──────────────────────┘
       ▼
┌──────────────────────────────┐
│ Identity Service             │
│ 1. Validate refresh token    │
│ 2. Check device ID match     │
│ 3. Revoke old refresh token  │
│ 4. Issue new pair (rotated)  │
└──────┬───────────────────────┘
       │ Return: { accessToken, refreshToken }
       ▼
┌──────────────────────────┐
│ Client updates tokens    │
└──────────────────────────┘
```

## Multi-Tenancy Architecture

```
┌───────────────────────────────────────────────┐
│        Request from Client                    │
│    Header: X-Tenant-Id: {GUID}               │
└────────────────┬────────────────────────────┘
                 │
                 ▼
        ┌──────────────────────┐
        │ TenantMiddleware     │
        │ - Resolve TenantId   │
        │ - Populate context   │
        └──────────┬───────────┘
                   │
                   ▼
        ┌──────────────────────────────────┐
        │ Authorization Policy             │
        │ - Check Role + Permissions       │
        │ - Verify TenantId in claims      │
        └──────────┬───────────────────────┘
                   │
                   ▼
        ┌──────────────────────────────────┐
        │ DbContext Setup                  │
        │ - GlobalFilter: EF.Property      │
        │   .HasValue(c.TenantId)          │
        └──────────┬───────────────────────┘
                   │
                   ▼
        ┌──────────────────────────────────┐
        │ Handler / Repository             │
        │ - All queries scoped to TenantId │
        │ - No cross-tenant data leaks     │
        └──────────────────────────────────┘
```

## Audit Logging

```
┌──────────────────────────────────────────┐
│ Application Event (e.g., JobCreated)     │
└────────────┬─────────────────────────────┘
             ▼
    ┌──────────────────────┐
    │ Domain Event Raised  │
    └────────┬─────────────┘
             ▼
    ┌──────────────────────┐
    │ EF Interceptor       │
    │ Captures CRUD ops    │
    └────────┬─────────────┘
             ▼
    ┌──────────────────────────────────┐
    │ AuditLog Entity Created          │
    │ - TenantId                       │
    │ - UserId                         │
    │ - Action (Create/Update/Delete)  │
    │ - EntityName, EntityId           │
    │ - OldValues, NewValues           │
    │ - Timestamp                      │
    └────────┬─────────────────────────┘
             ▼
    ┌──────────────────────┐
    │ Serilog Sink         │
    │ → File / Seq / Rabbit│
    └────────┬─────────────┘
             ▼
    ┌──────────────────────┐
    │ Audit DB             │
    │ (Partitioned tables) │
    │ (>1 year retention)  │
    └──────────────────────┘
```

## Database Schema Example (Auditing)

```sql
-- Partitioned by TenantId and Timestamp
CREATE TABLE AuditLog (
    Id BIGINT PRIMARY KEY,
    TenantId GUID NOT NULL,
    UserId GUID,
    Action NVARCHAR(50),          -- Create, Update, Delete, Login, TokenGenerated
    EntityName NVARCHAR(100),
    EntityId NVARCHAR(MAX),
    Changes NVARCHAR(MAX),         -- JSON differences
    Timestamp DATETIME2 NOT NULL,
    IpAddress NVARCHAR(45),
    UserAgent NVARCHAR(500)
) PARTITION BY HASH (TenantId);

-- Sample rows:
-- 1, GUID-123, USER-456, "Create", "Job", "JOB-789", '{"title":"Nurse","salary":50000}', 2026-03-03, 192.168.1.1, "Mozilla..."
-- 2, GUID-123, USER-456, "TokenGenerated", "RefreshToken", "RT-001", '{"deviceId":"mobile"}', 2026-03-03, 192.168.1.1, "Mozilla..."
```

## Deployment: Docker Compose (Development)

```yaml
version: '3.8'
services:
  # Databases
  postgres:
    image: postgres:15
    ports: ["5432:5432"]
    env: POSTGRES_PASSWORD=dev_password

  # Message Bus
  rabbitmq:
    image: rabbitmq:3.12-management
    ports: ["5672:5672", "15672:15672"]

  # Cache
  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]

  # Services
  identity-api:
    build: ./src/Services/Identity/Identity.Api
    ports: ["5001:5001"]
    depends_on: [postgres, rabbitmq]
    environment:
      Database__Provider: Postgres
      Database__ConnectionStrings__Postgres: "Host=postgres;..."

  professional-api:
    build: ./src/Services/Professional/Professional.Api
    ports: ["5002:5002"]
    depends_on: [postgres, rabbitmq]

  # ... more services
```

## Scaling Strategy

```
Horizontal Scaling:
┌─────────────────┐
│  Load Balancer  │
└────────┬────────┘
         │
    ┌────┴────┬──────┐
    ▼         ▼      ▼
┌───────┐┌───────┐┌───────┐
│Pod 1  ││Pod 2  ││Pod 3  │
│Identity││Identity││Identity│
└────┬──┘└───┬───┘└───┬───┘
     │       │        │
     └───────┴────────┘
           │
        ┌──────────────┐
        │ Shared RabbitMQ
        │ Redis       │
        │ Database    │
        └──────────────┘

Each service pod is stateless:
- Token validation via JWT (no session state)
- Cache layer (Redis) for frequently accessed data
- Database connection pooling
- Horizontal scaling on demand (K8s HPA)
```
