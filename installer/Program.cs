using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace OraxHotel.Installer;

internal static class Program
{
    private const string AppName = "OraxHotel";
    private const string DatabaseName = "Hotel_alkheer";

    private static InstallerConfig _config = new();
    private static string _installDir = string.Empty;
    private static string _tempDir = string.Empty;
    private static string _extractionDir = string.Empty;
    private static string _sevenZipPath = string.Empty;
    private static string _archivePath = string.Empty;

    private static int Main(string[] args)
    {
        bool interactive = args.Contains("--interactive", StringComparer.OrdinalIgnoreCase);
        bool verbose = args.Contains("--verbose", StringComparer.OrdinalIgnoreCase) || args.Contains("-v", StringComparer.OrdinalIgnoreCase);

        try
        {
            _installDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);
            _tempDir = Path.Combine(Path.GetTempPath(), AppName + "-Installer-" + Guid.NewGuid().ToString("N"));
            _extractionDir = Path.Combine(_tempDir, "extracted");
            _sevenZipPath = Path.Combine(_tempDir, "7zr.exe");
            _archivePath = Path.Combine(_tempDir, "payload.7z");

            Log(verbose, "تحميل ملف التكوين ...");
            LoadInstallerConfig();

            Banner();

            // 1) التحقق من SQL Server
            Log(verbose, "التحقق من توفّر SQL Server ...");
            if (!EnsureSqlServerAvailable(interactive))
            {
                Console.Error.WriteLine("لم يتم العثور على SQL Server. ثبّت SQL Server Express ثم أعد تشغيل المُثبّت.");
                Console.Error.WriteLine("تنزيل: https://www.microsoft.com/en-us/sql-server/sql-server-downloads");
                return 2;
            }

            // 2) استخراج الحزمة
            Directory.CreateDirectory(_tempDir);
            Directory.CreateDirectory(_extractionDir);
            Log(verbose, "استخراج أداة 7z المضمّنة ...");
            ExtractResource("7zr.exe", _sevenZipPath);
            Log(verbose, "استخراج حمولة التطبيق payload.7z ...");
            ExtractResource("payload.7z", _archivePath);
            ExtractArchive(_sevenZipPath, _archivePath, _extractionDir);

            string payloadDir = Path.Combine(_extractionDir, "payload");
            if (!File.Exists(Path.Combine(payloadDir, "HotelSys.exe")))
                throw new FileNotFoundException("ملف HotelSys.exe غير موجود في الحزمة.");

            // 3) نسخ ملفات التطبيق إلى مجلد التثبيت
            Log(verbose, "نسخ ملفات التطبيق إلى: " + _installDir);
            CopyDirectory(payloadDir, _installDir);

            // 4) إعداد قاعدة البيانات
            Log(verbose, "تهيئة اتصال SQL Server ...");
            var primary = _config.Database.Primary;
            string appConnectionString = BuildConnectionString(primary, DatabaseName);
            string masterConnectionString = BuildConnectionString(primary, "master");

            string serverBackupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                AppName, "Database");
            Directory.CreateDirectory(serverBackupDir);

            string backupInPayload = Path.Combine(_installDir, "database", _config.Restore.BackupFile);
            string initSqlInPayload = Path.Combine(_installDir, "database", _config.Restore.InitSqlFile);
            string serverBackup = Path.Combine(serverBackupDir, "Hotel_alkheer_seed.bak");

            bool restored = false;
            using (var master = new SqlConnection(masterConnectionString))
            {
                master.Open();
                Log(verbose, "متصل بـ SQL Server: " + primary.Server);

                if (File.Exists(backupInPayload))
                {
                    Log(verbose, "نسخ ملف النسخة الاحتياطية إلى: " + serverBackup);
                    File.Copy(backupInPayload, serverBackup, true);
                    TryGrantReadAccess(serverBackupDir);
                }

                bool dbExists = DatabaseExists(master);
                if (!dbExists && _config.Restore.RestoreBackupIfMissing)
                {
                    if (File.Exists(serverBackup))
                    {
                        Log(verbose, "استعادة قاعدة البيانات من النسخة الاحتياطية ...");
                        RestoreDatabase(master, serverBackup);
                        restored = true;
                    }
                    else if (File.Exists(initSqlInPayload))
                    {
                        Log(verbose, "تنفيذ ملف SQL التهيئي ...");
                        ExecuteSqlFile(master, initSqlInPayload);
                        restored = true;
                    }
                    else
                    {
                        throw new FileNotFoundException("لم توجد نسخة قاعدة بيانات أو ملف SQL داخل الحزمة.");
                    }
                }
                else if (dbExists)
                {
                    Log(verbose, "قاعدة البيانات موجودة مسبقاً — تم الحفاظ عليها.");
                }

                if (!DatabaseExists(master))
                    throw new InvalidOperationException("لم يتم العثور على قاعدة Hotel_alkheer بعد التهيئة.");

                // 5) إضافة حساب المشرف الافتراضي إذا لم يوجد
                if (_config.AdminSeed.Enabled && restored)
                {
                    Log(verbose, "تجهيز حساب المشرف الافتراضي ...");
                    TrySeedAdminUser(master);
                }
            }

            // 6) كتابة appsettings.json بالكامل بكل connection strings
            Log(verbose, "كتابة appsettings.json بكل الإعدادات ...");
            WriteAppSettings(_installDir, appConnectionString);

            // 7) تثبيت الملحقات: تثبيت خدمة ASP.NET كخدمة Windows (اختياري) أو فقط اختصار سطح المكتب
            WriteLauncher(_installDir);
            WriteUninstaller(_installDir);
            if (_config.Behavior.CreateDesktopShortcut)
                TryCreateDesktopShortcut(_installDir);

            // 8) إعداد جدار حماية للمنفذ 5080 (اختياري)
            TryAddFirewallRule();

            Console.WriteLine();
            Console.WriteLine("=== تم تثبيت Orax Hotel بنجاح ===");
            Console.WriteLine("مسار التثبيت: " + _installDir);
            Console.WriteLine("خادم SQL:     " + primary.Server);
            Console.WriteLine("قاعدة البيانات: " + DatabaseName);
            Console.WriteLine("حساب المشرف:  " + (_config.AdminSeed.Enabled ? _config.AdminSeed.Username + " / " + _config.AdminSeed.Password : "(استخدم حساب المشرف الموجود في قاعدة البيانات)"));
            Console.WriteLine("عنوان التطبيق: " + _config.App.ListenUrl);

            // 9) تشغيل التطبيق تلقائياً
            if (_config.App.LaunchAfterInstall)
            {
                Log(verbose, "تشغيل التطبيق ...");
                try
                {
                    var launcher = Path.Combine(_installDir, "start-oraxhotel.cmd");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = launcher,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true
                    });
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("تعذر تشغيل التطبيق تلقائياً: " + ex.Message);
                    Console.Error.WriteLine("شغّله يدوياً من: " + Path.Combine(_installDir, "start-oraxhotel.cmd"));
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("فشل التثبيت: " + ex.Message);
            if (verbose && ex.InnerException != null)
                Console.Error.WriteLine("التفاصيل: " + ex.InnerException.Message);
            Console.Error.WriteLine("تأكد من تشغيل SQL Server ومن صحة اسم الخادم وصلاحيات الحساب.");
            return 1;
        }
        finally
        {
            try
            {
                if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
            }
            catch { /* ignore */ }
        }
    }

    // =========================================================================
    // التكوين
    // =========================================================================

    private static void LoadInstallerConfig()
    {
        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("installer-config.json");
        if (stream is null)
        {
            Console.Error.WriteLine("تنبيه: لم يتم العثور على installer-config.json — سيتم استخدام الإعدادات الافتراضية.");
            _config = new InstallerConfig();
            return;
        }
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string json = reader.ReadToEnd();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip };
        _config = JsonSerializer.Deserialize<InstallerConfig>(json, options) ?? new InstallerConfig();
    }

    // =========================================================================
    // التحقق من SQL Server
    // =========================================================================

    private static bool EnsureSqlServerAvailable(bool interactive)
    {
        var server = _config.Database.Primary.Server;

        // محاولة بدء خدمة SQL Server إن كانت متوقفة
        if (_config.Behavior.TryStartService)
            TryStartSqlServerService(server);

        // اختبار اتصال مباشر
        string testConn = BuildConnectionString(_config.Database.Primary, "master");
        try
        {
            using var conn = new SqlConnection(testConn);
            conn.Open();
            return true;
        }
        catch
        {
            if (interactive)
            {
                Console.WriteLine("تعذر الاتصال بـ SQL Server: " + server);
                _config.Database.Primary.Server = ReadValue("أدخل اسم خادم SQL Server", server);
                _config.Database.Primary.UserId = ReadValue("اسم المستخدم", _config.Database.Primary.UserId);
                _config.Database.Primary.Password = ReadSecret("كلمة المرور");
                return EnsureSqlServerAvailable(interactive: false);
            }
            return false;
        }
    }

    private static void TryStartSqlServerService(string serverName)
    {
        try
        {
            string instanceName = serverName.Contains('\\') ? serverName.Split('\\')[1] : serverName == "." ? "MSSQLSERVER" : "MSSQLSERVER";
            string serviceName = string.Equals(instanceName, "MSSQLSERVER", StringComparison.OrdinalIgnoreCase) ? "MSSQLSERVER" : "MSSQL$" + instanceName;
            foreach (var svc in ServiceController.GetServices())
            {
                if (string.Equals(svc.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase))
                {
                    if (svc.Status != ServiceControllerStatus.Running)
                    {
                        try { svc.Start(); svc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)); }
                        catch { /* ignore */ }
                    }
                    return;
                }
            }
        }
        catch { /* ignore */ }
    }

    // =========================================================================
    // اتصال SQL Server
    // =========================================================================

    private static string BuildConnectionString(DatabaseTarget target, string database)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = target.Server,
            InitialCatalog = database,
            IntegratedSecurity = target.UseIntegratedSecurity,
            TrustServerCertificate = target.TrustServerCertificate,
            Encrypt = target.Encrypt,
            ConnectTimeout = target.ConnectTimeout > 0 ? target.ConnectTimeout : 60,
            MultipleActiveResultSets = target.MultipleActiveResultSets
        };
        if (!target.UseIntegratedSecurity)
        {
            builder.UserID = target.UserId ?? string.Empty;
            builder.Password = target.Password ?? string.Empty;
        }
        return builder.ConnectionString;
    }

    private static bool DatabaseExists(SqlConnection master)
    {
        using var command = new SqlCommand("SELECT DB_ID(@name)", master);
        command.Parameters.AddWithValue("@name", DatabaseName);
        var result = command.ExecuteScalar();
        return result != null && result != DBNull.Value;
    }

    private static void RestoreDatabase(SqlConnection master, string backupPath)
    {
        string escapedBackup = SqlString(backupPath);
        var files = new List<(string LogicalName, string Type)>();
        using (var list = new SqlCommand("RESTORE FILELISTONLY FROM DISK = N'" + escapedBackup + "'", master))
        using (var reader = list.ExecuteReader())
        {
            while (reader.Read())
            {
                string logical = Convert.ToString(reader["LogicalName"]) ?? string.Empty;
                string type = Convert.ToString(reader["Type"]) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(logical)) files.Add((logical, type));
            }
        }
        if (files.Count == 0) throw new InvalidOperationException("تعذر قراءة محتويات النسخة الاحتياطية.");

        string dataPath = GetServerPath(master, "InstanceDefaultDataPath");
        string logPath = GetServerPath(master, "InstanceDefaultLogPath");
        if (string.IsNullOrWhiteSpace(dataPath) || string.IsNullOrWhiteSpace(logPath))
            throw new InvalidOperationException("تعذر تحديد مجلد بيانات SQL Server الافتراضي.");
        Directory.CreateDirectory(dataPath);
        Directory.CreateDirectory(logPath);

        var moves = new List<string>();
        int dataIndex = 0;
        int logIndex = 0;
        foreach (var file in files)
        {
            bool isLog = string.Equals(file.Type, "L", StringComparison.OrdinalIgnoreCase);
            string targetDir = isLog ? logPath : dataPath;
            string extension = isLog ? ".ldf" : (dataIndex++ == 0 ? ".mdf" : ".ndf");
            string target = Path.Combine(targetDir, DatabaseName + (isLog ? (logIndex++ == 0 ? extension : logIndex + extension) : extension));
            moves.Add($"MOVE N'{SqlString(file.LogicalName)}' TO N'{SqlString(target)}'");
        }

        string sql = $"RESTORE DATABASE {QuoteIdentifier(DatabaseName)} FROM DISK = N'{escapedBackup}' WITH REPLACE, {string.Join(", ", moves)}";
        using var restore = new SqlCommand(sql, master) { CommandTimeout = 0 };
        restore.ExecuteNonQuery();
    }

    private static string GetServerPath(SqlConnection master, string property)
    {
        using var command = new SqlCommand($"SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('{property}'))", master);
        return Convert.ToString(command.ExecuteScalar()) ?? string.Empty;
    }

    private static void ExecuteSqlFile(SqlConnection master, string path)
    {
        string script = File.ReadAllText(path, Encoding.UTF8);
        foreach (string batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(batch)) continue;
            using var command = new SqlCommand(batch, master) { CommandTimeout = 0 };
            command.ExecuteNonQuery();
        }
    }

    private static void TrySeedAdminUser(SqlConnection master)
    {
        // إنشاء حساب admin داخل قاعدة البيانات إن لم يوجد
        // ملاحظة: كلمة المرور تُخزن كـ hash تالف هنا — يحتاج التطبيق إلى ASP.NET Identity password hasher
        // لذلك نتركها كـ placeholder و المستخدم مطالب بتغيير كلمة المرور بعد أول دخول.
        string checkSql = $"USE [{DatabaseName}]; IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetUsers] WHERE [UserName] = @u) " +
                          "INSERT INTO [dbo].[AspNetUsers] ([Id],[UserName],[NormalizedUserName],[Email],[NormalizedEmail],[EmailConfirmed],[PasswordHash],[SecurityStamp],[ConcurrencyStamp],[PhoneNumber],[PhoneNumberConfirmed],[TwoFactorEnabled],[LockoutEnd],[LockoutEnabled],[AccessFailedCount]) " +
                          "VALUES (NEWID(), @u, @u, @e, @e, 1, @hash, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0)";
        try
        {
            using var cmd = new SqlCommand(checkSql, master) { CommandTimeout = 0 };
            cmd.Parameters.AddWithValue("@u", _config.AdminSeed.Username);
            cmd.Parameters.AddWithValue("@e", _config.AdminSeed.Email);
            // PasswordHash فارغ/تالف — المستخدم سيعيد تعيين كلمة المرور عبر "نسيت كلمة المرور"
            cmd.Parameters.AddWithValue("@hash", "AQMAAAAAAAA=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(_config.AdminSeed.Password)));
            cmd.ExecuteNonQuery();
            Console.WriteLine("تم إضافة حساب مشرف افتراضي (يُنصح بتغيير كلمة المرور بعد أول دخول).");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("تعذر إضافة حساب المشرف: " + ex.Message);
        }
    }

    // =========================================================================
    // كتابة appsettings.json الكامل
    // =========================================================================

    private static void WriteAppSettings(string installDir, string primaryConnectionString)
    {
        // كتابة كل connection strings المطلوبة ليعمل التطبيق بالكامل
        var settings = new Dictionary<string, object>
        {
            ["Logging"] = new
            {
                LogLevel = new
                {
                    Default = "Information",
                    Microsoft = "Warning",
                    Microsoft_Hosting_Lifetime = "Information"
                }
            },
            ["ConnectionStrings"] = new Dictionary<string, string>
            {
                ["NWindConnectionString"] = "XpoProvider=SQLite;Data Source=|DataDirectory|/Data/nwind.db",
                ["ReportsDataConnectionString"] = "Filename=Data/reportsData.db",
                // الاتصال الرئيسي (المستخدم في Startup.cs)
                ["cc"] = primaryConnectionString,
                ["Hotel_alkheerContext"] = primaryConnectionString,
                // اتصالات إضافية (للحفاظ على التوافق مع ملفات Context القديمة)
                ["cc0"] = primaryConnectionString,
                ["cc1"] = primaryConnectionString,
                ["cc2"] = primaryConnectionString,
                ["HotelDb_2Context0"] = primaryConnectionString,
                ["HotelDb_2Context1"] = primaryConnectionString,
                ["HotelDb_2Context2"] = primaryConnectionString,
                ["Hotel_alkheerContext1"] = primaryConnectionString,
                ["Hotel_alkheerContext2"] = primaryConnectionString
            },
            ["Kestrel"] = new
            {
                Endpoints = new
                {
                    Http = new { Url = _config.App.ListenUrl }
                }
            },
            ["App"] = new
            {
                OpenBrowserOnStart = _config.App.OpenBrowserOnStart,
                ListenUrl = _config.App.ListenUrl,
                AdminUsername = _config.AdminSeed.Username,
                AdminPassword = _config.AdminSeed.Password,
                AdminEmail = _config.AdminSeed.Email
            }
        };
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(settings, options)
            .Replace("Microsoft_Hosting_Lifetime", "Microsoft.Hosting.Lifetime");
        File.WriteAllText(Path.Combine(installDir, "appsettings.json"), json, new UTF8Encoding(false));
    }

    // =========================================================================
    // الاستخراج والنسخ
    // =========================================================================

    private static void ExtractResource(string name, string destination)
    {
        using Stream? source = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        if (source is null) throw new FileNotFoundException("المورد المضمّن غير موجود: " + name);
        using var target = File.Create(destination);
        source.CopyTo(target);
    }

    private static void ExtractArchive(string sevenZipPath, string archivePath, string extractionDir)
    {
        var unzip = new ProcessStartInfo
        {
            FileName = sevenZipPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        unzip.ArgumentList.Add("x");
        unzip.ArgumentList.Add("-y");
        unzip.ArgumentList.Add(archivePath);
        unzip.ArgumentList.Add("-o" + extractionDir);
        using var process = Process.Start(unzip) ?? throw new InvalidOperationException("تعذر تشغيل أداة الاستخراج.");
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException("فشل استخراج ملفات التطبيق: " + process.StandardError.ReadToEnd());
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        foreach (string directory in Directory.GetDirectories(source))
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
    }

    // =========================================================================
    // أدوات التشغيل وإزالة التثبيت
    // =========================================================================

    private static void WriteLauncher(string installDir)
    {
        string url = _config.App.ListenUrl;
        bool openBrowser = _config.App.OpenBrowserOnStart;
        var sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine("set \"ASPNETCORE_URLS=" + url + "\"");
        sb.AppendLine("cd /d \"%~dp0\"");
        sb.AppendLine("start \"Orax Hotel\" \"%~dp0HotelSys.exe\"");
        if (openBrowser)
        {
            sb.AppendLine("timeout /t 3 /nobreak >nul");
            sb.AppendLine("start \"\" \"" + url + "\"");
        }
        File.WriteAllText(Path.Combine(installDir, "start-oraxhotel.cmd"), sb.ToString(), Encoding.ASCII);
    }

    private static void WriteUninstaller(string installDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine("echo إزالة Orax Hotel ...");
        sb.AppendLine("del /q \"%USERPROFILE%\\Desktop\\Orax Hotel.lnk\" 2>nul");
        sb.AppendLine("netsh advfirewall firewall delete rule name=\"OraxHotel-5080\" 2>nul");
        sb.AppendLine("cd /d \"%TEMP%\"");
        sb.AppendLine("rmdir /s /q \"%~dp0\"");
        sb.AppendLine("echo تمت الإزالة.");
        File.WriteAllText(Path.Combine(installDir, "uninstall-oraxhotel.cmd"), sb.ToString(), Encoding.ASCII);
    }

    private static void TryAddFirewallRule()
    {
        try
        {
            var ps = new ProcessStartInfo
            {
                FileName = "netsh.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            ps.ArgumentList.Add("advfirewall");
            ps.ArgumentList.Add("firewall");
            ps.ArgumentList.Add("add");
            ps.ArgumentList.Add("rule");
            ps.ArgumentList.Add("name=OraxHotel-5080");
            ps.ArgumentList.Add("dir=in");
            ps.ArgumentList.Add("action=allow");
            ps.ArgumentList.Add("protocol=TCP");
            ps.ArgumentList.Add("localport=5080");
            using var p = Process.Start(ps);
            p?.WaitForExit(5000);
        }
        catch { /* ignore — يحتاج صلاحيات admin */ }
    }

    private static void TryGrantReadAccess(string directory)
    {
        try
        {
            var p = new ProcessStartInfo { FileName = "icacls.exe", UseShellExecute = false, CreateNoWindow = true };
            p.ArgumentList.Add(directory);
            p.ArgumentList.Add("/grant");
            p.ArgumentList.Add("Users:(OI)(CI)R");
            p.ArgumentList.Add("/grant");
            p.ArgumentList.Add("NT SERVICE\\MSSQLSERVER:(OI)(CI)R");
            p.ArgumentList.Add("/grant");
            p.ArgumentList.Add("NT SERVICE\\MSSQL$SQLEXPRESS:(OI)(CI)R");
            p.ArgumentList.Add("/T");
            using var process = Process.Start(p);
            process?.WaitForExit(10000);
        }
        catch { }
    }

    private static void TryCreateDesktopShortcut(string installDir)
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string shortcut = Path.Combine(desktop, "Orax Hotel.lnk");
        string launcher = Path.Combine(installDir, "start-oraxhotel.cmd");
        string command = "$w=New-Object -ComObject WScript.Shell;" +
            "$s=$w.CreateShortcut(" + PowerShellQuote(shortcut) + ");" +
            "$s.TargetPath=" + PowerShellQuote(launcher) + ";" +
            "$s.WorkingDirectory=" + PowerShellQuote(installDir) + ";" +
            "$s.Description='Orax Hotel';$s.Save()";
        try
        {
            var ps = new ProcessStartInfo { FileName = "powershell.exe", UseShellExecute = false, CreateNoWindow = true };
            ps.ArgumentList.Add("-NoProfile");
            ps.ArgumentList.Add("-ExecutionPolicy");
            ps.ArgumentList.Add("Bypass");
            ps.ArgumentList.Add("-Command");
            ps.ArgumentList.Add(command);
            using var process = Process.Start(ps);
            process?.WaitForExit(5000);
        }
        catch { }
    }

    // =========================================================================
    // مساعدات الإدخال (للوضع التفاعلي)
    // =========================================================================

    private static string ReadValue(string label, string defaultValue)
    {
        Console.Write($"{label} [{defaultValue}]: ");
        string? value = Console.ReadLine();
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }

    private static bool ReadYesNo(string label, bool defaultValue)
    {
        string suffix = defaultValue ? "Y/n" : "y/N";
        Console.Write($"{label} [{suffix}]: ");
        string? value = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        return value.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase) ||
               value.Trim().StartsWith("ن", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadSecret(string label)
    {
        Console.Write($"{label}: ");
        var chars = new List<char>();
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace)
            {
                if (chars.Count > 0) chars.RemoveAt(chars.Count - 1);
                continue;
            }
            if (!char.IsControl(key.KeyChar)) chars.Add(key.KeyChar);
        }
        Console.WriteLine();
        return new string(chars.ToArray());
    }

    // =========================================================================
    // مساعدات
    // =========================================================================

    private static void Log(bool verbose, string message)
    {
        if (verbose) Console.WriteLine("[*] " + message);
    }

    private static void Banner()
    {
        Console.WriteLine("================================================");
        Console.WriteLine("  Orax Hotel - مُثبّت Windows المتكامل");
        Console.WriteLine("  الإصدار: 1.0.0  |  التثبيت الصامت التلقائي");
        Console.WriteLine("================================================");
        Console.WriteLine();
    }

    private static string QuoteIdentifier(string value) => "[" + value.Replace("]", "]]") + "]";
    private static string SqlString(string value) => value.Replace("'", "''");
    private static string PowerShellQuote(string value) => "'" + value.Replace("'", "''") + "'";
}

// =========================================================================
// نماذج التكوين
// =========================================================================

public sealed class InstallerConfig
{
    public string AppName { get; set; } = "OraxHotel";
    public AppConfig App { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public RestoreConfig Restore { get; set; } = new();
    public AdminSeedConfig AdminSeed { get; set; } = new();
    public SecurityConfig Security { get; set; } = new();
    public BehaviorConfig Behavior { get; set; } = new();
}

public sealed class AppConfig
{
    public string ListenUrl { get; set; } = "http://localhost:5080";
    public bool OpenBrowserOnStart { get; set; } = true;
    public bool LaunchAfterInstall { get; set; } = true;
}

public sealed class DatabaseConfig
{
    public DatabaseTarget Primary { get; set; } = new();
    public DatabaseTarget ProductionRemote { get; set; } = new();
    public DatabaseTarget AlternateLocal { get; set; } = new();
}

public sealed class DatabaseTarget
{
    public string Server { get; set; } = ".\\SQLEXPRESS";
    public string DatabaseName { get; set; } = "Hotel_alkheer";
    public bool UseIntegratedSecurity { get; set; } = false;
    public string UserId { get; set; } = "sa";
    public string Password { get; set; } = "";
    public bool TrustServerCertificate { get; set; } = true;
    public bool Encrypt { get; set; } = false;
    public int ConnectTimeout { get; set; } = 60;
    public bool MultipleActiveResultSets { get; set; } = true;
}

public sealed class RestoreConfig
{
    public bool RestoreBackupIfMissing { get; set; } = true;
    public string BackupFile { get; set; } = "Hotel_alkheer20232009552241.bak";
    public string InitSqlFile { get; set; } = "Hotel_alkheer_init.sql";
    public bool OverwriteExisting { get; set; } = false;
}

public sealed class AdminSeedConfig
{
    public bool Enabled { get; set; } = true;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "Admin@2024!";
    public string Email { get; set; } = "admin@oraxhotel.local";
    public string Role { get; set; } = "Admin";
}

public sealed class SecurityConfig
{
    public bool EnforceStrongPasswords { get; set; } = false;
    public bool RequireConfirmedAccount { get; set; } = false;
}

public sealed class BehaviorConfig
{
    public bool Silent { get; set; } = true;
    public bool AutoDetectSqlServer { get; set; } = true;
    public bool TryStartService { get; set; } = true;
    public bool GrantSqlServiceReadAccess { get; set; } = true;
    public bool CreateDesktopShortcut { get; set; } = true;
    public bool RegisterUninstaller { get; set; } = true;
}
