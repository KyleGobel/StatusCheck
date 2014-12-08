using System;

namespace StatusCheck.Lib
{
    public class StatusCheckResult
    {
        public string Name { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string ScriptName { get; set; }
    }
}
