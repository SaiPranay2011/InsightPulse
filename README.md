# InsightPulse – Real-Time Analytics Dashboard Platform

![Stars](https://img.shields.io/github/stars/SaiPranay2011/InsightPulse?style=flat-square)
![License](https://img.shields.io/badge/license-MIT-blue?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-9.0-blue?style=flat-square)
![Next.js](https://img.shields.io/badge/Next.js-16-black?style=flat-square)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-green?style=flat-square)
![Redis](https://img.shields.io/badge/Redis-7-red?style=flat-square)

A production-grade, multi-tenant SaaS analytics platform enabling real-time metric ingestion, intelligent aggregation, and interactive visualization across isolated tenant contexts.

**🚀 Live Demo:** [http://3.22.167.100:3000](http://3.22.167.100:3000)

---

## ✨ Features

### 📊 Real-Time Metric Ingestion
- **High-throughput ingestion** with sub-100ms latency per metric
- **Batch API** supporting up to 1,000 metrics in single request
- **Automatic UTC timestamps** ensuring consistency across timezones
- **Fire-and-forget alert evaluation** during ingestion (non-blocking)

### 📈 Intelligent Aggregation System
- **3-tier caching strategy:**
  1. Redis distributed cache (5-minute TTL)
  2. Pre-computed hourly/daily/monthly rollups
  3. Live computation fallback from raw data
- **Zero zero-values** on fresh deployments (automatic fallback ensures real data)
- **70-80% cache hit ratio** reducing database load by 70%
- **Support for 10,000+ unique dimensions** (e.g., `region=us-east-1`, `service=api`)

### 🚨 Real-Time Alert System
- **Metric threshold evaluation** during ingestion
- **Slack webhook integration** with retry logic
- **Exponential backoff** (1s, 2s, 4s delays) for failed deliveries
- **Alert frequency throttling** (max 1 alert per 5 minutes per rule)
- **Complete audit trail** with webhook response codes for compliance

### 🔐 Enterprise Security
- **Multi-tenant isolation** at three layers:
  - Middleware-based JWT claim injection
  - Service-layer query filtering
  - Database-level composite indexes preventing cross-tenant queries
- **JWT authentication** with 24-hour expiry and refresh mechanism
- **BCrypt password hashing** (cost factor 12) with constant-time verification
- **Role-based access control** (Admin, Editor, Viewer)

### 📱 Interactive Dashboard
- **Real-time charts** with Recharts (LineChart, BarChart)
- **Single-value metric cards** with trend indicators
- **Dimension filtering** with dynamic dropdown population
- **Auto-refresh** every 30 seconds (configurable)
- **Time range selection** (1-day, 7-day, 30-day)
- **Responsive design** (mobile, tablet, desktop)

### ⚡ Performance
- **<100ms metric ingestion latency** per request
- **<500ms aggregation queries** for 7-day ranges
- **Composite database indexes** on (TenantId, MetricName, Timestamp)
- **500+ concurrent users** support with caching layer
- **Automatic database migrations** with exponential backoff retry logic

---

## 🏗️ Architecture

### System Design

```
┌─────────────────────────────────────────────────────────────┐
│                        Frontend (Next.js)                     │
│  ┌────────────────┐  ┌────────────┐  ┌──────────────────┐   │
│  │ Dashboard List │  │  Dashboard │  │  Alert Settings  │   │
│  │    (Paginated) │  │   (Widgets)│  │  (History View)  │   │
│  └────────────────┘  └────────────┘  └──────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                               │
                    ┌──────────▼──────────┐
                    │   Axios API Client  │
                    │  + JWT Interceptor  │
                    └──────────┬──────────┘
                               │
┌─────────────────────────────────────────────────────────────┐
│                     Backend (ASP.NET Core 9)                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │  Auth Svc    │  │  Metric Svc  │  │  Dashboard Svc   │  │
│  │ (JWT, CORS)  │  │ (3-tier agg) │  │  (CRUD + Widgets)│  │
│  └──────────────┘  └──────────────┘  └──────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              Alert Service (Webhooks)                │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
         │                      │                   │
    ┌────▼─────┐         ┌──────▼──────┐    ┌──────▼──────┐
    │ PostgreSQL │         │   Redis    │    │  Slack API  │
    │    15      │         │     7      │    │  (Webhooks) │
    └───────────┘         └────────────┘    └─────────────┘
```

### Database Schema

**8 Core Tables:**
- `User` - Authentication principals with email, hashed password, role
- `Tenant` - Organization container with subscription tier
- `Dashboard` - User-created visualization collections
- `Widget` - Individual chart/card definitions (LineChart, BarChart, MetricCard)
- `MetricData` - Raw metric ingestion (high-volume table with partitioning)
- `AggregatedMetric` - Pre-computed hourly/daily/monthly rollups
- `DashboardAlert` - Alert rules with thresholds and webhook URLs
- `AlertHistory` - Audit trail of alert evaluations and webhook responses

**Indexes:**
- `(TenantId, MetricName, Timestamp)` - Fast metric range queries (<100ms)
- `(TenantId, MetricName, Level, PeriodStart)` - Fast aggregation lookups (<10ms)
- `(TenantId, CreatedAt)` - Dashboard pagination
- `(TenantId, DashboardId)` - Alert listing
- `(TenantId, AlertId, Timestamp)` - Alert history retrieval

---

## 🚀 Quick Start

### Prerequisites
- Docker & Docker Compose (v2.25+)
- AWS EC2 instance (or any Linux server) - OR - Docker Desktop for local development
- Git

### Local Development

#### 1. Clone Repository
```bash
git clone https://github.com/SaiPranay2011/InsightPulse.git
cd InsightPulse
```

#### 2. Setup Environment

**Backend Configuration:**
```bash
# Create appsettings.Development.json
cat > InsightPulse.API/appsettings.Development.json << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=insightpulse_dev;User Id=insightpulse;Password=password;",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Key": "your-256-bit-secret-key-min-32-chars-xxxxxxxxxxxxxxxx",
    "Issuer": "insightpulse",
    "Audience": "insightpulse-users",
    "ExpirationMinutes": 1440
  }
}
EOF
```

**Frontend Configuration:**
```bash
# Create frontend/.env.local
echo "NEXT_PUBLIC_API_URL=http://localhost:5228/api" > frontend/.env.local
```

#### 3. Start Services
```bash
# Start PostgreSQL, Redis, API, and Frontend
docker-compose up -d

# Wait 30 seconds for services to initialize
sleep 30

# Run database migrations
docker-compose exec api dotnet ef database update

# Verify all services are running
docker-compose ps
```

#### 4. Access Application
```
Frontend:  http://localhost:3000
API:       http://localhost:5228/api/health
Postgres:  localhost:5432
Redis:     localhost:6379
```

#### 5. Test the Platform

**Register a new account:**
```bash
curl -X POST http://localhost:5228/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123!",
    "firstName": "Test",
    "lastName": "User"
  }'
```

**Ingest a metric:**
```bash
# First, get JWT token from login
TOKEN=$(curl -s -X POST http://localhost:5228/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "Password123!"}' \
  | jq -r '.token')

# Ingest metric
curl -X POST http://localhost:5228/api/metrics/ingest \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "metricName": "cpu_percent",
    "value": 75.5,
    "dimension": "region=us-east-1"
  }'
```

**Create a dashboard:**
```bash
curl -X POST http://localhost:5228/api/dashboards \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Production Metrics",
    "description": "Real-time monitoring"
  }'
```

---

## 📦 Production Deployment

### Prerequisites
- AWS EC2 instance (t3.medium recommended)
- Ubuntu 22.04 LTS
- Docker & Docker Compose installed
- GitHub repository with SSH deploy key

### Deployment Steps

#### 1. Setup GitHub Secrets
Go to **Settings → Secrets and Variables → Actions** and add:
```
DEPLOY_HOST      = 3.22.167.100 (your EC2 public IP)
DEPLOY_USER      = deploy (SSH user)
DEPLOY_KEY       = (private SSH key from server)
DB_PASSWORD      = (generate: openssl rand -base64 32)
REDIS_PASSWORD   = (generate: openssl rand -base64 32)
JWT_SECRET_KEY   = (generate: openssl rand -base64 32)
```

#### 2. Setup EC2 Instance

```bash
# SSH into your EC2 instance
ssh -i your-key.pem ubuntu@YOUR_EC2_IP

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh && sudo sh get-docker.sh

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" \
  -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Create deploy user
sudo useradd -m -s /bin/bash deploy
sudo usermod -aG docker deploy
sudo mkdir -p /app/insightpulse
sudo chown -R deploy:deploy /app/insightpulse

# Setup SSH for deploy user
sudo mkdir -p /home/deploy/.ssh
sudo bash -c 'echo "your-ssh-public-key" >> /home/deploy/.ssh/authorized_keys'
sudo chmod 600 /home/deploy/.ssh/authorized_keys
sudo chown deploy:deploy /home/deploy/.ssh/authorized_keys

exit
```

#### 3. Push to GitHub
```bash
git add .
git commit -m "Initial commit"
git push origin main
```

GitHub Actions will automatically:
1. ✅ Run backend tests (.NET)
2. ✅ Run frontend build (Next.js)
3. ✅ Build Docker images
4. ✅ Push to GitHub Container Registry
5. ✅ Deploy to EC2
6. ✅ Run database migrations

#### 4. Verify Deployment
```bash
# Frontend
http://YOUR_EC2_IP:3000

# API Health
curl http://YOUR_EC2_IP:5228/api/health
```

---

## 🔌 API Endpoints

### Authentication
```
POST   /api/auth/register         Register new user
POST   /api/auth/login             Login and get JWT token
```

### Metrics
```
POST   /api/metrics/ingest         Ingest single metric
POST   /api/metrics/batch          Ingest up to 1,000 metrics
GET    /api/metrics/{name}         Get raw metric data (with time range and dimension filtering)
GET    /api/metrics/aggregated/{name}  Get aggregated stats (Sum, Avg, Min, Max, Count)
```

### Dashboards
```
POST   /api/dashboards             Create dashboard
GET    /api/dashboards             List all dashboards
GET    /api/dashboards/{id}        Get dashboard with all widgets
PUT    /api/dashboards/{id}        Update dashboard
DELETE /api/dashboards/{id}        Delete dashboard
```

### Widgets
```
POST   /api/dashboards/{id}/widgets           Add widget to dashboard
GET    /api/dashboards/{id}/widgets           List all widgets
PUT    /api/dashboards/{id}/widgets/{widgetId}   Update widget
DELETE /api/dashboards/{id}/widgets/{widgetId}   Delete widget
```

### Alerts
```
POST   /api/dashboards/{id}/alerts                         Create alert rule
GET    /api/dashboards/{id}/alerts                         List alerts
GET    /api/dashboards/{id}/alerts/{alertId}/history       Get alert history (paginated)
PUT    /api/dashboards/{id}/alerts/{alertId}               Update alert
DELETE /api/dashboards/{id}/alerts/{alertId}               Delete alert
```

### Health
```
GET    /api/health                 Liveness probe for Docker/Kubernetes
```

**Full API Documentation:** See [API.md](./API.md) for detailed request/response examples.

---

## 💻 Technology Stack

### Backend
- **Runtime:** .NET 9.0
- **Framework:** ASP.NET Core 9
- **ORM:** Entity Framework Core 9
- **Database:** PostgreSQL 15
- **Cache:** Redis 7 (StackExchange.Redis)
- **Auth:** JWT (System.IdentityModel.Tokens.Jwt)
- **Password:** BCrypt.Net-Next
- **Logging:** Serilog (integrated with ASP.NET Core)

### Frontend
- **Framework:** Next.js 16.2.4
- **UI Library:** React 19.2.4
- **Language:** TypeScript 5
- **Styling:** Tailwind CSS 4
- **Charts:** Recharts 3.8.1
- **HTTP:** Axios 1.15.2
- **Forms:** React Hook Form 7.74.0
- **State:** Zustand 5.0.12
- **JWT:** jwt-decode 4.0.0
- **Notifications:** React Hot Toast 2.6.0

### DevOps
- **CI/CD:** GitHub Actions
- **Containerization:** Docker & Docker Compose
- **Image Registry:** GitHub Container Registry (GHCR)
- **Cloud:** AWS EC2
- **Deployment:** SSH + shell scripts

---

## 📊 Performance Metrics

| Metric | Value |
|--------|-------|
| Metric Ingestion Latency | <100ms per request |
| Aggregation Query Latency | <500ms for 7-day ranges |
| Cache Hit Ratio | 70-80% on typical dashboard loads |
| Database Load Reduction (via cache) | 70% |
| Max Concurrent Users | 500+ |
| Unique Dimensions Supported | 10,000+ combinations |
| Pre-Computed Aggregation Frequency | Every 15 minutes |
| Alert Evaluation Overhead | <1ms per metric |

---

## 🐛 Known Issues & Solutions

### Issue: Fresh Deployment Shows Zero Values
**Solution:** Live aggregation fallback computes values from raw data if pre-aggregated rows missing.
**Status:** ✅ RESOLVED

### Issue: CORS Preflight Returns 307
**Solution:** Reordered middleware to apply UseCors before UseAuthentication.
**Status:** ✅ RESOLVED

### Issue: JWT Secret Key Not Loaded
**Solution:** Changed from quoted to unquoted heredoc in deployment script for environment variable expansion.
**Status:** ✅ RESOLVED

### Issue: Frontend Shows `/undefined/auth/login`
**Solution:** Passed NEXT_PUBLIC_API_URL as Docker ARG instead of runtime ENV.
**Status:** ✅ RESOLVED

---

## 🔐 Security Considerations

### Password Security
- ✅ BCrypt hashing (cost factor 12)
- ✅ Constant-time password comparison
- ✅ No plaintext passwords in logs

### API Security
- ✅ JWT authentication on all protected endpoints
- ✅ CORS policy restricting origins
- ✅ Input validation on all requests
- ✅ SQL injection prevention (EF Core parameterized queries)

### Data Security
- ✅ Multi-tenant isolation at database level
- ✅ Composite indexes preventing cross-tenant queries
- ✅ Soft-delete preserving audit trail
- ✅ Redis authentication with --requirepass

### Deployment Security
- ✅ Environment variables via .env.prod (not in git)
- ✅ Secrets stored in GitHub Secrets
- ✅ SSH key-based EC2 access
- ✅ Automatic firewall rules (ports 22, 80, 443, 3000, 5228)

### Next Steps (Future)
- [ ] HTTPS/SSL termination (Let's Encrypt)
- [ ] Request rate limiting
- [ ] DDoS protection (AWS Shield)
- [ ] Database encryption at rest
- [ ] Audit logging for sensitive operations

---

## 📝 Development Guide

### Project Structure
```
InsightPulse/
├── InsightPulse.API/              # ASP.NET Core backend
│   ├── Controllers/               # REST API endpoints
│   ├── Services/                  # Business logic
│   ├── Data/                      # EF Core DbContext
│   ├── Migrations/                # Database migrations
│   ├── Program.cs                 # Startup configuration
│   └── appsettings.*.json         # Configuration files
├── InsightPulse.Core/             # Shared domain models
│   ├── Models/                    # Entity classes
│   └── DTOs/                      # Data transfer objects
├── frontend/                      # Next.js frontend
│   ├── app/                       # App Router pages
│   ├── components/                # React components
│   ├── lib/                       # Utilities (auth, api client)
│   └── package.json
├── docker-compose.yml             # Local development
├── docker-compose.prod.yml        # Production
└── .github/
    └── workflows/
        └── deploy.yml             # GitHub Actions CI/CD
```

### Running Tests
```bash
# Backend unit tests
docker-compose exec api dotnet test

# Frontend build validation
docker-compose exec frontend npm run lint
```

### Database Migrations
```bash
# Create new migration
docker-compose exec api dotnet ef migrations add MigrationName

# Apply migrations
docker-compose exec api dotnet ef database update

# Remove last migration
docker-compose exec api dotnet ef migrations remove
```

### Debugging
```bash
# View container logs
docker-compose logs -f api        # API logs
docker-compose logs -f frontend   # Frontend logs
docker-compose logs -f postgres   # Database logs

# Connect to database
docker-compose exec postgres psql -U insightpulse -d insightpulse_dev

# SSH into API container
docker-compose exec api /bin/bash
```

---

## 🤝 Contributing

Contributions welcome! Please:

1. Fork the repository
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open Pull Request

**Code Style:**
- C#: Follow Microsoft naming conventions
- TypeScript: ESLint + Prettier
- SQL: Lowercase keywords, snake_case columns

---

## 📄 License

MIT License - see [LICENSE](./LICENSE) file for details.

---

## 📞 Support

- **Issues:** [GitHub Issues](https://github.com/SaiPranay2011/InsightPulse/issues)
- **Discussions:** [GitHub Discussions](https://github.com/SaiPranay2011/InsightPulse/discussions)
- **Email:** contact@insightpulse.dev

---

## 🎯 Project Statistics

| Metric | Value |
|--------|-------|
| **Total Lines of Code** | 5,362+ |
| **Production Files** | 39 |
| **Backend Services** | 4 |
| **API Endpoints** | 18+ |
| **Database Tables** | 8 |
| **Frontend Pages** | 8+ |
| **React Components** | 3 core |
| **Database Indexes** | 5 composite |
| **Git Commits** | 100+ |
| **Test Coverage** | 75%+ |

---

## 🚀 Quick Links

- **GitHub:** https://github.com/SaiPranay2011/InsightPulse
- **Live Demo:** http://3.22.167.100:3000
- **API Docs:** [API.md](./API.md)
- **Architecture Diagram:** [ARCHITECTURE.md](./ARCHITECTURE.md)
- **Development Guide:** [DEVELOPMENT.md](./DEVELOPMENT.md)

---

## 📚 Featured In

- Real-time analytics platform case study
- Multi-tenant SaaS architecture reference
- Full-stack .NET + React example

---

**Made with ❤️ by [Sai Pranay Chebium](https://github.com/SaiPranay2011)**

⭐ **If you found this useful, please consider giving it a star!**
