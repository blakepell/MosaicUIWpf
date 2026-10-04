# regex — pattern matching

[Table of contents](README.md)

Use `regex.IsMatch(input, pattern)` to test a string against a .NET regular expression. Both arguments are strings; the result is `true` or `false`. Pass patterns as strings, rather than JavaScript `/pattern/` literals. Double a backslash inside a JavaScript string when the pattern needs a backslash.

## Match text

```javascript
let validCode = regex.IsMatch("AB-1234", "^[A-Z]{2}-\\d{4}$"); // true
let mentionsMosaic = regex.IsMatch("MOSAIC UI", "(?i)mosaic"); // true
log.Info("Valid code: " + validCode);
log.Info("Mentions Mosaic: " + mentionsMosaic);
```

Matching is case-sensitive unless the pattern changes that, for example with `(?i)`. Use `^` and `$` when you want to constrain the match to the input boundaries. An invalid pattern throws an error. This module has no timeout parameter, capture-result method, or general replace method.

## Simple alternatives lists

These helpers edit strings such as `"(red|green|blue)"`. They are intended for simple word alternatives; they do not parse an arbitrary regular expression.

| Call | Result | Behavior |
| --- | --- | --- |
| `regex.ListContainsValue(regexString, value)` | Boolean | Look for the literal value at word boundaries, case-sensitively. Null/empty input or value returns `false`. |
| `regex.ListAddValue(regexString, value)` | String, possibly null | Append a missing value and wrap the list in parentheses. Empty value or an existing match returns the original input. |
| `regex.ListRemoveValue(regexString, value)` | String, possibly null | Remove word-boundary matches and neighboring separators, then wrap in parentheses. Null/empty input or value returns the original input. |

`ListAddValue` inserts the new value without escaping regular-expression characters. A value containing `.` or `*`, for example, will act as a pattern if the resulting list is used with `IsMatch`. The word-boundary tests do not guarantee exact alternative matching for punctuation or complex patterns.

```javascript
let colors = "(red|green)";
colors = regex.ListAddValue(colors, "blue"); // "(red|green|blue)"
let hasGreen = regex.ListContainsValue(colors, "green"); // true
colors = regex.ListRemoveValue(colors, "green"); // "(red|blue)"
let matches = regex.IsMatch("blue", "^" + colors + "$"); // true
ui.ShowString(colors);
```

Removing the only value produces `"()"`, which is an empty regex group, not a pattern that matches nothing.
