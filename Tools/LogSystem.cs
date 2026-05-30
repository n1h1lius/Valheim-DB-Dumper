namespace ValheimDBDumper.Tools;

using System;
using System.Diagnostics;
using System.IO;
using BepInEx.Logging;

public class LogSystem
{
    private static int VERBOSE_LEVEL = 3;
    private static bool OUTPUT_LOG = true;
    private static bool OUTPUT_PRINT = true;
    private static bool DEBUG_MODE = false;
    private static bool COLOUR = true;
    private static string OUTPUT_LOG_FILE = "LogSystem.txt";
    private static string OUTPUT_PRINT_FILE = "LogSystem.txt";

    // COLORS
    private static string RESET = "";
    private static string C_TIME = "";
    private static string C_FILE = "";
    private static string C_METHOD = "";
    private static string C_LINE = "";
    private static string C_INFO = "";
    private static string C_WARN = "";
    private static string C_ERROR = "";
    private static string C_SUCCESS = "";
    private static string C_TEXT = "";
    private static string C_PROMPT = "";
    private static string C_SEPARATOR = "";

    // VALHEIM MODS SECTION
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("Valheim-DB-Dumper");

    // ╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
    // ║                                                           INIT METHODS                                                          ║
    // ╚═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

    public static void Init()
    {
        SetColors();

        VERBOSE_LEVEL = 100;
        OUTPUT_LOG = true;
        OUTPUT_PRINT = true;
        DEBUG_MODE = true;
        COLOUR = true;
        OUTPUT_LOG_FILE = "LogSystem.txt";
        OUTPUT_PRINT_FILE = "LogSystem.txt";
    }

    public static void SetVerboseLevel(int level) => VERBOSE_LEVEL = level;
    public static void SetDebugMode(bool mode) => DEBUG_MODE = mode;
    public static void SetOutput(bool log, bool print) { OUTPUT_LOG = log; OUTPUT_PRINT = print; }
    public static void SetOutputFile(string log, string print) { OUTPUT_LOG_FILE = log; OUTPUT_PRINT_FILE = print; }
    public static void SetColour(bool colour) => COLOUR = colour;

    public static void TestLog()
    {
        Log("Testing No Colour Log", false);
        Log("Testing Standard Log");
        Warn("Testing Standard Warning");
        Error("Testing Standard Error");
        Success("Testing Standard Success");
        Info("Testing Standard Info");
    }

    // ╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
    // ║                                                           MAIN METHODS                                                          ║
    // ╚═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

    public static void Print(string msg)
    {
        Logger.LogMessage(msg);
        if (OUTPUT_PRINT) OutputFile("print", msg);
    }

    private static string InternalLog(string msg)
    {
        SetColors();

        string[] info = GetCallerInfo();

        string stackTraceLine = $"{C_FILE}{{ {info[0]} }}{C_SEPARATOR} | {C_METHOD}< {info[1]} >{C_SEPARATOR} | {C_LINE}( {info[2]} )";
        string outputStackTraceLine = $"{{ {info[0]} }} | < {info[1]} >";

        string finalString = $"{C_TIME} {GetTime()}{C_SEPARATOR} | {stackTraceLine}{C_SEPARATOR} | {C_PROMPT}->> {C_TEXT}{msg}{RESET}";
        string finalOutput = $" {GetTime()} | {outputStackTraceLine} | ->> {msg}";

        OutputFile("log", finalOutput);

        return finalString;
    }

    private static string InternalAlert(string msg, string bg)
    {

        SetColors();

        string[] info = GetCallerInfo();

        string stackTraceLine = $"{{ {info[0]} }} | < {info[1]} >";

        string header = "";

        if (bg == C_INFO) header = "[ INFO ]";
        if (bg == C_WARN) header = "[ WARNING ]";
        if (bg == C_ERROR) header = "[ ERROR ]";
        if (bg == C_SUCCESS) header = "[ SUCCESS ]";

        string finalString = C_TEXT + bg + $"{header} - {GetTime()} | {stackTraceLine} | ->> {msg}" + RESET;
        string outputString = $" {header} - {GetTime()} | {stackTraceLine} | ->> {msg}";

        OutputFile("log", outputString);

        return finalString;
    }

    // ╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
    // ║                                                        INTERNAL METHODS                                                         ║
    // ╚═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

    private static void SetColors()
    {
        RESET = COLOUR ? Color.RESET : "";
        C_TIME = COLOUR ? Color.CYAN : "";
        C_FILE = COLOUR ? Color.RED_BRIGHT : "";
        C_METHOD = COLOUR ? Color.BRIGHT_GREEN : "";
        C_LINE = COLOUR ? Color.YELLOW_BRIGHT : "";
        C_INFO = COLOUR ? Color.BG_CYAN : "";
        C_WARN = COLOUR ? Color.BG_ORANGE : "";
        C_ERROR = COLOUR ? Color.BG_RED : "";
        C_SUCCESS = COLOUR ? Color.BG_GREEN : "";
        C_TEXT = COLOUR ? Color.WHITE_BRIGHT : "";
        C_PROMPT = COLOUR ? Color.BRIGHT_ORANGE : "";
        C_SEPARATOR = COLOUR ? Color.BRIGHT_PINK : "";
    }

    private static string[] GetCallerInfo()
    {
        var stackTrace = new StackTrace(true);

        string logClassName = typeof(LogSystem).FullName ?? "LogSystem";

        for (int i = 0; i < stackTrace.FrameCount; i++)
        {
            StackFrame? frame = stackTrace.GetFrame(i);
            if (frame == null) continue;

            var method = frame.GetMethod();
            if (method == null) continue;

            string? typeName = method.DeclaringType?.FullName;
            if (string.IsNullOrEmpty(typeName)) continue;

            if (typeName.Contains(logClassName) || 
                typeName.StartsWith("System.") || 
                typeName.StartsWith("UnityEngine.") || 
                typeName.StartsWith("BepInEx.") ||
                typeName.StartsWith("Mono.") ||
                method.Name.StartsWith("<") ||         
                method.Name.Contains("Awake") && typeName.Contains("LogSystem"))
                continue;

            string fileName = frame.GetFileName() ?? "Unknown";
            string methodName = method.Name;
            int lineNumber = frame.GetFileLineNumber();

            if (lineNumber == 0)
            {
                lineNumber = frame.GetILOffset();          
                if (lineNumber == 0)
                    lineNumber = frame.GetNativeOffset(); 
            }

            if (fileName == "Unknown" && method.DeclaringType != null)
            {
                fileName = method.DeclaringType.Name + ".cs";
            }

            if (lineNumber == 0)
                lineNumber = -1;

            return new string[] { fileName, methodName, lineNumber.ToString() };
        }

        return new string[] { "Unknown", "Unknown", "0" };
    }

    private static string GetTime()
    {
        return $"[ {DateTime.Now:HH:mm:ss} ]";
    }

    private static void HandleFolder(string targetPath)
    {
        if (!Directory.Exists(targetPath))
        {
            try
            {
                Directory.CreateDirectory(targetPath);
            }
            catch (Exception e)
            {
                Error("COULD NOT CREATE FOLDER: " + e.Message);
            }
        }
    }

    private static void OutputFile(string mode, string msg)
    {
        string outputPath = "";
        string outputFileName = "";
        bool flag = false;

        switch (mode)
        {
            case "log":
                outputPath = "OUTPUT/LOG/";
                outputFileName = OUTPUT_LOG_FILE;
                flag = OUTPUT_LOG;
                break;
            case "print":
                outputPath = "OUTPUT/PRINT/";
                outputFileName = OUTPUT_PRINT_FILE;
                flag = OUTPUT_PRINT;
                break;
        }

        HandleFolder(outputPath);

        if (!flag) return;

        string fullPath = Path.Combine(outputPath, outputFileName);

        try
        {
            File.AppendAllText(fullPath, msg + Environment.NewLine);
        }
        catch (Exception e)
        {
            OUTPUT_LOG = false;
            OUTPUT_PRINT = false;
            Error("FILE COULD NOT BE WRITTEN: " + e.Message);
            OUTPUT_LOG = true;
            OUTPUT_PRINT = true;
        }
    }

    // ╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
    // ║                                                       OVERLOAD METHODS                                                          ║
    // ╚═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

    // ───────── LOG ─────────
    public static void Log(int level, string msg) { COLOUR = true; if (level <= VERBOSE_LEVEL) Logger.LogMessage(InternalLog(msg)); }
    public static void Log(int level, string msg, bool colour) { COLOUR = colour; if (level <= VERBOSE_LEVEL) Logger.LogMessage(InternalLog(msg)); }
    public static void Log(string msg, bool colour) { COLOUR = colour; Logger.LogMessage(InternalLog(msg)); }
    public static void Log(string msg) { COLOUR = true; Logger.LogMessage(InternalLog(msg)); }

    // ───────── WARNING ─────────
    public static void Warn(int level, string msg) { COLOUR = true; if (level <= VERBOSE_LEVEL) Logger.LogMessage(InternalAlert(msg, C_WARN)); }
    public static void Warn(int level, string msg, bool colour) { COLOUR = colour; if (level <= VERBOSE_LEVEL) Logger.LogMessage(InternalAlert(msg, C_WARN)); }
    public static void Warn(string msg, bool colour) { COLOUR = colour; Logger.LogMessage(InternalAlert(msg, C_WARN)); }
    public static void Warn(string msg) { COLOUR = true; Logger.LogMessage(InternalAlert(msg, C_WARN)); }

    // ───────── ERROR ─────────
    public static void Error(int level, string msg) { COLOUR = true; if (level <= VERBOSE_LEVEL) Logger.LogMessage(InternalAlert(msg, C_ERROR)); }
    public static void Error(int level, string msg, bool colour) { COLOUR = colour; if (level <= VERBOSE_LEVEL) Logger.LogMessage(InternalAlert(msg, C_ERROR)); }
    public static void Error(string msg, bool colour) { COLOUR = colour; Logger.LogMessage(InternalAlert(msg, C_ERROR)); }
    public static void Error(string msg) { COLOUR = true; Logger.LogMessage(InternalAlert(msg, C_ERROR)); }

    // ───────── SUCCESS ─────────
    public static void Success(int level, string msg) { COLOUR = true; if (level <= VERBOSE_LEVEL) Logger.LogMessage(InternalAlert(msg, C_SUCCESS)); }
    public static void Success(int level, string msg, bool colour) { COLOUR = colour; if (level <= VERBOSE_LEVEL) Logger.LogMessage(InternalAlert(msg, C_SUCCESS)); }
    public static void Success(string msg, bool colour) { COLOUR = colour; Logger.LogMessage(InternalAlert(msg, C_SUCCESS)); }
    public static void Success(string msg) { COLOUR = true; Logger.LogMessage(InternalAlert(msg, C_SUCCESS)); }

    // ───────── INFO ─────────
    public static void Info(int level, string msg) { COLOUR = true; if (level <= VERBOSE_LEVEL) Logger.LogMessage(InternalAlert(msg, C_INFO)); }
    public static void Info(int level, string msg, bool colour) { COLOUR = colour; if (level <= VERBOSE_LEVEL) Logger.LogMessage(InternalAlert(msg, C_INFO)); }
    public static void Info(string msg, bool colour) { COLOUR = colour; Logger.LogMessage(InternalAlert(msg, C_INFO)); }
    public static void Info(string msg) { COLOUR = true; Logger.LogMessage(InternalAlert(msg, C_INFO)); }
}
