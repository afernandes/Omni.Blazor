# Omni.Localization.Json

Strict JSON catalog support for `AndersonN.Omni.Localization`.

```json
{
  "culture": "en",
  "texts": {
    "Home": {
      "Title": "Home"
    }
  }
}
```

Nested objects and arrays use `__` in their flattened keys. Split catalogs can be registered
in filename order; later documents override earlier documents through explicit provider priority.
Parsing happens at startup and publishes immutable catalogs. The package does not create file
watchers, background tasks or unbounded caches.
