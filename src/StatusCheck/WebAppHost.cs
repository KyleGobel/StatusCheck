using Funq;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Razor;
using ServiceStack.Text;
using StatusCheck.Lib.Types;
using StatusCheck.Web;
using StatusCheck.Web.Services;

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

            container.Register<IDbConnectionFactory>(
                c => new OrmLiteConnectionFactory(@"Data Source=StatusCheck.db; Version=3;", SqliteDialect.Provider));

            container.Register(c => new OrmLiteAppSettings(c.Resolve<IDbConnectionFactory>()));
            container.Resolve<OrmLiteAppSettings>().InitSchema();
            AppSettings = container.Resolve<OrmLiteAppSettings>();

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.CreateTableIfNotExists<HistoryLog>();
                db.DropAndCreateTable<Script>();
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
            Routes.Add<Script>("/scripts", "GET, POST");
            Routes.Add<InitializeScripts>("/scripts/initialize", "GET");
        }
    }
}