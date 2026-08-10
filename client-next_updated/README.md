# MedicSocial Client - Next.js 14 Application

A modern healthcare platform client application built with Next.js 14, TypeScript, React 18, Tailwind CSS, and Redux Toolkit for professional medical networking and job management.

## Features

- ✅ User authentication (Login/Register with JWT tokens)
- ✅ Professional profile management
- ✅ Job browsing and search with filters
- ✅ Job application tracking
- ✅ Verification request management
- ✅ Responsive UI with Tailwind CSS
- ✅ Type-safe API integration with automatic token refresh
- ✅ Redux state management with persistence
- ✅ Protected routes and role-based access

## Tech Stack

- **Frontend Framework**: Next.js 14 with React 18
- **Language**: TypeScript
- **State Management**: Redux Toolkit with redux-persist
- **Styling**: Tailwind CSS with custom components
- **HTTP Client**: Axios with token refresh interceptor
- **UI Components**: Custom components (Button, Input, Card, Alert, Layout)

## Prerequisites

- Node.js 18+ or 20+
- npm or yarn package manager
- Backend microservices running on specified ports

## Installation

1. **Clone the repository** (if not already done):
```bash
cd src/apps/client-next
```

2. **Install dependencies**:
```bash
npm install
# or
yarn install
# or
pnpm install
```

3. **Setup environment variables**:
```bash
# Copy the example environment file
cp .env.local.example .env.local

# Edit .env.local with your API endpoints
# Default values should work if services run on localhost with standard ports
```

## Environment Variables

Configure the `.env.local` file:

```env
# Frontend Configuration
NEXT_PUBLIC_API_URL=http://localhost:3000

# Backend Service URLs (adjust ports if different)
NEXT_PUBLIC_AUTH_API_URL=http://localhost:5001
NEXT_PUBLIC_PROFESSIONAL_API_URL=http://localhost:5002
NEXT_PUBLIC_JOB_API_URL=http://localhost:5003
NEXT_PUBLIC_VERIFICATION_API_URL=http://localhost:5004
NEXT_PUBLIC_EMPLOYER_API_URL=http://localhost:5005

# Feature Flags
NEXT_PUBLIC_ENABLE_MESSAGING=true
```

## Running the Application

### Development Mode

```bash
npm run dev
# or
yarn dev
# or
pnpm dev
```

The application will start on `http://localhost:3000`

### Production Build

```bash
npm run build
npm start
# or
yarn build
yarn start
```

### Linting & Code Quality

```bash
npm run lint
# or
yarn lint
```

## Project Structure

```
client-next/
├── components/           # Reusable UI components
│   ├── Button.tsx
│   ├── Input.tsx
│   ├── Card.tsx
│   ├── Alert.tsx
│   └── Layout.tsx
├── lib/                  # Utility functions
│   └── apiClient.ts      # Axios configuration
├── pages/                # Next.js pages (file-based routing)
│   ├── _app.tsx         # Redux Provider setup
│   ├── login.tsx
│   ├── register.tsx
│   ├── dashboard.tsx
│   ├── jobs/
│   │   ├── index.tsx    # Job listing with search/filter
│   │   └── [id].tsx     # Job detail page
│   ├── professional/
│   │   └── profile.tsx  # Professional profile management
│   └── applications.tsx # Job applications tracking (TODO)
├── store/                # Redux state management
│   ├── authSlice.ts     # Authentication state
│   ├── jobSlice.ts      # Jobs state
│   ├── professionalSlice.ts
│   ├── verificationSlice.ts
│   └── index.ts         # Store configuration
├── types/                # TypeScript interface definitions
│   └── index.ts
├── lib/api/              # API client methods
│   ├── authApi.ts
│   ├── jobApi.ts
│   ├── professionalApi.ts
│   └── verificationApi.ts
├── hooks/                # React hooks
│   └── useAuth.ts       # Authentication hook
├── styles/               # Global styles
├── public/               # Static assets
├── .env.local.example   # Environment variables template
├── .eslintrc.json       # ESLint configuration
├── next.config.js       # Next.js configuration with rewrites
├── tailwind.config.js   # Tailwind CSS configuration
├── tsconfig.json        # TypeScript configuration
└── package.json         # Project dependencies

```

## API Integration

### Authentication Flow

1. User submits login credentials
2. API returns JWT access token and refresh token
3. Tokens stored in Redux with localStorage persistence
4. Axios interceptor automatically refreshes token on 401 response

### Making API Calls

```typescript
import { useAppDispatch, useAppSelector } from '@/store';
import * as jobApi from '@/lib/api/jobApi';

// In a component:
const dispatch = useAppDispatch();

// Call API
const jobs = await jobApi.getJobs(page, limit);

// Or using Redux thunk
dispatch(fetchJobs({ page: 1, limit: 10 }));
```

## Available Pages

| Route | Description | Status |
|-------|-------------|--------|
| `/` | Dashboard home | ✅ Complete |
| `/login` | User login | ✅ Complete |
| `/register` | User registration | ✅ Complete |
| `/jobs` | Job listing and search | ✅ Complete |
| `/jobs/[id]` | Job detail and application | ✅ Complete |
| `/professional/profile` | Professional profile management | ✅ Complete |
| `/applications` | Job applications tracking | 🟡 Pending |
| `/professionals` | Professional discovery | 🟡 Pending |
| `/admin/verification` | Verification request review | 🟡 Pending |
| `/messages` | Messaging interface | 🟡 Pending |

## Key Components

### Authentication Hook
```typescript
import { useAuth, ProtectedRoute } from '@/hooks/useAuth';

// In a component
const { login, register, logout, isAuthenticated } = useAuth();

// Protected component
const MyProtected = () => {
  useRequireAuth();
  return <>Protected content</>;
};
```

### Redux Store
```typescript
import { useAppDispatch, useAppSelector } from '@/store';

const MyComponent = () => {
  const dispatch = useAppDispatch();
  const { user } = useAppSelector(state => state.auth);
  
  return <div>{user?.email}</div>;
};
```

## Backend Service Ports

Ensure these services are running:

| Service | Port | Environment |
|---------|------|-------------|
| Identity API | 5001 | Auth endpoints |
| Professional API | 5002 | Professional management |
| Job API | 5003 | Job CRUD & applications |
| Verification API | 5004 | Verification workflows |
| Employer API | 5005 | Employer management |
| Frontend | 3000 | Next.js app |

## Common Issues & Solutions

### Issue: CORS errors when calling API
**Solution**: The `next.config.js` has rewrites configured. Ensure all API calls go through `/api/` routes which are rewritten to backend services.

### Issue: Token not refreshing automatically
**Solution**: Check that the backend returns `401` status on token expiration. The Axios interceptor in `lib/apiClient.ts` handles refresh automatically.

### Issue: State not persisting on page refresh
**Solution**: Redux-persist is configured in `store/index.ts`. Check browser localStorage to ensure tokens are being saved.

### Issue: Styles not applying
**Solution**: 
- Restart the dev server: `npm run dev`
- Clear `.next` build cache: `rm -rf .next`
- Verify `postcss.config.js` and `tailwind.config.js` are in the project root

## Development Workflow

1. Create feature branch
2. Update API types in `types/index.ts` if needed
3. Add API client methods in `lib/api/*.ts`
4. Create Redux actions if needed in `store/*.ts`
5. Implement UI components and pages
6. Test with running backend services
7. Run ESLint: `npm run lint`

## Docker Support

Build Docker image:
```bash
docker build -t medicsocial-client:latest .
```

Run container:
```bash
docker run -p 3000:3000 medicsocial-client:latest
```

## Contributing

1. Follow TypeScript strict mode
2. Use functional components with hooks
3. Keep components small and reusable
4. Add proper error handling
5. Test with various screen sizes

## Production Deployment

1. Build the application:
```bash
npm run build
```

2. Test the production build locally:
```bash
npm start
```

3. Use Docker or your preferred hosting platform
4. Set environment variables on the hosting platform
5. Ensure backend services are accessible from production environment

## Performance Optimization

- Next.js automatic code splitting
- Tailwind CSS tree-shaking removes unused styles
- Redux-persist prevents unnecessary API calls
- Axios interceptors prevent multiple token refresh requests
- Image optimization with Next.js Image component

## Security

- JWT tokens stored securely in Redux with localStorage persistence
- Automatic token refresh on 401 responses
- Protected routes prevent unauthorized access
- TypeScript strict mode catches type errors
- CSP headers and security headers configured in `next.config.js`

## Support

For issues or questions:
1. Check the backend service logs
2. Verify all services are running on correct ports
3. Check browser console for errors
4. Verify `.env.local` configuration

## License

Part of MedicSocial Platform
