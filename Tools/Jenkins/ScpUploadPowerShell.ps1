# SCP Upload Script for Hot Update Resources (PowerShell)
# Usage: .\ScpUploadPowerShell.ps1 -LocalDir "local_dir" -SshServer "server" -SshPort "port" -SshUser "user" -SshPass "password" -RemoteDir "remote_dir"
# Dependency: Requires pscp.exe (PuTTY) or Windows 10+ built-in ssh/scp

param(
    [Parameter(Mandatory=$true)]
    [string]$LocalDir,
    
    [Parameter(Mandatory=$true)]
    [string]$SshServer,
    
    [string]$SshPort = "22",
    
    [Parameter(Mandatory=$true)]
    [string]$SshUser,
    
    [Parameter(Mandatory=$true)]
    [string]$SshPass,
    
    [string]$RemoteDir = "/var/www/html/hotfix"
)

$ErrorActionPreference = "Stop"

# Convert forward slashes to backslashes for Windows compatibility
$LocalDir = $LocalDir -replace '/', '\'
$RemoteDir = $RemoteDir -replace '//', '/'

Write-Host "========================================"
Write-Host "SCP Upload Started"
Write-Host "Local Directory: $LocalDir"
Write-Host "Server: ${SshUser}@${SshServer}:${SshPort}"
Write-Host "Remote Directory: $RemoteDir"
Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host "========================================"

if (-not (Test-Path $LocalDir)) {
    Write-Error "[ERROR] Local directory does not exist: $LocalDir"
    exit 1
}

# Check for pscp (PuTTY SCP)
$pscpPath = $null
$usePscp = $false

# Search for pscp.exe
$possiblePaths = @(
    "C:\Program Files\PuTTY\pscp.exe",
    "C:\Program Files (x86)\PuTTY\pscp.exe",
    "$env:ProgramFiles\PuTTY\pscp.exe",
    "${env:ProgramFiles(x86)}\PuTTY\pscp.exe"
)

foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $pscpPath = $path
        $usePscp = $true
        break
    }
}

# Check if pscp is in PATH
if (-not $usePscp) {
    try {
        $null = Get-Command pscp -ErrorAction Stop
        $pscpPath = "pscp"
        $usePscp = $true
    }
    catch {
        # pscp not available
    }
}

# Check Windows built-in scp
$useBuiltinScp = $false
if (-not $usePscp) {
    try {
        $null = Get-Command scp -ErrorAction Stop
        $useBuiltinScp = $true
    }
    catch {
        # scp not available
    }
}

if (-not $usePscp -and -not $useBuiltinScp) {
    Write-Host "[WARNING] Neither pscp nor scp found..."
    Write-Error "[ERROR] Please install PuTTY (pscp.exe) or enable Windows OpenSSH client"
    Write-Host "Hint: You can install via:"
    Write-Host "  1. Download PuTTY: https://www.putty.org/"
    Write-Host "  2. Or enable OpenSSH Client in Windows Settings -> Apps -> Optional Features"
    exit 1
}

$successCount = 0
$failCount = 0

if ($usePscp) {
    Write-Host "[INFO] Using pscp for upload..."
    
    # Use pscp to upload entire directory (-r recursive)
    # -batch: disable interactive prompts
    # -pw: specify password
    # -P: specify port
    $pscpArgs = @(
        "-batch",
        "-r",
        "-pw", $SshPass,
        "-P", $SshPort,
        "$LocalDir\*",
        "${SshUser}@${SshServer}:${RemoteDir}/"
    )
    
    $displayArgs = $pscpArgs -join " " -replace [regex]::Escape($SshPass), "****"
    Write-Host "[EXEC] pscp $displayArgs"
    
    # First create remote directory
    $plinkPath = $pscpPath -replace "pscp", "plink"
    if (Test-Path $plinkPath) {
        Write-Host "[STEP1] Creating remote directory..."
        $mkdirArgs = @(
            "-batch",
            "-pw", $SshPass,
            "-P", $SshPort,
            "${SshUser}@${SshServer}",
            "mkdir -p `"$RemoteDir`""
        )
        & $plinkPath $mkdirArgs 2>&1 | Out-Null
    }
    
    Write-Host "[STEP2] Uploading files..."
    $process = Start-Process -FilePath $pscpPath -ArgumentList $pscpArgs -NoNewWindow -Wait -PassThru
    
    if ($process.ExitCode -eq 0) {
        Write-Host "[SUCCESS] File upload completed"
        $successCount = (Get-ChildItem -Path $LocalDir -Recurse -File).Count
    }
    else {
        Write-Host "[FAILED] pscp returned error code: $($process.ExitCode)"
        $failCount = 1
    }
}
elseif ($useBuiltinScp) {
    Write-Host "[INFO] Using Windows built-in scp for upload..."
    Write-Host "[WARNING] Built-in scp does not support password parameter, please ensure SSH key authentication is configured"
    Write-Host "          Or install PuTTY for password authentication support"
    
    # First create remote directory
    Write-Host "[STEP1] Creating remote directory..."
    $sshArgs = "-o StrictHostKeyChecking=no -p $SshPort ${SshUser}@${SshServer} `"mkdir -p $RemoteDir`""
    Start-Process -FilePath "ssh" -ArgumentList $sshArgs -NoNewWindow -Wait 2>&1 | Out-Null
    
    # Use built-in scp (-r recursive)
    Write-Host "[STEP2] Uploading files..."
    $scpArgs = "-o StrictHostKeyChecking=no -r -P $SshPort `"$LocalDir\*`" ${SshUser}@${SshServer}:${RemoteDir}/"
    
    $process = Start-Process -FilePath "scp" -ArgumentList $scpArgs -NoNewWindow -Wait -PassThru
    
    if ($process.ExitCode -eq 0) {
        Write-Host "[SUCCESS] File upload completed"
        $successCount = (Get-ChildItem -Path $LocalDir -Recurse -File).Count
    }
    else {
        Write-Host "[FAILED] scp returned error code: $($process.ExitCode)"
        $failCount = 1
    }
}

Write-Host "========================================"
Write-Host "SCP Upload Completed"
Write-Host "Success: $successCount files"
if ($failCount -gt 0) {
    Write-Host "Failed: $failCount"
}
Write-Host "========================================"

if ($failCount -gt 0) {
    exit 1
}

exit 0
