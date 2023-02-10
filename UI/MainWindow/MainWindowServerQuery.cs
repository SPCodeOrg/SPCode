using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SPCode.Interop;
using ValveQuery.GameServer;

namespace SPCode.UI;

public partial class MainWindow
{
    /// <summary>
    /// Queries the server with the specified command in the command box of the config.
    /// </summary>

    private void Server_Query()
    {
        var output = new List<string>();

        var c = Program.Configs[Program.SelectedConfig];
        if (string.IsNullOrWhiteSpace(c.RConIP) || string.IsNullOrWhiteSpace(c.RConCommands))
        {
            output.Add("The RCON IP or the Commands fields are empty.");
            goto Dispatcher;
        }

        try
        {
            using var server = ServerQuery.GetServerInstance(c.RConIP, c.RConPort, false, 1000, 1000, 2, true);
            var serverInfo = server.GetInfo();

            if (serverInfo == null)
            {
                output.Add("No server found to send commands to.");
                goto Dispatcher;
            }

            server.GetControl(c.RConPassword, false);

            output.Add($"Server: {serverInfo.Name}");
            output.Add("Sending commands...");

            var cmds = ReplaceRconCMDVariables(c.RConCommands).Split('\n');

            if (cmds.Any(x => x.Contains("{plugin")))
            {
                output.Add("No plugins available to replace placeholders commands with. Removing them...");
                cmds = cmds.Where(x => !x.Contains("{plugin")).ToArray();
                if (cmds.Length == 0)
                {
                    output.Add("No commands sent.");
                    goto Dispatcher;
                }
            }

            foreach (var cmd in cmds)
            {
                var t = Task.Run(() =>
                {
                    var command = cmd.Trim('\r').Trim();
                    if (!string.IsNullOrWhiteSpace(command))
                    {
                        output.Add(server.Rcon.SendCommand(command));
                    }
                });
                t.Wait();
            }
            output.Add("Commands sent.");
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Not authorized"))
            {
                output.Add("Incorrect RCON password.");
                goto Dispatcher;
            }
            if (ex is SocketException socketEx)
            {
                switch ((SocketError)socketEx.ErrorCode)
                {
                    case SocketError.ConnectionReset:
                    case SocketError.NotConnected:
                    case SocketError.TimedOut:
                        output.Add("Connection to the server reset or timed out. Make sure you've set it up correctly, and the server you're targeting is available.\n" +
                            "Your data:\n" +
                            $"  - IP: {c.RConIP}\n" +
                            $"  - Port: {c.RConPort}");
                        goto Dispatcher;
                }
            }
            output.Add($"SPCode unhandled error: {ex.Message}");
        }

    Dispatcher:

        Dispatcher.Invoke(() =>
        {
            output.ForEach(x => LoggingControl.LogAction(x));
            if (CompileOutputRow.Height.Value < 11.0)
            {
                CompileOutputRow.Height = new GridLength(200.0);
            }
        });
    }

    /// <summary>
    /// Replaces the placeholders from the commands box with the corresponding content.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private string ReplaceRconCMDVariables(string input)
    {
        if (CompiledFileNames.Count < 1)
        {
            return input;
        }

        if (input.IndexOf("{plugins_reload}", StringComparison.Ordinal) >= 0)
        {
            var replacement = new StringBuilder();
            replacement.AppendLine();
            foreach (var fileName in CompiledFileNames)
            {
                replacement.Append("sm plugins reload " + StripSMXPostFix(fileName) + ";");
            }

            replacement.AppendLine();
            input = input.Replace("{plugins_reload}", replacement.ToString());
        }

        if (input.IndexOf("{plugins_load}", StringComparison.Ordinal) >= 0)
        {
            var replacement = new StringBuilder();
            replacement.AppendLine();
            foreach (var fileName in CompiledFileNames)
            {
                replacement.Append("sm plugins load " + StripSMXPostFix(fileName) + ";");
            }

            replacement.AppendLine();
            input = input.Replace("{plugins_load}", replacement.ToString());
        }

        if (input.IndexOf("{plugins_unload}", StringComparison.Ordinal) >= 0)
        {
            var replacement = new StringBuilder();
            replacement.AppendLine();
            foreach (var fileName in CompiledFileNames)
            {
                replacement.Append("sm plugins unload " + StripSMXPostFix(fileName) + ";");
            }

            replacement.AppendLine();
            input = input.Replace("{plugins_unload}", replacement.ToString());
        }

        return input;
    }

    /// <summary>
    /// Strips the '.smx' from the specified string
    /// </summary>
    /// <param name="fileName">File name to strip the extension from.</param>
    /// <returns></returns>
    private string StripSMXPostFix(string fileName)
    {
        if (fileName.EndsWith(".smx"))
        {
            return fileName.Substring(0, fileName.Length - 4);
        }

        return fileName;
    }
}