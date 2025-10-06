using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Melissa.DesktopAvaloniaClient.ViewModels;

namespace Melissa.DesktopAvaloniaClient.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Captura os eventos mesmo que o Button consuma
        MicButton.AddHandler(PointerPressedEvent, OnMicButtonPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        MicButton.AddHandler(PointerReleasedEvent, OnMicButtonReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
    }
    
    public async void OnMicButtonPressed(object? sender, PointerPressedEventArgs e)
    {
        Console.WriteLine("Botão do microfone pressionado, iniciando captura de áudio...");
        await ((MainWindowViewModel)DataContext!).StartAudioCaptureAsync();
    }

    public void OnMicButtonReleased(object? sender, PointerReleasedEventArgs e)
    {
        Console.WriteLine("Botão do microfone liberado, parando captura de áudio...");
        ((MainWindowViewModel)DataContext!).StopAudioCapture();
    }
}