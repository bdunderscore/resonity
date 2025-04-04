﻿using System.Reflection;
using System.Runtime.InteropServices;

namespace nadena.dev.resonity.remote.bootstrap;

public class Launcher
{
    private string resoniteBase = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Resonite";
    private string assemblyBase;
    private string puppeteerBase;

    private List<string> assemblyPaths;
    private List<string> dllPaths;
    
    public Task Launch()
    {
        assemblyBase = resoniteBase + "\\Resonite_Data\\Managed\\";        
        
        System.Console.WriteLine("Starting Resonite Launcher");

        dllPaths = new()
        {
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

        return (Task)main.Invoke(null, new object[] { resoniteBase })!;
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