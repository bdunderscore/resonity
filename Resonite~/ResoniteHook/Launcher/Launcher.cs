﻿using System.CommandLine;
using System.Reflection;
using System.Runtime.InteropServices;

namespace nadena.dev.resonity.remote.bootstrap;

public class Launcher
{
    private const string defaultResoniteBase = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Resonite";
    private string assemblyBase;
    private string puppeteerBase;

    private List<string> assemblyPaths;
    private List<string> dllPaths;

    private string resoniteBase = defaultResoniteBase;
    public string? tempDirectory = ".";
    public string? pipeName = "MA_RESO_PUPPETEER_DEV";
    public int? autoShutdownTimeout;

    public Task Launch(string[] args)
    {
        ParseArgs(args);

        if (tempDirectory == null)
        {
            throw new ArgumentNullException(nameof(tempDirectory), "Temp directory cannot be null");
        }
        
        if (pipeName == null)
        {
            throw new ArgumentNullException(nameof(pipeName), "Pipe name cannot be null");
        }
        
        var gamePath = SteamUtils.GetGamePath(2519830);
        resoniteBase = gamePath ?? defaultResoniteBase;
        
        assemblyBase = resoniteBase + "\\Resonite_Data\\Managed\\";        
        
        System.Console.WriteLine("Starting Resonite Launcher");

        dllPaths = new()
        {
            Path.GetDirectoryName(typeof(Launcher).Assembly.Location)!,
            assemblyBase,
            resoniteBase + "\\Resonite_Data\\Plugins\\x86_64\\",
            resoniteBase + "\\Tools\\",
        };
        
        // Manually load assimp; it directly loads itself without going through NativeLibrary
        //NativeLibrary.Load(resoniteBase + "\\Tools\\assimp.dll");
        
        AppDomain.CurrentDomain.AssemblyLoad += (sender, args) =>
        {
            NativeLibrary.SetDllImportResolver(args.LoadedAssembly, DllImportResolver);    
        };
        
        AppDomain.CurrentDomain.AssemblyResolve += OnResolveFailed;

        /*
        var puppeteer = Assembly.LoadFile("Puppeteer.dll");
        var program = puppeteer.GetType("Puppeteer.Program");
        var main = program.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
        await (Task) main.Invoke(null, null);
        */

        Assembly puppeteerAssembly;
        try
        {
            puppeteerAssembly = Assembly.Load("Puppeteer");
        }
        catch (Exception e)
        {
            var path = Assembly.GetExecutingAssembly().Location;
            path = Path.Combine(Path.GetDirectoryName(path)!, "../../../../Puppeteer/bin/Debug/net9.0", "Puppeteer.dll");

            puppeteerAssembly = Assembly.LoadFile(path);
        }

        puppeteerBase = Path.GetDirectoryName(puppeteerAssembly.Location) + "//";
        
        var puppeteer = puppeteerAssembly.GetType("nadena.dev.resonity.remote.puppeteer.Program");
        var main = puppeteer?.GetMethod("Launch", BindingFlags.Static | BindingFlags.NonPublic);

        if (main == null)
        {
            throw new Exception("Could not find Main method in Puppeteer.Program");
        }

        return (Task)main.Invoke(null, [resoniteBase, tempDirectory, pipeName, autoShutdownTimeout])!;
    }

    private void ParseArgs(string[] args)
    {
        var resoInstallOption = new Option<string?>(
            name: "--resonite-install-path",
            description: "Path to the Resonite installation. Defaults to " + defaultResoniteBase);
        var tempDirectory = new Option<string?>(
            name: "--temp-directory",
            description: "Path to the temporary directory used for resonite's LocalDB.");
        var pipeName = new Option<string?>(
            name: "--pipe-name",
            description: "Name of the pipe used for communication with unity.");
        var autoShutdownTimeout = new Option<int?>(
            name: "--auto-shutdown-timeout",
            description: "Time in seconds to wait before shutting down resonite. Defaults to not shutting down.");

        var rootCommand = new RootCommand("Modular Avatar Resonite backend");
        rootCommand.AddOption(resoInstallOption);
        rootCommand.AddOption(tempDirectory);
        rootCommand.AddOption(pipeName);
        rootCommand.AddOption(autoShutdownTimeout);
        
        rootCommand.SetHandler((string? resoInstallPath, string? tempDirectory, string? pipeName, int? autoShutdownTimeout) =>
        {
            if (resoInstallPath != null)
            {
                resoniteBase = resoInstallPath;
            }

            if (tempDirectory != null)
            {
                this.tempDirectory = tempDirectory;
            }

            if (pipeName != null)
            {
                this.pipeName = pipeName;
            }

            if (autoShutdownTimeout != null)
            {
                this.autoShutdownTimeout = autoShutdownTimeout;
            }
            
            Console.WriteLine("resoInstallPath: " + this.resoniteBase);
            Console.WriteLine("tempDirectory: " + this.tempDirectory);
            Console.WriteLine("pipeName: " + this.pipeName);
            Console.WriteLine("autoShutdownTimeout: " + this.autoShutdownTimeout);
        }, resoInstallOption, tempDirectory, pipeName, autoShutdownTimeout);

        rootCommand.Invoke(args);
    }

    private IntPtr DllImportResolver(string libraryname, Assembly assembly, DllImportSearchPath? searchpath)
    {
        if (!libraryname.EndsWith(".dll"))
        {
            libraryname += ".dll";
        };
        foreach (var dllPath in dllPaths)
        {
            try
            {
                return NativeLibrary.Load(dllPath + libraryname, assembly, searchpath);
            }
            catch (DllNotFoundException)
            {
                continue;
            }
        }
        
        return IntPtr.Zero;
    }

    private Assembly? OnResolveFailed(object? sender, ResolveEventArgs args)
    {
        var name = args.Name;

        if (name.Contains(","))
        {
            name = name.Split(',')[0];
        }

        if (name.EndsWith(".resources"))
        {
            name = name.Substring(0, name.Length - ".resources".Length);
        }

        if (name == "Puppeteer") return null;
        
        var dll = assemblyBase + name + ".dll";

        try
        {
            return Assembly.LoadFile(puppeteerBase + name + ".dll");
        }
        catch (FileNotFoundException e)
        {
            try
            {
                return Assembly.LoadFile(dll);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
    }
}