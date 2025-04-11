﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using GrpcDotNetNamedPipes;
using JetBrains.Annotations;
using nadena.dev.ndmf.proto.rpc;
using Debug = UnityEngine.Debug;

namespace nadena.dev.ndmf.platform.resonite
{
    internal class RPCClientController
    {
        private const string DevPipeName = "MA_RESO_PUPPETEER_DEV";
        private const string ProdPipePrefix = "ModularAvatarResonite_PuppetPipe_";

        private static ResoPuppeteer.ResoPuppeteerClient? _client;
        private static Task<ResoPuppeteer.ResoPuppeteerClient>? _clientTask = null;
        private static Process? _lastProcess;
        private static bool _isDebugBackend;
        
        private static ResoPuppeteer.ResoPuppeteerClient OpenChannel(string pipeName)
        {
            var channel = new NamedPipeChannel(".", pipeName, new NamedPipeChannelOptions()
            {
                ImpersonationLevel = TokenImpersonationLevel.None,
            });
            return new ResoPuppeteer.ResoPuppeteerClient(channel);
        }
        
        public static Task<ResoPuppeteer.ResoPuppeteerClient> GetClient()
        {
            if (_clientTask != null && !_clientTask.IsCompleted)
            {
                return _clientTask;
            }

            return _clientTask = Task.Run(GetClient0);
        }

        private static HashSet<string> ActivePipes()
        {
            return new HashSet<string>(System.IO.Directory.GetFiles("\\\\.\\pipe\\")
                .Select(p => p.Split("\\").Last())
            );
        }
        
        private static async Task<ResoPuppeteer.ResoPuppeteerClient> GetClient0()
        {
            if (_client != null && (_isDebugBackend || _lastProcess?.HasExited == false))
            {
                try
                {
                    await _client.PingAsync(new(), deadline: DateTime.UtcNow.AddMilliseconds(2000));
                    return _client;
                }
                catch (Exception)
                {
                    // continue
                }
            }
            
            var activePipes = ActivePipes();
            if (activePipes.Contains(DevPipeName))
            {
                _isDebugBackend = true;
                return _client = OpenChannel(DevPipeName);
            }

            _isDebugBackend = false;
            var pipeName = ProdPipePrefix + Process.GetCurrentProcess().Id;

            // if there is already a server running, try to shut it down (since we've lost the process handle)
            if (activePipes.Contains(pipeName))
            {
                try
                {
                    var preexisting = OpenChannel(pipeName);
                    await preexisting.ShutdownAsync(new(), deadline: DateTime.UtcNow.AddMilliseconds(2000));
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Failed to shut down existing server: " + e);
                }
                await Task.Delay(250); // give it some time to exit
            }
            
            // Launch production server
            if (_lastProcess?.HasExited == false)
            {
                _lastProcess?.Kill();
                await Task.Delay(250); // give it some time to exit
            }
            
            var cwd = Path.GetFullPath("Packages/nadena.dev.modular-avatar.resonite/ResoPuppet~");
            var exe = Path.Combine(cwd, "Launcher.exe");
            
            if (!File.Exists(exe))
            {
                throw new FileNotFoundException("Resonite Launcher not found", exe);
            }
            
            var tempDir = Path.Combine(Path.GetTempPath(), "ResonitePuppet");
            // Clean up old temp dir
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to delete temp dir: {e}");
                }
            }
            
            var args = new string[]
            {
                "--pipe-name", pipeName,
                "--temp-directory", tempDir,
                "--auto-shutdown-timeout", "30"
            };
            
            var startInfo = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = string.Join(" ", args),
                WorkingDirectory = cwd,
                UseShellExecute = false,
                CreateNoWindow = false,
            };
            
            _lastProcess = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };
            _lastProcess.Exited += (sender, e) =>
            {
                Console.WriteLine("Resonite Launcher exited");
                _client = null;
            };
            
            if (!_lastProcess.Start())
            {
                throw new Exception("Failed to start Resonite Launcher");
            }
            
            // Register domain reload hook to shut down the server
            AppDomain.CurrentDomain.DomainUnload += (sender, e) =>
            {
                try
                {
                    _lastProcess.Kill();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to kill Resonite Launcher: {ex}");
                }
            };
            
            // Also register the process exit hook
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                try
                {
                    _lastProcess.Kill();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to kill Resonite Launcher: {ex}");
                }
            };
            
            var tmpClient = OpenChannel(pipeName);
            
            // Wait for the server to start
            await tmpClient.PingAsync(new(), deadline: DateTime.UtcNow.AddSeconds(60));
            _client = tmpClient;

            PostStartup(_lastProcess, _client);

            return _client;
        }

        private static void PostStartup(Process process, ResoPuppeteer.ResoPuppeteerClient client)
        {
            // TODO: only ping while we need the server for something
            
            // ping the client every second so it doesn't time out and shut down
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(1000);
                        client.Ping(new());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to ping Resonite Launcher: {e}");
                        break;
                    }
                }
            });
        }
    }
}