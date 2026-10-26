import { bearerHeaders, getAccessToken } from '/k6lib/auth.js';
import { createSummaryHandler } from '/k6lib/summary.js';

import { createOptions, getApiBaseUrl } from '../lib/config.js';
import { runAdminReadModelSmoke } from '../lib/workflows/admin-read-model.js';

export const options = createOptions();

export function setup() {
  return getAccessToken();
}

export default function (token) {
  runAdminReadModelSmoke(getApiBaseUrl(), bearerHeaders(token));
}

export const handleSummary = createSummaryHandler('admin-smoke');
