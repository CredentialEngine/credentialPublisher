//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Data
{
    using System;
    using System.Collections.Generic;
    
    public partial class ONET_SOC
    {
        public int Id { get; set; }
        public string OnetSocCode { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string SOC_code { get; set; }
        public Nullable<int> JobFamily { get; set; }
        public string URL { get; set; }
        public Nullable<int> Totals { get; set; }
    
        public virtual ONET_SOC_JobFamily ONET_SOC_JobFamily { get; set; }
    }
}
