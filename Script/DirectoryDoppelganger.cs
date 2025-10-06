using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

public class DirectoryDoppelganger
{
    enum EArgsOptions
    {
        UpdateTime,
        WatchedDir,
        CopyToDir,
        LogDir,
        ByteByByte

    }

    // OPTIONS
    float m_fUpdateDelay = 10f;
    string m_sWatchedDir = "";
    string m_sCopyToDir = "";
    string m_sLogDir = "";
    bool m_bUseByteByByte = false;

    // "readonly"
    string[] ValidUpdateDelayArgs = { "-update", "-updatedelay", "-updatetime", "-time", "-delay", "-t", "-d" };
    string[] ValidWatchedDirArgs = { "-watched", "-watcheddir", "-watcheddirectory", "-wd" };
    string[] ValidCopyToDirArgs = { "-copy", "-copydir", "-copydirectory", "-copyto", "-copytodir", "-copytodirectory", "-cd", "-ctd" };
    string[] ValidLogDirArgs = { "-log", "-logdir", "-logdirectory", "-ld" };
    string[] ValidUseByteByByteArgs = { "-bytebybyte", "-bbb" };

    // dynamic
    string[] m_Args;
    bool m_bProduceLogs = true;
    int m_iUpdateDelayMS;
    FileInfo? m_LogFileInfo;
    FileStream? m_LogFileStream;
    StringBuilder m_StringBuilder;

    // PUBLIC
    public DirectoryDoppelganger(string[] args)
    {
        m_Args = args;
        m_StringBuilder = new StringBuilder();

        Console.WriteLine("DirectoryDoppelganger booting up...");

        LoadArgs();
        if (!ValidateOptions())
            return;

        InitLog();
    }

    /// <summary>
    /// Starts infinite loop that calls it's update in intervals set via Command Line Args. 
    /// Only needs to be called once as it never exits.
    /// </summary>
    public void UpdatePeriodic()
    {
        Console.WriteLine("DirectoryDoppelganger Periodic Update Started!");

        while (true)
        {
            Update();
            Thread.Sleep(m_iUpdateDelayMS);
        }
    }

    /// <summary>
    /// Use when functionality of DirectoryDoppelganger is utilized as API.
    /// Updates the state of Copy-To folder once.
    /// </summary>
    public void UpdateOnce()
    {
        Update();
    }

    // PRIVATE
    // INITIALIZATION ---------------------------------------------------------------------------------------
    void LoadArgs()
    {
        for (int i = 0; i < m_Args.Length; i++)
        {
            string arg = m_Args[i];
            if (arg[0] == '-')
            {
                // Filter out negative numbers
                if (float.TryParse(arg[1].ToString(), out float f))
                    continue;

                // Search among valid args and execute if found
                string lcArg = arg.ToLower();
                if (ValidUpdateDelayArgs.Contains(lcArg))
                {
                    i += ArgHandler(EArgsOptions.UpdateTime, i);
                    continue;
                }
                if (ValidWatchedDirArgs.Contains(lcArg))
                {
                    i += ArgHandler(EArgsOptions.WatchedDir, i);
                    continue;
                }
                if (ValidCopyToDirArgs.Contains(lcArg))
                {
                    i += ArgHandler(EArgsOptions.CopyToDir, i);
                    continue;
                }
                if (ValidLogDirArgs.Contains(lcArg))
                {
                    i += ArgHandler(EArgsOptions.LogDir, i);
                    continue;
                }
                if (ValidUseByteByByteArgs.Contains(lcArg))
                {
                    i += ArgHandler(EArgsOptions.ByteByByte, i);
                    continue;
                }

                Console.WriteLine(arg + " is not valid command line argument!");
            }
        }
    }

    // Functionality within could be spread into multiple methods or moved into LoadArgs(),
    // but I found this way most straightforward to expand if needed while still easy to read
    /// <returns>How many following arguments to skip</returns>
    int ArgHandler(EArgsOptions option, int argID)
    {
        string valueArg;
        if(argID + 1 < m_Args.Length)
        {
            valueArg = m_Args[argID+1];
        }
        else
        {
            valueArg = "";
        }

        switch (option)
        {
            case EArgsOptions.UpdateTime:
                if (float.TryParse(valueArg, out float t))
                {
                    if (t > 0)
                    {
                        m_fUpdateDelay = t;
                        return 1;
                    }
                    Console.WriteLine("Invalid number input for time delay of " + t.ToString() + " ignored, using default value of " + m_fUpdateDelay.ToString() + " seconds instead. Use positive number if you want customised behaviour.");
                    return 0;

                }
                Console.WriteLine(valueArg + " is NOT valid value for Update Time, use number instead");
                return 0;


            case EArgsOptions.WatchedDir:
                if (Directory.Exists(valueArg))
                {
                    m_sWatchedDir = valueArg;
                    return 1;
                }
                Console.WriteLine("Desired directory to watch " + valueArg + " does not exist!");
                return 0;


            case EArgsOptions.CopyToDir:
                if (Directory.Exists(valueArg))
                {
                    m_sCopyToDir = valueArg;
                    return 1;
                }
                DirectoryInfo infoCD = Directory.CreateDirectory(valueArg);
                if (infoCD.Exists)
                {
                    Console.WriteLine("Created new directory for copying contents of watched folder at: " + infoCD.FullName);
                    m_sCopyToDir = infoCD.FullName;
                    return 1;
                }
                Console.WriteLine("FAILED to create new directory for copying contents of watched folder at: " + valueArg);
                return 0;


            case EArgsOptions.LogDir:
                if (Directory.Exists(valueArg))
                {
                    m_sLogDir = valueArg;
                    return 1;
                }
                DirectoryInfo infoLog = Directory.CreateDirectory(valueArg);
                if (infoLog.Exists)
                {
                    Console.WriteLine("Created new directory for storing logs at: " + infoLog.FullName);
                    m_sLogDir = infoLog.FullName;
                    return 1;
                }
                Console.WriteLine("FAILED to create new directory for storing logs at: " + valueArg);
                return 0;


            case EArgsOptions.ByteByByte:
                Console.WriteLine("Using hashes for file comparisons");
                m_bUseByteByByte = true;
                return 0;


            default:
                Console.WriteLine("ERROR - New arg option was created, but not implemented in ArgHandler()!!!");
                return 0;
        }
    }

    // Called after options are loaded in
    // Checks if values are valid and if not, use default values where applicable or send signal to shut down
    bool ValidateOptions()
    {
        Console.WriteLine("Validating options...");

        m_iUpdateDelayMS = (int)Math.Round(m_fUpdateDelay * 1000);
        Console.WriteLine("Update Delay: " + m_fUpdateDelay.ToString() + "s");

        if (m_sWatchedDir == "")
        {
            Console.WriteLine("No Watched Directory set, aborting...");
            return false;
        }
        m_sWatchedDir = Path.GetFullPath(m_sWatchedDir);
        Console.WriteLine("Watched Directory: " + m_sWatchedDir);

        if (m_sCopyToDir == "")
        {
            Console.WriteLine("No Copy-To Directory, aborting...");
            return false;
        }
        m_sCopyToDir = Path.GetFullPath(m_sCopyToDir);
        if(m_sCopyToDir == m_sWatchedDir)
        {
            Console.WriteLine("Copy-To Directory is same as Watched Directory, that is not allowed, aborting...");
            return false;
        }
        Console.WriteLine("Copy-To Directory: " + m_sCopyToDir);

        if (m_sLogDir == "")
        {
            Console.WriteLine("No Log Storage Directory set, no log files will be created.");
            m_bProduceLogs = false;
        }
        else
        {
            m_sLogDir = Path.GetFullPath(m_sLogDir);
            Console.WriteLine("Logs stored to: " + m_sLogDir);
        }

        Console.WriteLine("Options validated...");

        return true;
    }

    // UPDATE ---------------------------------------------------------------------------------------
    void Update()
    {
        if (m_bProduceLogs)
            m_LogFileStream = m_LogFileInfo.Open(FileMode.Append, FileAccess.Write, FileShare.Read);

        DirectoryPass("");

        if (m_bProduceLogs)
            m_LogFileStream.Dispose();
    }

    // The core recursive method that checks contents of directories and calls self for all applicable subdirectories
    void DirectoryPass(string relativePath)
    {
        relativePath += "\\";

        // FILES ----------------------------------------
        string thisPath = m_sWatchedDir + relativePath;
        string copyPath = m_sCopyToDir + relativePath;

        string[] thisFiles = Directory.GetFiles(thisPath);
        string[] copyFiles = Directory.GetFiles(copyPath);
        string[] thisFilesNames = new string[thisFiles.Length];        
        string[] copyFilesNames = new string[copyFiles.Length];
        for (int i = 0; i < thisFilesNames.Length; i++)
        {
            thisFilesNames[i] = Path.GetFileName(thisFiles[i]);
        }
        for (int i = 0; i < copyFilesNames.Length; i++)
        {
            copyFilesNames[i] = Path.GetFileName(copyFiles[i]);
        }

        // Check files in copy whether they exist in watched
        //    - If not, delete
        for (int i = 0; i < copyFiles.Length; i++)
        {
            if (thisFilesNames.Contains(copyFilesNames[i]))
                continue;

            Log("Deleting file: " + copyFiles[i]);
            File.Delete(copyFiles[i]);
        }

        // Check if files witin watched exist in copy
        //    - If not, copy
        //    - Else check if the file is same
        for (int i = 0; i < thisFiles.Length; i++)
        {
            string fileCopyPath = copyPath + thisFilesNames[i];
            if (!copyFilesNames.Contains(thisFilesNames[i]))
            {                
                File.Copy(thisFiles[i], fileCopyPath);
                Log("Copied file at " + fileCopyPath);
            }
            else
            {
                if (CompareIfFilesAreSame(thisFiles[i], fileCopyPath))
                    continue;

                // File content is not the same and so we delete copy and create new one from the original
                File.Delete(fileCopyPath);
                File.Copy(thisFiles[i], fileCopyPath);
                Log("Recopied file as changes were made to it at " + fileCopyPath);                               
            }
        }

        // SUBDIRECTORIES ----------------------------------------
        string[] thisSubdirs = Directory.GetDirectories(thisPath);
        string[] copySubdirs = Directory.GetDirectories(copyPath);
        string[] thisSubdirsNames = new string[thisSubdirs.Length];
        string[] copySubdirsNames = new string[copySubdirs.Length];
        for (int i = 0; i < thisSubdirsNames.Length; i++)
        {
            thisSubdirsNames[i] = Path.GetFileName(thisSubdirs[i]);
        }
        for (int i = 0; i < copySubdirsNames.Length; i++)
        {
            copySubdirsNames[i] = Path.GetFileName(copySubdirs[i]);
        }

        // Check whether subdirectories in copy exist within main
        //    - If not, delete
        for (int i = 0; i < copySubdirsNames.Length; i++)
        {
            if (thisSubdirsNames.Contains(copySubdirsNames[i]))
                continue;

            Log("Deleting directory including it's contents: " + copySubdirs[i]);
            Directory.Delete(copySubdirs[i], true);
        }

        // Check whether subdirectories exist in copy
        //    - If not, create new
        for (int i = 0; i < thisSubdirs.Length; i++)
        {
            if (!copySubdirsNames.Contains(thisSubdirsNames[i]))
            {
                string dirCopyPath = copyPath + thisSubdirsNames[i];
                Directory.CreateDirectory(dirCopyPath);
                Log("Copied directory at " + dirCopyPath);
            }
        }

        // RECURSION --------------------------------------------
        foreach (var subdir in thisSubdirsNames)
        {
            DirectoryPass(relativePath + subdir);
        }
    }

    bool CompareIfFilesAreSame(string pathA, string pathB)
    {
        FileInfo infoA = new FileInfo(pathA);
        FileInfo infoB = new FileInfo(pathB);

        // Check if they are same size
        //  - If not, return false
        if (infoA.Length != infoB.Length)
            return false;

        if (m_bUseByteByByte)
            return ByteByByteComparison(infoA, infoB);        
        else
            return HashesComparison(infoA, infoB);
    }

    bool ByteByByteComparison(FileInfo fileA, FileInfo fileB)
    {
        // FileStreams needed for entire operation
        using FileStream fsA = fileA.OpenRead();
        using FileStream fsB = fileB.OpenRead();        

        for (int i = 0; i < fileA.Length; i++)
        {
            if (fsA.ReadByte() != fsB.ReadByte())                
                return false;
        }

        return true;
    }

    bool HashesComparison(FileInfo fileA, FileInfo fileB)
    {
        byte[] hashA;
        byte[] hashB;

        // FileStreams only needed for creation of hashes
        using FileStream fsA = fileA.OpenRead();
        {
            using FileStream fsB = fileB.OpenRead();
            {
                hashA = MD5.Create().ComputeHash(fsA);
                hashB = MD5.Create().ComputeHash(fsB);
            }
        }

        for (int i = 0; i < hashA.Length; i++)
        {
            if (hashA[i] != hashB[i])            
                return false;
        }

        return true;
    }


    // LOGGING SYSTEM ---------------------------------------------------------------------------------------
    // Needs m_LogFileStream to be opened for write at start of update and close it at end of it,
    // alternativaly keeping the FileStream open for as long application is running can be utilized if use case of DirectoryDoppelganger is high frequency monitoring
    void InitLog()
    {
        if (!m_bProduceLogs)
            return;

        // Produces name looking like so: Logs_251003_055348.log
        string LogName = m_sLogDir + "\\Logs_" + DateTime.Now.ToString("yyMMdd_hhmmss") + ".log";

        // "Accidental" multilaunch safeguard
        if(File.Exists(LogName))
            File.Delete(LogName);
        
        m_LogFileStream = File.Create(LogName);
        m_LogFileInfo = new FileInfo(LogName);

        Console.WriteLine("Log file created at " + LogName);

        Log("DirectoryDoppelganger launched with following options:");
        Log("Update Delay: " + (m_iUpdateDelayMS/1000f).ToString()+ " seconds");
        Log("Watched Directory: " + m_sWatchedDir);
        Log("Copy-To Directory: " + m_sCopyToDir);
        Log("Logs Directory: " + m_sLogDir);
        if (m_bUseByteByByte)
            Log("Comparison Mode: Byte By Byte");
        else            
            Log("Comparison Mode: MD5 Hashes");

        m_LogFileStream.Dispose();
    }

    void Log(string message, bool newLine = true)
    {
        // Utilizing StringBuilder to take load off C# Garbage Manager, ideally utilized in all string conjunction, but I ommited that for readibility
        m_StringBuilder.Append(DateTime.Now.ToString("T"));
        m_StringBuilder.Append(" - ");
        m_StringBuilder.Append(message);

        Console.WriteLine(m_StringBuilder.ToString());

        if (!m_bProduceLogs)
        {
            m_StringBuilder.Clear();
            return;
        }
            
        if (newLine)
            m_StringBuilder.Append('\n');
        
        m_LogFileStream.Write(new UTF8Encoding().GetBytes(m_StringBuilder.ToString()));
        m_StringBuilder.Clear();
    }
}

