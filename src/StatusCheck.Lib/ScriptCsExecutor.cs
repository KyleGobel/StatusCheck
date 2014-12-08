using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using ScriptCs;
using ScriptCs.Contracts;
using ScriptCs.Engine.Roslyn;
using ScriptCs.Hosting;

namespace StatusCheck.Lib
{
    public class FakeConsole : IConsole
    {
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

        public ConsoleColor ForegroundColor { get; set; }
    }
    public class ScriptCsExecutor
    {
        private static ScriptServices GetScriptServices()
        {
            var console = new FakeConsole();
            var config = new LoggerConfigurator(LogLevel.Error);
            config.Configure(console);

            var logger = config.GetLogger();

            var builder = new ScriptServicesBuilder(console, logger);

            builder.ScriptEngine<RoslynScriptEngine>();
            return builder.Build();
        }

        public static ScriptResult ExecuteScript(string scriptContents)
        {
            var scriptServices = GetScriptServices();
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

    }
}