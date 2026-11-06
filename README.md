# Monitor Yayici

Make any window borderless and spread it across the monitors you choose, like Resize Raccoon.
Built for sim racing and multi-monitor games, but it works with any normal Windows window.

*Yayıcı* is Turkish for "spreader". The UI is in **English** by default, with a built-in **Turkish** option (EN / TR buttons, bottom right).

## Features

- Removes the title bar and border, then resizes the window to cover the monitors you pick.
- Clickable monitor map: toggle monitors, or type a custom area (X, Y, width, height).
- Window list with icons and search. "Find game" selects known games (BeamNG.drive, Assetto Corsa EVO, ETS2).
- **Keep applied** mode: re-applies the layout if the game resets it, and tells you when a game keeps fighting back.
- **Auto rules**: remember a layout per process and apply it automatically when that program opens.
- Result check: warns you when an app locks its own window size (see the BeamNG note below).
- Tray icon, optional start with Windows, single instance.
- Hotkeys: `Ctrl+Alt+B` span / restore the foreground window, `Ctrl+Alt+R` restore everything.
- Refreshes the layout when you plug or unplug a monitor.
- Dark UI, English and Turkish.

## Download / build

Requirements: Windows 10 or 11 (x64). Nothing to install: it uses the .NET Framework 4 that ships with Windows.

Build from source:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

This draws the app icon, then compiles `MonitorYayici.cs` with the C# compiler in `C:\Windows\Microsoft.NET\Framework64\v4.0.30319` and writes `MonitorYayici.exe` next to the script.

## Usage

1. Open the app and pick a window on the left (or press **Find game**).
2. Click the monitors it should cover, or use **Custom area**.
3. Press **Span (borderless)**. **Restore window** puts the original size and border back.

Command line (no window):

```text
MonitorYayici.exe --proc BeamNG.drive.x64 --monitors 1,2 --topmost --enforce
MonitorYayici.exe --tray
```

`--monitors` takes monitor numbers as shown in the app (default: all). `--enforce` keeps the process running and re-applies the layout until the window closes. `--tray` starts the app hidden in the tray.

Settings are stored in `%APPDATA%\MonitorYayici\ayarlar.ini`. Errors, if any, go to `hata.log` in the same folder.

## Notes and limits

- **Games that lock their window size (BeamNG.drive, for example).** Some games keep the window at the resolution set in their own graphics options, so a resize is refused. Set the game's display mode to **Window** and the resolution to your combined size (for two 2560x1440 monitors: `5120 x 1440`), then span. The app shows a red warning when this happens.
- **Exclusive fullscreen** windows cannot be resized from outside. Use windowed or borderless mode in the game first.
- Apps running **as administrator** can only be moved if Monitor Yayici is also run as administrator.
- The app does not hook into or modify any game. It only changes the window style and position through the Windows API.

## Türkçe

Herhangi bir pencerenin başlık çubuğunu ve kenarlığını kaldırıp seçtiğin monitörlere yayar (Resize Raccoon benzeri). Arayüz varsayılan olarak İngilizce, sağ alttaki **EN / TR** düğmeleriyle Türkçe'ye geçilir.

- Kullanım: soldan pencereyi seç, monitörlere tıklayarak ya da özel alan girerek hedefi belirle, **Yay (kenarlıksız)** düğmesine bas.
- **Korumalı mod** oyun pencereyi geri alırsa tekrar yayar. **Otomatik kurallar** bir program açılınca düzeni kendisi uygular.
- Kısayollar: `Ctrl+Alt+B` öndeki pencereyi yay / geri al, `Ctrl+Alt+R` hepsini geri al.
- BeamNG gibi pencere boyutunu kendi çözünürlüğüne kilitleyen oyunlarda önce oyunda ekran modunu **Window**, çözünürlüğü toplam boyuta (iki 2560x1440 monitör için `5120 x 1440`) ayarla.
- Derlemek için: `powershell -ExecutionPolicy Bypass -File .\build.ps1`
