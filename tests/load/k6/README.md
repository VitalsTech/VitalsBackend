# Vitals SLA Load Tests (k6)

Scripts for validating TZ SLA targets:

| Script | SLA | Target |
|--------|-----|--------|
| `routing-latency.js` | Routing p95 ≤ 500ms | POST internal routing |
| `consultation-chat-latency.js` | Consultation chat p95 ≤ 100ms | POST chat message |
| `notification-throughput.js` | Notification ≥ 1000/s | POST internal notification events |

## Prerequisites

- [k6](https://k6.io/docs/get-started/installation/) installed
- Target services running locally
- Set environment variables as needed (see each script header)

## Run

```bash
# Routing latency (p95 ≤ 500ms)
k6 run tests/load/k6/routing-latency.js

# Consultation chat latency (p95 ≤ 100ms)
k6 run tests/load/k6/consultation-chat-latency.js

# Notification throughput (≥ 1000 events/s sustained)
k6 run tests/load/k6/notification-throughput.js
```

## Notes

- These scripts hit **internal/dev** endpoints; production runs require valid JWT/service keys.
- Throughput test uses `shared-iterations` with high VU count; tune `K6_VUS` for your hardware.
- SLA thresholds are encoded as k6 `thresholds`; failed runs exit non-zero.
