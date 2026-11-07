// sorry for bad code, this is genuienly one of my first C# programs

using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using IniParser;
using IniParser.Model;
using Octokit;

const string clientHash = "33f6645d9cd61498748497a826c7a7ff47f0f214e04437454143994228580257";
const string downloadUrl =  "https://s3.g.megas4.com/2koayuyiwxv4groxzwdbbxg43cwustavrkvfb/recflare/2026/client.zip"; // community mirror
const string downloadPath = @"C:\Games\RecFlare\";
string clientZip = Path.Combine(downloadPath, "client.zip");
string patchzip = Path.Combine(downloadPath, "patch.zip");
string latestPatch = "";
const string patchHash = "2919e2de4ddeec18e26c9a37280afdfac92450cb99fa6f72e7a9f52cef31d975";
string patchConfig = Path.Combine(downloadPath, "2026Patch.ini");
const string apiHost = "https://ns.recflare.net";
const string serverDomain = "recflare.net";
const string voiceKeyXml =
    "<RSAKeyValue><Modulus>nu7lFZqLX+siEI2U8/JihESmm4JrBVLsCHdS8NoAipq2NdJqVouOHQoxtFfJTG6u/+kx4wocnsQ1Ydk5AISyy0GQeweQOTLKlkXsR6on/DL7LqKWxWp3M+MG4kkyZROWh2OELCAudC902p8VSaXmEyH8i1Sid9YiKe0FaNsyzWE=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

// Octokit stuff

var githubClient = new GitHubClient(new ProductHeaderValue("RecFlareInstaller"));

Release latest = await githubClient.Repository.Release.GetLatest("recflare", "patch-2026");
foreach (var release in latest.Assets)
{
    if (release.Name.StartsWith("2026Patch"))
    {
        latestPatch = release.BrowserDownloadUrl;
        break;
    }
}


Console.WriteLine("Would you like to begin?");
string confirmation = Console.ReadLine();

if (confirmation != "y" && confirmation != "")
{
    Console.WriteLine("ok bye!");
    Environment.Exit(0);
}
Console.WriteLine("Making download directory");
Directory.CreateDirectory(downloadPath);
Console.WriteLine("Downloading from community mirror (this will take some time!)");
await DownloadFromUrl(downloadUrl, clientZip);
Console.WriteLine("Checking client integrity..");
if (GetSha256Hash(clientZip) != clientHash)
{
    Console.WriteLine("Client integrity check failed! \nPlease re-run the launcher.");
    File.Delete(clientZip);
    Environment.Exit(0);
}

Console.WriteLine("Extracting client archive... (again, can take some time.)");
ZipFile.ExtractToDirectory(clientZip, downloadPath);
Console.WriteLine("Deleting client archive...");
File.Delete(clientZip);
AddDefenderExclusion(downloadPath);
Console.WriteLine("Downloading patch archive..");
await DownloadFromUrl(latestPatch, patchzip);
Console.WriteLine("Checking patch integrity..");
if (GetSha256Hash(patchzip) != patchHash)
{
    Console.WriteLine("Patch integrity check failed! \nPlease re-run the launcher.");
    File.Delete(patchzip);
    Environment.Exit(0);
}
Console.WriteLine("Extracting patch archive...");
ZipFile.ExtractToDirectory(patchzip, downloadPath);
Console.WriteLine("Deleting patch archive...");
File.Delete(patchzip);
Console.WriteLine("Fixing patch config..");

FileIniDataParser parser = new FileIniDataParser();
IniData data = parser.ReadFile(patchConfig);

// modify the values
data["config"]["ApiHost"] = apiHost;
data["config"]["ServerDomain"] = serverDomain;
data["config"]["VoiceKeyXml"] = voiceKeyXml;
parser.WriteFile(patchConfig, data);

Console.WriteLine("Done!");
Console.WriteLine("Press enter to exit..");
Console.ReadLine();
Environment.Exit(0);





static bool AddDefenderExclusion(string path)
{
    Console.WriteLine("Windows Defender usually flags the RecFlare patch as a virus.");
    Console.WriteLine("This is a false positive, the way that RecFlare injects the patch trips Windows Defender.");
    Console.WriteLine("It is recommended to add an exclusion to the RecFlare folder to prevent the patch from being deleted.");
    Console.WriteLine("Add exclusion? (default yes)");
    string confirmation = Console.ReadLine();
    if (confirmation != "y" && confirmation != "")
    {
        Console.WriteLine("Skipping defender exclusion.");
        return false;
    }

    try
    {
        string command = $"Add-MpPreference -ExclusionPath {path}";

        var ps1 = new ProcessStartInfo()
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -WindowStyle Hidden -Command \"{command}\"",
            Verb = "runas",
            UseShellExecute = true,
            CreateNoWindow = true
        };
        using (var process = Process.Start(ps1))
        {
            process.WaitForExit();
            return process.ExitCode == 0;
        }
    }
    catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
    {
        return false;
    }
}

async Task DownloadFromUrl(string url, string path)
{
    HttpClient client = new HttpClient();
    using Stream remotestream = await client.GetStreamAsync(url);
    using FileStream fileStream = new FileStream(path, System.IO.FileMode.Create);
    await remotestream.CopyToAsync(fileStream);
}

static string GetSha256Hash(string filePath)
{
    using var sha256 = SHA256.Create();
    using var stream = File.OpenRead(filePath);
    byte[] hashBytes = sha256.ComputeHash(stream);
    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
}