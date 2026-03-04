using System;
using System.Collections.Generic;

namespace apctray2;

/// <summary>
/// Representa um perfil de conexão com um nobreak via NIS
/// </summary>
public class UpsProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Nobreak";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 3551;
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public UpsProfile() { }

    public UpsProfile(string name, string host, int port, string description = "")
    {
        Name = name;
        Host = host;
        Port = port;
        Description = description;
    }

    public override string ToString() => $"{Name} ({Host}:{Port})";
}

/// <summary>
/// Gerencia múltiplos perfis de UPS
/// </summary>
public class UpsProfileManager
{
    public List<UpsProfile> Profiles { get; set; } = new();
    public string ActiveProfileId { get; set; } = "";

    public UpsProfile? GetActiveProfile()
    {
        return Profiles.Find(p => p.Id == ActiveProfileId);
    }

    public void SetActiveProfile(string profileId)
    {
        if (Profiles.Exists(p => p.Id == profileId))
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
        Profiles.RemoveAll(p => p.Id == profileId);
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
