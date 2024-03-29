using MiddlewareDatabaseAPI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MiddlewareDatabaseAPI.Models
{
    public class Subscription
    {
        public int id { get; set; }
        public string name { get; set; }
        public DateTime creation_dt { get; set; }
        public string endpoint { get; set;}
        public string event_mqqt { get; set; }
        public int parent { get; set; }
    }
}