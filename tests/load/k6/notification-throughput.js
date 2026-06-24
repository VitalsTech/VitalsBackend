import http from 'k6/http';
import { check } from 'k6';

// TZ SLA: Notification ≥ 1000 events/s
const TARGET_RPS = __ENV.TARGET_RPS ? parseInt(__ENV.TARGET_RPS) : 1000;
const DURATION = __ENV.K6_DURATION || '10s';

export const options = {
  scenarios: {
    notification_flood: {
      executor: 'constant-arrival-rate',
      rate: TARGET_RPS,
      timeUnit: '1s',
      duration: DURATION,
      preAllocatedVUs: Math.min(TARGET_RPS, 200),
      maxVUs: Math.min(TARGET_RPS * 2, 500),
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<2000'],
    checks: ['rate>0.90'],
  },
};

const BASE_URL = __ENV.NOTIFICATION_BASE_URL || 'http://localhost:5260';

export default function () {
  const payload = JSON.stringify({
    eventId: `${__VU}-${__ITER}-${Date.now()}`,
    eventType: 'system.maintenance',
    userId: '44444444-4444-4444-4444-444444444444',
    priority: 'Low',
    category: 'system',
    templateData: {
      maintenance_time: '02:00 MSK',
      message: 'load test',
    },
  });

  const res = http.post(`${BASE_URL}/internal/notifications/events`, payload, {
    headers: { 'Content-Type': 'application/json' },
  });

  check(res, {
    'accepted': (r) => r.status >= 200 && r.status < 300,
  });
}
