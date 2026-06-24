import http from 'k6/http';
import { check, sleep } from 'k6';

// TZ SLA: Routing p95 ≤ 500ms
export const options = {
  vus: __ENV.K6_VUS ? parseInt(__ENV.K6_VUS) : 20,
  duration: __ENV.K6_DURATION || '30s',
  thresholds: {
    http_req_duration: ['p(95)<500'],
    checks: ['rate>0.95'],
  },
};

const BASE_URL = __ENV.ROUTING_BASE_URL || 'http://localhost:5230';
const SERVICE_KEY = __ENV.SERVICE_KEY || 'vitals-internal-dev-key';

export default function () {
  const payload = JSON.stringify({
    eventId: `${__VU}-${__ITER}-${Date.now()}`,
    patientId: '11111111-1111-1111-1111-111111111111',
    sessionId: '22222222-2222-2222-2222-222222222222',
    urgencyLevel: 2,
    specialty: 'therapist',
    symptoms: 'headache',
  });

  const res = http.post(`${BASE_URL}/internal/routing/triage-completed`, payload, {
    headers: {
      'Content-Type': 'application/json',
      'X-Service-Key': SERVICE_KEY,
      'X-Service-Name': 'load-test',
    },
  });

  check(res, {
    'status is 2xx': (r) => r.status >= 200 && r.status < 300,
  });

  sleep(0.05);
}
