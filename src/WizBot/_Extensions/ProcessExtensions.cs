// // Copyright (c) .NET Foundation. All rights reserved.
// // Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// // https://github.com/aspnet/Common/blob/dev/shared/Microsoft.Extensions.Process.Sources/ProcessHelper.cs
//
// using System.Diagnostics;
// using System.Runtime.InteropServices;
//
// namespace WizBot.Extensions;
//
// public static class ProcessExtensions
// {
//     private static readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
//     private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);
//
//     public static void KillTree(this Process process)
//         => process.KillTree(_defaultTimeout);
//
//     public static void KillTree(this Process process, TimeSpan timeout)
//     {
//         if (_isWindows)
//             RunProcessAndWaitForExit("taskkill", $"/T /F /PID {process.Id}", timeout, out _);
//         else
//         {
//             var children = new HashSet<int>();
//             GetAllChildIdsUnix(process.Id, children, timeout);
//             foreach (var childId in children)
//                 KillProcessUnix(childId, timeout);
//
//             KillProcessUnix(process.Id, timeout);
//         }
//     }
//
//     private static void GetAllChildIdsUnix(int parentId, ISet<int> children, TimeSpan timeout)
//     {
//         var exitCode = RunProcessAndWaitForExit("pgrep", $"-P {parentId}", timeout, out var stdout);
//
//         if (exitCode == 0 && !string.IsNullOrEmpty(stdout))
//         {
//             using var reader = new StringReader(stdout);
//             while (true)
//             {
//                 var text = reader.ReadLine();
//                 if (text is null)
//                     return;
//
//                 if (int.TryParse(text, out var id))
//                 {
//                     children.Add(id);
//                     // Recursively get the children
//                     GetAllChildIdsUnix(id, children, timeout);
//                 }
//             }
//         }
//     }
//
//     private static void KillProcessUnix(int processId, TimeSpan timeout)
//         => RunProcessAndWaitForExit("kill", $"-TERM {processId}", timeout, out _);
//
//     private static int RunProcessAndWaitForExit(
//         string fileName,
//         string arguments,
//         TimeSpan timeout,
//         out string? stdout)
//     {
//         stdout = null;
//
//         var startInfo = new ProcessStartInfo
//         {
//             FileName = fileName,
//             Arguments = arguments,
//             RedirectStandardOutput = true,
//             UseShellExecute = false
//         };
//
//         using var process = Process.Start(startInfo);
//
//         if (process is null)
//             return -1;
//
//         if (process.WaitForExit((int)timeout.TotalMilliseconds))
//             stdout = process.StandardOutput.ReadToEnd();
//         else
//             process.Kill();
//
//         return process.ExitCode;
//     }
// }