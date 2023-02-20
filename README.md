Windows batch file which invokes appropriate version of `Unity.exe`.


Setup
-----

You can setup your Unity Project with the following commands:

```
pushd "\PATH\TO\Your\UnityProject\"
where ProjectSettings:ProjectVersion.txt && curl.exe -JOL https://raw.githubusercontent.com/t-mat/OpenUnityProject/main/OpenUnityProject.cmd
.\OpenUnityProject.cmd
```

Please note that `\PATH\TO\Your\UnityProject\` means the root directory of your Untiy Project.
The root directory must contains `Assets/` etc.

```
\PATH\TO\Your\UnityProject\
|
+---Assets
|
+---ProjectSettings
|   |
|   +---ProjectVersion.txt
|
+---OpenUnityProject.cmd      <===
```


Usage
-----

Run copied version of `OpenUnityProject.cmd`.
  - You can invoke it from Explorer or Command prompt.

It will read `ProjectSettings/ProjectVersion.txt`, find appropriate version of `Unity.exe` and invoke it.
  - Or it reports error.


Clone & Build
-------------

```bat
pushd "%USERPROFILE%\Documents"
git clone https://github.com/t-mat/OpenUnityProject.git
cd OpenUnityProject

@rem Edit and build
notepad src\OpenUnityProject.cs
cmd.exe /c .\scripts\build.cmd

@rem Make sure OpenUnityProject.cmd is updated
dir .\OpenUnityProject.cmd
```
