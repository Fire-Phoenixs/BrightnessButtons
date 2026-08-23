using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BrightnessButtons;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new BrightnessForm());
    }
}

internal sealed class BrightnessForm : Form
{
    private const int Step = 5;
    private const int Minimum = 50;
    private readonly GammaController controller = new();
    private readonly Label valueLabel = new();
    private int level = 100;

    public BrightnessForm()
    {
        Text = "软件亮度";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(368, 214);
        Font = new Font("Microsoft YaHei UI", 10F);

        var title = new Label
        {
            Text = "外接显示器软件亮度",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold),
            Bounds = new Rectangle(20, 20, 328, 34),
        };

        valueLabel.AutoSize = false;
        valueLabel.TextAlign = ContentAlignment.MiddleCenter;
        valueLabel.Font = new Font("Segoe UI", 28F, FontStyle.Bold);
        valueLabel.Bounds = new Rectangle(20, 58, 328, 56);

        var downButton = MakeButton("−  调低亮度", new Rectangle(25, 130, 150, 54));
        downButton.Click += (_, _) => SetLevel(level - Step);

        var upButton = MakeButton("＋  调高亮度", new Rectangle(193, 130, 150, 54));
        upButton.Click += (_, _) => SetLevel(level + Step);

        Controls.AddRange([title, valueLabel, downButton, upButton]);

        try
        {
            controller.Apply(1d);
            UpdateLabel();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法读取显示器的色彩映射。\n\n{ex.Message}", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
        }
    }

    private Button MakeButton(string text, Rectangle bounds) => new()
    {
        Text = text,
        Bounds = bounds,
        Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
        UseVisualStyleBackColor = true,
    };

    private void SetLevel(int requestedLevel)
    {
        var newLevel = Math.Clamp(requestedLevel, Minimum, 100);
        if (newLevel == level)
            return;

        try
        {
            controller.Apply(newLevel / 100d);
            level = newLevel;
            UpdateLabel();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"未能应用亮度。\n\n{ex.Message}", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateLabel() => valueLabel.Text = $"{level}%";
}

internal sealed class GammaController
{
    public void Apply(double factor)
    {
        var ramp = GammaRamp.Create(factor);
        var screens = Screen.AllScreens;
        if (screens.Length == 0)
            throw new InvalidOperationException("未找到活动显示器。");

        foreach (var screen in screens)
        {
            using var dc = DisplayDc.Open(screen.DeviceName);
            if (!NativeMethods.ApplyDeviceGammaRamp(dc.Handle, ramp))
                throw new InvalidOperationException($"无法应用 {screen.DeviceName} 的亮度设置。");
        }
    }
}

internal static class GammaRamp
{
    public const int Entries = 256;

    public static ushort[] Create(double factor)
    {
        var result = new ushort[Entries * 3];
        for (var valueIndex = 0; valueIndex < Entries; valueIndex++)
        {
            var value = (ushort)Math.Clamp(Math.Round(valueIndex * 257d * factor), 0, ushort.MaxValue);
            for (var channel = 0; channel < 3; channel++)
                result[channel * Entries + valueIndex] = value;
        }
        return result;
    }
}

internal sealed class DisplayDc : IDisposable
{
    private DisplayDc(IntPtr handle) => Handle = handle;
    public IntPtr Handle { get; }

    public static DisplayDc Open(string deviceName)
    {
        var handle = NativeMethods.CreateDC("DISPLAY", deviceName, null, IntPtr.Zero);
        if (handle == IntPtr.Zero)
            throw new InvalidOperationException($"无法打开显示器 {deviceName}。");
        return new DisplayDc(handle);
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
            NativeMethods.DeleteDC(Handle);
    }
}

internal static class NativeMethods
{
    [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr CreateDC(string driver, string device, string? output, IntPtr initData);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetDeviceGammaRamp(IntPtr hdc, IntPtr ramp);

    public static bool ApplyDeviceGammaRamp(IntPtr hdc, ushort[] ramp)
    {
        var bytes = ramp.Length * sizeof(ushort);
        var buffer = Marshal.AllocHGlobal(bytes);
        try
        {
            var rawBytes = new byte[bytes];
            Buffer.BlockCopy(ramp, 0, rawBytes, 0, bytes);
            Marshal.Copy(rawBytes, 0, buffer, bytes);
            return SetDeviceGammaRamp(hdc, buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
