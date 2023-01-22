@setlocal enabledelayedexpansion
pushd "%~dp0.."

for /f "tokens=*" %%a in ('git describe --tags') do (
    set TAG=%%a
)

rmdir /S /Q staging 2>nul
mkdir staging
pushd staging

copy ..\README.md .
copy ..\LICENSE .
copy ..\OpenUnityProject.cmd .

del  ..\OpenUnityProject_%TAG%.zip 2>nul
7z a ..\OpenUnityProject_%TAG%.zip *.* *

popd
rmdir /S /Q staging

popd
