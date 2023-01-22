Windows batch file which invokes appropriate version of `Unity.exe`.


Setup
-----

Download and unzip the following file

- [OpenUnityProject_v0.1.3.zip](https://github.com/t-mat/OpenUnityProject/releases/download/v0.1.3/OpenUnityProject_v0.1.3.zip)

Copy `OpenUnityProject.cmd` to your Untiy project folder which contains `Assets/`, `ProjectSettings/` etc.
  - You do not need to copy any other files.

```
YourUnityProjectFolder
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


Build
-----

```
cmd.exe /c .\scripts\build.cmd
```
