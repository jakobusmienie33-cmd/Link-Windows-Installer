using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Link.Windows.Installer.Core.Models;

namespace Link.Windows.Installer.Services;

public sealed class WindowsCredentialStore
{
    private const uint CredentialTypeGeneric = 1;
    private const uint CredentialPersistLocalMachine = 2;

    public string BuildDatabaseTarget(DatabaseSetupOptions options)
    {
        var host = string.IsNullOrWhiteSpace(options.Host) ? "cloud" : options.Host;
        var database = string.IsNullOrWhiteSpace(options.DatabaseName) ? "default" : options.DatabaseName;
        var user = string.IsNullOrWhiteSpace(options.UserName) ? "runtime" : options.UserName;
        return $"TheLink/Database/{options.Mode}/{host}/{database}/{user}";
    }

    public void Write(string target, string userName, string secret)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Windows Credential Manager is available on Windows only.");
        if (string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("Credential target is required.", nameof(target));
        if (string.IsNullOrEmpty(secret))
            throw new ArgumentException("Credential secret is required.", nameof(secret));

        var secretBytes = System.Text.Encoding.Unicode.GetBytes(secret);
        if (secretBytes.Length > 512)
            throw new ArgumentOutOfRangeException(nameof(secret), "Credential secret exceeds the Windows generic credential blob limit.");

        var blob = Marshal.AllocCoTaskMem(secretBytes.Length);
        try
        {
            Marshal.Copy(secretBytes, 0, blob, secretBytes.Length);
            var credential = new NativeCredential
            {
                Type = CredentialTypeGeneric,
                TargetName = target,
                CredentialBlobSize = (uint)secretBytes.Length,
                CredentialBlob = blob,
                Persist = CredentialPersistLocalMachine,
                UserName = userName ?? string.Empty
            };

            if (!CredWrite(ref credential, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not store the database credential in Windows Credential Manager.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secretBytes);
            Marshal.FreeCoTaskMem(blob);
        }
    }

    public bool Delete(string target)
    {
        if (!OperatingSystem.IsWindows()) return false;
        if (CredDelete(target, CredentialTypeGeneric, 0)) return true;

        const int ErrorNotFound = 1168;
        var error = Marshal.GetLastWin32Error();
        if (error == ErrorNotFound) return false;
        throw new Win32Exception(error, "Could not delete the Windows credential.");
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public uint Type;
        [MarshalAs(UnmanagedType.LPWStr)] public string? TargetName;
        [MarshalAs(UnmanagedType.LPWStr)] public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        [MarshalAs(UnmanagedType.LPWStr)] public string? TargetAlias;
        [MarshalAs(UnmanagedType.LPWStr)] public string? UserName;
    }

    [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredWrite(ref NativeCredential userCredential, uint flags);

    [DllImport("Advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredDelete(string target, uint type, uint flags);
}
