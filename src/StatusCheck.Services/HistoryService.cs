using System;
using System.Net;
using ServiceStack;
using ServiceStack.OrmLite;
using StatusCheck.Lib.Types;

namespace StatusCheck.Services
{
    public class HistoryService : Service
    {
        public HttpResult Post(HistoryLog log)
        {
            Db.Insert(log);
            return new HttpResult(HttpStatusCode.NoContent, "Insert success");
        }
        public LastRunResponse Get(LastRun request)
        {
            const string sql = "select timestamp, success from history_log where script_file = @Name order by timestamp desc limit 1";

            return Db.Single<LastRunResponse>(sql, request);
        }
    }

    public class LastRunResponse
    {
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
    }
    public class LastRun
    {
        public string Name { get; set; }
    }
}