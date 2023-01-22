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
// Copy `OpenUnityProject.cmd` (this file) to your Untiy project folder which contains `Assets/`, `ProjectSettings/` etc.
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
// JSON Parser is written by Daniel Crenna.
//   https://github.com/jorik041/json
//   This work is public domain.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

public static class Program {
    private const string UnityArgFormat    = "-projectPath \"{0}\" {1}";
    private const string VersionLinePrefix = "m_EditorVersion: ";

    public static void Main() {
        string currentDir      = Directory.GetCurrentDirectory();
        string versionFileName = GetVersionFileName(currentDir);
        string version         = GetProjectVersionString(versionFileName);
        string unityEditor     = GetUnityEditorByVersion(version, versionFileName);
        string argString       = Environment.GetEnvironmentVariable("ARGS") ?? "";
        string unityArg        = string.Format(UnityArgFormat, currentDir, argString);

        Console.WriteLine("\"{0}\" {1}", unityEditor, unityArg);
        System.Diagnostics.Process.Start(unityEditor, unityArg);
    }

    private static void Error(string s) {
        Console.WriteLine("\x1b[91m==== ERROR ====\x1b[0m");
        Console.WriteLine(s);
        Console.WriteLine("\x1b[91m==== ERROR ====\x1b[0m");
        Environment.Exit(1);
    }

    private static string GetVersionFileName(string unityProjectPath) {
        return string.Format("{0}\\ProjectSettings\\ProjectVersion.txt", unityProjectPath);
    }

    private static string GetProjectVersionString(string projectVersionFileName) {
        // Does ProjectVersion.txt exist?
        if (!File.Exists(projectVersionFileName)) {
            Error(
                string.Format("ProjectVersion.txt does not found at\n  {0}", projectVersionFileName)
            );
        }

        // Get version string from ProjectVersion.txt
        string version = "";
        {
            string[] lines = File.ReadAllLines(projectVersionFileName);
            foreach (string line in lines) {
                if (line.StartsWith(VersionLinePrefix)) {
                    version = line.Substring(VersionLinePrefix.Length);
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(version)) {
            Error(
                string.Format("The following ProjectVersion.txt does not contain version number\n  {0}", projectVersionFileName)
            );
        }

        return version;
    }

    private static string GetUnityEditorByVersion(string version, string projectVersionFileName) {
        var triedPaths = new List<string>();

        // (1) %ProgramFiles%\Unity\Hub\Editor\(Version)\Editor\Unity.exe
        {
            string p0 = string.Format("%ProgramFiles%\\Unity\\Hub\\Editor\\{0}\\Editor\\Unity.exe", version);
            string p  = Environment.ExpandEnvironmentVariables(p0);
            if (File.Exists(p)) {
                return p;
            }
            triedPaths.Add(p);
        }

        // (2) %APPDATA%\UnityHub\secondaryInstallPath.json
        {
            string jsonPath   = Environment.ExpandEnvironmentVariables("%APPDATA%\\UnityHub\\secondaryInstallPath.json");
            string jsonString = File.ReadAllText(jsonPath);
            string altPath    = jsonString.Replace('"', ' ').Trim();
            if (!string.IsNullOrEmpty(altPath)) {
                string p = string.Format("{0}\\{1}\\Editor\\Unity.exe", altPath, version);
                if (File.Exists(p)) {
                    return p;
                }
                triedPaths.Add(p);
            }
        }

        // (3) %APPDATA%\UnityHub\editors.json
        {
            string jsonPath   = Environment.ExpandEnvironmentVariables("%APPDATA%\\UnityHub\\editors.json");
            string jsonString = File.ReadAllText(jsonPath);

            DcJson.JsonToken            jsonTokenType;
            IDictionary<string, object> keyValuePairs0 = DcJson.JsonParser.FromJson(jsonString, out jsonTokenType);
            if (keyValuePairs0 != null) {
                foreach (KeyValuePair<string, object> keyValuePair0 in keyValuePairs0) {
                    var keyValuePairs = keyValuePair0.Value as IDictionary<string, object>;
                    if (keyValuePairs != null) {
                        foreach (KeyValuePair<string, object> keyValuePair in keyValuePairs) {
                            string versionKey = keyValuePair.Key;
                            if (version == versionKey) {
                                var entries = keyValuePair.Value as Dictionary<string, object>;
                                if (entries != null) {
                                    var locations = entries["location"] as List<object>;
                                    if (locations != null) {
                                        var p = locations[0] as string;
                                        if (File.Exists(p)) {
                                            return p;
                                        }
                                        triedPaths.Add(p);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // not found
        var sb = new StringBuilder();
        sb.AppendLine("Unity Editor does not found in the following paths");
        foreach (string path in triedPaths) {
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
}

//
// https://github.com/jorik041/json
//
// JSON Parser
// Written by Daniel Crenna
// (http://danielcrenna.com)
//  
// This work is public domain.
// "The person who associated a work with this document has 
//  dedicated the work to the Commons by waiving all of his
//  or her rights to the work worldwide under copyright law
//  and all related or neighboring legal rights he or she
//  had in the work, to the extent allowable by law."
//   
// For more information, please visit:
// http://creativecommons.org/publicdomain/zero/1.0/
//
namespace DcJson {
    /// <summary>
    /// Possible JSON tokens in parsed input.
    /// </summary>
    public enum JsonToken {
        Unknown,
        LeftBrace,    // {
        RightBrace,   // }
        Colon,        // :
        Comma,        // ,
        LeftBracket,  // [
        RightBracket, // ]
        String,
        Number,
        True,
        False,
        Null
    }

    /// <summary>
    /// Exception raised when <see cref="JsonParser" /> encounters an invalid token.
    /// </summary>
    public class InvalidJsonException : Exception {
        public InvalidJsonException(string message) : base(message) {
        }
    }

    /// <summary>
    /// A parser for JSON.
    /// <seealso cref="http://json.org" />
    /// </summary>
    public static class JsonParser {
        private const NumberStyles JsonNumbers = NumberStyles.Float;

        public static IDictionary<string, object> FromJson(string json, out JsonToken type) {
            char[] data  = json.ToCharArray();
            var    index = 0;

            // Rewind index for first token
            JsonToken token = NextToken(data, ref index);
            switch (token) {
                case JsonToken.LeftBrace:   // Start Object
                case JsonToken.LeftBracket: // Start Array
                    index--;
                    type = token;
                    break;
                default:
                    throw new InvalidJsonException("JSON must begin with an object or array");
            }

            return ParseObject(data, ref index);
        }

        private static KeyValuePair<string, object> ParsePair(IReadOnlyList<char> data, ref int index) {
            var valid = true;

            string name = ParseString(data, ref index);
            if (name == null) {
                valid = false;
            }

            if (!ParseToken(JsonToken.Colon, data, ref index)) {
                valid = false;
            }

            if (!valid) {
                throw new InvalidJsonException(
                    string.Format(
                        "Invalid JSON found while parsing a value pair at index {0}.",
                        index
                    )
                );
            }

            index++;
            object value = ParseValue(data, ref index);
            return new KeyValuePair<string, object>(name, value);
        }

        private static bool ParseToken(JsonToken token, IReadOnlyList<char> data, ref int index) {
            JsonToken nextToken = NextToken(data, ref index);
            return token == nextToken;
        }

        private static string ParseString(IReadOnlyList<char> data, ref int index) {
            char symbol = data[index];
            IgnoreWhitespace(data, ref index, symbol);
            symbol = data[++index]; // Skip first quotation

            var sb = new StringBuilder();
            while (true) {
                if (index >= data.Count - 1) {
                    return null;
                }
                switch (symbol) {
                    case '"': // End String
                        index++;
                        return sb.ToString();
                    case '\\': // Control Character
                        symbol = data[++index];
                        switch (symbol) {
                            case '"':  // @t-mat : See "string" in https://www.json.org/json-en.html
                            case '\\': // @t-mat : See "string" in https://www.json.org/json-en.html
                            case '/':
                                sb.Append(symbol);
                                break;
                            case 'b':
                            case 'f':
                            case 'n':
                            case 'r':
                            case 't':
                                break;
                            case 'u': // Unicode literals
                                if (index < data.Count - 5) {
                                    char[] array  = data.ToArray();
                                    var    buffer = new char[4];
                                    Array.Copy(array, index + 1, buffer, 0, 4);

                                    // http://msdn.microsoft.com/en-us/library/aa664669%28VS.71%29.aspx
                                    // http://www.yoda.arachsys.com/csharp/unicode.html
                                    // http://en.wikipedia.org/wiki/UTF-32/UCS-4
                                    var hex     = new string(buffer);
                                    var unicode = (char)Convert.ToInt32(hex, 16);
                                    sb.Append(unicode);
                                    index += 4;
                                }
                                break;
                        }
                        break;
                    default:
                        sb.Append(symbol);
                        break;
                }
                symbol = data[++index];
            }
        }

        private static object ParseValue(IReadOnlyList<char> data, ref int index) {
            JsonToken token = NextToken(data, ref index);
            switch (token) {
                // End Tokens
                case JsonToken.RightBracket: // Bad Data
                case JsonToken.RightBrace:
                case JsonToken.Unknown:
                case JsonToken.Colon:
                case JsonToken.Comma:
                    throw new InvalidJsonException(
                        string.Format(
                            "Invalid JSON found while parsing a value at index {0}.",
                            index
                        )
                    );
                // Value Tokens
                case JsonToken.LeftBrace:
                    return ParseObject(data, ref index);
                case JsonToken.LeftBracket:
                    return ParseArray(data, ref index);
                case JsonToken.String:
                    return ParseString(data, ref index);
                case JsonToken.Number:
                    return ParseNumber(data, ref index);
                case JsonToken.True:
                    return true;
                case JsonToken.False:
                    return false;
                case JsonToken.Null:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static IDictionary<string, object> ParseObject(IReadOnlyList<char> data, ref int index) {
            var result = new Dictionary<string, object>(0, StringComparer.OrdinalIgnoreCase);

            index++; // Skip first token

            while (index < data.Count - 1) {
                JsonToken token = NextToken(data, ref index);
                switch (token) {
                    // End Tokens
                    case JsonToken.Unknown: // Bad Data
                    case JsonToken.True:
                    case JsonToken.False:
                    case JsonToken.Null:
                    case JsonToken.Colon:
                    case JsonToken.RightBracket:
                    case JsonToken.Number:
                        throw new InvalidJsonException(
                            string.Format(
                                "Invalid JSON found while parsing an object at index {0}.",
                                index
                            )
                        );
                    case JsonToken.RightBrace: // End Object
                        index++;
                        return result;
                    // Skip Tokens
                    case JsonToken.Comma:
                        index++;
                        break;
                    // Start Tokens
                    case JsonToken.LeftBrace: // Start Object
                        IDictionary<string, object> @object = ParseObject(data, ref index);
                        if (@object != null) {
                            result.Add(string.Concat("object", result.Count), @object);
                        }
                        index++;
                        break;
                    case JsonToken.LeftBracket: // Start Array
                        IEnumerable<object> @array = ParseArray(data, ref index);
                        if (@array != null) {
                            result.Add(string.Concat("array", result.Count), @array);
                        }
                        index++;
                        break;
                    case JsonToken.String:
                        KeyValuePair<string, object> pair = ParsePair(data, ref index);
                        result.Add(pair.Key, pair.Value);
                        break;
                    default:
                        throw new NotSupportedException("Invalid token expected.");
                }
            }

            return result;
        }

        private static IEnumerable<object> ParseArray(IReadOnlyList<char> data, ref int index) {
            var result = new List<object>();

            index++; // Skip first bracket
            while (index < data.Count - 1) {
                JsonToken token = NextToken(data, ref index);
                switch (token) {
                    // End Tokens
                    case JsonToken.Unknown: // Bad Data
                        throw new InvalidJsonException(
                            string.Format(
                                "Invalid JSON found while parsing an array at index {0}.",
                                index
                            )
                        );
                    case JsonToken.RightBracket: // End Array
                        index++;
                        return result;
                    // Skip Tokens
                    case JsonToken.Comma:      // Separator
                    case JsonToken.RightBrace: // End Object
                    case JsonToken.Colon:      // Separator
                        index++;
                        break;
                    // Value Tokens
                    case JsonToken.LeftBrace: // Start Object
                        IDictionary<string, object> nested = ParseObject(data, ref index);
                        result.Add(nested);
                        break;
                    case JsonToken.LeftBracket: // Start Array
                    case JsonToken.String:
                    case JsonToken.Number:
                    case JsonToken.True:
                    case JsonToken.False:
                    case JsonToken.Null:
                        object value = ParseValue(data, ref index);
                        result.Add(value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return result;
        }

        private static object ParseNumber(IReadOnlyList<char> data, ref int index) {
            char symbol = data[index];
            IgnoreWhitespace(data, ref index, symbol);

            int start  = index;
            var length = 0;
            while (ParseToken(JsonToken.Number, data, ref index)) {
                length++;
                index++;
            }

            var number = new char[length];
            Array.Copy(data.ToArray(), start, number, 0, length);

            double result;
            var    buffer = new string(number);
            if (!double.TryParse(buffer, JsonNumbers, CultureInfo.InvariantCulture, out result)) {
                throw new InvalidJsonException(
                    string.Format("Value '{0}' was not a valid JSON number", buffer)
                );
            }

            return result;
        }

        private static JsonToken NextToken(IReadOnlyList<char> data, ref int index) {
            char      symbol = data[index];
            JsonToken token  = GetTokenFromSymbol(symbol);
            token = IgnoreWhitespace(data, ref index, ref token, symbol);

            GetKeyword("true",  JsonToken.True,  data, ref index, ref token);
            GetKeyword("false", JsonToken.False, data, ref index, ref token);
            GetKeyword("null",  JsonToken.Null,  data, ref index, ref token);

            return token;
        }

        private static JsonToken GetTokenFromSymbol(char symbol) {
            return GetTokenFromSymbol(symbol, JsonToken.Unknown);
        }

        private static JsonToken GetTokenFromSymbol(char symbol, JsonToken token) {
            switch (symbol) {
                case '{':
                    token = JsonToken.LeftBrace;
                    break;
                case '}':
                    token = JsonToken.RightBrace;
                    break;
                case ':':
                    token = JsonToken.Colon;
                    break;
                case ',':
                    token = JsonToken.Comma;
                    break;
                case '[':
                    token = JsonToken.LeftBracket;
                    break;
                case ']':
                    token = JsonToken.RightBracket;
                    break;
                case '"':
                    token = JsonToken.String;
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '.': // @t-mat : This is an invalid first character of "Number".  See "number" in https://www.json.org/json-en.html
                case 'e': // @t-mat : This is an invalid first character of "Number".  See "number" in https://www.json.org/json-en.html
                case 'E': // @t-mat : This is an invalid first character of "Number".  See "number" in https://www.json.org/json-en.html
                case '+': // @t-mat : This is an invalid first character of "Number".  See "number" in https://www.json.org/json-en.html
                case '-':
                    token = JsonToken.Number;
                    break;
            }
            return token;
        }

        private static void IgnoreWhitespace(IReadOnlyList<char> data, ref int index, char symbol) {
            var token = JsonToken.Unknown;
            IgnoreWhitespace(data, ref index, ref token, symbol);
        }

        private static JsonToken IgnoreWhitespace(IReadOnlyList<char> data, ref int index, ref JsonToken token, char symbol) {
            switch (symbol) {
                case ' ':  // 'U+0020', Space
                case '\n': // 'U+000a', Linefeed
                case '\r': // 'U+000d', Carriage return
                case '\t': // 'U+0009', Horizontal tab
                    index++;
                    token = NextToken(data, ref index);
                    break;
            }
            return token;
        }

        private static void GetKeyword(
            string              word,
            JsonToken           target,
            IReadOnlyList<char> data,
            ref int             index,
            ref JsonToken       result
        ) {
            int buffer = data.Count - index;
            if (buffer < word.Length) {
                return;
            }

            for (var i = 0; i < word.Length; i++) {
                if (data[index + i] != word[i]) {
                    return;
                }
            }

            result =  target;
            index  += word.Length;
        }
    }
}
