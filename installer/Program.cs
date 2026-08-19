using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Data.SqlClient;

internal static class Program
{
    private const string AppName = "OraxHotel";
    private const string DatabaseName = "Hotel_alkheer";
    private const string SqlInstanceName = "SQLEXPRESS";
    private const string DefaultSqlServer = @".\SQLEXPRESS";

    private static int Main()
    {
        string installDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);
        string tempDir = Path.Combine(Path.GetTempPath(), AppName + "-Installer-" + Guid.NewGuid().ToString("N"));
        string extractionDir = Path.Combine(tempDir, "extracted");
        string sevenZipPath = Path.Combine(tempDir, "7zr.exe");
        string archivePath = Path.Combine(tempDir, "payload.7z");

        try
        {
            Console.WriteLine("Orax Hotel - تثبيت النظام وقاعدة البيانات");
            Console.WriteLine("سيتم تثبيت SQL Server Express محلياً عند الحاجة، ثم استعادة حساب المشرف الموجود داخل النسخة الاحتياطية.");
            Console.WriteLine();

            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(extractionDir);
            ExtractResource("7zr.exe", sevenZipPath);
            ExtractResource("payload.7z", archivePath);
            string sqlExpressMediaPath = Path.Combine(tempDir, "SQLEXPR_x64_ENU.exe");
            ExtractResource("SQLEXPR_x64_ENU.exe", sqlExpressMediaPath);
            ExtractArchive(sevenZipPath, archivePath, extractionDir);

            EnsureSqlExpress(sqlExpressMediaPath);
            const string server = DefaultSqlServer;
            const bool integratedSecurity = true;
            string? sqlUser = null;
            string? sqlPassword = null;

            string payloadDir = Path.Combine(extractionDir, "payload");
            if (!File.Exists(Path.Combine(payloadDir, "HotelSys.exe")))
                throw new FileNotFoundException("ملف HotelSys.exe غير موجود في الحزمة.");

            CopyDirectory(payloadDir, installDir);
            string backupInPayload = Path.Combine(installDir, "database", "Hotel_alkheer20232009552241.bak");
            string initSqlInPayload = Path.Combine(installDir, "database", "Hotel_alkheer_init.sql");
            string serverBackupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), AppName, "Database");
            Directory.CreateDirectory(serverBackupDir);

            string appConnectionString;
            string masterConnectionString = BuildConnectionString(server, integratedSecurity, sqlUser, sqlPassword, "master");
            using (var master = new SqlConnection(masterConnectionString))
            {
                master.Open();
                string serverBackup = Path.Combine(serverBackupDir, "Hotel_alkheer_seed.bak");
                if (File.Exists(backupInPayload))
                {
                    File.Copy(backupInPayload, serverBackup, true);
                    TryGrantReadAccess(serverBackupDir);
                }

                bool restored = false;
                if (!DatabaseExists(master))
                {
                    if (File.Exists(serverBackup))
                    {
                        RestoreDatabase(master, serverBackup);
                        restored = true;
                    }
                    else if (File.Exists(initSqlInPayload))
                    {
                        ExecuteSqlFile(master, initSqlInPayload);
                        restored = true;
                    }
                    else
                    {
                        throw new FileNotFoundException("لم توجد نسخة قاعدة بيانات أو ملف SQL داخل الحزمة.");
                    }
                }

                if (!DatabaseExists(master))
                    throw new InvalidOperationException("لم يتم العثور على قاعدة Hotel_alkheer بعد التهيئة.");

                appConnectionString = BuildConnectionString(server, integratedSecurity, sqlUser, sqlPassword, DatabaseName);
                Console.WriteLine(restored
                    ? "تمت تهيئة قاعدة البيانات مع بيانات حساب المشرف الموجودة فيها."
                    : "قاعدة البيانات موجودة مسبقًا؛ تم الحفاظ عليها دون استبدالها.");
            }

            WriteAppSettings(installDir, appConnectionString);
            WriteLauncher(installDir);
            WriteUninstaller(installDir);
            TryCreateDesktopShortcut(installDir);

            Console.WriteLine();
            Console.WriteLine("تم تثبيت Orax Hotel بنجاح في:");
            Console.WriteLine(installDir);
            Console.WriteLine("افتح الاختصار Orax Hotel ثم سجّل الدخول بحساب المشرف الموجود في قاعدة البيانات.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("فشل التثبيت: " + ex.Message);
            Console.Error.WriteLine("تأكد من تشغيل SQL Server ومن صحة اسم الخادم وصلاحيات الحساب.");
            return 1;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
            catch
            {
                // يمكن للنظام حذف الملفات المؤقتة لاحقًا.
            }
        }
    }

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
        return value.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase) || value.Trim().StartsWith("ن", StringComparison.OrdinalIgnoreCase);
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

    private static void EnsureSqlExpress(string mediaPath)
    {
        string masterConnectionString = BuildConnectionString(DefaultSqlServer, true, null, null, "master");

        // إذا كانت النسخة المحلية مثبتة وتعمل، لا نعيد تثبيتها ولا نلمس قواعدها.
        if (CanConnect(masterConnectionString)) return;

        TryStartSqlExpressService();
        if (WaitForConnection(masterConnectionString, TimeSpan.FromSeconds(30))) return;

        if (!File.Exists(mediaPath))
            throw new FileNotFoundException("وسيط SQL Server Express غير موجود داخل الحزمة.", mediaPath);

        Console.WriteLine("جاري تثبيت SQL Server Express محلياً؛ قد يستغرق ذلك عدة دقائق...");
        string currentUser = Environment.UserDomainName + "\\" + Environment.UserName;
        var setup = new ProcessStartInfo
        {
            FileName = mediaPath,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        setup.ArgumentList.Add("/Q");
        setup.ArgumentList.Add("/ACTION=Install");
        setup.ArgumentList.Add("/FEATURES=SQL");
        setup.ArgumentList.Add("/INSTANCENAME=" + SqlInstanceName);
        setup.ArgumentList.Add("/SQLSYSADMINACCOUNTS=" + currentUser);
        setup.ArgumentList.Add("/SQLSVCSTARTUPTYPE=Automatic");
        setup.ArgumentList.Add("/TCPENABLED=1");
        setup.ArgumentList.Add("/IACCEPTSQLSERVERLICENSETERMS");

        using Process process = Process.Start(setup)
            ?? throw new InvalidOperationException("تعذر تشغيل برنامج تثبيت SQL Server Express.");
        process.WaitForExit();
        if (process.ExitCode != 0 && process.ExitCode != 3010)
            throw new InvalidOperationException("فشل تثبيت SQL Server Express. رمز الخروج: " + process.ExitCode);

        if (!WaitForConnection(masterConnectionString, TimeSpan.FromMinutes(5)))
            throw new InvalidOperationException("تم تشغيل تثبيت SQL Server Express، لكن خدمة قاعدة البيانات لم تصبح جاهزة خلال المهلة المحددة.");
    }

    private static bool CanConnect(string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool WaitForConnection(string connectionString, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (CanConnect(connectionString)) return true;
            Thread.Sleep(TimeSpan.FromSeconds(2));
        }
        return CanConnect(connectionString);
    }

    private static void TryStartSqlExpressService()
    {
        try
        {
            var start = new ProcessStartInfo
            {
                FileName = "sc.exe",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            start.ArgumentList.Add("start");
            start.ArgumentList.Add("MSSQL$" + SqlInstanceName);
            using var process = Process.Start(start);
            process?.WaitForExit(15000);
        }
        catch
        {
            // إذا لم تكن الخدمة موجودة فسيتم تثبيتها في الخطوة التالية.
        }
    }

    private static string BuildConnectionString(string server, bool integratedSecurity, string? user, string? password, string database)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            IntegratedSecurity = integratedSecurity,
            TrustServerCertificate = true,
            ConnectTimeout = 60,
            MultipleActiveResultSets = true
        };
        if (!integratedSecurity)
        {
            builder.UserID = user ?? string.Empty;
            builder.Password = password ?? string.Empty;
        }
        return builder.ConnectionString;
    }

    private static bool DatabaseExists(SqlConnection master)
    {
        using var command = new SqlCommand("SELECT DB_ID(@name)", master);
        command.Parameters.AddWithValue("@name", DatabaseName);
        object? result = command.ExecuteScalar();
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
            string target = Path.Combine(targetDir, DatabaseName + (isLog ? logIndex++ == 0 ? extension : logIndex + extension : extension));
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

    private static void WriteAppSettings(string installDir, string connectionString)
    {
        var settings = new
        {
            Logging = new { LogLevel = new { Default = "Information", Microsoft = "Warning", Microsoft_Hosting_Lifetime = "Information" } },
            ConnectionStrings = new Dictionary<string, string>
            {
                ["NWindConnectionString"] = "XpoProvider=SQLite;Data Source=|DataDirectory|/Data/nwind.db",
                ["ReportsDataConnectionString"] = "Filename=Data/reportsData.db",
                ["cc"] = connectionString,
                ["Hotel_alkheerContext"] = connectionString
            }
        };
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(settings, options).Replace("Microsoft_Hosting_Lifetime", "Microsoft.Hosting.Lifetime");
        File.WriteAllText(Path.Combine(installDir, "appsettings.json"), json, new UTF8Encoding(false));
    }

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
        if (process.ExitCode != 0) throw new InvalidOperationException("فشل استخراج ملفات التطبيق: " + process.StandardError.ReadToEnd());
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        foreach (string directory in Directory.GetDirectories(source))
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
    }

    private static void WriteLauncher(string installDir)
    {
        string path = Path.Combine(installDir, "start-oraxhotel.cmd");
        File.WriteAllText(path, "@echo off\r\n" +
            "set \"ASPNETCORE_URLS=http://localhost:5080\"\r\n" +
            "start \"Orax Hotel\" \"%~dp0HotelSys.exe\"\r\n" +
            "timeout /t 3 /nobreak >nul\r\n" +
            "start \"\" \"http://localhost:5080\"\r\n", Encoding.ASCII);
    }

    private static void WriteUninstaller(string installDir)
    {
        string path = Path.Combine(installDir, "uninstall-oraxhotel.cmd");
        File.WriteAllText(path, "@echo off\r\n" +
            "del /q \"%USERPROFILE%\\Desktop\\Orax Hotel.lnk\" 2>nul\r\n" +
            "cd /d \"%TEMP%\"\r\n" +
            "rmdir /s /q \"%~dp0\"\r\n", Encoding.ASCII);
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

    private static string QuoteIdentifier(string value) => "[" + value.Replace("]", "]]" ) + "]";
    private static string SqlString(string value) => value.Replace("'", "''");
    private static string PowerShellQuote(string value) => "'" + value.Replace("'", "''") + "'";
}
