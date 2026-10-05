using Rivet.Homeland;
using Rivet.Homeland.Infrastructure;

var options = HomelandLaunchOptions.Parse(args);
if (options.SteamCheck)
{
    using var steam = SteamRuntime.Start(options);
    using var transport = HomelandTransport.Create(options);
    transport.Poll();
    Console.WriteLine($"Homeland Steam transport ready at {transport.LocalAddress}.");
    return;
}
using var app = new HomelandApplication(options);
app.Run();
