import http from 'k6/http';

function mergeTags(name, extraTags) {
  return Object.assign({ name }, extraTags || {});
}

export function getJson(url, name, extraTags = {}) {
  return http.get(url, {
    headers: {
      Accept: 'application/json',
    },
    tags: mergeTags(name, extraTags),
  });
}

export function getRequest(url, name, extraTags = {}) {
  return http.get(url, {
    tags: mergeTags(name, extraTags),
  });
}
