$env:PATH = "G:\net10;" + $env:PATH
$env:DOTNET_ROOT = "G:\net10"
$env:DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR = "G:\net10"
$env:DOTNET_CLI_ENABLE_SLNX = "true"

# Khởi động Kiro Agent trong chính phiên làm việc này
kiro .
