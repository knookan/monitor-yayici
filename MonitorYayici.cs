// Monitor Yayici - makes a window borderless and spreads it across your monitors (like Resize Raccoon).
// Build: build.ps1   (C# 5 / .NET Framework 4, no extra install needed)
// Command line: MonitorYayici.exe --proc BeamNG.drive.x64 [--monitors 1,2] [--topmost] [--enforce]
//               MonitorYayici.exe --tray      (start in the tray)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

// ------------------------------------------------------------------------------------------------
// Localization (English default, Turkish)

static class L
{
    public static string Lang = "en";

    static readonly Dictionary<string, string[]> d = new Dictionary<string, string[]>
    {
        { "cardWindows", new[] { "Windows", "Pencereler" } },
        { "cardMap", new[] { "Monitor layout  -  click to toggle", "Monitör düzeni  -  tıkla: seç / bırak" } },
        { "cardTarget", new[] { "Target area and options", "Hedef alan ve seçenekler" } },
        { "cardRules", new[] { "Auto rules", "Otomatik kurallar" } },
        { "search", new[] { "Search: process name or title", "Ara: işlem adı veya başlık" } },
        { "refresh", new[] { "Refresh", "Yenile" } },
        { "findGame", new[] { "Find game", "Oyunu bul" } },
        { "selAll", new[] { "Select all", "Hepsini seç" } },
        { "primaryOnly", new[] { "Primary only", "Sadece ana" } },
        { "selMon", new[] { "Selected monitors", "Seçili monitörler" } },
        { "custom", new[] { "Custom area:", "Özel alan:" } },
        { "w", new[] { "W", "Gen." } },
        { "h", new[] { "H", "Yük." } },
        { "topmost", new[] { "Always on top (covers the taskbar)", "Her zaman üstte (görev çubuğunu ortar)" } },
        { "enforce", new[] { "Keep applied (re-apply if the game resets it)", "Korumalı (oyun geri alırsa tekrar yay)" } },
        { "apply", new[] { "Span (borderless)", "Yay (kenarlıksız)" } },
        { "restore", new[] { "Restore window", "Eski haline döndür" } },
        { "restoreAll", new[] { "Restore all", "Hepsini geri al" } },
        { "addRule", new[] { "Add rule for selected window", "Seçili pencere için kural ekle" } },
        { "delete", new[] { "Delete", "Sil" } },
        { "rulesAuto", new[] { "Apply rules automatically", "Kuralları otomatik uygula" } },
        { "startup", new[] { "Start with Windows (in tray)", "Windows ile tepside başlat" } },
        { "trayClose", new[] { "Minimize to tray on close", "Kapatınca tepsiye küçülsün" } },
        { "spanned", new[] { "SPANNED", "YAYILDI" } },
        { "primaryTag", new[] { "(primary)", "(ana)" } },
        { "target", new[] { "Target: {0} x {1}   ({2}, {3})", "Hedef: {0} x {1}   ({2}, {3})" } },
        { "yes", new[] { "Yes", "Evet" } },
        { "ready", new[] { "Ready. Pick a window on the left, then choose where it should span.", "Hazır. Soldan bir pencere seç, sağdan nereye yayılacağını belirle." } },
        { "pickWindow", new[] { "Pick a window from the list first.", "Önce soldan bir pencere seç." } },
        { "applying", new[] { "Applying...", "Uygulanıyor..." } },
        { "applied", new[] { "Spanned: {0}  ->  {1} x {2}  ({3}, {4})", "Yayıldı: {0}  ->  {1} x {2}  ({3}, {4})" } },
        { "locked", new[] {
            "The window was set to {0}x{1} but the app locked it to {2}x{3}. In the game, set the display mode to 'Window' and the resolution to {0}x{1}, then span again.",
            "Pencere {0}x{1} istendi ama uygulama {2}x{3}'e kilitledi. Oyunda ekran modunu 'Window' yapıp çözünürlüğü {0}x{1} seç, sonra tekrar yay." } },
        { "notPlaced", new[] {
            "The window did not reach the target (now {0} x {1} at {2},{3}). It may be an app running as administrator.",
            "Pencere istenen yere gelmedi (şu an {0} x {1} @ {2},{3}). Yönetici olarak çalışan bir uygulama olabilir." } },
        { "restored", new[] { "Restored: {0}", "Eski haline döndü: {0}" } },
        { "restoredAll", new[] { "All windows restored.", "Tüm pencereler eski haline döndü." } },
        { "ruleNeedsWin", new[] { "Select a window on the left first (the process name is taken from it).", "Kural için önce soldan bir pencere seç (işlem adı buradan alınır)." } },
        { "ruleAdded", new[] {
            "Rule added: {0} will be spanned automatically when it opens (while this app is running).",
            "Kural eklendi: {0} açılınca otomatik yayılacak (program açık kaldıkça)." } },
        { "ruleApplied", new[] { "Rule applied: {0}", "Kural uygulandı: {0}" } },
        { "monChanged", new[] { "Monitor setup changed, layout refreshed.", "Monitör ayarı değişti, yerleşim yenilendi." } },
        { "gameNotFound", new[] { "No known game window found. Pick one from the list.", "Bilinen bir oyun penceresi bulunamadı. Listeden elle seç." } },
        { "startupFail", new[] { "Could not change the startup setting: {0}", "Başlangıç ayarı yazılamadı: {0}" } },
        { "enforceGaveUp", new[] {
            "The game keeps resetting the window size, so I stopped re-applying: {0}",
            "Oyun pencereyi tekrar tekrar eski boyutuna çekiyor, korumayı bıraktım: {0}" } },
        { "trayShow", new[] { "Show", "Göster" } },
        { "trayToggle", new[] { "Span / restore foreground window   (Ctrl+Alt+B)", "Öndeki pencereyi yay / geri al   (Ctrl+Alt+B)" } },
        { "trayRestoreAll", new[] { "Restore all   (Ctrl+Alt+R)", "Hepsini geri al   (Ctrl+Alt+R)" } },
        { "trayExit", new[] { "Exit", "Çıkış" } },
        { "balloon", new[] {
            "Still running in the tray. To quit, right-click the icon > Exit.",
            "Tepside çalışmaya devam ediyor. Kapatmak için simgeye sağ tıkla > Çıkış." } },
        { "areaAll", new[] { "All monitors", "Tüm monitörler" } },
        { "areaMon", new[] { "Monitor {0}", "Monitör {0}" } },
        { "areaCustom", new[] { "Custom {0}", "Özel {0}" } },
    };

    public static CultureInfo Culture { get { return new CultureInfo(Lang == "tr" ? "tr-TR" : "en-US"); } }

    public static string T(string key)
    {
        string[] v;
        if (!d.TryGetValue(key, out v)) return key;
        return Lang == "tr" ? v[1] : v[0];
    }

    public static string F(string key, params object[] args) { return string.Format(T(key), args); }
}

// ------------------------------------------------------------------------------------------------
// Win32

static class Win
{
    public const int GWL_STYLE = -16, GWL_EXSTYLE = -20;
    public const long WS_CAPTION = 0x00C00000, WS_THICKFRAME = 0x00040000, WS_SYSMENU = 0x00080000,
        WS_MINIMIZEBOX = 0x00020000, WS_MAXIMIZEBOX = 0x00010000;
    public const long WS_EX_DLGMODALFRAME = 0x1, WS_EX_CLIENTEDGE = 0x200, WS_EX_STATICEDGE = 0x20000,
        WS_EX_WINDOWEDGE = 0x100, WS_EX_TOOLWINDOW = 0x80;
    public const uint SWP_FRAMECHANGED = 0x20, SWP_SHOWWINDOW = 0x40;
    public static readonly IntPtr HWND_TOP = IntPtr.Zero, HWND_TOPMOST = new IntPtr(-1), HWND_NOTOPMOST = new IntPtr(-2);

    public delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint cmd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder sb, int max);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr h);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] public static extern IntPtr GetWindowLongPtr(IntPtr h, int idx);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] public static extern IntPtr SetWindowLongPtr(IntPtr h, int idx, IntPtr v);
    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h, int id, uint mod, uint vk);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("dwmapi.dll")] public static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int val, int size);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h, int attr, out int val, int size);

    // hidden UWP "ghost" windows should not show up in the list
    public static bool IsCloaked(IntPtr h)
    {
        int v;
        try { return DwmGetWindowAttribute(h, 14, out v, 4) == 0 && v != 0; } catch { return false; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    public static Rectangle GetRect(IntPtr h)
    {
        RECT r; GetWindowRect(h, out r);
        return Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
    }

    public static string Title(IntPtr h)
    {
        var sb = new StringBuilder(256);
        GetWindowText(h, sb, sb.Capacity);
        return sb.ToString();
    }

    public static uint Pid(IntPtr h) { uint pid; GetWindowThreadProcessId(h, out pid); return pid; }

    public static string ProcName(IntPtr h)
    {
        try { return Process.GetProcessById((int)Pid(h)).ProcessName; } catch { return "?"; }
    }

    public static void DarkTitleBar(IntPtr h)
    {
        int one = 1;
        try { if (DwmSetWindowAttribute(h, 20, ref one, 4) != 0) DwmSetWindowAttribute(h, 19, ref one, 4); } catch { }
    }
}

// ------------------------------------------------------------------------------------------------
// Window operations

class WinItem
{
    public IntPtr H;
    public uint Pid;
    public string Proc, Title;
}

class Saved
{
    public long Style, ExStyle;
    public Rectangle Rect;
    public bool Zoomed;
}

class Enforce
{
    public Rectangle Rect;
    public bool Topmost;
    public int Fails;
}

static class Spanner
{
    static readonly Dictionary<IntPtr, Saved> saved = new Dictionary<IntPtr, Saved>();
    static readonly Dictionary<IntPtr, Enforce> enforced = new Dictionary<IntPtr, Enforce>();
    static string notice;

    public static bool IsSpanned(IntPtr h) { return saved.ContainsKey(h); }
    public static int SpannedCount { get { return saved.Count; } }

    public static string TakeNotice() { string n = notice; notice = null; return n; }

    public static List<WinItem> ListWindows(IntPtr skip)
    {
        var list = new List<WinItem>();
        Win.EnumWindows((h, l) =>
        {
            if (h == skip || !Win.IsWindowVisible(h) || Win.GetWindow(h, 4) != IntPtr.Zero) return true;
            if (((long)Win.GetWindowLongPtr(h, Win.GWL_EXSTYLE) & Win.WS_EX_TOOLWINDOW) != 0) return true;
            if (Win.IsCloaked(h)) return true;
            string t = Win.Title(h);
            if (t.Length == 0) return true;
            list.Add(new WinItem { H = h, Pid = Win.Pid(h), Title = t, Proc = Win.ProcName(h) });
            return true;
        }, IntPtr.Zero);
        return list.OrderBy(w => w.Proc, StringComparer.OrdinalIgnoreCase).ThenBy(w => w.Title).ToList();
    }

    public static IntPtr FindByProcess(string name)
    {
        IntPtr best = IntPtr.Zero; long bestArea = 0;
        foreach (var p in Process.GetProcessesByName(name))
        {
            try
            {
                IntPtr h = p.MainWindowHandle;
                if (h == IntPtr.Zero) continue;
                Rectangle r = Win.GetRect(h);
                long a = (long)r.Width * r.Height;
                if (a > bestArea) { best = h; bestArea = a; }
            }
            catch { }
        }
        return best;
    }

    static void ApplyStyle(IntPtr h, Saved s)
    {
        long style = s.Style & ~(Win.WS_CAPTION | Win.WS_THICKFRAME | Win.WS_SYSMENU | Win.WS_MINIMIZEBOX | Win.WS_MAXIMIZEBOX);
        long ex = s.ExStyle & ~(Win.WS_EX_DLGMODALFRAME | Win.WS_EX_CLIENTEDGE | Win.WS_EX_STATICEDGE | Win.WS_EX_WINDOWEDGE);
        Win.SetWindowLongPtr(h, Win.GWL_STYLE, new IntPtr(style));
        Win.SetWindowLongPtr(h, Win.GWL_EXSTYLE, new IntPtr(ex));
    }

    public static void Apply(IntPtr h, Rectangle target, bool topmost, bool enforce)
    {
        if (!saved.ContainsKey(h))
        {
            var s = new Saved
            {
                Style = (long)Win.GetWindowLongPtr(h, Win.GWL_STYLE),
                ExStyle = (long)Win.GetWindowLongPtr(h, Win.GWL_EXSTYLE),
                Rect = Win.GetRect(h),
                Zoomed = Win.IsZoomed(h),
            };
            if (s.Zoomed) Win.ShowWindow(h, 9); // SW_RESTORE
            saved[h] = s;
        }
        ApplyStyle(h, saved[h]);
        Win.SetWindowPos(h, topmost ? Win.HWND_TOPMOST : Win.HWND_TOP, target.X, target.Y, target.Width, target.Height,
            Win.SWP_FRAMECHANGED | Win.SWP_SHOWWINDOW);
        if (enforce) enforced[h] = new Enforce { Rect = target, Topmost = topmost };
        else enforced.Remove(h);
    }

    public static void Restore(IntPtr h)
    {
        Saved s;
        enforced.Remove(h);
        if (!saved.TryGetValue(h, out s)) return;
        saved.Remove(h);
        if (!Win.IsWindow(h)) return;
        Win.SetWindowLongPtr(h, Win.GWL_STYLE, new IntPtr(s.Style));
        Win.SetWindowLongPtr(h, Win.GWL_EXSTYLE, new IntPtr(s.ExStyle));
        Win.SetWindowPos(h, Win.HWND_NOTOPMOST, s.Rect.X, s.Rect.Y, s.Rect.Width, s.Rect.Height,
            Win.SWP_FRAMECHANGED | Win.SWP_SHOWWINDOW);
        if (s.Zoomed) Win.ShowWindow(h, 3); // SW_MAXIMIZE
    }

    public static void RestoreAll() { foreach (var h in saved.Keys.ToList()) Restore(h); }

    // re-applies the layout if the game resets size/position; gives up (and says so) if it keeps losing
    public static void EnforceTick()
    {
        foreach (var kv in enforced.ToList())
        {
            IntPtr h = kv.Key; Enforce e = kv.Value;
            if (!Win.IsWindow(h)) { enforced.Remove(h); saved.Remove(h); continue; }
            Rectangle r = Win.GetRect(h);
            bool same = Math.Abs(r.X - e.Rect.X) <= 2 && Math.Abs(r.Y - e.Rect.Y) <= 2 &&
                        Math.Abs(r.Width - e.Rect.Width) <= 2 && Math.Abs(r.Height - e.Rect.Height) <= 2;
            if (same) { e.Fails = 0; continue; }
            if (++e.Fails > 6)
            {
                enforced.Remove(h);
                notice = L.F("enforceGaveUp", Win.Title(h));
                continue;
            }
            ApplyStyle(h, saved[h]);
            Win.SetWindowPos(h, e.Topmost ? Win.HWND_TOPMOST : Win.HWND_TOP, e.Rect.X, e.Rect.Y, e.Rect.Width, e.Rect.Height,
                Win.SWP_FRAMECHANGED | Win.SWP_SHOWWINDOW);
        }
    }
}

// ------------------------------------------------------------------------------------------------
// Auto rule

class Rule
{
    public string Proc;
    public string Area;       // "all", "1,2" or "r:x,y,w,h"
    public bool Topmost, Enforce;

    public Rectangle Resolve()
    {
        var screens = Screen.AllScreens;
        if (Area != null && Area.StartsWith("r:"))
        {
            var p = Area.Substring(2).Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
            if (p.Length == 4) return new Rectangle(p[0], p[1], p[2], p[3]);
        }
        IEnumerable<int> idx = Enumerable.Range(0, screens.Length);
        if (Area != null && Area != "all")
        {
            var chosen = Area.Split(',').Select(s => { int v; return int.TryParse(s, out v) ? v - 1 : -1; })
                .Where(i => i >= 0 && i < screens.Length).ToList();
            if (chosen.Count > 0) idx = chosen;
        }
        return idx.Select(i => screens[i].Bounds).Aggregate(Rectangle.Union);
    }

    public string AreaText()
    {
        if (Area == null || Area == "all") return L.T("areaAll");
        if (Area.StartsWith("r:")) return L.F("areaCustom", Area.Substring(2).Replace(',', ' '));
        return L.F("areaMon", Area.Replace(",", " + "));
    }
}

// ------------------------------------------------------------------------------------------------
// Visual pieces

static class Theme
{
    public static readonly Color Bg = Color.FromArgb(18, 20, 26), Panel = Color.FromArgb(27, 30, 39),
        Panel2 = Color.FromArgb(37, 41, 54), Border = Color.FromArgb(52, 57, 73), Text = Color.FromArgb(232, 235, 242),
        Dim = Color.FromArgb(139, 147, 165), Accent = Color.FromArgb(82, 153, 255), AccentHi = Color.FromArgb(112, 175, 255),
        Good = Color.FromArgb(78, 201, 126), Warn = Color.FromArgb(242, 183, 64), Err = Color.FromArgb(244, 98, 98);

    public static GraphicsPath Round(Rectangle r, int rad)
    {
        var p = new GraphicsPath();
        int d = rad * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}

class Card : Panel
{
    string title = "";
    public string Title { get { return title; } set { title = value; Invalidate(); } }
    public Card()
    {
        DoubleBuffered = true;
        BackColor = Theme.Bg;
        ResizeRedraw = true;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var path = Theme.Round(r, 10))
        using (var fill = new SolidBrush(Theme.Panel))
        using (var pen = new Pen(Theme.Border))
        {
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }
        using (var f = new Font(Font, FontStyle.Bold))
        using (var b = new SolidBrush(Theme.Dim))
            g.DrawString(title.ToUpper(L.Culture), f, b, 14, 10);
    }
}

class FlatButton : Button
{
    public bool Primary;
    bool hover;
    public FlatButton(string text, bool primary)
    {
        Text = text; Primary = primary;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI Semibold", 9.5f);
        SetStyle(ControlStyles.Selectable, false);
        MouseEnter += delegate { hover = true; Invalidate(); };
        MouseLeave += delegate { hover = false; Invalidate(); };
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(Parent is Form ? Theme.Bg : Theme.Panel);
        Color c = !Enabled ? Theme.Panel2 : Primary ? (hover ? Theme.AccentHi : Theme.Accent) : (hover ? Theme.Border : Theme.Panel2);
        using (var path = Theme.Round(new Rectangle(0, 0, Width - 1, Height - 1), 7))
        using (var br = new SolidBrush(c))
            g.FillPath(br, path);
        var fc = !Enabled ? Theme.Dim : (Primary ? Color.White : Theme.Text);
        TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), fc,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}

class DarkCheck : CheckBox
{
    public DarkCheck()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Theme.Panel);
        var box = new Rectangle(1, (Height - 18) / 2, 18, 18);
        using (var path = Theme.Round(box, 4))
        {
            using (var fill = new SolidBrush(Checked ? Theme.Accent : Theme.Panel2)) g.FillPath(fill, path);
            using (var pen = new Pen(Checked ? Theme.Accent : Theme.Dim)) g.DrawPath(pen, path);
        }
        if (Checked)
            using (var tick = new Pen(Color.White, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                g.DrawLines(tick, new[] { new Point(box.X + 4, box.Y + 9), new Point(box.X + 8, box.Y + 13), new Point(box.X + 14, box.Y + 5) });
        TextRenderer.DrawText(g, Text, Font, new Rectangle(26, 0, Width - 26, Height), Enabled ? Theme.Text : Theme.Dim,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }
}

class DbListBox : ListBox
{
    public DbListBox()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
    }
    protected override void OnPaintBackground(PaintEventArgs e) { e.Graphics.Clear(BackColor); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        for (int i = 0; i < Items.Count; i++)
        {
            var r = GetItemRectangle(i);
            if (r.Bottom < 0 || r.Top > Height) continue;
            OnDrawItem(new DrawItemEventArgs(e.Graphics, Font, r, i,
                SelectedIndex == i ? DrawItemState.Selected : DrawItemState.None, ForeColor, BackColor));
        }
    }
}

// draws the monitor arrangement; clicking a monitor toggles it
class MonitorMap : Control
{
    public Screen[] Screens = Screen.AllScreens;
    public bool[] Selected = new bool[0];
    public Rectangle Target = Rectangle.Empty;
    public Rectangle WinRect = Rectangle.Empty;
    public bool ShowWin;
    public bool Interactive = true;
    public event EventHandler SelectionChanged;
    Rectangle union; float scale; PointF origin;

    public MonitorMap()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Theme.Panel;
        Cursor = Cursors.Hand;
        Reload();
    }

    public void Reload()
    {
        var old = Selected;
        Screens = Screen.AllScreens;
        Selected = new bool[Screens.Length];
        for (int i = 0; i < Selected.Length; i++) Selected[i] = i < old.Length ? old[i] : true;
        Invalidate();
    }

    void Layout2()
    {
        union = Screens.Select(s => s.Bounds).Aggregate(Rectangle.Union);
        if (!Target.IsEmpty) union = Rectangle.Union(union, Target);
        const int pad = 16;
        float sx = (Width - 2 * pad) / (float)Math.Max(union.Width, 1);
        float sy = (Height - 2 * pad) / (float)Math.Max(union.Height, 1);
        scale = Math.Min(sx, sy);
        origin = new PointF((Width - union.Width * scale) / 2f, (Height - union.Height * scale) / 2f);
    }

    RectangleF ToCtl(Rectangle r)
    {
        return new RectangleF(origin.X + (r.X - union.X) * scale, origin.Y + (r.Y - union.Y) * scale, r.Width * scale, r.Height * scale);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.Panel);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        if (Screens.Length == 0) return;
        Layout2();

        using (var big = new Font("Segoe UI Semibold", 20f))
        using (var small = new Font("Segoe UI", 8.5f))
        {
            for (int i = 0; i < Screens.Length; i++)
            {
                var rf = ToCtl(Screens[i].Bounds);
                rf.Inflate(-2, -2);
                var ri = Rectangle.Round(rf);
                bool sel = i < Selected.Length && Selected[i];
                using (var path = Theme.Round(ri, 6))
                using (var fill = new SolidBrush(sel ? Color.FromArgb(46, Theme.Accent) : Theme.Panel2))
                using (var pen = new Pen(sel ? Theme.Accent : Theme.Border, sel ? 2f : 1f))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(pen, path);
                }
                string num = (i + 1).ToString();
                string res = Screens[i].Bounds.Width + " x " + Screens[i].Bounds.Height + (Screens[i].Primary ? "  " + L.T("primaryTag") : "");
                var ns = g.MeasureString(num, big);
                using (var tb = new SolidBrush(sel ? Theme.Text : Theme.Dim))
                {
                    g.DrawString(num, big, tb, ri.X + (ri.Width - ns.Width) / 2, ri.Y + ri.Height / 2 - ns.Height / 2 - 8);
                    var rs = g.MeasureString(res, small);
                    g.DrawString(res, small, tb, ri.X + (ri.Width - rs.Width) / 2, ri.Y + ri.Height / 2 + ns.Height / 2 - 8);
                }
            }
        }

        if (ShowWin && !WinRect.IsEmpty)
        {
            var wr = Rectangle.Round(ToCtl(WinRect));
            using (var pen = new Pen(Theme.Dim, 1.5f) { DashStyle = DashStyle.Dash })
                g.DrawRectangle(pen, wr);
        }
        if (!Target.IsEmpty)
        {
            var tr = Rectangle.Round(ToCtl(Target));
            using (var pen = new Pen(Theme.Good, 2.5f))
                g.DrawRectangle(pen, tr);
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (!Interactive || Screens.Length == 0) return;
        Layout2();
        for (int i = 0; i < Screens.Length; i++)
        {
            if (ToCtl(Screens[i].Bounds).Contains(e.Location))
            {
                Selected[i] = !Selected[i];
                if (!Selected.Any(b => b)) Selected[i] = true; // keep at least one selected
                Invalidate();
                if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
                return;
            }
        }
    }
}

// ------------------------------------------------------------------------------------------------
// Main window

class MainForm : Form
{
    const int HK_TOGGLE = 1, HK_RESTORE_ALL = 2;
    static readonly string SettingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonitorYayici");
    static readonly string SettingsPath = Path.Combine(SettingsDir, "ayarlar.ini");

    readonly DbListBox lstWindows = new DbListBox();
    readonly TextBox txtSearch = new TextBox();
    readonly Label searchHint = new Label();
    readonly MonitorMap map = new MonitorMap();
    readonly RadioButton rbMon = new RadioButton(), rbCustom = new RadioButton();
    readonly NumericUpDown numX = new NumericUpDown(), numY = new NumericUpDown(), numW = new NumericUpDown(), numH = new NumericUpDown();
    readonly DarkCheck chkTop = new DarkCheck(), chkEnf = new DarkCheck(), chkRules = new DarkCheck(),
        chkStartup = new DarkCheck(), chkTray = new DarkCheck();
    readonly ListView lvRules = new ListView();
    readonly Label lblTarget = new Label(), lblStatus = new Label();
    readonly Panel statusBar = new Panel();
    readonly FlatButton btnEn = new FlatButton("EN", false), btnTr = new FlatButton("TR", false);
    readonly System.Windows.Forms.Timer tick = new System.Windows.Forms.Timer(), verifyTimer = new System.Windows.Forms.Timer();
    readonly NotifyIcon tray = new NotifyIcon();
    readonly List<Rule> rules = new List<Rule>();
    readonly List<Action> translators = new List<Action>();
    readonly Dictionary<string, Image> icons = new Dictionary<string, Image>();
    readonly Dictionary<string, IntPtr> ruleSeen = new Dictionary<string, IntPtr>();
    readonly Dictionary<string, int> ruleTicks = new Dictionary<string, int>();
    readonly HashSet<IntPtr> ruleDone = new HashSet<IntPtr>();
    List<WinItem> allWindows = new List<WinItem>();
    string winSignature = "";
    int statusKind;
    bool exiting, loading = true, startHidden;
    int tickCount;
    IntPtr verifyH; Rectangle verifyRect;
    ContextMenuStrip trayMenu;

    public MainForm(bool startHidden)
    {
        this.startHidden = startHidden;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96f, 96f);
        Text = "Monitor Yayici";
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        ClientSize = new Size(1000, 700);
        MinimumSize = Size.Empty;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        BuildWindowsCard();
        BuildMapCard();
        BuildTargetCard();
        BuildActions();
        BuildRulesCard();
        BuildStatusBar();
        BuildTray();

        map.SelectionChanged += delegate { UpdateTarget(); };
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplayChanged;

        LoadSettings();
        loading = false;
        ApplyLanguage();
        RefreshWindows(true);
        UpdateTarget();

        tick.Interval = 1000;
        tick.Tick += delegate { OnTick(); };
        tick.Start();
        verifyTimer.Interval = 700;
        verifyTimer.Tick += delegate { verifyTimer.Stop(); Verify(); };

        HandleCreated += delegate { Win.DarkTitleBar(Handle); };
        Load += delegate
        {
            Win.RegisterHotKey(Handle, HK_TOGGLE, 0x1 | 0x2, (uint)Keys.B);
            Win.RegisterHotKey(Handle, HK_RESTORE_ALL, 0x1 | 0x2, (uint)Keys.R);
        };
        FormClosing += OnClosing;
        SetStatus(L.T("ready"), 0);
    }

    protected override void SetVisibleCore(bool value)
    {
        if (startHidden) { startHidden = false; value = false; if (!IsHandleCreated) CreateHandle(); }
        base.SetVisibleCore(value);
    }

    // ---------------------------------------------------------------- localization

    // registers a text setter that is re-run whenever the language changes
    void T(string key, Action<string> setter)
    {
        translators.Add(() => setter(L.T(key)));
    }

    void ApplyLanguage()
    {
        foreach (var t in translators) t();
        btnEn.Primary = L.Lang == "en"; btnTr.Primary = L.Lang == "tr";
        btnEn.Invalidate(); btnTr.Invalidate();
        RefreshRulesList();
        UpdateTarget();
        lstWindows.Invalidate();
        map.Invalidate();
    }

    void SetLang(string lang)
    {
        if (L.Lang == lang) return;
        L.Lang = lang;
        ApplyLanguage();
        SetStatus(L.T("ready"), 0);
        SaveSettings();
    }

    // ---------------------------------------------------------------- UI construction

    Label Lbl(Control parent, string text, int x, int y, int w, int h, bool dim)
    {
        var l = new Label { Text = text, Left = x, Top = y, Width = w, Height = h, ForeColor = dim ? Theme.Dim : Theme.Text, BackColor = Color.Transparent };
        parent.Controls.Add(l);
        return l;
    }

    void StyleNum(NumericUpDown n, int x, int y, int value)
    {
        n.Minimum = -30000; n.Maximum = 30000; n.Value = value;
        n.SetBounds(x, y, 76, 26);
        n.BackColor = Theme.Panel2; n.ForeColor = Theme.Text; n.BorderStyle = BorderStyle.FixedSingle;
        n.ValueChanged += delegate { if (!loading) { rbCustom.Checked = true; UpdateTarget(); } };
    }

    void StyleCheck(DarkCheck c, string key, int x, int y, int w)
    {
        c.SetBounds(x, y, w, 24);
        c.ForeColor = Theme.Text;
        c.CheckedChanged += delegate { if (!loading) SaveSettings(); };
        T(key, s => c.Text = s);
    }

    FlatButton Btn(Control parent, string key, bool primary, int x, int y, int w, int h)
    {
        var b = new FlatButton("", primary) { Left = x, Top = y, Width = w, Height = h };
        T(key, s => b.Text = s);
        parent.Controls.Add(b);
        return b;
    }

    Card NewCard(string key, int x, int y, int w, int h)
    {
        var card = new Card { Left = x, Top = y, Width = w, Height = h };
        T(key, s => card.Title = s);
        Controls.Add(card);
        return card;
    }

    void BuildWindowsCard()
    {
        var card = NewCard("cardWindows", 16, 16, 340, 610);
        var sbox = new Panel { Left = 14, Top = 38, Width = 312, Height = 32, BackColor = Theme.Panel2 };
        txtSearch.BorderStyle = BorderStyle.None; txtSearch.BackColor = Theme.Panel2; txtSearch.ForeColor = Theme.Text;
        txtSearch.Left = 10; txtSearch.Top = 7; txtSearch.Width = 292;
        txtSearch.TextChanged += delegate { RebuildList(); };
        sbox.Controls.Add(txtSearch);
        searchHint.ForeColor = Theme.Dim; searchHint.BackColor = Theme.Panel2; searchHint.Left = 10; searchHint.Top = 7; searchHint.AutoSize = true;
        T("search", s => searchHint.Text = s);
        txtSearch.TextChanged += delegate { searchHint.Visible = txtSearch.Text.Length == 0; };
        searchHint.Click += delegate { txtSearch.Focus(); };
        sbox.Controls.Add(searchHint); searchHint.BringToFront();
        card.Controls.Add(sbox);

        lstWindows.SetBounds(14, 80, 312, 470);
        lstWindows.BorderStyle = BorderStyle.None; lstWindows.BackColor = Theme.Panel; lstWindows.ForeColor = Theme.Text;
        lstWindows.DrawMode = DrawMode.OwnerDrawFixed; lstWindows.ItemHeight = 46;
        lstWindows.DrawItem += DrawWindowItem;
        lstWindows.SelectedIndexChanged += delegate { UpdateTarget(); };
        card.Controls.Add(lstWindows);

        Btn(card, "refresh", false, 14, 560, 100, 36).Click += delegate { RefreshWindows(true); };
        Btn(card, "findGame", false, 122, 560, 100, 36).Click += delegate { SelectGame(); };
    }

    void BuildMapCard()
    {
        var card = NewCard("cardMap", 372, 16, 612, 210);
        map.SetBounds(12, 34, 588, 128);
        card.Controls.Add(map);
        Btn(card, "selAll", false, 12, 166, 120, 32).Click += delegate
        {
            for (int i = 0; i < map.Selected.Length; i++) map.Selected[i] = true;
            map.Invalidate(); rbMon.Checked = true; UpdateTarget();
        };
        Btn(card, "primaryOnly", false, 140, 166, 120, 32).Click += delegate
        {
            for (int i = 0; i < map.Selected.Length; i++) map.Selected[i] = map.Screens[i].Primary;
            map.Invalidate(); rbMon.Checked = true; UpdateTarget();
        };
        lblTarget.SetBounds(276, 172, 324, 24);
        lblTarget.ForeColor = Theme.Good; lblTarget.BackColor = Color.Transparent; lblTarget.TextAlign = ContentAlignment.MiddleRight;
        card.Controls.Add(lblTarget);
    }

    void BuildTargetCard()
    {
        var card = NewCard("cardTarget", 372, 238, 612, 130);
        rbMon.Checked = true; rbMon.SetBounds(14, 36, 170, 24);
        rbCustom.SetBounds(14, 66, 110, 24);
        T("selMon", s => rbMon.Text = s);
        T("custom", s => rbCustom.Text = s);
        foreach (var r in new[] { rbMon, rbCustom })
        {
            r.ForeColor = Theme.Text; r.BackColor = Color.Transparent;
            r.CheckedChanged += delegate { if (!loading) { UpdateTarget(); SaveSettings(); } };
        }
        var vs = SystemInformation.VirtualScreen;
        StyleNum(numX, 146, 66, vs.X); StyleNum(numY, 254, 66, vs.Y); StyleNum(numW, 380, 66, vs.Width); StyleNum(numH, 504, 66, vs.Height);
        Lbl(card, "X", 126, 70, 18, 20, true); Lbl(card, "Y", 234, 70, 18, 20, true);
        var lw = Lbl(card, "", 340, 70, 38, 20, true); T("w", s => lw.Text = s);
        var lh = Lbl(card, "", 464, 70, 38, 20, true); T("h", s => lh.Text = s);
        StyleCheck(chkTop, "topmost", 200, 34, 400);
        StyleCheck(chkEnf, "enforce", 14, 98, 480);
        chkTop.Checked = true; chkEnf.Checked = false;
        card.Controls.AddRange(new Control[] { rbMon, rbCustom, numX, numY, numW, numH, chkTop, chkEnf });
    }

    void BuildActions()
    {
        Btn(this, "apply", true, 372, 380, 236, 44).Click += delegate { ApplySelected(); };
        Btn(this, "restore", false, 616, 380, 186, 44).Click += delegate { RestoreSelected(); };
        Btn(this, "restoreAll", false, 810, 380, 174, 44).Click += delegate
        {
            Spanner.RestoreAll(); lstWindows.Invalidate(); SetStatus(L.T("restoredAll"), 1);
        };
    }

    void BuildRulesCard()
    {
        var card = NewCard("cardRules", 372, 436, 612, 190);
        lvRules.SetBounds(12, 34, 588, 84);
        lvRules.View = View.Details; lvRules.FullRowSelect = true; lvRules.HeaderStyle = ColumnHeaderStyle.None;
        lvRules.BackColor = Theme.Panel2; lvRules.ForeColor = Theme.Text; lvRules.BorderStyle = BorderStyle.None;
        lvRules.Columns.Add("", 190); lvRules.Columns.Add("", 230); lvRules.Columns.Add("", 70); lvRules.Columns.Add("", 70);
        card.Controls.Add(lvRules);
        Btn(card, "addRule", false, 12, 124, 240, 30).Click += delegate { AddRule(); };
        Btn(card, "delete", false, 260, 124, 80, 30).Click += delegate { DeleteRule(); };
        StyleCheck(chkRules, "rulesAuto", 356, 126, 240);
        StyleCheck(chkStartup, "startup", 12, 158, 260);
        StyleCheck(chkTray, "trayClose", 290, 158, 300);
        chkTray.Checked = true;
        chkStartup.CheckedChanged += delegate { if (!loading) SetStartup(chkStartup.Checked); };
        card.Controls.AddRange(new Control[] { chkRules, chkStartup, chkTray });
    }

    void BuildStatusBar()
    {
        statusBar.SetBounds(16, 640, 968, 44);
        statusBar.BackColor = Theme.Panel;
        statusBar.Paint += delegate(object s, PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            Color c = statusKind == 2 ? Theme.Err : statusKind == 3 ? Theme.Warn : statusKind == 1 ? Theme.Good : Theme.Accent;
            using (var path = Theme.Round(new Rectangle(0, 0, statusBar.Width - 1, statusBar.Height - 1), 8))
            using (var pen = new Pen(Theme.Border)) g.DrawPath(pen, path);
            using (var br = new SolidBrush(c)) g.FillEllipse(br, 14, 16, 12, 12);
        };
        lblStatus.SetBounds(36, 4, 830, 36);
        lblStatus.BackColor = Color.Transparent; lblStatus.ForeColor = Theme.Text; lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        statusBar.Controls.Add(lblStatus);

        btnEn.SetBounds(876, 8, 40, 28); btnTr.SetBounds(920, 8, 40, 28);
        btnEn.Click += delegate { SetLang("en"); };
        btnTr.Click += delegate { SetLang("tr"); };
        statusBar.Controls.Add(btnEn); statusBar.Controls.Add(btnTr);
        Controls.Add(statusBar);
    }

    void BuildTray()
    {
        tray.Text = "Monitor Yayici";
        try { tray.Icon = Icon ?? SystemIcons.Application; } catch { tray.Icon = SystemIcons.Application; }
        trayMenu = new ContextMenuStrip();
        var show = trayMenu.Items.Add("", null, delegate { ShowMain(); });
        var toggle = trayMenu.Items.Add("", null, delegate { ToggleForeground(); });
        var all = trayMenu.Items.Add("", null, delegate { Spanner.RestoreAll(); });
        trayMenu.Items.Add(new ToolStripSeparator());
        var exit = trayMenu.Items.Add("", null, delegate { exiting = true; Close(); });
        T("trayShow", s => show.Text = s);
        T("trayToggle", s => toggle.Text = s);
        T("trayRestoreAll", s => all.Text = s);
        T("trayExit", s => exit.Text = s);
        tray.ContextMenuStrip = trayMenu;
        tray.DoubleClick += delegate { ShowMain(); };
        tray.Visible = true;
    }

    // ---------------------------------------------------------------- list

    void DrawWindowItem(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lstWindows.Items.Count) return;
        var w = (WinItem)lstWindows.Items[e.Index];
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        bool sel = (e.State & DrawItemState.Selected) != 0;
        var r = e.Bounds;
        g.FillRectangle(new SolidBrush(Theme.Panel), r);
        var inner = new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4);
        if (sel)
        {
            using (var path = Theme.Round(inner, 7))
            {
                using (var br = new SolidBrush(Theme.Panel2)) g.FillPath(br, path);
                using (var pen = new Pen(Theme.Accent, 1.5f)) g.DrawPath(pen, path);
            }
        }
        var ic = IconFor(w);
        if (ic != null) g.DrawImage(ic, inner.X + 10, inner.Y + (inner.Height - 24) / 2, 24, 24);
        using (var f = new Font("Segoe UI Semibold", 9.5f))
        using (var fd = new Font("Segoe UI", 8.5f))
        {
            var tb = new SolidBrush(Theme.Text); var db = new SolidBrush(Theme.Dim);
            var fmt = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            bool sp = Spanner.IsSpanned(w.H);
            int tx = inner.X + 44, tw = inner.Width - 54 - (sp ? 78 : 0);
            g.DrawString(w.Proc, f, tb, new RectangleF(tx, inner.Y + 4, tw, 18), fmt);
            g.DrawString(w.Title, fd, db, new RectangleF(tx, inner.Y + 22, inner.Width - 54, 16), fmt);
            if (sp)
            {
                var br = new Rectangle(inner.Right - 80, inner.Y + 6, 72, 18);
                using (var path = Theme.Round(br, 8))
                using (var fill = new SolidBrush(Color.FromArgb(50, Theme.Good))) g.FillPath(fill, path);
                using (var bf = new Font("Segoe UI Semibold", 7.5f))
                    TextRenderer.DrawText(g, L.T("spanned"), bf, br, Theme.Good, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            tb.Dispose(); db.Dispose(); fmt.Dispose();
        }
    }

    Image IconFor(WinItem w)
    {
        Image img;
        if (icons.TryGetValue(w.Proc, out img)) return img;
        try
        {
            string path = Process.GetProcessById((int)w.Pid).MainModule.FileName;
            using (var ico = Icon.ExtractAssociatedIcon(path))
                img = new Bitmap(ico.ToBitmap(), new Size(24, 24));
        }
        catch { img = null; }
        icons[w.Proc] = img;
        return img;
    }

    void RefreshWindows(bool force)
    {
        allWindows = Spanner.ListWindows(Handle);
        string sig = string.Join("|", allWindows.Select(w => w.H.ToString() + w.Title));
        if (!force && sig == winSignature) return;
        winSignature = sig;
        RebuildList();
    }

    void RebuildList()
    {
        var prev = Selected();
        string q = txtSearch.Text.Trim();
        lstWindows.BeginUpdate();
        lstWindows.Items.Clear();
        foreach (var w in allWindows)
        {
            if (q.Length > 0 && w.Proc.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 &&
                w.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
            lstWindows.Items.Add(w);
        }
        if (prev != null)
            for (int i = 0; i < lstWindows.Items.Count; i++)
                if (((WinItem)lstWindows.Items[i]).H == prev.H) { lstWindows.SelectedIndex = i; break; }
        lstWindows.EndUpdate();
        lstWindows.Invalidate();
    }

    WinItem Selected() { return lstWindows.SelectedItem as WinItem; }

    void SelectGame()
    {
        string[] names = { "BeamNG.drive.x64", "AssettoCorsaEVO", "eurotrucks2", "acs" };
        RefreshWindows(true);
        for (int i = 0; i < lstWindows.Items.Count; i++)
            if (names.Contains(((WinItem)lstWindows.Items[i]).Proc, StringComparer.OrdinalIgnoreCase)) { lstWindows.SelectedIndex = i; return; }
        SetStatus(L.T("gameNotFound"), 3);
    }

    // ---------------------------------------------------------------- target

    Rectangle TargetRect()
    {
        if (rbCustom.Checked) return new Rectangle((int)numX.Value, (int)numY.Value, (int)numW.Value, (int)numH.Value);
        var sel = new List<Rectangle>();
        for (int i = 0; i < map.Screens.Length; i++) if (map.Selected[i]) sel.Add(map.Screens[i].Bounds);
        if (sel.Count == 0) sel = map.Screens.Select(s => s.Bounds).ToList();
        return sel.Aggregate(Rectangle.Union);
    }

    string AreaSpec()
    {
        if (rbCustom.Checked)
            return string.Format(CultureInfo.InvariantCulture, "r:{0},{1},{2},{3}", numX.Value, numY.Value, numW.Value, numH.Value);
        if (map.Selected.All(b => b)) return "all";
        var idx = new List<string>();
        for (int i = 0; i < map.Selected.Length; i++) if (map.Selected[i]) idx.Add((i + 1).ToString());
        return string.Join(",", idx.ToArray());
    }

    void UpdateTarget()
    {
        var r = TargetRect();
        map.Target = r;
        var w = Selected();
        map.ShowWin = w != null;
        if (w != null) map.WinRect = Win.GetRect(w.H);
        map.Invalidate();
        lblTarget.Text = L.F("target", r.Width, r.Height, r.X, r.Y);
        if (!loading) SaveSettings();
    }

    void OnDisplayChanged(object s, EventArgs e)
    {
        BeginInvoke((MethodInvoker)delegate
        {
            map.Reload();
            var vs = SystemInformation.VirtualScreen;
            if (!rbCustom.Checked) { numX.Value = vs.X; numY.Value = vs.Y; numW.Value = vs.Width; numH.Value = vs.Height; }
            UpdateTarget();
            SetStatus(L.T("monChanged"), 0);
        });
    }

    // ---------------------------------------------------------------- applying

    void ApplySelected()
    {
        var w = Selected();
        if (w == null) { SetStatus(L.T("pickWindow"), 3); return; }
        DoApply(w.H, TargetRect(), chkTop.Checked, chkEnf.Checked);
    }

    void DoApply(IntPtr h, Rectangle r, bool topmost, bool enforce)
    {
        Spanner.Apply(h, r, topmost, enforce);
        verifyH = h; verifyRect = r;
        verifyTimer.Stop(); verifyTimer.Start();
        SetStatus(L.T("applying"), 0);
        lstWindows.Invalidate();
    }

    void Verify()
    {
        if (!Win.IsWindow(verifyH)) return;
        Rectangle now = Win.GetRect(verifyH);
        var r = verifyRect;
        bool okPos = Math.Abs(now.X - r.X) <= 2 && Math.Abs(now.Y - r.Y) <= 2;
        bool okSize = Math.Abs(now.Width - r.Width) <= 2 && Math.Abs(now.Height - r.Height) <= 2;
        string title = Win.Title(verifyH);
        if (okPos && okSize)
            SetStatus(L.F("applied", title, r.Width, r.Height, r.X, r.Y), 1);
        else if (okPos)
            SetStatus(L.F("locked", r.Width, r.Height, now.Width, now.Height), 2);
        else
            SetStatus(L.F("notPlaced", now.Width, now.Height, now.X, now.Y), 2);
        UpdateTarget();
        lstWindows.Invalidate();
    }

    void RestoreSelected()
    {
        var w = Selected();
        if (w == null) return;
        Spanner.Restore(w.H);
        lstWindows.Invalidate();
        UpdateTarget();
        SetStatus(L.F("restored", w.Title), 1);
    }

    void ToggleForeground()
    {
        IntPtr h = Win.GetForegroundWindow();
        if (h == IntPtr.Zero || h == Handle) return;
        if (Spanner.IsSpanned(h)) { Spanner.Restore(h); SetStatus(L.F("restored", Win.Title(h)), 1); lstWindows.Invalidate(); }
        else DoApply(h, TargetRect(), chkTop.Checked, chkEnf.Checked);
    }

    // ---------------------------------------------------------------- rules

    void AddRule()
    {
        var w = Selected();
        if (w == null) { SetStatus(L.T("ruleNeedsWin"), 3); return; }
        rules.RemoveAll(r => string.Equals(r.Proc, w.Proc, StringComparison.OrdinalIgnoreCase));
        rules.Add(new Rule { Proc = w.Proc, Area = AreaSpec(), Topmost = chkTop.Checked, Enforce = chkEnf.Checked });
        RefreshRulesList(); SaveSettings();
        chkRules.Checked = true;
        SetStatus(L.F("ruleAdded", w.Proc), 1);
    }

    void DeleteRule()
    {
        if (lvRules.SelectedIndices.Count == 0) return;
        rules.RemoveAt(lvRules.SelectedIndices[0]);
        RefreshRulesList(); SaveSettings();
    }

    void RefreshRulesList()
    {
        lvRules.Items.Clear();
        foreach (var r in rules)
        {
            var it = new ListViewItem(r.Proc);
            it.SubItems.Add(r.AreaText());
            it.SubItems.Add(r.Topmost ? L.T("yes") : "-");
            it.SubItems.Add(r.Enforce ? L.T("yes") : "-");
            lvRules.Items.Add(it);
        }
    }

    void RulesTick()
    {
        if (!chkRules.Checked) return;
        foreach (var rule in rules)
        {
            string key = rule.Proc;
            IntPtr h = Spanner.FindByProcess(rule.Proc);
            if (h == IntPtr.Zero || ruleDone.Contains(h)) { ruleSeen.Remove(key); continue; }
            IntPtr prev;
            if (!ruleSeen.TryGetValue(key, out prev) || prev != h) { ruleSeen[key] = h; ruleTicks[key] = 0; continue; }
            if (++ruleTicks[key] < 3) continue; // wait until the window has settled
            ruleDone.Add(h);
            DoApply(h, rule.Resolve(), rule.Topmost, rule.Enforce);
            SetStatus(L.F("ruleApplied", rule.Proc), 1);
        }
    }

    // ---------------------------------------------------------------- timer, tray, closing

    void OnTick()
    {
        Spanner.EnforceTick();
        string n = Spanner.TakeNotice();
        if (n != null) SetStatus(n, 3);
        RulesTick();
        if (Visible && WindowState != FormWindowState.Minimized && (++tickCount % 3) == 0) { RefreshWindows(false); }
    }

    void ShowMain()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        RefreshWindows(true);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0312)
        {
            int id = m.WParam.ToInt32();
            if (id == HK_TOGGLE) ToggleForeground();
            else if (id == HK_RESTORE_ALL) { Spanner.RestoreAll(); lstWindows.Invalidate(); }
        }
        base.WndProc(ref m);
    }

    void OnClosing(object s, FormClosingEventArgs e)
    {
        if (!exiting && chkTray.Checked && e.CloseReason != CloseReason.WindowsShutDown && e.CloseReason != CloseReason.ApplicationExitCall)
        {
            e.Cancel = true;
            Hide();
            tray.ShowBalloonTip(1500, "Monitor Yayici", L.T("balloon"), ToolTipIcon.Info);
            return;
        }
        Win.UnregisterHotKey(Handle, HK_TOGGLE);
        Win.UnregisterHotKey(Handle, HK_RESTORE_ALL);
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
        SaveSettings();
        tray.Visible = false;
        tray.Dispose();
    }

    void SetStatus(string text, int kind)
    {
        statusKind = kind;
        lblStatus.Text = text;
        statusBar.Invalidate();
    }

    // ---------------------------------------------------------------- settings

    void SetStartup(bool on)
    {
        try
        {
            using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (on) k.SetValue("MonitorYayici", "\"" + Application.ExecutablePath + "\" --tray");
                else k.DeleteValue("MonitorYayici", false);
            }
            SaveSettings();
        }
        catch (Exception ex) { SetStatus(L.F("startupFail", ex.Message), 2); }
    }

    void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            foreach (var line in File.ReadAllLines(SettingsPath))
            {
                var kv = line.Split(new[] { '=' }, 2);
                if (kv.Length != 2) continue;
                string v = kv[1];
                switch (kv[0])
                {
                    case "lang": L.Lang = v == "tr" ? "tr" : "en"; break;
                    case "topmost": chkTop.Checked = v == "1"; break;
                    case "enforce": chkEnf.Checked = v == "1"; break;
                    case "rules": chkRules.Checked = v == "1"; break;
                    case "tray": chkTray.Checked = v == "1"; break;
                    case "startup": chkStartup.Checked = v == "1"; break;
                    case "custom": rbCustom.Checked = v == "1"; rbMon.Checked = v != "1"; break;
                    case "mon":
                        var set = new HashSet<int>(v.Split(',').Select(s => { int i; return int.TryParse(s, out i) ? i - 1 : -1; }));
                        if (set.Any(i => i >= 0 && i < map.Selected.Length))
                            for (int i = 0; i < map.Selected.Length; i++) map.Selected[i] = set.Contains(i);
                        break;
                    case "rect":
                        var p = v.Split(',').Select(s => { int i; return int.TryParse(s, out i) ? i : 0; }).ToArray();
                        if (p.Length == 4 && p[2] > 0 && p[3] > 0) { numX.Value = p[0]; numY.Value = p[1]; numW.Value = p[2]; numH.Value = p[3]; }
                        break;
                    case "rule":
                        var f = v.Split('|');
                        if (f.Length == 4) rules.Add(new Rule { Proc = f[0], Area = f[1], Topmost = f[2] == "1", Enforce = f[3] == "1" });
                        break;
                }
            }
            map.Invalidate();
        }
        catch { }
    }

    void SaveSettings()
    {
        if (loading) return;
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var lines = new List<string>
            {
                "lang=" + L.Lang,
                "topmost=" + (chkTop.Checked ? "1" : "0"), "enforce=" + (chkEnf.Checked ? "1" : "0"),
                "rules=" + (chkRules.Checked ? "1" : "0"), "tray=" + (chkTray.Checked ? "1" : "0"),
                "startup=" + (chkStartup.Checked ? "1" : "0"), "custom=" + (rbCustom.Checked ? "1" : "0"),
                "mon=" + string.Join(",", Enumerable.Range(0, map.Selected.Length).Where(i => map.Selected[i]).Select(i => (i + 1).ToString()).ToArray()),
                string.Format(CultureInfo.InvariantCulture, "rect={0},{1},{2},{3}", numX.Value, numY.Value, numW.Value, numH.Value),
            };
            foreach (var r in rules)
                lines.Add("rule=" + r.Proc + "|" + r.Area + "|" + (r.Topmost ? "1" : "0") + "|" + (r.Enforce ? "1" : "0"));
            File.WriteAllLines(SettingsPath, lines.ToArray());
        }
        catch { }
    }
}

// ------------------------------------------------------------------------------------------------

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        Win.SetProcessDPIAware();

        // command line mode: --proc name [--monitors 1,2] [--topmost] [--enforce]
        string proc = null, mons = null; bool top = false, enf = false, tray = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--proc" && i + 1 < args.Length) proc = args[++i];
            else if (args[i] == "--monitors" && i + 1 < args.Length) mons = args[++i];
            else if (args[i] == "--topmost") top = true;
            else if (args[i] == "--enforce") enf = true;
            else if (args[i] == "--tray") tray = true;
        }
        if (proc != null)
        {
            IntPtr h = Spanner.FindByProcess(proc);
            if (h == IntPtr.Zero) return 1;
            var rule = new Rule { Proc = proc, Area = mons ?? "all", Topmost = top, Enforce = enf };
            Spanner.Apply(h, rule.Resolve(), top, enf);
            if (!enf) return 0;
            while (Win.IsWindow(h)) { Thread.Sleep(1000); Spanner.EnforceTick(); } // enforce mode: stay until the window closes
            return 0;
        }

        bool created;
        using (var mutex = new Mutex(true, "MonitorYayici_SingleInstance", out created))
        {
            if (!created) return 0; // already running
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e)
            {
                try
                {
                    string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonitorYayici");
                    Directory.CreateDirectory(dir);
                    File.AppendAllText(Path.Combine(dir, "hata.log"), DateTime.Now + "  " + e.Exception + Environment.NewLine);
                }
                catch { }
            };
            Application.Run(new MainForm(tray));
        }
        return 0;
    }
}
