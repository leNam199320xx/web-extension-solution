# 📅 Scheduler Model
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

Defines how plugin executions are scheduled, queued, and dispatched.

For execution model, see `docs/runtime/execution-model.md`.
For resource governance, see `docs/runtime/resource-governance.md`.

---

# 2. CORE PRINCIPLE

> Scheduling is a control plane function that protects the system from overload.

---

# 3. PHASE 1 DECISION

**In-process queue using `System.Threading.Channels`** for Phase 1 implementation.

Rationale:
- Simpler deployment (no external queue dependency for execution)
- Adequate for single-node and small cluster
- Low latency

Future (Phase 2+): Migrate to Redis-backed distributed queue when multi-node coordination is needed.

---

# 4. SCHEDULER INTERFACE

```csharp
public interface IExecutionScheduler
{
    /// Enqueue an execution request. Returns immediately.
    Task<string> EnqueueAsync(
        ExecutionRequest request,
        ExecutionPriority priority = ExecutionPriority.Normal,
        CancellationToken cancellationToken = default);

    /// Wait for execution result (used by sync API endpoint).
    Task<ExecutionResult> WaitForResultAsync(
        string executionId,
        CancellationToken cancellationToken);
}

public enum ExecutionPriority
{
    High = 0,
    Normal = 1,
    Background = 2
}
```

---

# 5. SCHEDULING FLOW

```
API Request
  → Scheduler.EnqueueAsync()
  → Channel<ExecutionRequest>
  → Worker reads from Channel
  → ExecutionPipeline.ProcessAsync()
  → Result stored
  → Caller notified (TaskCompletionSource)
```

---

# 6. WORKER MODEL

```csharp
public class ExecutionWorker : BackgroundService
{
    private readonly Channel<ScheduledExecution> _channel;
    private readonly IExecutionPipeline _pipeline;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            var result = await _pipeline.ProcessAsync(item.Request, stoppingToken);
            item.Completion.SetResult(result);
        }
    }
}
```

Multiple workers can read from the same channel for concurrency control.

---

# 7. CONCURRENCY CONTROL

```csharp
// Bounded channel enforces max queue depth
var channel = Channel.CreateBounded<ScheduledExecution>(
    new BoundedChannelOptions(maxQueueSize)
    {
        FullMode = BoundedChannelFullMode.DropWrite // reject when full
    });
```

Configuration via `RuntimeOptions.MaxConcurrentExecutions`.

---

# 8. PRIORITY QUEUES

Three channels (high, normal, background):

```csharp
// Worker checks channels in priority order:
// 1. High priority (admin, security ops)
// 2. Normal (standard plugin execution)
// 3. Background (async jobs)
```

---

# 9. LOAD CONTROL

| Condition | Action |
|-----------|--------|
| Queue full | Reject with 429 Too Many Requests |
| System CPU > 90% | Stop accepting Background priority |
| Memory pressure high | Reject new requests, drain existing |

---

# 10. DISTRIBUTED SCHEDULING (Future Phase 2+)

When multi-node coordination is needed:

- Replace in-process Channel with Redis Stream or Redis List
- Each node runs workers that consume from shared queue
- Execution assignment becomes pull-based

Interface remains the same (`IExecutionScheduler`) — only implementation changes.

---

# 11. FAILURE HANDLING

- If scheduler crashes: requests in-flight are lost (acceptable for sync requests — client retries)
- If worker crashes: execution is marked as `Failed`
- If queue is full: new requests rejected immediately
- No silent task loss

---

# 12. DESIGN PRINCIPLE

> Scheduling protects the system from overload.
> It never compromises isolation or security to increase throughput.

---

# 🏁 END
