//
// SPDX-FileCopyrightText: Copyright (c) Takayuki Matsuoka
// SPDX-License-Identifier: CC0-1.0
//
// OpenUnityProject.cmd
// ====================
//
// Windows batch file which invokes appropriate version of `Unity.exe`.
//
//
// ## Setup
//
// Copy `OpenUnityProject.cmd` (this file) to your Unity project folder which contains `Assets/`, `ProjectSettings/` etc.
//   - You do not need to copy any other files.
//
// ## Usage
//
// Run copied version of `OpenUnityProject.cmd`.
//   - You can invoke it from Explorer or Command prompt.
// 
// It will read `ProjectSettings/ProjectVersion.txt`, find appropriate version of `Unity.exe` and invoke it.
//   - Or it reports error.
//
// ## License
//
// OpenUnityProject.cmd is written by Takayuki Matsuoka.
//   This code is licensed under CC0-1.0,
//   https://creativecommons.org/publicdomain/zero/1.0/
//
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

public static class Program {
    private const string UnityArgFormat    = "-projectPath \"{0}\" {1}";
    private const string VersionLinePrefix = "m_EditorVersion: ";

    public static void Main() {
        string  currentDir      = Directory.GetCurrentDirectory();
        string  argString       = Environment.GetEnvironmentVariable("ARGS") ?? "";
        string  unityArg        = string.Format(UnityArgFormat, currentDir, argString);
        string  versionFileName = GetVersionFileName(currentDir);
        string  version         = GetProjectVersionString(versionFileName);
        string? unityEditor     = GetUnityEditorByVersion(version, versionFileName);

        if (unityEditor == null) {
            Abort();
        } else {
            Console.WriteLine("\"{0}\" {1}", unityEditor, unityArg);
            System.Diagnostics.Process.Start(unityEditor, unityArg);
        }
    }

    private static void Abort() => Environment.Exit(1);

    private static void Error(string s) {
        Console.WriteLine("\x1b[91m==== ERROR ====\x1b[0m");
        Console.WriteLine(s);
        Console.WriteLine("\x1b[91m==== ERROR ====\x1b[0m");
        Abort();
    }

    private static string GetVersionFileName(string unityProjectPath) =>
        $"{unityProjectPath}\\ProjectSettings\\ProjectVersion.txt";

    private static string GetProjectVersionString(string projectVersionFileName) {
        // Does ProjectVersion.txt exist?
        if (!File.Exists(projectVersionFileName)) {
            Error($"ProjectVersion.txt does not found at\n  {projectVersionFileName}");
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
        }

        return version;
    }

    private static string? GetUnityEditorByVersion(string version, string projectVersionFileName) {
        List<string>  hits       = new();
        List<string?> triedPaths = new();

        void CheckPath(string path) {
            if (File.Exists(path)) {
                hits.Add(path);
            } else {
                triedPaths.Add(path);
            }
        }

        CheckProgramFiles(version, CheckPath);
        CheckProgramFiles2(version, CheckPath);
        CheckSecondaryInstallPath(version, CheckPath);
        CheckEditorsJson(version, CheckPath);

        if (hits.Count > 0) {
            return hits[0];
        }

        // not found
        StringBuilder sb = new();
        sb.AppendLine($"Unity Editor {version} does not found in the following paths");
        foreach (string? path in triedPaths) {
            sb.AppendFormat("  {0}\n", path);
        }
        sb.AppendLine("");
        sb.AppendLine("Please check :");
        sb.AppendLine("  (1) Installed Unity Editor versions");
        sb.AppendLine("  (2) Content of ProjectVersion.txt at");
        sb.AppendFormat("  {0}\n", projectVersionFileName);
        Error(sb.ToString());
        return null;
    }

    private static void CheckProgramFiles(string version, Action<string> checkPath) {
        // (1) %ProgramFiles%\Unity\Hub\Editor\(Version)\Editor\Unity.exe
        var    p0 = $"%ProgramFiles%\\Unity\\Hub\\Editor\\{version}\\Editor\\Unity.exe";
        string p  = Environment.ExpandEnvironmentVariables(p0);
        checkPath(p);
    }

	private static void CheckProgramFiles2(string version, Action<string> checkPath) {
        // %ProgramFiles%\Unity <VERSION>\Editor\Unity.exe
        var    p0 = $"%ProgramFiles%\\Unity {version}\\Editor\\Unity.exe";
        string p  = Environment.ExpandEnvironmentVariables(p0);
        checkPath(p);
    }

    private static void CheckSecondaryInstallPath(string version, Action<string> checkPath) {
        // (2) %APPDATA%\UnityHub\secondaryInstallPath.json
        string jsonPath   = Environment.ExpandEnvironmentVariables("%APPDATA%\\UnityHub\\secondaryInstallPath.json");
        string jsonString = File.ReadAllText(jsonPath);
        string altPath    = jsonString.Replace('"', ' ').Trim();
        if (!string.IsNullOrEmpty(altPath)) {
            checkPath($"{altPath}\\{version}\\Editor\\Unity.exe");
        }
    }

    private static void CheckEditorsJson(string version, Action<string> checkPath) {
        // (3) %APPDATA%\UnityHub\editors.json
        string jsonPath    = Environment.ExpandEnvironmentVariables("%APPDATA%\\UnityHub\\editors.json");
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
