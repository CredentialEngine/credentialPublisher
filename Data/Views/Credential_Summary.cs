//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Data.Views
{
    using System;
    using System.Collections.Generic;
    
    public partial class Credential_Summary
    {
        public int Id { get; set; }
        public System.Guid EntityUid { get; set; }
        public string Name { get; set; }
        public string CredentialType { get; set; }
        public string CredentialTypeSchema { get; set; }
        public string Description { get; set; }
        public Nullable<int> ManagingOrgId { get; set; }
        public string ManagingOrganization { get; set; }
        public string Url { get; set; }
        public string Version { get; set; }
        public string LatestVersionUrl { get; set; }
        public string ReplacesVersionUrl { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public Nullable<System.DateTime> EffectiveDate { get; set; }
        public int CreatedById { get; set; }
        public int LastUpdatedById { get; set; }
        public int CredentialTypeId { get; set; }
        public Nullable<int> StatusId { get; set; }
        public string CTID { get; set; }
        public string CredentialRegistryId { get; set; }
        public string availableOnlineAt { get; set; }
        public System.Guid RowId { get; set; }
        public string AlternateName { get; set; }
        public int IsAQACredential { get; set; }
        public int HasQualityAssurance { get; set; }
        public string CreatorOrgs { get; set; }
        public string OwningOrgs { get; set; }
        public int LearningOppsCompetenciesCount { get; set; }
        public int AssessmentsCompetenciesCount { get; set; }
        public int QARolesCount { get; set; }
        public int HasPartCount { get; set; }
        public int IsPartOfCount { get; set; }
        public int RequiresCount { get; set; }
        public int RecommendsCount { get; set; }
        public int IsRecommendedForCount { get; set; }
        public int IsAdvancedStandingForCount { get; set; }
        public int AdvancedStandingFromCount { get; set; }
        public Nullable<int> EntityId { get; set; }
        public string PreviousVersion { get; set; }
        public Nullable<System.Guid> OwningAgentUid { get; set; }
        public string OwningOrganization { get; set; }
        public Nullable<int> OwningOrganizationId { get; set; }
        public int isRequiredForCount { get; set; }
        public int isPreparationForCount { get; set; }
        public int isPreparationFromCount { get; set; }
        public string QARolesList { get; set; }
        public string QAOrgRolesList { get; set; }
        public string AgentAndRoles { get; set; }
        public int entryConditionCount { get; set; }
        public int corequisiteConditionCount { get; set; }
    }
}
