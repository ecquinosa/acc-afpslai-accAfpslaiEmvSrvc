//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace accAfpslaiEmvSrvc
{
    using System;
    using System.Collections.Generic;
    
    public partial class system_user
    {
        public int id { get; set; }
        public string user_name { get; set; }
        public string user_pass { get; set; }
        public string last_name { get; set; }
        public string first_name { get; set; }
        public string middle_name { get; set; }
        public string suffix { get; set; }
        public Nullable<int> role_id { get; set; }
        public string status { get; set; }
        public Nullable<bool> is_deleted { get; set; }
        public Nullable<System.DateTime> date_post { get; set; }
        public Nullable<System.TimeSpan> time_post { get; set; }
    }
}
