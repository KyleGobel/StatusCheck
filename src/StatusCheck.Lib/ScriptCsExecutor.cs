using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ScriptCs;
using ScriptCs.Contracts;
using ScriptCs.Engine.Roslyn;
using ScriptCs.Hosting;
using ServiceStack;
using ServiceStack.Text;
using StatusCheck.Lib.Types;
using LogLevel = ScriptCs.Contracts.LogLevel;

namespace StatusCheck.Lib
{
    public class FakeConsole : IConsole
    {
        StringBuilder sb = new StringBuilder();
        public FakeConsole()
        {
            ForegroundColor = ConsoleColor.Blue;
        }
        public void Write(string value)
        {
        }

        public void WriteLine()
        {
        }

        public void WriteLine(string value)
        {
            sb.Append(value + Environment.NewLine);
        }

        public string ReadLine()
        {
            return "";
        }

        public void Clear()
        {
        }

        public void Exit()
        {
        }

        public void ResetColor()
        {
        }

        public string GetOutput()
        {
            return sb.ToString();
        }
        public ConsoleColor ForegroundColor { get; set; }
    }

    public interface IStatusCheckExecutor
    {
        StatusCheckResult ExecuteScript(Script script, string currentDirectory);

    }

    public class ScriptCsExecutor : IStatusCheckExecutor
    {
        private static ScriptServices GetScriptServices()
        {
            var console = new FakeConsole();
            //var console = new ScriptConsole();
            var config = new LoggerConfigurator(LogLevel.Debug);
            config.Configure(console);

            var logger = config.GetLogger();

            var builder = new ScriptServicesBuilder(console, logger);
            builder.ScriptEngine<RoslynScriptEngine>();
            return builder.Build();
        }

        private static ScriptResult ExecuteScriptCsScript(string scriptContents, string currentDirectory)
        {
            var scriptServices = GetScriptServices();
            scriptServices.FileSystem.CurrentDirectory = currentDirectory;
            var executor = scriptServices.Executor;
            //var resolver = scriptServices.ScriptPackResolver;
            scriptServices.InstallationProvider.Initialize();
            var paths = new List<string>();
            var scriptPacks = new List<IScriptPack>();

            executor.Initialize(paths, scriptPacks);


            var result = executor.ExecuteScript(scriptContents);
            executor.Terminate();

            return result;
        }
        private static StatusCheckResult ConvertToStatusCheckResult(ScriptResult result, string scriptPath)
        {
            if (result.ReturnValue == null)
            {
                if (result.CompileExceptionInfo != null)
                {
                    return new StatusCheckResult
                    {
                        Name = Path.GetFileName(scriptPath),
                        Message = "Compile Exception: {0}".Fmt(result.CompileExceptionInfo.SourceException.Message),
                        Success = false,
                        Timestamp = DateTime.UtcNow,
                        ScriptName = Path.GetFileName(scriptPath)
                    };
                }
                else if (result.ExecuteExceptionInfo != null)
                {
                    return new StatusCheckResult
                    {
                        Name = result.ExecuteExceptionInfo.SourceException.Source,
                        Message = "Execution Exception: {0}".Fmt(result.ExecuteExceptionInfo.SourceException.Message),
                        Success = false,
                        Timestamp = DateTime.UtcNow,
                        ScriptName = Path.GetFileName(scriptPath)
                    };
                }
                return new StatusCheckResult
                {
                    Name = "No name found",
                    Message = "'{0}' Script contained no return value".Fmt(Path.GetFileName(scriptPath)),
                    Success = false,
                    Timestamp = DateTime.UtcNow,
                    ScriptName = Path.GetFileName(scriptPath)
                };
            }

            var scResult = new StatusCheckResult { ScriptName = Path.GetFileName(scriptPath) };

            try
            {
                scResult.Success = (bool)result.ReturnValue
                    .GetType()
                    .GetProperty("Success")
                    .GetValue(result.ReturnValue);
            }
            catch
            {
                scResult.Success = false;
            }

            try
            {
                scResult.Message = (string)result.ReturnValue
                    .GetType()
                    .GetProperty("Message")
                    .GetValue(result.ReturnValue);
            }
            catch
            {
                scResult.Message = String.Empty;
            }

            try
            {
                scResult.Name = (string)result.ReturnValue
                    .GetType()
                    .GetProperty("Name")
                    .GetValue(result.ReturnValue);
            }
            catch
            {
                scResult.Name = "Unnamed Status Check";
            }
            scResult.Timestamp = DateTime.UtcNow;
            return scResult;
        }

        public StatusCheckResult ExecuteScript(Script script, string currentDirectory)
        {
            var scriptResult = ExecuteScriptCsScript(script.Contents, currentDirectory);
            return ConvertToStatusCheckResult(scriptResult, script.Name);
        }
    }
}