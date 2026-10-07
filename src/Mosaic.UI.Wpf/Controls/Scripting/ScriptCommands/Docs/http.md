# http — HTTP requests

[Table of contents](README.md)

Use `http` to send a request and read the response body as a string. Calls are synchronous: execution waits for the request to finish. Responses are not automatically parsed as JSON, and no response status or header object is returned.

## Request reference

All calls below return a string. `url`, `data`, `headerKey`, and `headerValue` are strings. Header overloads set one request header; headers are not retained for the next call.

| Call | Request |
| --- | --- |
| `http.Get(url)` | GET. |
| `http.Get(url, headerKey, headerValue)` | GET with one header. |
| `http.Post(url)` | POST with an empty body. |
| `http.Post(url, data)` | POST with the supplied body. |
| `http.Post(url, data, headerKey, headerValue)` | POST with a body and one header. |
| `http.Put(url)` | **Current behavior: POST with the literal body `PUT`.** Use the next overload to send PUT. |
| `http.Put(url, data)` | PUT with the supplied body; use `""` for an empty body. |
| `http.Put(url, data, headerKey, headerValue)` | PUT with a body and one header. |
| `http.Delete(url)` | DELETE with an empty body. |
| `http.Delete(url, data)` | DELETE with the supplied body. |
| `http.Delete(url, data, headerKey, headerValue)` | DELETE with a body and one header. |

The methods transmit the `data` string as supplied. They do not encode a JavaScript object as a form or automatically set a JSON/form content type. Set the appropriate `Content-Type` header when the server needs it.

The following request examples use placeholder URLs. Replace them with your service's endpoints before running them; POST, PUT, and DELETE can change server data.

## GET text or JSON

```javascript
try {
    let text = http.Get("https://example.com/api/status", "Accept", "application/json");
    let status = JSON.parse(text);
    ui.ShowString(JSON.stringify(status));
} catch (error) {
    log.Error(error.Message);
}
```

This example expects a JSON response. Network failures, HTTP error responses, and invalid JSON can throw errors.

## POST form values

```javascript
let data = "name=" + http.UrlEncode("Ada Lovelace")
    + "&city=" + http.UrlEncode("London");
let response = http.Post(
    "https://example.com/api/people",
    data,
    "Content-Type",
    "application/x-www-form-urlencoded"
);
ui.ShowString(response);
```

## PUT JSON

```javascript
let body = JSON.stringify({ name: "Ada", active: true });
let response = http.Put(
    "https://example.com/api/people/42",
    body,
    "Content-Type",
    "application/json"
);
ui.ShowString(response);
```

## Encoding helpers

| Call | Result |
| --- | --- |
| `http.UrlEncode(value)` | URL-encoded text; null or empty input returns `""`. |
| `http.UrlDecode(value)` | Decoded text; null or empty input returns `""`. |

Encode each query or form value separately. The built-in module has no async request methods, per-call timeout, cancellation argument, or multi-header overload. Applications can provide a richer HTTP helper when those features are needed.
