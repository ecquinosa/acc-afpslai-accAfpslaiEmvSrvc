using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace accAfpslaiEmvSrvc.Models
{
    public class cbsData: member
    {
              
        public string civilStatus { get; set; }
        public string membershipStatus { get; set; }
        public string membershipType { get; set; }
        public string associateType { get; set; }

        public string address1 { get; set; }
        public string address2 { get; set; }
        public string address3 { get; set; }
        public string city { get; set; }
        public string province { get; set; }
        public string country { get; set; }
        public string zipCode { get; set; }


    }


}