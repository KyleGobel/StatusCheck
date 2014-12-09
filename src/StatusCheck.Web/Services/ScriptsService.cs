using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Serilog;
using ServiceStack;
using ServiceStack.OrmLite;
using StatusCheck.Lib;
using StatusCheck.Lib.Types;

namespace StatusCheck.Web.Services
{
    [DefaultView("Scripts")]
    public class ScriptsService : Service
    {
        public List<Script> Get(Script script)
        {
            var scripts = Db.Select<Script>();
            return scripts;
        }

        public List<Script> Post(Script script)
        {
            Db.UpdateOnly(script, s => new {s.Enabled, s.DisplayName});
            return Get(new Script());
        }

        public HttpResult Get(InitializeScripts request)
        {
            var path = ServiceStackHost.Instance.AppSettings.Get("LocalScriptsPath", @"C:\Mobile\Scripts");
            if (!Directory.Exists(path))
            {
                Log.Warning("{Path} directory does not exist", path);
                return new HttpResult("Directory not found");
            }
            var files = Directory.GetFiles(path, "*.csx");

            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                if (Db.Exists<Script>(new {Name = name}))
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

    public class InitializeScripts
    {
    }
}