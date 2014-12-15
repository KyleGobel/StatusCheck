using System;
using System.Collections.Generic;
using System.IO;
using Serilog;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.OrmLite;
using StatusCheck.Lib;
using StatusCheck.Lib.Types;

namespace StatusCheck.Services
{
    public class ScriptsService : Service
    {
        public OrmLiteAppSettings AppSettings { get; set; }
        public IStatusCheckExecutor StatusCheckExecutor { get; set; }


        /// <summary>
        /// Get single script if Id is passed in, otherwise return all scripts
        /// </summary>
        [DefaultView("Script")]
        public Script Get(Script script)
        {
            var s = Db.SingleById<Script>(script.Id);
            return s;
        }

        [DefaultView("Scripts")]
        public List<Script> Get(AllScripts script)
        {
            var scripts = Db.Select<Script>();
            return scripts;
        }

        /// <summary>
        /// Update a script
        /// </summary>
        [DefaultView("Script")]
        public HttpResult Post(Script script)
        {
            const string sql = "update script set enabled = @enabled, display_name = @displayName, seconds_between_checks = @secondsBetweenChecks where id = @id";
            Db.ExecuteNonQuery(sql, script);
            return HttpResult.Redirect("/scripts");
        }

        /// <summary>
        /// Run a specific script by id
        /// </summary>
        public StatusCheckResult Get(RunScript request)
        {
            var currentScriptsDirectory = AppSettings.Get("LocalScriptsPath", @"C:\Mobile\Scripts");

            var script = Get(request.ConvertTo<Script>())
                    .GetResponseDto<Script>();

            var result = StatusCheckExecutor.ExecuteScript(script,currentScriptsDirectory);
            return result;
        }

        /// <summary>
        /// Re-populate db from file system
        /// </summary>
        public HttpResult Get(InitializeScripts request)
        {
            var path = AppSettings.Get("LocalScriptsPath", @"C:\Mobile\Scripts");

            if (!Directory.Exists(path))
            {
                Log.Warning("{Path} directory does not exist", path);
                return new HttpResult("Directory not found");
            }
            var files = Directory.GetFiles(path, "*.csx");

            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                if (Db.Single<bool>("select exists(select 1 from script where name = @name)",new {name}))
                {
                    var script = Db.Single<Script>(x => x.Name == name);
                    script.Contents = File.ReadAllText(file);
                    Db.Update(script);
                }
                else
                {
                    var script = new Script
                    {
                        Name = name,
                        Contents = File.ReadAllText(file),
                        Enabled = false,
                        Id = Guid.NewGuid()
                    };

                    Db.Insert(script);
                }
            }
            return new HttpResult("Initialize complete");
        }
    }

    public class AllScripts
    {
    }

    public class RunScript
    {
        public Guid Id { get; set; }
    }

    public class InitializeScripts
    {
    }
}