import http from 'k6/http';

export function getJson(url, name, extraTags = {}) {
  return http.get(url, {
    headers: {
      Accept: 'application/json',
    },
    tags: {
      name,
      ...extraTags,
    },
  });
}

export function getRequest(url, name, extraTags = {}) {
  return http.get(url, {
    tags: {
      name,
      ...extraTags,
    },
  });
}
