# mouse — pointer automation

[Table of contents](README.md)

Use `mouse` to control the desktop pointer and mouse buttons. These commands affect the real desktop, including other applications. Positions are screen coordinates, not positions inside the script editor.

## Position

| Member | Result or behavior |
| --- | --- |
| `mouse.X` | Read/write integer X coordinate. Setting it preserves Y. |
| `mouse.Y` | Read/write integer Y coordinate. Setting it preserves X. |
| `mouse.SetPosition(x, y)` | Set both integer coordinates; return no value. |

Read properties without parentheses:

```javascript
log.Info("Pointer: " + mouse.X + ", " + mouse.Y);
```

## Buttons and wheel

All of these methods return no value and act at the current pointer position.

| Call | Action |
| --- | --- |
| `mouse.LeftClick()` | Press and release the left button. |
| `mouse.LeftDoubleClick()` | Send two left clicks with a short delay. |
| `mouse.LeftDown()` / `mouse.LeftUp()` | Press / release the left button. |
| `mouse.RightClick()` | Press and release the right button. |
| `mouse.RightDown()` / `mouse.RightUp()` | Press / release the right button. |
| `mouse.MiddleClick()` | Press and release the middle button. |
| `mouse.MiddleDown()` / `mouse.MiddleUp()` | Press / release the middle button. |
| `mouse.ScrollUp(clicks)` | Scroll upward by an integer number of wheel clicks. |
| `mouse.ScrollDown(clicks)` | Scroll downward by an integer number of wheel clicks. |

Use positive counts for scrolling. The target application's focus and input handling determine which control receives an action.

## Drag between two points

Choose coordinates for your desktop before running this example. It moves the pointer, holds the left button, moves again, and releases it. A `finally` block pairs button release with button press if the intervening code fails.

```javascript
mouse.SetPosition(200, 200);
mouse.LeftDown();
try {
    await ui.PauseAsync(100);
    mouse.SetPosition(400, 300);
    await ui.PauseAsync(100);
} finally {
    mouse.LeftUp();
}
```

For keyboard input, see [ui.SendKeys](ui.md#keyboard-input). For requesting foreground focus, see [process](process.md).
