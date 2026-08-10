# MedSocial2

MedSocial2 is the new full product home for the healthcare jobs platform. It consolidates the old multi-service startup model into one backend host, one Aspire AppHost, and three UI surfaces that support public job browsing, professional onboarding, employer onboarding, admin operations, and platform configuration.

## Structure

- `src/MedSocial2.Api`: single modular-monolith ASP.NET Core backend host
- `src/MedSocial2.AppHost`: Aspire orchestration host for local startup, dashboard, Prometheus, and Grafana
- `apps/client-next`: public, professional, and employer-facing Next.js app
- `apps/admin-next`: operations/admin Next.js app
- `apps/admin-blazor`: heavier configuration and super-admin console
- `docs`: architecture and observability assets
- `tests`: future MedSocial2-specific test projects

## Backend modules

- `Identity`
- `Professional`
- `Employer`
- `Job`
- `Verification`
- `Matching`

## Product direction

- Anonymous users can browse jobs without signing in.
- Professionals register to apply, complete biodata and qualifications, and upload proof documents for verification.
- Employers register facilities, upload business documents, and create jobs under configurable subscription entitlements.
- Admins configure document requirements, verification policies, subscription plans, categories, and operational rules.
