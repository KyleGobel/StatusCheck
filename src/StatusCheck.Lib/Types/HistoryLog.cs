using System;
using ServiceStack.DataAnnotations;

namespace StatusCheck.Lib.Types
{
    public class HistoryLog
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; } 
        public DateTime Timestamp { get; set; }
        public string ScriptFile { get; set; }
        public bool Success { get; set; }
        public string ResultData { get; set; }

    }
}