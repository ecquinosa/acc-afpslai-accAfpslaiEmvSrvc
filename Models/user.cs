using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace accAfpslaiEmvSrvc.Models
{
    public class user
    {
        
        public int userId { get; set; }
        public string userName { get; set; }
        public string userPass { get; set; }
        public string fullName { get; set; }
        public string roleId { get; set; }
        public string roleDesc { get; set; }

    }


}