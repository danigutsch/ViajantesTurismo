import { check } from 'k6';

function hasJsonContentType(response) {
  const contentType = response.headers['Content-Type'] || response.headers['content-type'] || '';
  return contentType.toLowerCase().includes('application/json');
}

export function checkHealth(response) {
  check(response, {
    'health status is expected': (r) => r.status >= 200 && r.status < 400,
  }, { endpoint: 'health' });
}

export function checkCollectionResponse(response, endpointName) {
  check(response, {
    [`${endpointName} status is 200`]: (r) => r.status === 200,
    [`${endpointName} uses json`]: (r) => hasJsonContentType(r),
    [`${endpointName} body parses as array`]: (r) => Array.isArray(r.json()),
  }, { endpoint: endpointName });
}
