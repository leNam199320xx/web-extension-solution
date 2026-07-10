@echo off
title Plugin Runtime Platform - Full Stack
echo.
echo ================================================================
echo   Plugin Runtime Platform - Full Stack Local Launcher
echo ================================================================
echo.

set DOTNET=G:\net10\dotnet.exe
set ROOT=%~dp0src

echo   Building all projects...
echo.

:: Build System.Auth plugin
%DOTNET% build "%ROOT%\Plugins\System.Auth\System.Auth.csproj" -c Debug --nologo -v q
if %errorlevel% neq 0 ( echo   [FAIL] System.Auth Plugin & pause & exit /b 1 )
echo   [OK] System.Auth Plugin

:: Publish plugin DLL to plugins directory
if not exist "%ROOT%\PluginRuntime.Api\bin\Debug\net10.0\plugins\System.Auth" mkdir "%ROOT%\PluginRuntime.Api\bin\Debug\net10.0\plugins\System.Auth"
copy /Y "%ROOT%\Plugins\System.Auth\bin\Debug\net10.0\System.Auth.dll" "%ROOT%\PluginRuntime.Api\bin\Debug\net10.0\plugins\System.Auth\" >nul
copy /Y "%ROOT%\Plugins\System.Auth\bin\Debug\net10.0\PluginRuntime.Sdk.dll" "%ROOT%\PluginRuntime.Api\bin\Debug\net10.0\plugins\System.Auth\" >nul
copy /Y "%ROOT%\Plugins\System.Auth\manifest.json" "%ROOT%\PluginRuntime.Api\bin\Debug\net10.0\plugins\System.Auth\" >nul
echo   [OK] Plugin deployed

:: Build API
%DOTNET% build "%ROOT%\PluginRuntime.Api\PluginRuntime.Api.csproj" -c Debug --nologo -v q
if %errorlevel% neq 0 ( echo   [FAIL] API & pause & exit /b 1 )
echo   [OK] API Backend

:: Build Gateway
%DOTNET% build "%ROOT%\PublicApiGateway\PublicApiGateway.csproj" -c Debug --nologo -v q
if %errorlevel% neq 0 ( echo   [FAIL] Gateway & pause & exit /b 1 )
echo   [OK] API Gateway

:: Build Marketplace Portal
%DOTNET% build "%ROOT%\Marketplace\PluginRuntime.Marketplace\PluginRuntime.Marketplace.csproj" -c Debug --nologo -v q
if %errorlevel% neq 0 ( echo   [FAIL] Marketplace & pause & exit /b 1 )
echo   [OK] Marketplace Portal

:: Build Consumer Portal
%DOTNET% build "%ROOT%\ConsumerPortal\PluginRuntime.ConsumerPortal\PluginRuntime.ConsumerPortal.csproj" -c Debug --nologo -v q
if %errorlevel% neq 0 ( echo   [FAIL] Consumer Portal & pause & exit /b 1 )
echo   [OK] Consumer Portal

:: Build Admin Portal
%DOTNET% build "%ROOT%\Admin\PluginRuntime.Admin\PluginRuntime.Admin.csproj" -c Debug --nologo -v q
if %errorlevel% neq 0 ( echo   [FAIL] Admin Portal & pause & exit /b 1 )
echo   [OK] Admin Portal

echo.
echo ================================================================
echo   Starting all services (JSON storage, no DB required)
echo ================================================================
echo.
echo   Backend:
echo     API Backend       http://localhost:6100
echo     Swagger UI        http://localhost:6100/swagger
echo     API Gateway       http://localhost:6200
echo.
echo   Frontend Portals:
echo     Marketplace       http://localhost:6300
echo     Consumer Portal   http://localhost:6400
echo     Admin Portal      http://localhost:6500
echo.
echo ================================================================
echo.

:: Start API Backend
start "PluginRuntime-API" cmd /c "set ASPNETCORE_URLS=http://localhost:6100&& set ASPNETCORE_ENVIRONMENT=Development&& %DOTNET% run --no-build --project "%ROOT%\PluginRuntime.Api\PluginRuntime.Api.csproj""

timeout /t 3 /nobreak >nul

:: Start API Gateway
start "PluginRuntime-Gateway" cmd /c "set ASPNETCORE_URLS=http://localhost:6200&& set Upstream__BaseUrl=http://localhost:6100&& set "ConnectionStrings__Redis=localhost:6379,abortConnect=false"&& set ConnectionStrings__PostgreSQL=&& set ASPNETCORE_ENVIRONMENT=Development&& %DOTNET% run --no-build --project "%ROOT%\PublicApiGateway\PublicApiGateway.csproj""

timeout /t 2 /nobreak >nul

:: Start Marketplace Portal
start "PluginRuntime-Marketplace" cmd /c "set ASPNETCORE_URLS=http://localhost:6300&& set ASPNETCORE_ENVIRONMENT=Development&& %DOTNET% run --no-build --project "%ROOT%\Marketplace\PluginRuntime.Marketplace\PluginRuntime.Marketplace.csproj""

timeout /t 1 /nobreak >nul

:: Start Consumer Portal
start "PluginRuntime-Consumer" cmd /c "set ASPNETCORE_URLS=http://localhost:6400&& set ASPNETCORE_ENVIRONMENT=Development&& %DOTNET% run --no-build --project "%ROOT%\ConsumerPortal\PluginRuntime.ConsumerPortal\PluginRuntime.ConsumerPortal.csproj""

timeout /t 1 /nobreak >nul

:: Start Admin Portal
start "PluginRuntime-Admin" cmd /c "set ASPNETCORE_URLS=http://localhost:6500&& set ASPNETCORE_ENVIRONMENT=Development&& %DOTNET% run --no-build --project "%ROOT%\Admin\PluginRuntime.Admin\PluginRuntime.Admin.csproj""

echo.
echo   All 5 services started.
echo.
echo   Press any key to STOP ALL services and close windows...
echo.
pause

:: Kill all service windows
echo.
echo   Stopping all services...
taskkill /FI "WINDOWTITLE eq PluginRuntime-API*" /F >nul 2>&1
taskkill /FI "WINDOWTITLE eq PluginRuntime-Gateway*" /F >nul 2>&1
taskkill /FI "WINDOWTITLE eq PluginRuntime-Marketplace*" /F >nul 2>&1
taskkill /FI "WINDOWTITLE eq PluginRuntime-Consumer*" /F >nul 2>&1
taskkill /FI "WINDOWTITLE eq PluginRuntime-Admin*" /F >nul 2>&1

:: Also kill any dotnet processes on our ports
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":6100 :6200 :6300 :6400 :6500" ^| findstr "LISTENING"') do (
    taskkill /PID %%a /F >nul 2>&1
)

echo   All services stopped.
echo.
