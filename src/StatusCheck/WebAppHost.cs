using Funq;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Razor;
using ServiceStack.Text;
using StatusCheck.Lib;
using StatusCheck.Lib.Types;
using StatusCheck.Services;
using StatusCheck.Web;

namespace StatusCheck
{
    public class WebAppHost : AppSelfHostBase
    {
        public WebAppHost() : base("Status Check Self-Host", typeof(HistoryService).Assembly) {}

        public static string BaseUrl = "http://localhost:33936/";
        public void Start()
        {
            this
                .Init()
                .Start(BaseUrl);
        }
        public override void Configure(Container container)
        {
            ConfigRoutes();

            JsConfig.EmitCamelCaseNames = true;
            JsConfig.PropertyConvention = PropertyConvention.Lenient;
            JsConfig.DateHandler = DateHandler.ISO8601;
            JsConfig.AssumeUtc = true;
            JsConfig.AlwaysUseUtc = true;
            

            container.Register<IDbConnectionFactory>(
                c => new OrmLiteConnectionFactory(ConfigUtils.GetConnectionString("Postgres") , PostgreSqlDialect.Provider));

            container.Register(c => new OrmLiteAppSettings(c.Resolve<IDbConnectionFactory>()));
            container.Register<IStatusCheckExecutor>(c => new ScriptCsExecutor());
            container.Resolve<OrmLiteAppSettings>().InitSchema();
            AppSettings = container.Resolve<OrmLiteAppSettings>();

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.CreateTableIfNotExists<HistoryLog>();
                db.CreateTableIfNotExists<Script>();
            }

            Plugins.Add(new RazorFormat
            {
                LoadFromAssemblies = { typeof(TypeMarker).Assembly }
            });
            SetConfig(new HostConfig
            {
                EmbeddedResourceBaseTypes = {GetType(), typeof (TypeMarker)}
            });
        }

        public void ConfigRoutes()
        {
            Routes.Add<SettingsRequest>("/settings", "GET, POST");
            Routes.Add<HistoryLog>("/history", "POST");
            Routes.Add<AllScripts>("/scripts", "GET");
            Routes
                .Add<Script>("/script/{Id}", "GET")
                .Add<Script>("/script", "POST");
            
            Routes.Add<InitializeScripts>("/scripts/initialize", "GET");
            Routes.Add<RunScript>("/script/{Id}/run", "GET");
            Routes.Add<LastRun>("/lastRun", "GET");
        }
    }
}