using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MiddlewareDatabaseAPI.Models
{
    public class DataOrSubscription
    {
        public string res_type { get; set; }
        public string name { get; set; }
        public int parent { get; set; }
        public string content { get; set; }
        public string event_mqtt { get; set; }
        public string endpoint { get; set; }
    }
}