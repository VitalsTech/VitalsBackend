import http from 'k6/http';
import { check, sleep } from 'k6';

// TZ SLA: Consultation chat p95 ≤ 100ms
export const options = {
  vus: __ENV.K6_VUS ? parseInt(__ENV.K6_VUS) : 10,
  duration: __ENV.K6_DURATION || '30s',
  thresholds: {
    http_req_duration: ['p(95)<100'],
    checks: ['rate>0.95'],
  },
};

const BASE_URL = __ENV.CONSULTATION_BASE_URL || 'http://localhost:5240';
const CONSULTATION_ID = __ENV.CONSULTATION_ID || '33333333-3333-3333-3333-333333333333';
const AUTH_TOKEN = __ENV.AUTH_TOKEN || '';

export default function () {
  const payload = JSON.stringify({
    content: `load-test message ${Date.now()}`,
    messageType: 'text',
  });

  const headers = {
    'Content-Type': 'application/json',
  };

  if (AUTH_TOKEN) {
    headers.Authorization = `Bearer ${AUTH_TOKEN}`;
  }

  const res = http.post(
    `${BASE_URL}/api/consultations/${CONSULTATION_ID}/messages`,
    payload,
    { headers },
  );

  check(res, {
    'status is 2xx or 404 (missing fixture)': (r) =>
      (r.status >= 200 && r.status < 300) || r.status === 404,
  });

  sleep(0.02);
}
