// Shared dispatcher for Omni.Blazor feature modules.
// Each feature owns a private API object; no helper is published on window.

export function invokeApi(api, identifier, args) {
  if (!identifier) {
    throw new TypeError('An Omni JavaScript identifier is required.');
  }

  let owner = api;
  let target = api;
  let start = 0;
  while (start < identifier.length) {
    const separator = identifier.indexOf('.', start);
    const end = separator < 0 ? identifier.length : separator;
    const segment = identifier.slice(start, end);
    if (!Object.prototype.hasOwnProperty.call(target, segment)) {
      throw new TypeError(`Unknown Omni JavaScript helper: ${identifier}`);
    }
    owner = target;
    target = target[segment];
    start = end + 1;
  }

  if (typeof target !== 'function') {
    throw new TypeError(`Unknown Omni JavaScript helper: ${identifier}`);
  }

  return target.apply(owner, Array.isArray(args) ? args : []);
}
