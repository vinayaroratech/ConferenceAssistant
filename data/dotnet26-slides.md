---
title: .NET 10 Deep Dive
subtitle: C# 14, Performance, ASP.NET Core 10, and New Runtime APIs
date: 2026
---

<!-- topic: csharp14 -->
<!-- layout: centered -->
# .NET 10 Deep Dive

## C# 14 · Performance · ASP.NET Core 10 · Runtime APIs

<!-- speaker: Welcome! This session covers what's new across the entire .NET 10 stack - from language features down to the runtime. -->

---

<!-- topic: csharp14 -->
# C# 14 Highlights

## The Language Keeps Getting Sharper

<!-- speaker: C# 14 ships with .NET 10 in November 2026. Focus on the field keyword first - it solves a long-standing pain point. -->

---

<!-- topic: csharp14 -->
# The `field` Keyword

```csharp
public class Order
{
    public decimal Total
    {
        get;
        set => field = value > 0 ? value
                     : throw new ArgumentOutOfRangeException();
    }
}
```

<!-- speaker: No more manual backing field just to add validation. 'field' accesses the compiler-generated backing field directly inside the accessor. -->

---

<!-- topic: csharp14 -->
# Extension Members

```csharp
extension StringExtensions for string
{
    public bool IsEmail => Contains('@') && Contains('.');
    public static string Placeholder => "(none)";
}

// Usage
"hello@world.com".IsEmail  // true
string.Placeholder          // "(none)"
```

<!-- speaker: Extension members let you add properties, indexers, and static members - not just methods. This is bigger than it looks. -->

---

<!-- topic: csharp14 -->
# params Collections

```csharp
void Log(params IEnumerable<string> messages) { ... }
void Send(params ReadOnlySpan<byte> data) { ... }

// All work:
Log("a", "b", "c");
Log(["a", "b"]);
Log(myList);
```

<!-- speaker: params now works with any collection type - Span, IEnumerable, custom types. No more ToArray() ceremony. -->

---

<!-- topic: performance -->
# Performance in .NET 10

## Faster by Default

<!-- speaker: .NET 10 ships benchmarks showing 15-40% throughput gains on common workloads. Let's look at why. -->

---

<!-- topic: performance -->
# Dynamic PGO - Smarter Than Ever

- Collects real runtime profiles during warmup
- JIT uses those profiles to optimise **your** hot paths
- **On by default** since .NET 8 - now even more accurate
- Works with tiered compilation and OSR

<!-- speaker: Dynamic PGO is the single biggest bang-for-buck perf win in the last few releases. Most apps see 5-20% improvement with zero code changes. -->

---

<!-- topic: performance -->
# Native AOT - Now Mainstream

```bash
dotnet publish -r linux-x64 -p:PublishAot=true
```

- Startup: **< 10 ms**
- Memory: **50-80% less** than JIT
- Single self-contained binary
- Supported: ASP.NET minimal APIs, console, gRPC

<!-- speaker: AOT is no longer a niche feature. In .NET 10 the BCL trimming annotations are complete and most NuGet packages support it. -->

---

<!-- topic: performance -->
# AVX-512 Auto-Vectorisation

```csharp
// The JIT automatically vectorises this loop on AVX-512 hardware
float DotProduct(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
{
    float sum = 0;
    for (int i = 0; i < a.Length; i++) sum += a[i] * b[i];
    return sum;
}
// 16x float32 per instruction on AVX-512
```

<!-- speaker: If you're on a Sapphire Rapids or Zen 4 CPU you're already benefiting from this in .NET 10 without changing a line of code. -->

---

<!-- topic: aspnet10 -->
# ASP.NET Core 10

## Web APIs, Blazor, and Identity

<!-- speaker: Three big themes: built-in OpenAPI, Blazor streaming, and passkeys via Identity v3. -->

---

<!-- topic: aspnet10 -->
# Built-In OpenAPI 3.1

```csharp
// No more Swashbuckle!
builder.Services.AddOpenApi();
app.MapOpenApi();    // /openapi/v1.json

app.MapGet("/orders/{id}", (int id) => Results.Ok(new Order(id)))
   .WithName("GetOrder")
   .WithSummary("Retrieve a single order by ID");
```

<!-- speaker: OpenAPI document generation is now a first-class feature of ASP.NET Core. Zero extra packages for most scenarios. -->

---

<!-- topic: aspnet10 -->
# Identity v3 - Passkeys

```csharp
builder.Services.AddIdentityCore<User>()
    .AddPasskeys()          // WebAuthn/FIDO2
    .AddEntityFrameworkStores<AppDbContext>();
```

- Browser/OS generates a public-key credential
- No passwords stored anywhere
- Works with biometrics, hardware keys, and phone confirmation

<!-- speaker: Passkeys are the future of authentication. Identity v3 makes them a one-liner. -->

---

<!-- topic: runtimeapis -->
# New Runtime & Library APIs

## Tensor<T>, LINQ++, TimeProvider, Cryptography

<!-- speaker: The BCL got a lot of new APIs in .NET 10. Let's hit the highlights. -->

---

<!-- topic: runtimeapis -->
# System.Numerics.Tensors

```csharp
using System.Numerics.Tensors;

Tensor<float> a = Tensor.Create<float>([2, 3]); // 2×3 matrix
Tensor<float> b = Tensor.Create<float>([3, 4]); // 3×4 matrix

var result = Tensor.MatMul(a, b);               // 2×4 output
```

- Strongly typed multi-dimensional arrays
- Hardware-accelerated via SIMD internally
- Foundation for on-device AI in .NET

<!-- speaker: Tensor<T> bridges the gap between .NET and the ML ecosystem. Combined with Microsoft.Extensions.AI you get a complete on-device AI stack. -->

---

<!-- topic: runtimeapis -->
# LINQ Improvements

```csharp
var sales = orders.CountBy(o => o.Region);
// Dictionary<string, int> by region

var totals = orders.AggregateBy(
    o => o.Region,
    seed: 0m,
    (acc, o) => acc + o.Total);
// Dictionary<string, decimal>

var indexed = items.Index();
// (int Index, T Item) pairs
```

<!-- speaker: CountBy and AggregateBy eliminate a whole class of GroupBy + ToDictionary patterns. Index() replaces the classic Select((x,i) => ...) idiom. -->

---

<!-- topic: devtools -->
# Developer Tooling

## SDK, Aspire, Hot Reload, NuGet

<!-- speaker: Last topic - the tools you use every day got meaningfully better in .NET 10. -->

---

<!-- topic: devtools -->
# New Build Output Layout

```
MySolution/
  artifacts/
    bin/
      MyApp/debug/
      MyApp/release/
    obj/
      MyApp/
    publish/
      MyApp/linux-x64/
```

```xml
<!-- Directory.Build.props -->
<UseArtifactsOutput>true</UseArtifactsOutput>
```

<!-- speaker: One opt-in property gives you a clean, predictable output layout. No more digging through nested bin/obj folders. -->

---

<!-- topic: devtools -->
# .NET Aspire 9

- **Dashboard v2**: resource graph, structured log search, trace waterfall
- **Azure deployment wizard**: push to ACA with one command
- **New integrations**: Kafka, RabbitMQ, Azure Service Bus, OpenAI
- **Testing support**: DistributedApplicationTestingBuilder for integration tests

<!-- speaker: Aspire has matured enormously. It's no longer just for demos - teams are shipping production Aspire apps. -->
