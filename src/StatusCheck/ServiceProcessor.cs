using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.Text;
using StatusCheck.Lib;
using StatusCheck.Lib.Types;
using StatusCheck.Web.Services;

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
            var client = new JsonServiceClient(WebAppHost.BaseUrl);
            Log.Information("Initializing Scripts");
            client.Get(new InitializeScripts());

            var scripts = client.Get<List<Script>>(new Script()); 
            Log.Debug("Found {0} script files",scripts.Count);

            foreach (var script in scripts.Where(x => x.Enabled))
            {
                try
                {
                    Log.Debug("Running {0}.", script.Name);

                    var result = ScriptCsExecutor.ExecuteScript(script.Contents);
                    var checkResult = Utils.ConvertToStatusCheckResult(result, script.Name);
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