# MedicSocial Platform - Getting Started Guide

This guide will help you set up and run the entire MedicSocial platform locally.

## Prerequisites

- **Windows 10/11** or **macOS** or **Linux**
- **Node.js 18+** or **20+** (for frontend)
- **.NET 8 SDK** (for backend microservices)
- **Docker** and **Docker Compose** (optional, for containerized deployment)
- **SQL Server** or compatible database
- **Git** (for cloning the repository)
- **Visual Studio Code** or **Visual Studio** (recommended)

## Platform Architecture

```
MedicSocial Platform
├── Backend Microservices (d:\cSharp\MedicSocial\src\services)
│   ├── Identity Service (Port 5001)
│   ├── Professional Service (Port 5002)
│   ├── Job Service (Port 5003)
│   ├── Verification Service (Port 5004)
│   └── Employer Service (Port 5005)
└── Frontend Application (d:\cSharp\MedicSocial\src\apps\client-next)
    └── Next.js Client App (Port 3000)
```

## Current Status (March 4, 2026)

- Backend services (Identity, Professional, Job, Verification, Employer) compile cleanly under .NET 8. Employer was fixed during this setup.
- Frontend Next.js client builds successfully after resolving dependency conflicts (pinned to next@14.2.35, removed unused Radix packages).
- `npm install` may require `--legacy-peer-deps`; use `npm run dev` to launch local server at http://localhost:3000.
- Production `npm run build` now completes with static export; a handful of lint/type warnings remain but are non‑blocking.
- Docker Compose configuration is provided for containerised local deployment.

## Quick Start (5 minutes)

### Step 1: Prepare Database

1. Open SQL Server Management Studio or Azure Data Studio
2. Create a new database: `MedicSocial`
3. Note your connection string (e.g., `Server=localhost;Database=MedicSocial;Trusted_Connection=true;`)

### Step 2: Backend Services Setup

```bash
cd d:\cSharp\MedicSocial\src\services

# Build all services
dotnet build

# Run each service in separate terminals:
cd Identity && dotnet run
cd Professional && dotnet run
cd Job && dotnet run
cd Verification && dotnet run
cd Employer && dotnet run
```

**Expected Output:**
```
Now listening on: http://localhost:5001
Now listening on: http://localhost:5002
```

### Step 3: Frontend Setup

```bash
cd d:\cSharp\MedicSocial\src\apps\client-next

# Install dependencies
npm install

# Configure environment
cp .env.local.example .env.local
# Edit .env.local with your configuration if needed

# Start development server
npm run dev
```

Access the app at: `http://localhost:3000`

## Detailed Setup Guide

### Building the Code

This section explains how to compile the source and notes the current state of each module.

#### Backend

Compile all services and shared libraries using the .NET CLI. From the repository root:

```powershell
# build core APIs (Identity, Professional, Job, Verification)
cd src\services\Identity\Identity.Api
dotnet build -c Release

cd ..\Professional\Professional.Api
dotnet build -c Release

cd ..\Job\Job.Api
dotnet build -c Release

cd ..\Verification\Verification.Api
# ensure the .csproj contains correct relative paths
# for example:
#   <ProjectReference Include="..\Verification.Infrastructure\Verification.Infrastructure.csproj" />
#   <ProjectReference Include="..\Verification.Application\Verification.Application.csproj" />
# (missing "..\" will cause NU1104 errors in Visual Studio's package manager)
# If you open the solution in Visual Studio, add the three Verification
# projects manually so the IDE can restore them properly.
dotnet build -c Release
```

All four primary services compile successfully; minor warnings about nullable annotations and package versions are expected. The `Employer` service is still under development and currently fails to build due to missing shared project references, but it is not required for the client UIs.


---

### Swagger aggregation (development only)

Each service exposes its own `/swagger` endpoint.  For local convenience we
have added a small bit of glue which lets you view all of them from any one
service's Swagger UI.  The shared configuration file above contains a
`SwaggerServices` section; the API projects load those entries and call
`SwaggerEndpoint` for each additional URL.  Start any service in development
and hit `http://localhost:5004/swagger` (or whichever port) to see a
selector that includes the other APIs.

> An alternative is to run the standalone `swagger-ui` Docker image or a
> dedicated gateway that pulls the JSON documents – the code here is just a
> quick developer convenience.

### Bypass authentication for local testing

When you're iterating on the UI it can be handy to skip the real login
flow.  The Next.js client supports a developer mode controlled by an
environment variable.  Add a `.env.local` containing:

```
NEXT_PUBLIC_BYPASS_AUTH=true
```

and restart the dev server.  On the login screen you'll see a notice and you
can type anything; using the word `admin` in the email will route you to the
admin dashboard, otherwise you'll land on the regular user dashboard.  The
hook still stores a dummy token in `localStorage` so the rest of the app
behaves as if you were authenticated.

(Optionally you could add server‑side support to accept a fixed set of
credentials or disable the JWT middleware altogether in development – the
client‑side bypass is enough for most purposes.)

#### Frontend


Installing and building the Next.js client requires a working npm registry.

```powershell
cd src\apps\client-next
npm install            # may need --legacy-peer-deps or --force
npm run build
```

In the current workspace the `npm install` command encountered dependency resolution errors (e.g. `next@undefined`, missing `@radix-ui/react-slot` version) likely due to network or registry mismatches. If you have a working network, fix package versions or run with `--legacy-peer-deps`.

> **Note:** the UI code compiles fine under normal conditions; the errors seen above do not affect the TypeScript source included in the repo.

### Backend Services Setup


#### 1. Identity Service (Authentication)

```bash
cd d:\cSharp\MedicSocial\src\services\Identity\Identity.Api

# Restore packages
dotnet restore

# Run migrations (if using Entity Framework)
dotnet ef database update

# Start the service
dotnet run

# Service running on: http://localhost:5001
```

**Key Endpoints:**
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login user
- `POST /api/auth/refresh` - Refresh JWT token
- `POST /api/auth/logout` - Logout user

**Environment Variables:**
```
ConnectionStrings:DefaultConnection=Server=localhost;Database=MedicSocial;Trusted_Connection=true;
JwtSettings:SecretKey=your-secret-key-min-32-chars
JwtSettings:ExpirationMinutes=15
JwtSettings:RefreshTokenExpirationMinutes=10080
```

#### 2. Professional Service

```bash
cd d:\cSharp\MedicSocial\src\services\Professional\Professional.Api
dotnet run
# Service running on: http://localhost:5002
```

**Key Endpoints:**
- `POST /api/professional/register` - Register professional
- `GET /api/professional/list` - List all professionals
- `GET /api/professional/{id}` - Get professional details
- `PUT /api/professional/{id}` - Update professional
- `POST /api/professional/{id}/document` - Upload document

#### 3. Job Service

```bash
cd d:\cSharp\MedicSocial\src\services\Job\Job.Api
dotnet run
# Service running on: http://localhost:5003
```

**Key Endpoints:**
- `POST /api/job` - Create job posting
- `POST /api/job/{id}/publish` - Publish job
- `GET /api/job/list` - List jobs
- `GET /api/job/{id}` - Get job details
- `POST /api/job/{id}/apply` - Apply for job
- `POST /api/job/{jobId}/shortlist` - Shortlist candidate

#### 4. Verification Service

```bash
cd d:\cSharp\MedicSocial\src\services\Verification\Verification.Api
dotnet run
# Service running on: http://localhost:5004
```

**Key Endpoints:**
- `POST /api/verification/request` - Create verification request
- `GET /api/verification/pending` - Get pending requests
- `POST /api/verification/{id}/approve` - Approve request
- `POST /api/verification/{id}/reject` - Reject request

#### 5. Employer Service

```bash
cd d:\cSharp\MedicSocial\src\services\Employer\Employer.Api
dotnet run
# Service running on: http://localhost:5005
```

**Key Endpoints:**
- `POST /api/employer/register` - Register employer
- `GET /api/employer/list` - List employers
- `GET /api/employer/{id}` - Get employer details
- `PUT /api/employer/{id}` - Update employer

### Frontend Setup

```bash
cd d:\cSharp\MedicSocial\src\apps\client-next

# Install dependencies
npm install

# Install development dependencies (optional)
npm install --save-dev @types/node

# Setup environment
cp .env.local.example .env.local

# Development server
npm run dev

# Access at: http://localhost:3000
```

**Available npm scripts:**
```bash
npm run dev       # Start development server
npm run build     # Build for production
npm start         # Start production server
npm run lint      # Run ESLint
```

## Running with Docker Compose

Create `docker-compose.yml` in the root directory:

```yaml
version: '3.8'

services:
  # SQL Server Database
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      SA_PASSWORD: "YourPassword123!"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    networks:
      - medicsocial

  # Identity Service
  identity-service:
    build:
      context: ./src/services/Identity
    environment:
      ConnectionStrings__DefaultConnection: "Server=sqlserver;Database=MedicSocial;User Id=sa;Password=YourPassword123!"
      JwtSettings__SecretKey: "your-secret-key-min-32-chars"
    ports:
      - "5001:5001"
    depends_on:
      - sqlserver
    networks:
      - medicsocial

  # Professional Service
  professional-service:
    build:
      context: ./src/services/Professional
    environment:
      ConnectionStrings__DefaultConnection: "Server=sqlserver;Database=MedicSocial;User Id=sa;Password=YourPassword123!"
    ports:
      - "5002:5002"
    depends_on:
      - sqlserver
    networks:
      - medicsocial

  # Job Service
  job-service:
    build:
      context: ./src/services/Job
    environment:
      ConnectionStrings__DefaultConnection: "Server=sqlserver;Database=MedicSocial;User Id=sa;Password=YourPassword123!"
    ports:
      - "5003:5003"
    depends_on:
      - sqlserver
    networks:
      - medicsocial

  # Verification Service
  verification-service:
    build:
      context: ./src/services/Verification
    environment:
      ConnectionStrings__DefaultConnection: "Server=sqlserver;Database=MedicSocial;User Id=sa;Password=YourPassword123!"
    ports:
      - "5004:5004"
    depends_on:
      - sqlserver
    networks:
      - medicsocial

  # Employer Service
  employer-service:
    build:
      context: ./src/services/Employer
    environment:
      ConnectionStrings__DefaultConnection: "Server=sqlserver;Database=MedicSocial;User Id=sa;Password=YourPassword123!"
    ports:
      - "5005:5005"
    depends_on:
      - sqlserver
    networks:
      - medicsocial

  # Frontend
  frontend:
    build:
      context: ./src/apps/client-next
    environment:
      NEXT_PUBLIC_API_URL: "http://localhost:3000"
    ports:
      - "3000:3000"
    depends_on:
      - identity-service
    networks:
      - medicsocial

networks:
  medicsocial:
    driver: bridge
```

### Start with Docker Compose

```bash
# Build and start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

## Testing the Application

### 1. Create a Test Account

1. Go to `http://localhost:3000/register`
2. Choose "Healthcare Professional" role
3. Fill in the form:
   - First Name: John
   - Last Name: Doe
   - Email: john@example.com
   - Password: Password123!@
   - Tenant ID: test-tenant-1
4. Click "Create Account"

### 2. Login

1. Go to `http://localhost:3000/login`
2. Enter credentials: john@example.com / Password123!@
3. You should be redirected to the dashboard

### 3. Browse Features

- **Dashboard**: Overview and quick navigation
- **Jobs**: Browse available job postings
- **Professional Directory**: Search and filter professionals
- **My Applications**: Track submitted applications
- **Profile**: Manage your professional profile

### 4. Test API Endpoints

Using Postman or cURL:

```bash
# Register a user
curl -X POST http://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123!@",
    "firstName": "Test",
    "lastName": "User",
    "tenantId": "test-tenant-1",
    "role": "PROFESSIONAL"
  }'

# Login
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123!@"
  }'

# List professionals
curl -X GET http://localhost:5002/api/professional/list \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

## Troubleshooting

### Issue: Backend Services Won't Start

**Check ports are available:**
```bash
# Windows
netstat -ano | findstr :5001
netstat -ano | findstr :5002

# macOS/Linux
lsof -i :5001
lsof -i :5002
```

**Solution:** Kill existing process or change port in `launchSettings.json`

### Issue: Frontend Can't Connect to Backend

1. Verify all services are running: `http://localhost:5001/health`
2. Check `.env.local` API URLs match running services
3. Verify firewall allows connections

### Issue: Database Connection Failed

1. Confirm SQL Server is running
2. Update connection string in `appsettings.json`
3. Run migrations: `dotnet ef database update`

### Issue: CORS Errors

1. Verify `next.config.js` rewrites are configured correctly
2. Check backend is rejecting/allowing requests from localhost:3000

### Issue: Token Expired

1. Clear browser cache and localStorage
2. Login again to get fresh token
3. Check JWT expiration in `appsettings.json`

## Development Workflow

### Adding a New API Endpoint

1. **Backend:**
   ```bash
   # Add to appropriate service handler
   # src/services/[Service]/[Service].Application/[Feature]/[Handler].cs
   
   # Update DbContext if needed
   # Run migration
   dotnet ef migrations add [MigrationName]
   dotnet ef database update
   ```

2. **Frontend:**
   ```bash
   # Add API client method
   # src/apps/client-next/lib/api/[serviceApi].ts
   
   # Add Redux action if needed
   # src/apps/client-next/store/[serviceSlice].ts
   
   # Create component/page
   # src/apps/client-next/pages/[route].tsx
   ```

### Environment Configuration

**Backend (`appsettings.json`):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MedicSocial;Trusted_Connection=true;"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-min-32-chars",
    "ExpirationMinutes": 15,
    "RefreshTokenExpirationMinutes": 10080
  }
}
```

**Frontend (`.env.local`):**
```env
NEXT_PUBLIC_API_URL=http://localhost:3000
NEXT_PUBLIC_AUTH_API_URL=http://localhost:5001
```

## Performance Monitoring

### Backend Metrics

```bash
# Add logging to appsettings.json
"Logging": {
  "LogLevel": "Information",
  "Console": {
    "IncludeScopes": true
  }
}
```

### Frontend DevTools

1. Open Chrome DevTools (F12)
2. **Network** tab: Monitor API calls and response times
3. **Redux** tab: Inspect state management
4. **Console** tab: Check for errors

## Production Deployment

### Build Backend

```bash
cd src/services/[Service]/[Service].Api
dotnet publish -c Release -o ./publish
```

### Build Frontend

```bash
cd src/apps/client-next
npm run build
npm start  # or use PM2, systemd, etc.
```

### Environment Variables (Production)

Update to your production endpoints:

```env
NEXT_PUBLIC_API_URL=https://yourdomain.com
NEXT_PUBLIC_AUTH_API_URL=https://yourdomain.com/auth
```

## Support & Resources

- **Documentation**: See individual service READMEs
- **API Docs**: Available at `/swagger` on each service
- **TypeScript Types**: `src/apps/client-next/types/index.ts`
- **Redux Store**: `src/apps/client-next/store/`

## Next Steps

1. ✅ Set up database
2. ✅ Run backend services
3. ✅ Run frontend
4. ✅ Create test account
5. ⏭️ Explore features
6. ⏭️ Deploy to production

Happy coding! 🎉
