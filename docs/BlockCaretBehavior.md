# BlockCaretBehavior

A behavior that replaces the standard I-beam caret in a TextBox with a full-character-width block caret, similar to terminal emulators and classic text editors.

This behavior targets a plain WPF `TextBox` only &mdash; it cannot be attached to an AvalonEdit `TextEditor`. For AvalonEdit use [`AvalonEditBlockCaretBehavior`](./AvalonEditBlockCaretBehavior.md), or [`CommandBox`](./CommandBox.md), which has the same block caret built in.

![BlockCaretBehavior](./images/BlockCaretBehavior.png)
