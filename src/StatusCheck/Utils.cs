using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Chronos.Configuration;
using ScriptCs.Contracts;
using Serilog;
using ServiceStack;
using ServiceStack.Configuration;
using StatusCheck.Lib;

namespace StatusCheck
{
    public class Utils
    {
        public static List<string> GetScriptFiles()
        {
            var path = ServiceStackHost.Instance.AppSettings.Get("LocalScriptsPath", @"C:\Mobile\Scripts");
            if (!Directory.Exists(path))
            {
                Log.Information("{Path} directory not exist", path);
                return new List<string>();
            }
            return Directory.GetFiles(path, "*.csx").ToList();
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
        public static StatusCheckResult ConvertToStatusCheckResult(ScriptResult result, string scriptPath)
        {
            if (result.ReturnValue == null)
            {
                return new StatusCheckResult
                {
                    Name = "No name found",
                    Message = "'{0}' Script contained no return value".Fmt(Path.GetFileName(scriptPath)),
                    Success = false,
                    Timestamp = DateTime.UtcNow,
                    ScriptName = Path.GetFileName(scriptPath)
                };
            }

            var scResult = new StatusCheckResult {ScriptName = Path.GetFileName(scriptPath)};

            try
            {
                scResult.Success = (bool) result.ReturnValue
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
                scResult.Message = (string) result.ReturnValue
                    .GetType()
                    .GetProperty("Message")
                    .GetValue(result.ReturnValue);
            }
            catch
            {
                scResult.Message = string.Empty;
            }

            try
            {
                scResult.Name = (string) result.ReturnValue
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
    }
}