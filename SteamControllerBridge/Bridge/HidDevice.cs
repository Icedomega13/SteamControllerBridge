using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SteamControllerBridge.Bridge;

internal sealed class HidDevice : IDisposable
{
    private const int DigcfPresent = 0x00000002;
    private const int DigcfDeviceInterface = 0x00000010;
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareWrite = 0x00000002;
    private const uint OpenExisting = 3;
    private const uint FileFlagOverlapped = 0x40000000;
    private const int HidpStatusSuccess = 0x00110000;

    private readonly SafeFileHandle _handle;
    private readonly FileStream _stream;
    private readonly ushort _featureReportLength;
    private readonly ushort _outputReportLength;
    public ushort FeatureReportLength => _featureReportLength;
    public ushort OutputReportLength => _outputReportLength;

    private HidDevice(SafeFileHandle handle, ushort featureReportLength, ushort outputReportLength)
    {
        _handle = handle;
        _featureReportLength = featureReportLength == 0 ? (ushort)64 : featureReportLength;
        _outputReportLength = outputReportLength == 0 ? (ushort)64 : outputReportLength;
        _stream = new FileStream(handle, FileAccess.ReadWrite, 64, isAsync: true);
    }

    public static HidDevice? Open(string path)
    {
        var handle = CreateFile(path, GenericRead | GenericWrite, FileShareRead | FileShareWrite,
            IntPtr.Zero, OpenExisting, FileFlagOverlapped, IntPtr.Zero);

        if (handle.IsInvalid)
        {
            handle.Dispose();
            return null;
        }

        var lengths = GetReportLengths(handle);
        return new HidDevice(handle, lengths.Feature, lengths.Output);
    }

    public static IEnumerable<string> Enumerate(ushort vendorId, ushort productId, ushort usagePage)
    {
        HidD_GetHidGuid(out var hidGuid);
        var infoSet = SetupDiGetClassDevs(ref hidGuid, null, IntPtr.Zero, DigcfPresent | DigcfDeviceInterface);
        if (infoSet == IntPtr.Zero || infoSet == new IntPtr(-1))
        {
            yield break;
        }

        try
        {
            var index = 0u;
            var interfaceData = new SpDeviceInterfaceData { cbSize = Marshal.SizeOf<SpDeviceInterfaceData>() };
            while (SetupDiEnumDeviceInterfaces(infoSet, IntPtr.Zero, ref hidGuid, index++, ref interfaceData))
            {
                SetupDiGetDeviceInterfaceDetail(infoSet, ref interfaceData, IntPtr.Zero, 0, out var required, IntPtr.Zero);
                var detail = Marshal.AllocHGlobal((int)required);
                try
                {
                    Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                    if (!SetupDiGetDeviceInterfaceDetail(infoSet, ref interfaceData, detail, required, out _, IntPtr.Zero))
                    {
                        continue;
                    }

                    var path = Marshal.PtrToStringUni(detail + 4);
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    using var handle = CreateFile(path, 0, FileShareRead | FileShareWrite,
                        IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
                    if (handle.IsInvalid)
                    {
                        continue;
                    }

                    var attributes = new HiddAttributes { Size = Marshal.SizeOf<HiddAttributes>() };
                    if (!HidD_GetAttributes(handle, ref attributes))
                    {
                        continue;
                    }

                    if (attributes.VendorID != vendorId || attributes.ProductID != productId)
                    {
                        continue;
                    }

                    if (usagePage == 0)
                    {
                        yield return path;
                        continue;
                    }

                    if (!HidD_GetPreparsedData(handle, out var preparsedData))
                    {
                        continue;
                    }

                    try
                    {
                        if (HidP_GetCaps(preparsedData, out var caps) == HidpStatusSuccess &&
                            caps.UsagePage == usagePage)
                        {
                            yield return path;
                        }
                    }
                    finally
                    {
                        HidD_FreePreparsedData(preparsedData);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(detail);
                }
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(infoSet);
        }
    }

    public static IEnumerable<string> EnumerateValveInterfaces()
    {
        return Enumerate(SteamControllerReports.ValveVendorId, SteamControllerReports.PuckProductId, 0)
            .Concat(Enumerate(SteamControllerReports.ValveVendorId, SteamControllerReports.WiredProductId, 0))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public static IEnumerable<HidInterfaceInfo> EnumerateInterfaces()
    {
        HidD_GetHidGuid(out var hidGuid);
        var infoSet = SetupDiGetClassDevs(ref hidGuid, null, IntPtr.Zero, DigcfPresent | DigcfDeviceInterface);
        if (infoSet == IntPtr.Zero || infoSet == new IntPtr(-1))
        {
            yield break;
        }

        try
        {
            var index = 0u;
            var interfaceData = new SpDeviceInterfaceData { cbSize = Marshal.SizeOf<SpDeviceInterfaceData>() };
            while (SetupDiEnumDeviceInterfaces(infoSet, IntPtr.Zero, ref hidGuid, index++, ref interfaceData))
            {
                SetupDiGetDeviceInterfaceDetail(infoSet, ref interfaceData, IntPtr.Zero, 0, out var required, IntPtr.Zero);
                var detail = Marshal.AllocHGlobal((int)required);
                try
                {
                    Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                    if (!SetupDiGetDeviceInterfaceDetail(infoSet, ref interfaceData, detail, required, out _, IntPtr.Zero))
                    {
                        continue;
                    }

                    var path = Marshal.PtrToStringUni(detail + 4);
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    var canOpenMetadata = false;
                    ushort vendorId = 0;
                    ushort productId = 0;
                    ushort versionNumber = 0;
                    ushort usagePage = 0;
                    ushort usage = 0;
                    ushort inputLength = 0;
                    ushort outputLength = 0;
                    ushort featureLength = 0;
                    string? openError = null;

                    using var handle = CreateFile(path, 0, FileShareRead | FileShareWrite,
                        IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
                    if (handle.IsInvalid)
                    {
                        openError = $"metadata open failed: {Marshal.GetLastWin32Error()}";
                    }
                    else
                    {
                        canOpenMetadata = true;
                        var attributes = new HiddAttributes { Size = Marshal.SizeOf<HiddAttributes>() };
                        if (HidD_GetAttributes(handle, ref attributes))
                        {
                            vendorId = attributes.VendorID;
                            productId = attributes.ProductID;
                            versionNumber = attributes.VersionNumber;
                        }
                        else
                        {
                            openError = $"attributes failed: {Marshal.GetLastWin32Error()}";
                        }

                        if (HidD_GetPreparsedData(handle, out var preparsedData))
                        {
                            try
                            {
                                if (HidP_GetCaps(preparsedData, out var caps) == HidpStatusSuccess)
                                {
                                    usagePage = caps.UsagePage;
                                    usage = caps.Usage;
                                    inputLength = caps.InputReportByteLength;
                                    outputLength = caps.OutputReportByteLength;
                                    featureLength = caps.FeatureReportByteLength;
                                }
                            }
                            finally
                            {
                                HidD_FreePreparsedData(preparsedData);
                            }
                        }
                    }

                    yield return new HidInterfaceInfo(
                        path,
                        vendorId,
                        productId,
                        versionNumber,
                        usagePage,
                        usage,
                        inputLength,
                        outputLength,
                        featureLength,
                        canOpenMetadata,
                        openError);
                }
                finally
                {
                    Marshal.FreeHGlobal(detail);
                }
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(infoSet);
        }
    }

    public async Task<int> ReadReportAsync(byte[] buffer, TimeSpan timeout, CancellationToken token)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeoutCts.CancelAfter(timeout);
        try
        {
            return await _stream.ReadAsync(buffer.AsMemory(0, buffer.Length), timeoutCts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return 0;
        }
    }

    public bool SendFeatureReport(ReadOnlySpan<byte> report)
    {
        var buffer = new byte[_featureReportLength];
        report[..Math.Min(report.Length, buffer.Length)].CopyTo(buffer);
        return HidD_SetFeature(_handle, buffer, buffer.Length);
    }

    public bool SendOutputReport(ReadOnlySpan<byte> report)
    {
        try
        {
            var buffer = new byte[_outputReportLength];
            report[..Math.Min(report.Length, buffer.Length)].CopyTo(buffer);
            _stream.Write(buffer, 0, buffer.Length);
            _stream.Flush();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _stream.Dispose();
        _handle.Dispose();
    }

    [DllImport("hid.dll")]
    private static extern void HidD_GetHidGuid(out Guid hidGuid);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_GetAttributes(SafeFileHandle hidDeviceObject, ref HiddAttributes attributes);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_GetPreparsedData(SafeFileHandle hidDeviceObject, out IntPtr preparsedData);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern int HidP_GetCaps(IntPtr preparsedData, out HidpCaps capabilities);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_SetFeature(SafeFileHandle hidDeviceObject, byte[] reportBuffer, int reportBufferLength);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, string? enumerator, IntPtr hwndParent, int flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData,
        ref Guid interfaceClassGuid, uint memberIndex, ref SpDeviceInterfaceData deviceInterfaceData);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet,
        ref SpDeviceInterfaceData deviceInterfaceData, IntPtr deviceInterfaceDetailData,
        uint deviceInterfaceDetailDataSize, out uint requiredSize, IntPtr deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
        IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    private static (ushort Feature, ushort Output) GetReportLengths(SafeFileHandle handle)
    {
        if (!HidD_GetPreparsedData(handle, out var preparsedData))
        {
            return (64, 64);
        }

        try
        {
            if (HidP_GetCaps(preparsedData, out var caps) != HidpStatusSuccess)
            {
                return (64, 64);
            }

            return (caps.FeatureReportByteLength, caps.OutputReportByteLength);
        }
        finally
        {
            HidD_FreePreparsedData(preparsedData);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SpDeviceInterfaceData
    {
        public int cbSize;
        public Guid InterfaceClassGuid;
        public int Flags;
        public IntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HiddAttributes
    {
        public int Size;
        public ushort VendorID;
        public ushort ProductID;
        public ushort VersionNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HidpCaps
    {
        public ushort Usage;
        public ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public ushort[] Reserved;
        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    }
}

internal sealed record HidInterfaceInfo(
    string Path,
    ushort VendorId,
    ushort ProductId,
    ushort VersionNumber,
    ushort UsagePage,
    ushort Usage,
    ushort InputReportLength,
    ushort OutputReportLength,
    ushort FeatureReportLength,
    bool CanOpenMetadata,
    string? OpenError);
