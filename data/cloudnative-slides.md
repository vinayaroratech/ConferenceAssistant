---
title: Cloud-Native Patterns with .NET
subtitle: Containers, Service Mesh, Observability, Resilience, and GitOps
date: 2026
---

<!-- topic: containers -->
<!-- layout: centered -->
# Cloud-Native Patterns with .NET

## Containers · Service Mesh · Observability · Resilience · GitOps

<!-- speaker: Welcome! We'll go end-to-end from building a container image to deploying it with GitOps. Every pattern is shown with real .NET code. -->

---

<!-- topic: containers -->
# Why Container Size Matters

| Image | Compressed Size |
|---|---|
| `aspnet:10.0` (Debian) | ~120 MB |
| `aspnet:10.0-alpine` | ~42 MB |
| `aspnet:10.0-jammy-chiseled` | ~28 MB |

<!-- speaker: Smaller images pull faster, have a smaller attack surface, and cost less in storage and egress. Chiseled Ubuntu is rootless and has no shell - perfect for production. -->

---

<!-- topic: containers -->
# SDK-Managed Container Builds

```xml
<!-- MyApp.csproj -->
<PropertyGroup>
  <ContainerBaseImage>mcr.microsoft.com/dotnet/aspnet:10.0-jammy-chiseled</ContainerBaseImage>
  <ContainerRepository>myorg/myapp</ContainerRepository>
  <ContainerImageTag>$(Version)</ContainerImageTag>
</PropertyGroup>
```

```bash
dotnet publish --os linux --arch x64 /t:PublishContainer
```

<!-- speaker: No Dockerfile needed for the common case. The SDK knows how to layer your app on the base image using OCI layer caching. -->

---

<!-- topic: containers -->
# Multi-Stage Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-jammy-chiseled AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

<!-- speaker: The build stage contains the full SDK - never shipped to production. The runtime stage is the minimal chiseled image with just your app binaries. -->

---

<!-- topic: containers -->
# Deploying to AKS

```yaml
# deployment.yaml
spec:
  template:
    spec:
      securityContext:
        runAsNonRoot: true
        seccompProfile:
          type: RuntimeDefault
      containers:
      - name: myapp
        image: myorg/myapp:1.0.0
        resources:
          requests: { cpu: "100m", memory: "128Mi" }
          limits:   { cpu: "500m", memory: "256Mi" }
```

<!-- speaker: Always set resource requests and limits. Without them the scheduler can't make good placement decisions and you'll get OOM kills. -->

---

<!-- topic: servicemesh -->
# Service Mesh & Communication

## mTLS, Traffic Management, and DAPR

<!-- speaker: Once you have more than a handful of services, you need a structured answer to: how do they talk securely, and what happens when one is slow? -->

---

<!-- topic: servicemesh -->
# What a Service Mesh Does

```
Service A  ─────►  [Envoy Sidecar]  ──mTLS──►  [Envoy Sidecar]  ─────►  Service B
              │                                          │
              └──── telemetry, retry, circuit break ───┘
```

- **mTLS**: every connection is mutually authenticated
- **Traffic management**: canary weights, timeouts, retries
- **Observability**: traces and metrics from the mesh layer
- Zero application code changes required

<!-- speaker: The sidecar proxy intercepts all network traffic and handles cross-cutting concerns so your app code doesn't have to. -->

---

<!-- topic: servicemesh -->
# DAPR Building Blocks

```csharp
// State - works with Redis, CosmosDB, or any state store
await daprClient.SaveStateAsync("statestore", "order-42", order);

// Pub/Sub - works with Kafka, Service Bus, or RabbitMQ
await daprClient.PublishEventAsync("pubsub", "orders", order);

// Service invocation with retries built in
await daprClient.InvokeMethodAsync("inventory-service", "reserve", item);
```

<!-- speaker: DAPR makes your app portable. Swap the backing store without changing application code. In dev it uses in-memory; in production it points at real infrastructure. -->

---

<!-- topic: observability -->
# Observability with OpenTelemetry

## Traces · Metrics · Logs

<!-- speaker: The goal is correlation - when a request fails, you can trace it from the browser through every service and see exactly where time was spent and what went wrong. -->

---

<!-- topic: observability -->
# One SDK, Three Signals

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(b => b
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(b => b
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter())
    .WithLogging(b => b
        .AddOtlpExporter());
```

<!-- speaker: One AddOpenTelemetry() call wires up traces, metrics, and logs. The OTLP exporter sends everything to Grafana, Jaeger, or Aspire's built-in dashboard. -->

---

<!-- topic: observability -->
# Custom Spans and Metrics

```csharp
// Custom trace span
using var activity = MyActivitySource.StartActivity("ProcessOrder");
activity?.SetTag("order.id", orderId);
activity?.SetTag("order.total", total);

// Custom metric
private static readonly Counter<int> _ordersProcessed =
    Meter.CreateCounter<int>("orders.processed");

_ordersProcessed.Add(1, new TagList { { "region", region } });
```

<!-- speaker: Instrumentation is cheap. Adding spans and metrics to business-critical code paths gives you the context to debug production issues in minutes, not hours. -->

---

<!-- topic: resilience -->
# Resilience Patterns

## Retry · Circuit Breaker · Hedging · Rate Limiting

<!-- speaker: Every distributed system will experience transient failures. The question isn't if - it's whether your code handles them gracefully. -->

---

<!-- topic: resilience -->
# Standard Resilience Handler

```csharp
builder.Services.AddHttpClient<OrderClient>()
    .AddStandardResilienceHandler();
// Adds: retry (3x), circuit breaker, timeout, total timeout
// All configurable, all observable via metrics
```

```csharp
// Or build your own pipeline
builder.Services.AddResiliencePipeline("orders", builder =>
{
    builder
        .AddRetry(new() { MaxRetryAttempts = 3, Delay = TimeSpan.FromMilliseconds(200) })
        .AddCircuitBreaker(new() { SamplingDuration = TimeSpan.FromSeconds(30) })
        .AddTimeout(TimeSpan.FromSeconds(5));
});
```

<!-- speaker: AddStandardResilienceHandler() is the right default for 90% of HttpClient usage. It follows the Polly v8 pipeline model and emits OpenTelemetry metrics automatically. -->

---

<!-- topic: resilience -->
# Circuit Breaker in Action

```
Requests ──► [CLOSED] ──► too many failures ──► [OPEN]
                                                    │
                                              all requests
                                              fail fast
                                                    │
                                          half-open after
                                          break duration
                                                    │
                                        probe succeeds ──► [CLOSED]
```

<!-- speaker: The circuit breaker protects your dependencies. When a downstream service is struggling, failing fast prevents your thread pool from exhausting waiting for timeouts. -->

---

<!-- topic: resilience -->
# Rate Limiting Middleware

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.SegmentsPerWindow = 6;
        opt.PermitLimit = 100;
    });
});

app.UseRateLimiter();
app.MapGet("/orders", GetOrders).RequireRateLimiting("api");
```

<!-- speaker: Rate limiting is now in ASP.NET Core itself - no extra packages. Sliding window is usually right for APIs; token bucket is better for burst workloads. -->

---

<!-- topic: gitops -->
# GitOps & CI/CD Pipelines

## GitHub Actions · Flux CD · Image Promotion · Security

<!-- speaker: GitOps means the cluster's desired state is always stored in git. No manual kubectl applies. The cluster reconciles itself. -->

---

<!-- topic: gitops -->
# GitHub Actions Pipeline

```yaml
jobs:
  build:
    steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with: { dotnet-version: '10.x' }
    - run: dotnet test
    - run: dotnet publish --os linux --arch x64 /t:PublishContainer
    - uses: docker/login-action@v3
    - run: docker push myorg/myapp:${{ github.sha }}
    - uses: fluxcd/flux2-sync-action@v1
      with:
        image-tag: ${{ github.sha }}
```

<!-- speaker: Every push builds, tests, publishes a container, and opens a Flux image update PR. No manual steps, no drift. -->

---

<!-- topic: gitops -->
# Flux CD Image Automation

```yaml
# imagepolicy.yaml
apiVersion: image.toolkit.fluxcd.io/v1beta2
kind: ImagePolicy
metadata:
  name: myapp
spec:
  imageRepositoryRef:
    name: myapp
  policy:
    semver:
      range: ">=1.0.0"
```

- Flux watches your container registry
- Detects new tags matching your policy
- Opens a git PR to update the deployment YAML
- Merging the PR triggers a cluster rollout

<!-- speaker: Flux keeps the cluster in sync with git automatically. You review and approve image promotions the same way you review code - through pull requests. -->
