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
    
    public partial class Entity_Competency
    {
        public int Id { get; set; }
        public int EntityId { get; set; }
        public int CompetencyId { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<int> CreatedById { get; set; }
    
        public virtual EducationFramework_Competency EducationFramework_Competency { get; set; }
        public virtual Entity Entity { get; set; }
    }
}
