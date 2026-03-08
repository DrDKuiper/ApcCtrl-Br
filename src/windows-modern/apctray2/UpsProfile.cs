using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;

namespace apctray2;

/// <summary>
/// Representa um perfil de conexão com um nobreak via NIS
/// </summary>
public class UpsProfile : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _id = Guid.NewGuid().ToString();
    public string Id 
    { 
        get => _id; 
        set { _id = value; OnPropertyChanged(nameof(Id)); } 
    }

    private string _name = "Nobreak";
    public string Name 
    { 
        get => _name; 
        set { _name = value; OnPropertyChanged(nameof(Name)); } 
    }

    private string _host = "127.0.0.1";
    public string Host 
    { 
        get => _host; 
        set { _host = value; OnPropertyChanged(nameof(Host)); } 
    }

    private int _port = 3551;
    public int Port 
    { 
        get => _port; 
        set { _port = value; OnPropertyChanged(nameof(Port)); } 
    }

    private string _description = "";
    public string Description 
    { 
        get => _description; 
        set { _description = value; OnPropertyChanged(nameof(Description)); } 
    }

    private bool _isActive = true;
    public bool IsActive 
    { 
        get => _isActive; 
        set { _isActive = value; OnPropertyChanged(nameof(IsActive)); } 
    }

    public UpsProfile() { }

    public UpsProfile(string name, string host, int port, string description = "")
    {
        _id = Guid.NewGuid().ToString();
        _name = name;
        _host = host;
        _port = port;
        _description = description;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        SimpleLogger.Info($"UpsProfile.PropertyChanged: {propertyName} = {GetPropertyValue(propertyName)}");
    }

    private object? GetPropertyValue(string propertyName) => propertyName switch
    {
        nameof(Id) => _id,
        nameof(Name) => _name,
        nameof(Host) => _host,
        nameof(Port) => _port,
        nameof(Description) => _description,
        nameof(IsActive) => _isActive,
        _ => null
    };

    public override string ToString() => $"{Name} ({Host}:{Port})";
}

/// <summary>
/// Gerencia múltiplos perfis de UPS
/// </summary>
public class UpsProfileManager
{
    public ObservableCollection<UpsProfile> Profiles { get; set; } = new();
    public string ActiveProfileId { get; set; } = "";

    public UpsProfile? GetActiveProfile()
    {
        return Profiles.Count > 0 ? Profiles.FirstOrDefault(p => p.Id == ActiveProfileId) : null;
    }

    public void SetActiveProfile(string profileId)
    {
        if (Profiles.Any(p => p.Id == profileId))
        {
            ActiveProfileId = profileId;
        }
    }

    public void AddProfile(UpsProfile profile)
    {
        Profiles.Add(profile);
        if (Profiles.Count == 1)
        {
            ActiveProfileId = profile.Id;
        }
    }

    public void RemoveProfile(string profileId)
    {
        var profile = Profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile != null)
        {
            Profiles.Remove(profile);
        }
        if (ActiveProfileId == profileId && Profiles.Count > 0)
        {
            ActiveProfileId = Profiles[0].Id;
        }
    }

    public void EnsureDefaultProfile()
    {
        if (Profiles.Count == 0)
        {
            var defaultProfile = new UpsProfile("Nobreak Local", "127.0.0.1", 3551, "Servidor local");
            AddProfile(defaultProfile);
        }
    }
}
