# Orax Hotel — سكربت بناء المُثبّت المتكامل
# Build-Installer.ps1
# يبني HotelSys.exe (نشر ذاتي على Windows x64) ثم يجمّع الحمولة payload.7z
# ثم يبني OraxHotel-Setup.exe المُثبّت النهائي.

[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipAppBuild,
    [switch]$SkipInstallerBuild,
    [string]$OutputDir = ".\build-output"
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path "$PSScriptRoot\.."
$installerDir = "$root\installer"
$hotelSysDir = "$root\HotelSys"
$outputDir = "$installerDir\$OutputDir"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Orax Hotel — سكربت بناء المُثبّت المتكامل" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Project root:    $root"
Write-Host "Configuration:   $Configuration"
Write-Host "Runtime:         $Runtime"
Write-Host "Output dir:      $outputDir"
Write-Host ""

# ============================================================================
# 1) التحقق من توفر dotnet SDK 8
# ============================================================================
Write-Host "[1/5] التحقق من dotnet SDK 8 ..." -ForegroundColor Yellow
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Error "dotnet SDK غير مُثبّت. نزّله من: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
}
$sdks = & dotnet --list-sdks 2>$null
if (-not ($sdks -match "^8\.")) {
    Write-Error "dotnet SDK 8 غير مُثبّت. الإصدارات المتاحة: $($sdks -join ', ')"
    exit 1
}
Write-Host "  [OK] dotnet SDK 8 متوفر" -ForegroundColor Green

# ============================================================================
# 2) التحقق من وسيط SQL Server Express المضمّن
# ============================================================================
$sqlMedia = "$installerDir\SQLEXPR_x64_ENU.exe"
$sqlMediaUrl = "https://download.microsoft.com/download/7/c/1/7c14e92e-bdcb-4f89-b7cf-93543e7112d1/SQLEXPR_x64_ENU.exe"
$sqlExpectedSize = 261082544
$sqlExpectedHash = "BEA033E778048748EB1C87BF57597F7F5449B6A15BAC55DDC08263C57F7A1CA8"
if (-not (Test-Path $sqlMedia) -or (Get-Item $sqlMedia).Length -ne $sqlExpectedSize) {
    Write-Host "[2/6] تنزيل وسيط SQL Server Express من Microsoft ..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri $sqlMediaUrl -OutFile $sqlMedia -UseBasicParsing
}
$sqlHash = (Get-FileHash $sqlMedia -Algorithm SHA256).Hash.ToUpperInvariant()
if ((Get-Item $sqlMedia).Length -ne $sqlExpectedSize -or $sqlHash -ne $sqlExpectedHash) {
    Write-Error "فشل التحقق من وسيط SQL Server Express. الحجم أو SHA-256 غير مطابق."
    exit 1
}
Write-Host "  [OK] وسيط SQL Server Express صالح" -ForegroundColor Green

# ============================================================================
# 3) بناء HotelSys.exe — نشر ذاتي على win-x64
# ============================================================================
if (-not $SkipAppBuild) {
    Write-Host ""
    Write-Host "[3/6] بناء HotelSys.exe (نشر ذاتي) ..." -ForegroundColor Yellow
    Push-Location $hotelSysDir
    try {
        & dotnet restore HotelSys.csproj
        if ($LASTEXITCODE -ne 0) { Write-Error "فشل dotnet restore"; exit 1 }

        & dotnet publish HotelSys.csproj `
            -c $Configuration `
            -r $Runtime `
            --self-contained true `
            -p:PublishSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:DebugType=None `
            -o "$outputDir\payload"
        if ($LASTEXITCODE -ne 0) { Write-Error "فشل dotnet publish لـ HotelSys"; exit 1 }
        Write-Host "  [OK] تم نشر HotelSys في: $outputDir\payload" -ForegroundColor Green
    }
    finally { Pop-Location }
} else {
    Write-Host "[3/6] تم تخطي بناء التطبيق" -ForegroundColor DarkGray
}

# ============================================================================
# 3) تجهيز ملفات الحمولة
# ============================================================================
Write-Host ""
Write-Host "[4/6] تجهيز ملفات الحمولة ..." -ForegroundColor Yellow

# نسخ appsettings.json إلى payload (سيُكتب فوقه المُثبّت أثناء التثبيت)
if (Test-Path "$hotelSysDir\appsettings.json") {
    Copy-Item "$hotelSysDir\appsettings.json" "$outputDir\payload\appsettings.json" -Force
    Write-Host "  [OK] تم نسخ appsettings.json النموذجي" -ForegroundColor Green
}

# نسخ نسخة قاعدة البيانات الاحتياطية إلى payload
$backupSrc = "$root\HotelSys\database\Hotel_alkheer20232009552241.bak"
$backupDstDir = "$outputDir\payload\database"
$backupDst = "$backupDstDir\Hotel_alkheer20232009552241.bak"
if (-not (Test-Path $backupDstDir)) { New-Item -ItemType Directory -Force -Path $backupDstDir | Out-Null }
if (Test-Path $backupSrc) {
    Copy-Item $backupSrc $backupDst -Force
    Write-Host "  [OK] تم نسخ ملف النسخة الاحتياطية ($(Get-Item $backupDst | Select-Object -ExpandProperty Length) bytes)" -ForegroundColor Green
} else {
    Write-Warning "ملف النسخة الاحتياطية غير موجود: $backupSrc"
}

# نسخ ملف SQL التهيئي إن وُجد
$initSql = "$hotelSysDir\database\Hotel_alkheer_init.sql"
if (Test-Path $initSql) {
    Copy-Item $initSql "$backupDstDir\Hotel_alkheer_init.sql" -Force
    Write-Host "  [OK] تم نسخ ملف SQL التهيئي" -ForegroundColor Green
}

# نسخ مجلد wwwroot بالكامل
$wwwrootSrc = "$hotelSysDir\wwwroot"
$wwwrootDst = "$outputDir\payload\wwwroot"
if (Test-Path $wwwrootSrc) {
    if (Test-Path $wwwrootDst) { Remove-Item $wwwrootDst -Recurse -Force }
    Copy-Item $wwwrootSrc $wwwrootDst -Recurse -Force
    Write-Host "  [OK] تم نسخ wwwroot" -ForegroundColor Green
}

# ============================================================================
# 4) ضغط الحمولة إلى payload.7z
# ============================================================================
Write-Host ""
Write-Host "[5/6] ضغط الحمولة إلى payload.7z ..." -ForegroundColor Yellow
$sevenZip = "$installerDir\7zr.exe"
if (-not (Test-Path $sevenZip) -or (Get-Item $sevenZip).Length -lt 100000) {
    Write-Host "  تنزيل 7zr.exe من 7-zip.org ..." -ForegroundColor DarkGray
    Invoke-WebRequest -Uri "https://www.7-zip.org/a/7zr.exe" -OutFile $sevenZip -UseBasicParsing
}
if ((Get-Item $sevenZip).Length -lt 100000) { Write-Error "7zr.exe غير صالح"; exit 1 }
$payloadStaging = "$outputDir\payload"
$payloadArchive = "$installerDir\payload.7z"
if (Test-Path $payloadArchive) { Remove-Item $payloadArchive -Force }
& $sevenZip a -t7z -mx=5 -mmt=on $payloadArchive "$payloadStaging\*"
if ($LASTEXITCODE -ne 0) { Write-Error "فشل ضغط الحمولة"; exit 1 }
Write-Host "  [OK] payload.7z جاهز ($(Get-Item $payloadArchive | Select-Object -ExpandProperty Length) bytes)" -ForegroundColor Green

# ============================================================================
# 5) بناء المُثبّت OraxHotel-Setup.exe
# ============================================================================
if (-not $SkipInstallerBuild) {
    Write-Host ""
    Write-Host "[6/6] بناء OraxHotel-Setup.exe ..." -ForegroundColor Yellow
    Push-Location $installerDir
    try {
        & dotnet restore Installer.csproj
        if ($LASTEXITCODE -ne 0) { Write-Error "فشل dotnet restore للمُثبّت"; exit 1 }

        & dotnet publish Installer.csproj `
            -c $Configuration `
            -r $Runtime `
            --self-contained true `
            -p:PublishSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:DebugType=None `
            -o "$outputDir\installer"
        if ($LASTEXITCODE -ne 0) { Write-Error "فشل بناء المُثبّت"; exit 1 }
        Write-Host "  [OK] OraxHotel-Setup.exe جاهز في: $outputDir\installer" -ForegroundColor Green
    }
    finally { Pop-Location }
} else {
    Write-Host "[6/6] تم تخطي بناء المُثبّت" -ForegroundColor DarkGray
}

# ============================================================================
# إنهاء
# ============================================================================
Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  [نجاح] تم بناء الحزمة الكاملة" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host "ملف المُثبّت النهائي: $outputDir\installer\OraxHotel-Setup.exe"
Write-Host ""
Write-Host "لاستخدام المُثبّت:"
Write-Host "  1) انسخ OraxHotel-Setup.exe إلى جهاز Windows المستهدف"
Write-Host "  2) شغّله (يُفضّل Run as Administrator)"
Write-Host "  3) سيقوم تلقائياً بـ:"
Write-Host "     - تثبيت SQL Server Express محلياً عند الحاجة"
Write-Host "     - استخراج ملفات التطبيق إلى %LOCALAPPDATA%\OraxHotel"
Write-Host "     - استعادة قاعدة Hotel_alkheer من النسخة الاحتياطية"
Write-Host "     - كتابة اتصال Windows المحلي في appsettings.json"
Write-Host "     - الحفاظ على حساب المشرف الموجود وعدم إنشاء admin جديد"
Write-Host "     - إنشاء اختصار على سطح المكتب"
Write-Host "     - تشغيل التطبيق وفتح المتصفح على http://localhost:5080"
Write-Host ""
Write-Host "ملاحظة: يجب إدخال بيانات المشرف الأصلية الموجودة في النسخة الاحتياطية عند فتح شاشة الدخول."
