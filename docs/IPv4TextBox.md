# IPv4TextBox

A themed, four-segment editor for IPv4 addresses. Each segment accepts one to three decimal digits from 0 through 255, while the `Text` dependency property exposes the last complete valid address as a single bindable value.

## Usage

```xml
<mosaic:IPv4TextBox
    Width="220"
    Text="{Binding ServerAddress, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
```

`Text` binds two way and updates the source on property change by default, so the explicit binding options above are optional. Its default value is an empty string.

## Validation and binding

The control accepts exactly four dot-separated decimal segments. Leading zeroes are preserved, so `192.168.001.010` is valid and remains in that form. Abbreviated addresses, empty segments, non-decimal text, and values greater than 255 are rejected.

While the user is editing an incomplete address, `Text` retains the last complete valid address. Assigning an invalid value through a binding or in code is likewise coerced back to the last accepted value. This prevents partially edited input from leaking into a view model.

## Keyboard and clipboard

| Input | Action |
|---|---|
| `.` or keypad decimal | Moves to the next segment when the current segment is valid. |
| Three valid digits | Automatically advances to the next segment. |
| `Left` at the start | Moves to the preceding segment. |
| `Right` at the end | Moves to the next segment. |
| `Backspace` in an empty segment | Moves to the preceding segment. |
| Paste | Accepts a complete valid IPv4 address transactionally; partial or invalid paste is discarded. |

The context menu’s `Copy IP Address` action invokes `IPv4TextBox.CopyAddressCommand` and copies the complete aggregate value. The command can also be used from a custom template or command source.

## Accessibility

Although four text boxes implement the visual editor, UI Automation exposes `IPv4TextBox` as one editable value through the Value pattern. Focusing the composite control sends focus to the first empty segment, or selects the first segment when the address is complete.
