using ServiceStack;
using ServiceStack.Configuration;

namespace StatusCheck.Services
{
    [DefaultView("Settings")]
    public class SettingsService : Service
    {
        public OrmLiteAppSettings AppSettings { get; set; }
        public HttpResult Post(SettingsRequest req)
        {
            AppSettings.Set("TimeBetweenLoops", req.TimeBetweenLoops);
            AppSettings.Set("AlertCooldown", req.AlertCooldown);
            AppSettings.Set("LocalScriptsPath", req.LocalScriptsPath);

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