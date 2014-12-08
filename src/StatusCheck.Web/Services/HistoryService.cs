using System.Net;
using ServiceStack;
using ServiceStack.OrmLite;
using StatusCheck.Lib.Types;

namespace StatusCheck.Web.Services
{
    public class HistoryService : Service
    {
        public HttpResult Post(HistoryLog log)
        {
            Db.Insert(log);
            return new HttpResult(HttpStatusCode.NoContent, "Insert success");
        }
        public object Get()
        {
            return "Hey there";
        }
    }
}