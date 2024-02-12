@setlocal enabledelayedexpansion & set "ARGS=%*" & pwsh -nop -ex Bypass -c "Add-Type (gc '%~dpf0'|select -Skip 1|Out-String); exit [Program]::Main();" || pause & exit /b !ERRORLEVEL!
// OpenUnityProject.cmd
// ====================
//
// Windows batch file which invokes the appropriate version of `Unity.exe` without UnityHub.
//
//
// ## Setup
//
// Copy `OpenUnityProject.cmd` (this file) to your Unity project folder which contains `Assets/`, `ProjectSettings/` etc.
//   - You do not need to copy any other files.
//
//
// ## Usage
//
// Run copied version of `OpenUnityProject.cmd`.
//   - You can invoke it from Explorer or the Command prompt.
// 
// It will read `ProjectSettings/ProjectVersion.txt`, find the appropriate version of `Unity.exe`, and invoke it.
//   - Or it reports an error.
//
// If you need, you can also specify the desired version of Unity Editor with `--open-unity-project-with=XXX`.
//   - For example: `OpenUnityProject.cmd --open-unity-project-with=2022.3.19f1`
//
//
// ## License
//
// OpenUnityProject.cmd is written by Takayuki Matsuoka.
//   This code is licensed under CC0-1.0,
//   https://creativecommons.org/publicdomain/zero/1.0/
//
// SPDX-FileCopyrightText: Copyright (c) Takayuki Matsuoka
// SPDX-License-Identifier: CC0-1.0
//

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

public static class Program {
    private const string UnityArgFormat    = "-projectPath \"{0}\" {1}";
    private const string VersionLinePrefix = "m_EditorVersion: ";

    /*
    ## How can I check the IpcName?

    You can check it with the following commands:

        cd /path/to/Unity Hub/
        # https://github.com/electron/asar
        npm install --engine-strict @electron/asar
        mkdir extracted
        ./node_modules/.bin/asar extract app.asar extracted
        cat extracted/build/main/services/localIPC/hubIPCService.js
        cat extracted/build/main/common/ipc/UnityIPCServer.js
        cat extracted/build/main/common/ipc/unityIPCNameFormatter.js

    See HubIPCService.constructor(), UnityIPCServer.constructor()
    and unityIPCNameFormatter.formatName().
    */
    private const string IpcName = "Unity-hubIPCService";

    public static void Main() {
        string argString0 = Environment.GetEnvironmentVariable("ARGS") ?? "";
        (string argString, string? desiredUnityVersion) = FilterArgString(argString0);

        string currentDir      = Directory.GetCurrentDirectory();
        string unityArg        = string.Format(UnityArgFormat, currentDir, argString);
        string versionFileName = GetVersionFileName(currentDir);
        string version         = desiredUnityVersion ?? GetProjectVersionString(versionFileName);

        (List<string> candidates, List<string?> triedPaths) = GetUnityEditorCandidatesByVersion(version);
        if (candidates.Count == 0) {
            string report = MakeFailureReport(version, versionFileName, triedPaths);
            Error(report);
            Abort();
        }

        string unityEditor = candidates[0];
        Console.WriteLine($"\"{unityEditor}\" {unityArg}");

        try {
            // Creation of NamedPipeServerStream may fail when actual UnityHub already exists.
            NamedPipeServerStream pipeServer = new(IpcName, PipeDirection.InOut, maxNumberOfServerInstances: 1);
            System.Diagnostics.Process.Start(fileName: unityEditor, arguments: unityArg);
            pipeServer.WaitForConnection();
            pipeServer.Close();
        }
        catch {
            // Retry
            System.Diagnostics.Process.Start(fileName: unityEditor, arguments: unityArg);
        }
    }

    private static string MakeFailureReport(string version, string versionFileName, List<string?> triedPaths) {
        // remove "f1"
        string versionWithoutF1 = version.EndsWith("f1")
            ? version.Substring(0, version.Length - 2)
            : version;

        StringBuilder sb = new();
        sb.AppendLine($"Unity Editor {version} is not found in the following paths");
        foreach (string? path in triedPaths) {
            sb.Append($"  {path}\n");
        }
        sb.AppendLine("");
        sb.AppendLine("Please check :");
        sb.AppendLine("  (1) Installed Unity Editor versions");
        sb.AppendLine("  (2) Content of ProjectVersion.txt at");
        sb.AppendLine($"      {versionFileName}");
        sb.AppendLine("  (3) Official download page:");
        sb.Append("\x1b[93m");
        sb.AppendLine($"      https://unity.com/releases/editor/whats-new/{versionWithoutF1}");
        sb.Append("\x1b[0m");
        return sb.ToString();
    }

    private static void Abort() => Environment.Exit(1);

    private static void Error(string s) {
        Console.WriteLine("\x1b[91m==== ERROR ====\x1b[0m");
        Console.WriteLine(s);
        Console.WriteLine("\x1b[91m==== ERROR ====\x1b[0m");
    }

    private static (string, string?) FilterArgString(string argString) {
        Regex r = new("--open-unity-project-with=([^ ]*)");
        Match m = r.Match(argString);
        if (! m.Success) {
            return (argString, null);
        }
        Group  g              = m.Groups[1];
        string desiredVersion = g.ToString();
        string newArgString   = r.Replace(argString, string.Empty);
        return (newArgString, desiredVersion);
    }

    private static string GetVersionFileName(string unityProjectPath) =>
        $@"{unityProjectPath}\ProjectSettings\ProjectVersion.txt";

    private static string GetProjectVersionString(string projectVersionFileName) {
        // Does ProjectVersion.txt exist?
        if (! File.Exists(projectVersionFileName)) {
            Error($"ProjectVersion.txt is not found at\n  {projectVersionFileName}");
            Abort();
        }

        // Get version string from ProjectVersion.txt
        var version = "";
        {
            string[] lines = File.ReadAllLines(projectVersionFileName);
            foreach (string line in lines) {
                if (line.StartsWith(VersionLinePrefix)) {
                    version = line[VersionLinePrefix.Length..];
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(version)) {
            Error($"The following ProjectVersion.txt does not contain version number\n  {projectVersionFileName}");
            Abort();
        }

        return version;
    }

    private static (List<string> hits, List<string?> triedPaths) GetUnityEditorCandidatesByVersion(string version) {
        List<string>  hits       = new();
        List<string?> triedPaths = new();

        try {
            // %ProgramFiles%\Unity\Hub\Editor\(Version)\Editor\Unity.exe
            CheckProgramFiles(version, CheckPath);
        }
        catch {
            // ignored
        }
        try {
            // %ProgramFiles%\Unity <VERSION>\Editor\Unity.exe
            CheckProgramFiles2(version, CheckPath);
        }
        catch {
            // ignored
        }
        try {
            // %APPDATA%\UnityHub\secondaryInstallPath.json
            CheckSecondaryInstallPath(version, CheckPath);
        }
        catch {
            // ignored
        }
        try {
            // %APPDATA%\UnityHub\editors.json
            CheckEditorsJson(version, CheckPath);
        }
        catch {
            // ignored
        }
        try {
            // %APPDATA%\UnityHub\editors-v2.json
            CheckEditorsV2Json(version, CheckPath);
        }
        catch {
            // ignored
        }

        return (hits, triedPaths);

        void CheckPath(string path) {
            if (File.Exists(path)) {
                hits.Add(path);
            } else {
                triedPaths.Add(path);
            }
        }
    }

    private static void CheckProgramFiles(string version, Action<string> checkPath) {
        // %ProgramFiles%\Unity\Hub\Editor\(Version)\Editor\Unity.exe
        var    p0 = $@"%ProgramFiles%\Unity\Hub\Editor\{version}\Editor\Unity.exe";
        string p  = Environment.ExpandEnvironmentVariables(p0);
        checkPath(p);
    }

    private static void CheckProgramFiles2(string version, Action<string> checkPath) {
        // %ProgramFiles%\Unity <VERSION>\Editor\Unity.exe
        var    p0 = $@"%ProgramFiles%\Unity {version}\Editor\Unity.exe";
        string p  = Environment.ExpandEnvironmentVariables(p0);
        checkPath(p);
    }

    private static void CheckSecondaryInstallPath(string version, Action<string> checkPath) {
        // %APPDATA%\UnityHub\secondaryInstallPath.json
        string jsonPath   = Environment.ExpandEnvironmentVariables(@"%APPDATA%\UnityHub\secondaryInstallPath.json");
        string jsonString = File.ReadAllText(jsonPath);
        string altPath    = jsonString.Replace(oldChar: '"', newChar: ' ').Trim();
        if (! string.IsNullOrEmpty(altPath)) {
            checkPath($@"{altPath}\{version}\Editor\Unity.exe");
        }
    }

    private static void CheckEditorsJson(string version, Action<string> checkPath) {
        // %APPDATA%\UnityHub\editors.json
        string jsonPath = Environment.ExpandEnvironmentVariables(@"%APPDATA%\UnityHub\editors.json");
        CheckEditorsJsonBase(jsonPath, version, checkPath);
    }

    private static void CheckEditorsV2Json(string version, Action<string> checkPath) {
        // %APPDATA%\UnityHub\editors-v2.json
        string jsonPath = Environment.ExpandEnvironmentVariables(@"%APPDATA%\UnityHub\editors-v2.json");
        CheckEditorsJsonBase(jsonPath, version, checkPath);
    }

    private static void CheckEditorsJsonBase(string jsonPath, string version, Action<string> checkPath) {
        string jsonString  = File.ReadAllText(jsonPath);
        var    topElements = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        if (topElements == null) {
            return;
        }
        foreach ((string? topName, JsonElement topElement) in topElements) {
            if (version == topName) {
                foreach (JsonProperty property in topElement.EnumerateObject()) {
                    if (property.Name == "location") {
                        JsonElement locations = property.Value;
                        foreach (JsonElement locationElement in locations.EnumerateArray()) {
                            var location = locationElement.ToString();
                            checkPath(location);
                        }
                    }
                }
            }
        }
    }
}
