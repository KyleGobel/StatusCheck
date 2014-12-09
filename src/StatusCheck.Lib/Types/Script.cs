using System;
using ServiceStack.DataAnnotations;

namespace StatusCheck.Lib.Types
{
    public class Script
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool Enabled { get; set; }
        public string Contents { get; set; }

    } 
}