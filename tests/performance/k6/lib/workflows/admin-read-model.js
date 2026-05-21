import { group } from 'k6';

import { checkCollectionResponse, checkHealth } from '../checks.js';
import { getJson, getRequest } from '../http.js';

export function runAdminReadModelSmoke(apiBaseUrl) {
  group('admin smoke', () => {
    const healthResponse = getRequest(`${apiBaseUrl}/health`, 'health', { flow: 'admin-smoke' });
    checkHealth(healthResponse);

    group('read collections', () => {
      const customersResponse = getJson(`${apiBaseUrl}/customers/`, 'customers-list', { flow: 'admin-smoke', resource: 'customers' });
      checkCollectionResponse(customersResponse, 'customers-list');

      const toursResponse = getJson(`${apiBaseUrl}/tours/`, 'tours-list', { flow: 'admin-smoke', resource: 'tours' });
      checkCollectionResponse(toursResponse, 'tours-list');

      const bookingsResponse = getJson(`${apiBaseUrl}/bookings/`, 'bookings-list', { flow: 'admin-smoke', resource: 'bookings' });
      checkCollectionResponse(bookingsResponse, 'bookings-list');
    });
  });
}
