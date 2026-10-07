# hash — hashes and text encoding

[Table of contents](README.md)

Use `hash` to calculate a digest or transform a text value. These commands take the text itself, not a file path.

## Reference

| Call | Result | Purpose |
| --- | --- | --- |
| `hash.MD5(value)` | String | MD5 digest of the text. |
| `hash.SHA1(value)` | String | SHA-1 digest of the text. |
| `hash.SHA256(value)` | String | SHA-256 digest of the text. |
| `hash.SHA384(value)` | String | SHA-384 digest of the text. |
| `hash.SHA512(value)` | String | SHA-512 digest of the text. |
| `hash.CRC32(value)` | Unsigned 32-bit number | CRC32 checksum of the text's UTF-8 bytes. |
| `hash.EncodeBase64(value)` | String | Encode the text's UTF-8 bytes as Base64. |
| `hash.DecodeBase64(value)` | String | Decode Base64 bytes as UTF-8 text. Invalid Base64 throws an error. |
| `hash.UrlEncode(value)` | String | Encode a URL component; spaces become `+`. |
| `hash.UrlDecode(value)` | String | Decode a URL-encoded component, including `+` as a space. |

Supply a string for `value`. Base64 is a reversible encoding; a hash is not a reversible encoding. These digest methods are not password-storage APIs.

## Hash text

```javascript
let text = "Mosaic";
log.Info("MD5: " + hash.MD5(text));
log.Info("SHA-256: " + hash.SHA256(text));
log.Info("CRC32: " + hash.CRC32(text));
```

Calling `hash.MD5("C:\\Reports\\summary.txt")` hashes that path string, not the file's contents. Reading a text file first hashes the decoded text, which is not a binary-file checksum.

## Round-trip text through Base64

```javascript
let encoded = hash.EncodeBase64("Hello"); // "SGVsbG8="
let decoded = hash.DecodeBase64(encoded); // "Hello"
ui.ShowString(encoded + "\n" + decoded);
```

## Encode a query value

```javascript
let query = hash.UrlEncode("Mosaic UI"); // "Mosaic+UI"
let url = "https://example.com/search?q=" + query;
ui.ShowString(url);
```

Encode individual query values, not the full URL including its separators. [http](http.md) also provides URL encoding helpers.
