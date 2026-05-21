import { createOptions, getApiBaseUrl } from '../lib/config.js';
import { runAdminReadModelSmoke } from '../lib/workflows/admin-read-model.js';

export const options = createOptions();

export default function () {
  runAdminReadModelSmoke(getApiBaseUrl());
}
