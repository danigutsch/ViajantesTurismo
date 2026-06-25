const defaultSmokeProfile = Object.freeze({
  scenarioName: 'admin_smoke',
  executor: 'constant-vus',
  vus: 1,
  duration: '30s',
  gracefulStop: '5s',
  thresholds: Object.freeze({
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<1000'],
    checks: ['rate>0.99'],
  }),
});

const profiles = Object.freeze({
  smoke: defaultSmokeProfile,
  'average-load': Object.freeze({
    scenarioName: 'admin_average_load',
    executor: 'constant-vus',
    vus: 5,
    duration: '2m',
    gracefulStop: '10s',
    thresholds: Object.freeze({
      http_req_failed: ['rate<0.01'],
      http_req_duration: ['p(95)<1500'],
      checks: ['rate>0.99'],
    }),
  }),
  stress: Object.freeze({
    scenarioName: 'admin_stress',
    executor: 'constant-vus',
    vus: 15,
    duration: '5m',
    gracefulStop: '15s',
    thresholds: Object.freeze({
      http_req_failed: ['rate<0.05'],
      http_req_duration: ['p(95)<3000'],
      checks: ['rate>0.95'],
    }),
  }),
});

function trimTrailingSlash(value) {
  return value.endsWith('/') ? value.slice(0, -1) : value;
}

export function getApiBaseUrl() {
  const baseUrl = __ENV.VT_API_BASE_URL;

  if (!baseUrl) {
    throw new Error('VT_API_BASE_URL is required.');
  }

  return trimTrailingSlash(baseUrl);
}

export function getSelectedProfileName() {
  return (__ENV.VT_K6_PROFILE || 'smoke').trim().toLowerCase();
}

export function getScenarioProfile() {
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

export function createOptions() {
  const profile = getScenarioProfile();

  return {
    thresholds: profile.thresholds,
    scenarios: {
      [profile.scenarioName]: {
        executor: profile.executor,
        vus: profile.vus,
        duration: profile.duration,
        gracefulStop: profile.gracefulStop,
        tags: {
          area: 'admin',
          profile: getSelectedProfileName(),
          service: 'admin-api',
          suite: 'performance',
          tool: 'k6',
        },
      },
    },
  };
}
