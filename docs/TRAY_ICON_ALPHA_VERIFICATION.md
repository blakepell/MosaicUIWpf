# Tray icon frame and alpha regression checks

`TrayIconImageTests.cs` runs on STA threads in the existing nonparallel tray test
collection. `Assets/TrayFrames.ico` is a synthetic, opaque, 32-bit ICO containing
solid red 16×16, green 24×24 and blue 32×32 DIB frames. Frame tests check center
colors for `BitmapFrame`, `new BitmapImage(uri)`, relative file URIs, and absolute
and relative assembly pack-resource URIs. This distinguishes frame selection
from merely resizing the first decoded frame.

## Native alpha contract

Microsoft documents that [DrawIconEx](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-drawiconex)
uses AC_SRC_OVER for 32-bit alpha icons, and that
[alpha blending](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-blendfunction)
uses premultiplied RGB. This describes the blending representation, rather than
establishing the pixel representation accepted by an icon creation API.
[CreateIconIndirect](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-createiconindirect)
documents bitmap copying and ownership but does not explicitly state its alpha
conversion behavior. The premultiplied-pixel example in
[Using Cursors](https://learn.microsoft.com/en-us/windows/win32/menurc/using-cursors#creating-an-alpha-blended-cursor)
conflicts with the native rendering result below on the tested Windows build.

The native control check on Windows 11 build 26200 supplies raw pixels directly
to CreateIconIndirect, avoiding WPF format conversion. DrawIconEx renders the
result into a 32-bit DIB, followed by GdiFlush and a pixel readback:

| Input BGRA | Background RGB | Actual rendered BGRA |
| --- | --- | --- |
| Straight `(32,128,240,64)` | `(24,24,24)` | `(26,50,78,255)` |
| Premultiplied `(8,32,60,64)` | `(24,24,24)` | `(20,26,33,255)` |

The first result matches source-over compositing of the original color. The
second exhibits a second multiplication by alpha. The result was reproduced
outside the execution sandbox; diagnostic checks with a top-down or bottom-up
DIB, an explicit V5 alpha mask, and a device-dependent color bitmap agreed.

Consequently, Mosaic keeps its rasterization pipeline in Pbgra32 and converts
once to Bgra32 at the CreateIconIndirect boundary. Passing premultiplied pixels
at that boundary would introduce dimming on this system. The regression suite
checks exact and scaled straight/premultiplied bitmaps, PNG and vector sources
against light (240) and dark (24) backgrounds at 16, 20, 24 and 32 physical pixels.
It also checks opaque colors, fully transparent pixels and row orientation.

## Scope and fallback

Only URI paths ending in `.ico` are reopened to recover BitmapImage frames.
Caller-owned streams are never read, repositioned or disposed by frame recovery.
Stream-backed images, unavailable original files, and sources whose frames
cannot be recovered use the supplied bitmap. Download completion handling stays
with TrayIcon. No artwork opacity is increased; translucent application artwork
remains translucent. Native results should also be checked on other supported
Windows builds if their icon creation behavior differs.
