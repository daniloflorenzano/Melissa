using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Melissa.DesktopAvaloniaClient.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    // A propriedade que armazena o endereço do servidor na janela de configurações
    [ObservableProperty]
    private string _serverAddress;

    // Ação para fechar a janela com o resultado
    private readonly Action<bool> _closeAction;

    public SettingsViewModel(string currentServerAddress, Action<bool> closeAction)
    {
        var setupSettings = new SetupSettings("settings.json");
        setupSettings.CreateSettingsFileIfNotExist();
        setupSettings.SaveNewServerAddress(currentServerAddress);
        
        _serverAddress = currentServerAddress;
        _closeAction = closeAction;
    }

    // Comando para o botão "Salvar".
    [RelayCommand]
    private void Save()
    {
        // Chama a ação de fechar, passando 'true' para indicar que foi salvo
        _closeAction(true);
    }

    // Comando para o botão "Cancelar"
    [RelayCommand]
    private void Cancel()
    {
        // Chama a ação de fechar, passando 'false' para indicar que foi cancelado
        _closeAction(false);
    }
}