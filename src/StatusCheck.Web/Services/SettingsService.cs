using System.Net;
using ServiceStack;
using ServiceStack.Configuration;

namespace StatusCheck.Web.Services
{
    [DefaultView("Settings")]
    public class SettingsService : Service
    {
        public HttpResult Post(SettingsRequest req)
        {
            ServiceStackHost.Instance.AppSettings.Set("TimeBetweenLoops", req.TimeBetweenLoops);
            ServiceStackHost.Instance.AppSettings.Set("AlertCooldown", req.AlertCooldown);
            ServiceStackHost.Instance.AppSettings.Set("LocalScriptsPath", req.LocalScriptsPath);

            return new HttpResult("Success");
        }

        public HttpResult Get(SettingsRequest req)
        {
            return new HttpResult();
        }
    }

    public class SettingsRequest
    {
        public int TimeBetweenLoops { get; set; }
        public int AlertCooldown { get; set; }
        public string LocalScriptsPath { get; set; }
    }
}