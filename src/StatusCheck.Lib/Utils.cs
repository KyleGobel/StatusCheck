using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ScriptCs.Contracts;
using Serilog;
using ServiceStack;
using StatusCheck.Lib.Types;

namespace StatusCheck.Lib
{
    public class Utils
    {
        public static List<Script> GetScriptFiles()
        {
            var path = ServiceStackHost.Instance.AppSettings.Get("LocalScriptsPath", @"C:\Mobile\Scripts");
            if (!Directory.Exists(path))
            {
                Log.Information("{Path} directory not exist", path);
                return new List<Script>();
            }
            var files = Directory.GetFiles(path, "*.csx");

            return files.Select(x => new Script
            {
                Contents = File.ReadAllText(x),
                Name = Path.GetFileName(x)
            })
            .ToList();
        }


        public static IEnumerable<T> GetInstancesOfImplementingTypes<T>()
        {
            AppDomain app = AppDomain.CurrentDomain;
            Assembly[] ass = app.GetAssemblies();
            Type[] types;
            Type targetType = typeof(T);

            foreach (Assembly a in ass)
            {
                types = a.GetTypes();
                foreach (Type t in types)
                {
                    if (t.IsInterface) continue;
                    if (t.IsAbstract) continue;
                    foreach (Type iface in t.GetInterfaces())
                    {
                        if (!iface.Equals(targetType)) continue;
                        yield return (T)Activator.CreateInstance(t);
                        break;
                    }
                }
            }
        }
    }
}