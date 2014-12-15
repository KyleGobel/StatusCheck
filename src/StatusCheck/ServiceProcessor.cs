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
using StatusCheck.Services;

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

            var scripts = client.Get<List<Script>>(new AllScripts()); 
            Log.Debug("Found {0} script files",scripts.Count);

            foreach (var script in scripts.Where(x => x.Enabled))
            {
                try
                {
                    Log.Verbose("Checking script {Script} for run", script.Name);
                    var lastRun = client.Get<LastRunResponse>(script.ConvertTo<LastRun>());
                    if (lastRun != null)
                    {
                        Log.Verbose("{Script} last run at {LastRun}", script.Name, lastRun.Timestamp);
                        if (script.SecondsBetweenChecks > 0)
                        {
                            var tspan = TimeSpan.FromSeconds(script.SecondsBetweenChecks);
                            if ((lastRun.Timestamp + tspan) <= DateTime.UtcNow)
                            {
                                Log.Debug("{Script} due for run {Timespan} ago", script.Name, DateTime.UtcNow - (lastRun.Timestamp + tspan));
                            }
                            else
                            {
                                Log.Debug("{Script} not ready for {Timespan}", script.Name, (tspan - (DateTime.UtcNow - lastRun.Timestamp)));
                                continue;
                            }
                        }
                        else
                        {
                            Log.Verbose("{Script} set to 0 wait time, running", script.Name);
                        }
                        
                    }
                    else
                    {
                        Log.Debug("{Script} never run.  Running {0}.", script.Name);
                    }

                    var checkResult = client.Get<StatusCheckResult>(script.ConvertTo<RunScript>());
                    Log.Information("Status Check Result = {Result}", checkResult.Dump());

                    if (!checkResult.Success)
                    {
                        var notifiers = Utils.GetInstancesOfImplementingTypes<INotifier>();
                        foreach (var n in notifiers)
                        {
                            var minutesToWait = ServiceStackHost.Instance.AppSettings.Get("AlertCooldown", 15);

                            if (DateTime.UtcNow - TimeSpan.FromMinutes(minutesToWait) > n.LastNotification)
                            {
                                n.Notify(checkResult);
                            }
                            else
                            {
                                Log.Verbose("Alert on cooldown, skipping notification");
                            }
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