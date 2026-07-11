import http from 'k6/http';
import { check } from 'k6';
import { Counter } from 'k6/metrics';

const uploadBytes = new Counter('file_upload_bytes');

const profiles = Object.freeze({
  smoke: Object.freeze({
    scenarioName: 'file_upload_smoke',
    executor: 'constant-vus',
    vus: 1,
    duration: '20s',
    gracefulStop: '5s',
    thresholds: Object.freeze({
      http_req_failed: ['rate<0.01'],
      http_req_duration: ['p(95)<1000'],
      checks: ['rate>0.99'],
    }),
  }),
  'average-load': Object.freeze({
    scenarioName: 'file_upload_average_load',
    executor: 'constant-vus',
    vus: 5,
    duration: '1m',
    gracefulStop: '10s',
    thresholds: Object.freeze({
      http_req_failed: ['rate<0.01'],
      http_req_duration: ['p(95)<1500'],
      checks: ['rate>0.99'],
    }),
  }),
  stress: Object.freeze({
    scenarioName: 'file_upload_stress',
    executor: 'constant-vus',
    vus: 10,
    duration: '3m',
    gracefulStop: '15s',
    thresholds: Object.freeze({
      http_req_failed: ['rate<0.05'],
      http_req_duration: ['p(95)<3000'],
      checks: ['rate>0.95'],
    }),
  }),
});

const payloadBytes = getPayloadBytes();
const payload = createPayload(payloadBytes);

export const options = createOptions();

export function setup() {
  const baseUrl = getUploadBaseUrl();
  const response = http.get(`${baseUrl}/health`, {
    tags: createTags('file_upload_health', 'health'),
  });

  check(response, {
    'upload host health status is expected': (r) => r.status >= 200 && r.status < 400,
  }, { endpoint: 'health' });

  return { baseUrl };
}

export default function (data) {
  const response = http.post(`${data.baseUrl}/upload/scan`, {
    file: http.file(payload, `file-upload-${payloadBytes}.bin`, 'application/octet-stream'),
  }, {
    tags: createTags('file_upload_scan', 'upload-scan'),
  });

  const expectedBytes = `bytes=${payloadBytes}`;
  const accepted = check(response, {
    'upload scan status is 200': (r) => r.status === 200,
    'upload scan reports expected bytes': (r) => r.body ? r.body.includes(expectedBytes) : false,
  }, { endpoint: 'upload-scan' });

  if (accepted) {
    uploadBytes.add(payloadBytes, { endpoint: 'upload-scan' });
  }
}

function createOptions() {
  const profile = getScenarioProfile();

  return {
    thresholds: profile.thresholds,
    scenarios: {
      [profile.scenarioName]: {
        executor: profile.executor,
        vus: profile.vus,
        duration: profile.duration,
        gracefulStop: profile.gracefulStop,
        tags: createTags('file_upload_scan', 'upload-scan'),
      },
    },
  };
}

function getUploadBaseUrl() {
  const baseUrl = __ENV.VT_UPLOAD_BASE_URL;

  if (!baseUrl) {
    throw new Error('VT_UPLOAD_BASE_URL is required.');
  }

  const normalizedBaseUrl = trimTrailingSlash(baseUrl);
  validateUploadBaseUrl(normalizedBaseUrl);

  return normalizedBaseUrl;
}

function validateUploadBaseUrl(baseUrl) {
  if (!baseUrl.startsWith('http://') && !baseUrl.startsWith('https://')) {
    throw new Error('VT_UPLOAD_BASE_URL must start with http:// or https://.');
  }

  if (__ENV.VT_K6_ALLOW_EXTERNAL === '1') {
    return;
  }

  if (!isLocalUploadBaseUrl(baseUrl)) {
    throw new Error('VT_UPLOAD_BASE_URL must target localhost, 127.0.0.1, [::1], or host.docker.internal unless VT_K6_ALLOW_EXTERNAL=1 is set.');
  }
}

function isLocalUploadBaseUrl(baseUrl) {
  const authority = getAuthority(baseUrl);
  if (authority.includes('@')) {
    return false;
  }

  const host = getHost(authority).toLowerCase();
  return host === '127.0.0.1'
    || host === 'localhost'
    || host === '[::1]'
    || host === 'host.docker.internal';
}

function getAuthority(baseUrl) {
  const schemeSeparator = baseUrl.indexOf('://');
  const pathSeparator = baseUrl.indexOf('/', schemeSeparator + 3);
  if (pathSeparator === -1) {
    return baseUrl.substring(schemeSeparator + 3);
  }

  return baseUrl.substring(schemeSeparator + 3, pathSeparator);
}

function getHost(authority) {
  if (authority.startsWith('[')) {
    const ipv6HostEnd = authority.indexOf(']');
    return ipv6HostEnd === -1 ? authority : authority.substring(0, ipv6HostEnd + 1);
  }

  const portSeparator = authority.indexOf(':');
  return portSeparator === -1 ? authority : authority.substring(0, portSeparator);
}

function getSelectedProfileName() {
  return (__ENV.VT_K6_PROFILE || 'smoke').trim().toLowerCase();
}

function getScenarioProfile() {
  const profileName = getSelectedProfileName();
  const profile = profiles[profileName];

  if (!profile) {
    throw new Error(`Unsupported VT_K6_PROFILE '${profileName}'. Supported profiles: ${Object.keys(profiles).join(', ')}.`);
  }

  const vusOverride = Number.parseInt(__ENV.VT_K6_VUS || '', 10);
  const durationOverride = (__ENV.VT_K6_DURATION || '').trim();

  return Object.assign({}, profile, {
    vus: Number.isNaN(vusOverride) ? profile.vus : vusOverride,
    duration: durationOverride || profile.duration,
  });
}

function getPayloadBytes() {
  const value = Number.parseInt(__ENV.VT_UPLOAD_PAYLOAD_BYTES || '262144', 10);

  if (Number.isNaN(value) || value < 1 || value > 16777216) {
    throw new Error('VT_UPLOAD_PAYLOAD_BYTES must be between 1 and 16777216.');
  }

  return value;
}

function createPayload(size) {
  const chunk = '0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_.~';
  const repetitions = Math.ceil(size / chunk.length);

  return chunk.repeat(repetitions).slice(0, size);
}

function createTags(name, endpoint) {
  return {
    area: 'benchmark',
    endpoint,
    name,
    service: 'file-upload-benchmark-host',
    suite: 'performance',
    tool: 'k6',
  };
}

function trimTrailingSlash(value) {
  return value.endsWith('/') ? value.slice(0, -1) : value;
}
