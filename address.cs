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
    
    public partial class address
    {
        public int id { get; set; }
        public Nullable<int> member_id { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string address3 { get; set; }
        public string city { get; set; }
        public string province { get; set; }
        public Nullable<int> country_id { get; set; }
        public string zipcode { get; set; }
        public Nullable<System.DateTime> date_post { get; set; }
        public Nullable<System.TimeSpan> time_post { get; set; }
        public Nullable<bool> is_cancel { get; set; }
    }
}
