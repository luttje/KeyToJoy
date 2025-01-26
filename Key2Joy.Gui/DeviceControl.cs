using System;
using System.Drawing;
using System.Windows.Forms;
using Key2Joy.LowLevelInput;

namespace Key2Joy.Gui;

public partial class DeviceControl : UserControl
{
    private const int FADE_DURATION = 2;
    private const int BORDER_WIDTH = 2;
    private DateTime lastActivityOccurred = DateTime.Now;
    private readonly Timer fadeTimer = new();
    private readonly IGamePadInfo device;

    private DeviceControl()
    {
        this.InitializeComponent();

        this.fadeTimer.Interval = 50;
        this.fadeTimer.Tick += this.FadeTimer_Tick;
        this.fadeTimer.Start();

        this.pnlDevice.Paint += this.DeviceControl_Paint;
        this.Layout += this.DeviceControl_Layout;
    }

    public DeviceControl(IGamePadInfo device)
        : this()
    {
        this.device = device;
        this.lblIndex.Text = $"#{device.Index}";
        this.lblDevice.Text = device.Name;
        //this.picImage.Image = null;
        device.ActivityOccurred += this.Device_ActivityOccurred;
    }

    private void DeviceControl_Layout(object sender, LayoutEventArgs e)
    {
        if (this.Height == this.Width)
        {
            return;
        }

        this.Height = this.Width;
    }

    private void Device_ActivityOccurred(object sender, GamePadActivityOccurredEventArgs e)
    {
        this.lastActivityOccurred = DateTime.Now;

        this.Invoke((MethodInvoker)delegate
        {
            try
            {
                this.fadeTimer.Stop();
                this.fadeTimer.Start();
                this.Invalidate();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Ignore (happens if we spam the chkArmed in MainForm)
            }
        });
    }

    private void FadeTimer_Tick(object sender, EventArgs e)
    {
        if ((DateTime.Now - this.lastActivityOccurred).TotalSeconds <= 2)
        {
            this.Invalidate();
        }
        else
        {
            this.fadeTimer.Stop();
        }
    }

    private void DeviceControl_Paint(object sender, PaintEventArgs e)
    {
        var owner = (Control)sender;
        var g = e.Graphics;

        var elapsed = (DateTime.Now - this.lastActivityOccurred).TotalSeconds;

        var currentColor = this.InterpolateColors(Color.White, Color.Gold, elapsed / FADE_DURATION);
        using var brush = new SolidBrush(currentColor);
        g.FillRectangle(brush, BORDER_WIDTH, BORDER_WIDTH, owner.Width - (BORDER_WIDTH * 2), owner.Height - (BORDER_WIDTH * 2));
    }

    private Color InterpolateColors(Color start, Color end, double ratio)
    {
        var r = (int)((start.R * (1 - ratio)) + (end.R * ratio));
        var g = (int)((start.G * (1 - ratio)) + (end.G * ratio));
        var b = (int)((start.B * (1 - ratio)) + (end.B * ratio));

        return Color.FromArgb(
            Math.Max(r, 0),
            Math.Max(g, 0),
            Math.Max(b, 0)
        );
    }
}
