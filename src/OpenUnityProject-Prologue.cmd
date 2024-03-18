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

