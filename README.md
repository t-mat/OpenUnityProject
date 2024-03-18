Windows batch file which invokes appropriate version of `Unity.exe` without UnityHub.


Prerequisites
-------------

- Windows 10/11
- `pwsh` ([PowerShell7](https://microsoft.com/PowerShell))
- Unity 2019 or greater.


Setup
-----

You can set up your Unity project with the following commands:

```
pushd "\PATH\TO\Your\UnityProject\"
where.exe ProjectSettings:ProjectVersion.txt && curl.exe -JOL https://raw.githubusercontent.com/t-mat/OpenUnityProject/main/OpenUnityProject.cmd
.\OpenUnityProject.cmd
```

Please note that `\PATH\TO\Your\UnityProject\` means the root directory of your Untiy project.
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

Run the copied version of `OpenUnityProject.cmd`.
  - You can invoke it via Explorer, Command prompt or PowerShell.

It will read `ProjectSettings/ProjectVersion.txt`, find appropriate version of `Unity.exe` and invoke it.
  - Or it reports an error.
  - It tries to avoid to launch UnityHub.


Usage (#2)
----------

If you put `OpenUnityProject.cmd` to the `%PATH%`, you can invoke it as a "normal" command.

```
cd /d C:\Path\To\Your\UnityProject
OpenUnityProject.cmd
```


Special CLI argument
--------------------

`--open-unity-project-with=XXX`
- You can specify the desired version of `unity.exe`.
- For example: `OpenUnityProject.cmd --open-unity-project-with=2022.3.19f1`


Note about Unity 6
------------------

New versioning convention specifies Unity 6 as version `6000`.
For example, you can invoke `Unity 6000.0.0b11` by the following command:

```bat
cd /d C:\Path\To\Your\UnityProject
OpenUnityProject.cmd --open-unity-project-with=6000.0.0b11
```


Clone & Build
-------------

```bat
pushd "%USERPROFILE%\Documents"
git clone https://github.com/t-mat/OpenUnityProject.git
cd OpenUnityProject

: Edit and build
notepad src\OpenUnityProject.cs
cmd.exe /c .\scripts\build.cmd

: Make sure OpenUnityProject.cmd is updated
dir .\OpenUnityProject.cmd
```
