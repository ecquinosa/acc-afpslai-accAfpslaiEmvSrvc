using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace accAfpslaiEmvSrvc.Models
{
    public class requestCredential
    {

        public string key { get; set; }
        public int userId { get; set; }
        public string userName { get; set; }
        public string userPass { get; set; }
        public string branch { get; set; }
        public string dateRequest { get; set; }

    }


}