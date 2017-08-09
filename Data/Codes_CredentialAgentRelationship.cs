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
    
    public partial class Codes_CredentialAgentRelationship
    {
        public Codes_CredentialAgentRelationship()
        {
            this.Entity_AgentRelationship = new HashSet<Entity_AgentRelationship>();
            this.Entity_QA_Action = new HashSet<Entity_QA_Action>();
        }
    
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string SchemaTag { get; set; }
        public string ReverseRelation { get; set; }
        public string ReverseSchemaTag { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<bool> IsQARole { get; set; }
        public Nullable<bool> IsAgentToAgentRole { get; set; }
        public Nullable<bool> IsEntityToAgentRole { get; set; }
        public Nullable<bool> IsAssessmentAgentRole { get; set; }
        public Nullable<bool> IsLearningOppAgentRole { get; set; }
        public Nullable<int> Totals { get; set; }
        public Nullable<bool> IsOwnerAgentRole { get; set; }
    
        public virtual ICollection<Entity_AgentRelationship> Entity_AgentRelationship { get; set; }
        public virtual ICollection<Entity_QA_Action> Entity_QA_Action { get; set; }
    }
}
