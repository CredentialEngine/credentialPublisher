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
    
    public partial class Entity_TaskProfile
    {
        public int Id { get; set; }
        public int EntityId { get; set; }
        public string ProfileName { get; set; }
        public string Description { get; set; }
        public Nullable<System.DateTime> DateEffective { get; set; }
        public Nullable<System.Guid> AffiliatedAgentUid { get; set; }
        public string Url { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<int> CreatedById { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public Nullable<int> LastUpdatedById { get; set; }
        public System.Guid RowId { get; set; }
        public string AvailableOnlineAt { get; set; }
        public string AvailabilityListing { get; set; }
    
        public virtual Entity Entity { get; set; }
        public virtual Account Account_Creator { get; set; }
        public virtual Account Account_Modifier { get; set; }
    }
}
