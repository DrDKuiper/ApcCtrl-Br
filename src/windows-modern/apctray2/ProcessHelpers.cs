using System;
using System.Diagnostics;
using System.Linq;

namespace apctray2;

public static class ProcessHelpers
{
    public static void KillOtherInstances()
    {
        try
        {
            var current = Process.GetCurrentProcess();
            var name = current.ProcessName;
            foreach (var p in Process.GetProcessesByName(name))
            {
                if (p.Id != current.Id)
                {
                    try { p.Kill(entireProcessTree: true); } catch { }
                }
            }
        }
        catch { }
    }
}
