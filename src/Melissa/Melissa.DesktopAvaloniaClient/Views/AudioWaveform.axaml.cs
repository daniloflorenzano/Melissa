using System;
using System.Collections.Generic;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Melissa.DesktopAvaloniaClient.Views;

public partial class AudioWaveform : UserControl
{
    public static readonly StyledProperty<bool> IsAnimatingProperty =
        AvaloniaProperty.Register<AudioWaveform, bool>(nameof(IsAnimating));

    public bool IsAnimating
    {
        get => GetValue(IsAnimatingProperty);
        set => SetValue(IsAnimatingProperty, value);
    }

    public static readonly StyledProperty<List<short>> AudioDataProperty =
        AvaloniaProperty.Register<AudioWaveform, List<short>>(nameof(AudioData));

    public List<short> AudioData
    {
        get => GetValue(AudioDataProperty);
        set => SetValue(AudioDataProperty, value);
    }

    private Timer? _timer;
    private double _phase = 0;

    public AudioWaveform()
    {
        InitializeComponent();
        AudioData = new List<short>();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsAnimatingProperty)
        {
            if (IsAnimating)
                StartAnimation();
            else
                StopAnimation();
        }
        else if (change.Property == AudioDataProperty)
        {
            InvalidateVisual();
        }
    }

    private void StartAnimation()
    {
        _timer = new Timer(33); // ~30 FPS
        _timer.Elapsed += (s, e) =>
        {
            var samples = 200;
            var amplitude = short.MaxValue / 2;
            var freq = 2.0; // número de ciclos na tela
            var data = new List<short>(samples);
            for (int i = 0; i < samples; i++)
            {
                double t = (double)i / samples;
                double value = Math.Sin(2 * Math.PI * (freq * t + _phase));
                data.Add((short)(amplitude * value));
            }

            _phase += 0.03;
            if (_phase > 1) _phase -= 1;

            Dispatcher.UIThread.Post(() => AudioData = data);
        };
        _timer.Start();
    }

    private void StopAnimation()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
        AudioData = new List<short>();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (AudioData == null || AudioData.Count == 0)
            return;

        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0)
            return;

        var pen = new Pen(Brushes.Orange, 10);
        var maxVal = short.MaxValue;
        var center = height / 2;

        var geo = new StreamGeometry();
        using (var gc = geo.Open())
        {
            gc.BeginFigure(new Point(0, center), false);
            for (int i = 0; i < AudioData.Count; i++)
            {
                var x = (double)i / AudioData.Count * width;
                var y = center + (double)AudioData[i] / maxVal * center;
                gc.LineTo(new Point(x, y));
            }

            gc.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geo);
    }
}