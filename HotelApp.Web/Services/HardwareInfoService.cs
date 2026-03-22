using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace HotelApp.Web.Services;

/// <summary>
/// Reads hardware identifiers (MAC address, hard-disk serial, motherboard serial)
/// from the host machine. Uses PowerShell/CIM on Windows and shell commands on
/// macOS/Linux. No extra NuGet packages required.
/// </summary>
public class HardwareInfoService : IHardwareInfoService
{
    public HardwareInfo GetHardwareInfo()
    {
        return new HardwareInfo
        {
            MacId            = GetMacAddress(),
            HardDiskSerial   = GetHardDiskSerial(),
            MotherboardSerial = GetMotherboardSerial()
        };
    }

    // ─── MAC Address ───────────────────────────────────────────────────────────

    private static string GetMacAddress()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // IMPORTANT: do NOT use ifconfig or .NET NetworkInterface on macOS.
                // Since macOS 15 (Sequoia), Wi-Fi uses a randomized Private Address
                // per SSID by default, so ifconfig returns a different locally-
                // administered MAC whenever the network changes.
                // networksetup -listallhardwareports always reports the PERMANENT
                // factory-burned hardware MAC regardless of the privacy setting.
                var mac = RunShell(
                    "networksetup -listallhardwareports 2>/dev/null" +
                    " | grep -A3 'Wi-Fi'" +
                    " | grep 'Ethernet Address'" +
                    " | tr -d ':'" +
                    " | awk '{print toupper($NF)}'");
                if (!string.IsNullOrWhiteSpace(mac) && mac != "000000000000")
                    return mac.Trim();

                // Fallback: IORegistry firmware-level permanent MAC for built-in controller
                var ioMac = RunShell(
                    "ioreg -rc IONetworkController -k IOBuiltin 2>/dev/null" +
                    " | grep -m1 'IOMACAddress'" +
                    " | awk -F'[<>]' '{print toupper($2)}'" +
                    " | tr -d ':'");
                if (!string.IsNullOrWhiteSpace(ioMac) && ioMac != "000000000000")
                    return ioMac.Trim();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Win32_NetworkAdapter with PhysicalAdapter=$true filters out Hyper-V,
                // VMware, loopback and other virtual adapters. Sort by Index so the
                // selection is stable across reboots.
                var result = RunWindowsPs(
                    "$n = Get-CimInstance Win32_NetworkAdapter | " +
                    "Where-Object { $_.PhysicalAdapter -eq $true -and $_.MACAddress -ne $null } | " +
                    "Sort-Object Index | Select-Object -First 1; " +
                    "if ($n) { $n.MACAddress -replace ':','' }");
                if (!string.IsNullOrWhiteSpace(result) && result != "000000000000")
                    return result.Trim().ToUpperInvariant();

                // Fallback: .NET NetworkInterface (filters loopback/tunnel, sorted by name)
                var nic = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                             && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                             && n.OperationalStatus == OperationalStatus.Up)
                    .OrderBy(n => n.Name)
                    .FirstOrDefault();
                if (nic != null)
                {
                    var mac = nic.GetPhysicalAddress().ToString();
                    if (!string.IsNullOrWhiteSpace(mac) && mac != "000000000000")
                        return mac.ToUpperInvariant();
                }
            }
            else
            {
                // Linux: .NET NetworkInterface sorted by name for determinism
                var nic = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                             && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                             && n.OperationalStatus == OperationalStatus.Up)
                    .OrderBy(n => n.Name)
                    .FirstOrDefault();
                if (nic != null)
                {
                    var mac = nic.GetPhysicalAddress().ToString();
                    if (!string.IsNullOrWhiteSpace(mac) && mac != "000000000000")
                        return mac.ToUpperInvariant();
                }
            }
        }
        catch { /* fall through */ }

        return "NOINSTANCESAVAILABLE";
    }

    // ─── Hard Disk Serial ──────────────────────────────────────────────────────

    private static string GetHardDiskSerial()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Filter out blank serials (common on VMs/SANs), sort by DeviceID for stability.
            var result = RunWindowsPs(
                "$d = Get-CimInstance Win32_DiskDrive | " +
                "Where-Object { $_.SerialNumber -ne $null -and $_.SerialNumber.Trim() -ne '' } | " +
                "Sort-Object DeviceID | Select-Object -First 1; " +
                "if ($d) { $d.SerialNumber.Trim() }");
            if (!string.IsNullOrWhiteSpace(result))
                return result.Trim().ToUpperInvariant();

            // Fallback 1: BIOS / firmware UUID — always available, even on VMs
            var biosUuid = RunWindowsPs(
                "(Get-CimInstance Win32_ComputerSystemProduct).UUID");
            if (!string.IsNullOrWhiteSpace(biosUuid) &&
                biosUuid != "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")
                return biosUuid.Trim().ToUpperInvariant().Replace("-", "");

            // Fallback 2: wmic (older Windows)
            var wmicRaw = RunWindowsCmd("wmic diskdrive get SerialNumber /value");
            var wmicParsed = ParseWmicValue(wmicRaw, "SerialNumber");
            if (!string.IsNullOrWhiteSpace(wmicParsed))
                return wmicParsed.ToUpperInvariant();

            return "NOINSTANCESAVAILABLE";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Use the APFS/HFS+ Volume UUID of the root mount point.
            // This is the permanent identity of the physical boot volume and is
            // stable across reboots. It only changes if macOS is reinstalled or
            // the volume is erased — the right semantics for a "disk serial".
            var volUuid = RunShell(
                "diskutil info / 2>/dev/null | grep 'Volume UUID' | awk '{print $NF}'");
            if (!string.IsNullOrWhiteSpace(volUuid))
                return volUuid.Trim().ToUpperInvariant().Replace("-", "");

            // Fallback: IOPlatformUUID (logic board UUID, always available)
            var uuid = RunShell(
                "ioreg -rd1 -c IOPlatformExpertDevice 2>/dev/null | awk -F'\"' '/IOPlatformUUID/{print $4}'");
            if (!string.IsNullOrWhiteSpace(uuid))
                return uuid.Trim().ToUpperInvariant().Replace("-", "");

            return "NOINSTANCESAVAILABLE";
        }

        // Linux
        var linuxResult = RunShell("lsblk -d -n -o SERIAL 2>/dev/null | head -1");
        if (!string.IsNullOrWhiteSpace(linuxResult))
            return linuxResult.Trim().ToUpperInvariant();

        return "NOINSTANCESAVAILABLE";
    }

    // ─── Motherboard Serial ────────────────────────────────────────────────────

    private static string GetMotherboardSerial()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Some OEMs ship boards with placeholder strings — filter those out.
            var bad = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "", "default string", "to be filled by o.e.m.", "none", "n/a" };

            var result = RunWindowsPs(
                "(Get-CimInstance Win32_BaseBoard | Select-Object -First 1).SerialNumber");
            if (!string.IsNullOrWhiteSpace(result) && !bad.Contains(result.Trim()))
                return result.Trim().ToUpperInvariant();

            // Fallback: BIOS/firmware product UUID (reliable on physical machines and VMs)
            var biosUuid = RunWindowsPs(
                "(Get-CimInstance Win32_ComputerSystemProduct).UUID");
            if (!string.IsNullOrWhiteSpace(biosUuid) &&
                biosUuid != "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")
                return biosUuid.Trim().ToUpperInvariant().Replace("-", "");

            // Fallback 2: wmic (older Windows)
            var wmicRaw = RunWindowsCmd("wmic baseboard get SerialNumber /value");
            var wmicParsed = ParseWmicValue(wmicRaw, "SerialNumber");
            if (!string.IsNullOrWhiteSpace(wmicParsed) && !bad.Contains(wmicParsed.Trim()))
                return wmicParsed.ToUpperInvariant();

            return "NOINSTANCESAVAILABLE";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Mac hardware serial number is the board-level permanent identifier.
            var result = RunShell(
                "system_profiler SPHardwareDataType 2>/dev/null | grep -m1 'Serial Number' | awk -F': ' '{print $2}'");
            if (!string.IsNullOrWhiteSpace(result))
                return result.Trim().ToUpperInvariant();

            return "NOINSTANCESAVAILABLE";
        }

        // Linux
        var linuxResult = RunShell("cat /sys/class/dmi/id/board_serial 2>/dev/null");
        if (!string.IsNullOrWhiteSpace(linuxResult))
            return linuxResult.Trim().ToUpperInvariant();

        return "NOINSTANCESAVAILABLE";
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static string RunWindowsPs(string psCommand)
    {
        try
        {
            // Use ArgumentList (not a combined string) so single-quotes, dollar signs,
            // and other special characters inside psCommand are never misinterpreted.
            var psi = new ProcessStartInfo("powershell.exe")
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-NonInteractive");
            psi.ArgumentList.Add("-ExecutionPolicy");
            psi.ArgumentList.Add("Bypass");
            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add(psCommand);
            using var p = Process.Start(psi);
            if (p == null) return string.Empty;
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(10_000);
            return output.Trim();
        }
        catch { return string.Empty; }
    }

    private static string RunWindowsCmd(string command)
    {
        try
        {
            var psi = new ProcessStartInfo("cmd.exe", $"/c {command}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            using var p = Process.Start(psi);
            if (p == null) return string.Empty;
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(10_000);
            return output;
        }
        catch { return string.Empty; }
    }

    private static string RunShell(string command)
    {
        try
        {
            var psi = new ProcessStartInfo("/bin/sh", $"-c \"{command}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            using var p = Process.Start(psi);
            if (p == null) return string.Empty;
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(10_000);
            return output.Trim();
        }
        catch { return string.Empty; }
    }

    private static string? ParseWmicValue(string output, string key)
    {
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
            {
                var value = trimmed[(key.Length + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }
        return null;
    }
}
