# FileCard

A clickable card that represents a single file on disk. The operating system's icon for the file type
is shown on the left, with the file name and its formatted size (B, KB, MB, GB) stacked to the right.
Only the file name is displayed; the full path is the tooltip.

Missing files fall back to an error glyph and drop the size, while still showing the intended name.
With `IsTintEnabled` the card background is washed with a small amount of the icon's dominant color,
always mixed into the active theme's control background so the card stays inside the Light, Dark, and
Blue palettes. The card raises on hover, lowers while pressed, and fires `Click` plus a `Command`
(which receives the `FilePath` when no `CommandParameter` is set) on release.
