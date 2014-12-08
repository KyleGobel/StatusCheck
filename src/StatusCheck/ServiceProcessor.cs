using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.Text;
using StatusCheck.Lib;
using StatusCheck.Lib.Types;

namespace StatusCheck
{
    public class ServiceProcessor
    {
        public TimeSpan TimeBetweenLoops { get { return TimeSpan.FromSeconds(ServiceStackHost.Instance.AppSettings.Get("TimeBetweenLoops", 45)); } }
        protected Task T;
        protected CancellationTokenSource Ts;
        protected CancellationToken CancellationToken;

        public void ProcessLoop()
        {
            var scripts = Utils.GetScriptFiles();

            Log.Debug("Found {0} script files",scripts.Count);

            foreach (var script in scripts)
            {
                try
                {
                    Log.Debug("Running {0}.", Path.GetFileName(script));

                    var contents = File.ReadAllText(script);
                    var result = ScriptCsExecutor.ExecuteScript(contents);
                    var checkResult = Utils.ConvertToStatusCheckResult(result, script);
                    Log.Information("Status Check Result = {Result}", checkResult.Dump());

                    if (!checkResult.Success)
                    {
                        var notifiers = Utils.GetInstancesOfImplementingTypes<INotifier>();
                        foreach (var n in notifiers)
                        {
                            n.Notify(checkResult);
                        }
                    }

                    var historyLog = new HistoryLog
                    {
                        ScriptFile = checkResult.ScriptName,
                        Success = checkResult.Success,
                        Timestamp = checkResult.Timestamp,
                        ResultData = checkResult.ToJson()
                    };

                    var historyLogUrl = (WebAppHost.BaseUrl + historyLog.ToPostUrl());
                    historyLogUrl.PostJsonToUrl(historyLog);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error running script {Script}", script);
                }
            }



        }

        public ServiceProcessor()
        {
            Ts = new CancellationTokenSource();
            CancellationToken = Ts.Token;

            T = new Task(() =>
            {
                for (; ; )
                {
                    Log.Debug("Starting process loop");
                    try
                    {
                        ProcessLoop();
                    }
                    catch (Exception x)
                    {
                        Log.Fatal("Fatal Error in process loop, exiting", x);
                        Environment.Exit(-1);
                    }
                    Log.Information("Finished process loop, waiting {0}", TimeBetweenLoops.ToString("g"));
                    Thread.Sleep(TimeBetweenLoops);
                    if (CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            });
        }

        public virtual void Start() { T.Start(); }
        public virtual void Stop() { Ts.Cancel(); }
    }
}