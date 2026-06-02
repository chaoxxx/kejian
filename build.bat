@echo off
chcp 65001 >nul
title 刻简 - 构建脚本

echo ========================================
echo   📖 刻简 - 构建脚本
echo ========================================
echo.

:: 检查 .NET Framework SDK 是否安装
where msbuild >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [错误] 未找到 MSBuild！
    echo.
    echo 请确保已安装以下之一：
    echo   - Visual Studio 2022 (勾选 ".NET 桌面开发" 工作负载)
    echo   - .NET Framework 4.8 SDK
    echo   - 或使用以下命令安装构建工具：
    echo     winget install Microsoft.VisualStudio.2022.BuildTools
    echo.
    pause
    exit /b 1
)

:: 方式一：使用 dotnet CLI（推荐，如果安装了 .NET SDK 6.0+）
where dotnet >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo [信息] 检测到 dotnet CLI，使用 dotnet 构建...
    echo.
    
    :: 还原 NuGet 包
    echo [1/3] 还原 NuGet 包...
    call dotnet restore -p:RestorePackagesPath=.\packages
    if %ERRORLEVEL% NEQ 0 goto error
    
    :: 构建 Release
    echo [2/3] 编译 Release...
    call dotnet build -c Release --no-restore
    if %ERRORLEVEL% NEQ 0 goto error
    
    :: 发布单文件
    echo [3/3] 发布单文件...
    call dotnet publish -c Release -o .\publish --no-build
    if %ERRORLEVEL% NEQ 0 goto error
    
    goto success
)

:: 方式二：使用 MSBuild（备用）
echo [信息] 使用 MSBuild 构建...
echo.

echo [1/2] 还原 NuGet 包...
nuget restore KeJian.csproj -PackagesDirectory .\packages
if %ERRORLEVEL% NEQ 0 (
    echo [警告] NuGet restore 失败，尝试直接构建...
)

echo [2/2] 编译...
msbuild KeJian.csproj /p:Configuration=Release /t:Build /p:RestorePackagesPath=.\packages
if %ERRORLEVEL% NEQ 0 goto error

goto success

:success
echo.
echo ========================================
echo   ✅ 构建成功！
echo ========================================
echo.
echo 输出文件:
if exist .\bin\Release\net48\KeJian.exe (
    echo   .\bin\Release\net48\KeJian.exe
)
if exist .\publish\KeJian.exe (
    echo   .\publish\KeJian.exe  ^(单文件版^)
)
echo.
echo 启动方式：双击 KeJian.exe
echo 数据目录：自动在 exe 同目录创建 data/
echo.
pause
exit /b 0

:error
echo.
echo ========================================
echo   ❌ 构建失败！
echo ========================================
echo.
echo 请检查：
echo   1. 是否安装了 .NET Framework 4.8 SDK
echo   2. NuGet 包是否可访问 (nuget.org)
echo   3. 网络连接是否正常
echo.
pause
exit /b 1
