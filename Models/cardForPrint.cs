using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace accAfpslaiEmvSrvc.Models
{
    public class cardForPrint
    {

        public int cardId { get; set; }
        public int memberId { get; set; }
        public string cif { get; set; }
        public string cardNo { get; set; }
        public string first_name { get; set; }
        public string middle_name { get; set; }
        public string last_name { get; set; }
        public string suffix { get; set; }
        public string membership_date { get; set; }
        public string card_valid_thru { get; set; }
        public string gender { get; set; }
        public string branch_issued { get; set; }
        public string dateCaptured { get; set; }
        public string datePrinted { get; set; }
        public string base64Photo { get; set; }

    }


}