@setlocal enabledelayedexpansion & set "ARGS=%*" & pwsh -nop -ex Bypass -c "Add-Type (gc '%~dpf0'|select -Skip 1|Out-String); exit [Program]::Main();" || pause & exit /b !ERRORLEVEL!
