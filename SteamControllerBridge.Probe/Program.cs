using SteamControllerBridge.Bridge;

Console.WriteLine("Steam Controller Bridge probe");
Console.WriteLine();

var paths = HidDevice.EnumerateValveInterfaces().ToList();
Console.WriteLine($"Valve HID interfaces found: {paths.Count}");

foreach (var path in paths)
{
    Console.WriteLine();
    Console.WriteLine(path);
    using var device = HidDevice.Open(path);
    if (device is null)
    {
        Console.WriteLine("  open: failed");
        continue;
    }

    Console.WriteLine($"  open: ok, feature report length: {device.FeatureReportLength}");
    var buffer = new byte[256];
    var gotAny = false;
    var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(3);
    while (DateTime.UtcNow < deadline)
    {
        var read = await device.ReadReportAsync(buffer, TimeSpan.FromMilliseconds(250), CancellationToken.None);
        if (read <= 0)
        {
            continue;
        }

        gotAny = true;
        var preview = BitConverter.ToString(buffer, 0, Math.Min(read, 24));
        Console.WriteLine($"  report: {read} bytes, id=0x{buffer[0]:X2}, first={preview}");
    }

    if (!gotAny)
    {
        Console.WriteLine("  report: none within 3s");
    }
}
