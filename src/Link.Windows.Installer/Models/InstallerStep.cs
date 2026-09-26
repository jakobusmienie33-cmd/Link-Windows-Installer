using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Link.Windows.Installer.Models;

public sealed class InstallerStep : INotifyPropertyChanged
{
    private bool _isComplete;
    private bool _isCurrent;

    public InstallerStep(int number, string title)
    {
        Number = number;
        Title = title;
    }

    public int Number { get; }
    public string Title { get; }

    public bool IsComplete
    {
        get => _isComplete;
        set
        {
            if (_isComplete == value) return;
            _isComplete = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StateText));
        }
    }

    public bool IsCurrent
    {
        get => _isCurrent;
        set
        {
            if (_isCurrent == value) return;
            _isCurrent = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StateText));
        }
    }

    public string StateText => IsComplete ? "Done" : IsCurrent ? "Current" : string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
