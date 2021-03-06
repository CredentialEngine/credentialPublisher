﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Models;
using MC = Models.Common;
using Factories;
using Utilities;

namespace CTIServices
{
	public class EnumerationServices
	{

		#region enumerations 
		/// <summary>
		/// Get an MC.Enumeration (by default a checkbox list) by schemaName
		/// </summary>
		/// <param name="dataSource"></param
		/// <param name="interfaceType"></param>
		/// <param name="showOtherValue">If true, a text box for entering other values will be displayed</param>
		/// <returns></returns>
		public MC.Enumeration GetEnumeration( string dataSource, MC.EnumerationType interfaceType = MC.EnumerationType.MULTI_SELECT, 
				bool showOtherValue = false, 
				bool getAll = true)
		{
			MC.Enumeration e = CodesManager.GetEnumeration( dataSource, getAll );
			e.InterfaceType = interfaceType;
			e.ShowOtherValue = showOtherValue;
			return e;
		}
		public MC.Enumeration GetEnumerationForRadioButtons( string dataSource, int preselectId = -1, bool getAll = true)
		{
			MC.Enumeration e = CodesManager.GetEnumeration( dataSource, getAll );
			e.InterfaceType = MC.EnumerationType.SINGLE_SELECT;
			if ( preselectId > -1 && e.HasItems() )
			{
				int cntr = 0;
				foreach(MC.EnumeratedItem item in e.Items) 
				{
					if ( cntr == preselectId )
					{
						item.Selected = true;
						break;
					}
					cntr++;
				}
			}
			return e;
		}

		public MC.Enumeration GetJurisdictionAssertions( string filter, MC.EnumerationType interfaceType = MC.EnumerationType.MULTI_SELECT,
			bool showOtherValue = true,
			bool getAll = true )
		{
			MC.Enumeration e = CodesManager.GetJurisdictionAssertions_Filtered( filter );
			e.InterfaceType = interfaceType;
			e.ShowOtherValue = showOtherValue;
			return e;
		}
		#endregion

		/// <summary>
		/// Get a list of properties - typically called from Views
		/// </summary>
		/// <param name="dataSource"></param>
		/// <param name="getAll"></param>
		/// <returns></returns>
		public List<CodeItem> GetPropertiesList( string dataSource, bool getAll = true )
		{
			bool insertSelectTitle = false;
			List<CodeItem> list = CodesManager.Property_GetValues( dataSource, insertSelectTitle, getAll );

			return list;
		}
		public List<CodeItem> GetPropertiesList( string dataSource, bool insertSelectTitle, bool getAll = true )
		{
			List<CodeItem> list = CodesManager.Property_GetValues( dataSource, insertSelectTitle, getAll );

			return list;
		}

		public static MC.Enumeration GetPropertiesList( int CategoryId , bool getAll = true )
		{
			MC.Enumeration e = CodesManager.GetEnumeration( CategoryId, getAll );
			e.InterfaceType = MC.EnumerationType.MULTI_SELECT;
			e.ShowOtherValue = true;
			return e;
		}
		public static CodeItem GetPropertyBySchema( string categoryCode, string schemaName )
		{
			CodeItem item = CodesManager.GetPropertyBySchema( categoryCode, schemaName );

			return item;
		}

		#region credential enumerations
		public MC.Enumeration GetCredentialType( MC.EnumerationType interfaceType, bool getAll = true )
		{

			MC.Enumeration e = CodesManager.GetEnumeration( "credentialType", getAll );
			e.ShowOtherValue = true;
			e.InterfaceType = interfaceType;
			return e;
		}
		//
		public MC.Enumeration GetEducationCredentialType( MC.EnumerationType interfaceType, bool getAll = true )
		{

			MC.Enumeration e = CodesManager.GetEnumeration( "credentialType", getAll, true );
			e.ShowOtherValue = true;
			e.InterfaceType = interfaceType;
			return e;
		}
		//
		public MC.Enumeration GetCredentialPurpose( MC.EnumerationType interfaceType, bool getAll = true )
		{
			MC.Enumeration e = CodesManager.GetEnumeration( "purpose", getAll );
			e.ShowOtherValue = true;
			e.InterfaceType = interfaceType;
			return e;
		}
		//
		public MC.Enumeration GetAudienceLevel( MC.EnumerationType interfaceType, bool getAll = true )
		{
			MC.Enumeration e = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL, getAll );
			e.InterfaceType = interfaceType;
			e.ShowOtherValue = true;
			return e;
		}
        public static List<string> GetPropertiesSchemaNameList(int categoryId, bool getAll = true)
        {
            List<CodeItem> list = CodesManager.Property_GetValues(categoryId, "", false, getAll);
            List<string> output = new List<string>();
            foreach (var item in list)
            {
                if (!string.IsNullOrWhiteSpace(item.SchemaName))
                {
                    string[] parts = item.SchemaName.Split(':');
                    if (parts.Count() == 2)
                        output.Add(parts[1]);
                }
            }
            return output;
        }
        public MC.Enumeration GetAudienceTypes(MC.EnumerationType interfaceType, bool getAll = true)
        {
            MC.Enumeration e = CodesManager.GetEnumeration(CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE, getAll);
            e.InterfaceType = interfaceType;
            e.ShowOtherValue = true;
            return e;
        }
		//
		public MC.Enumeration GetSiteTotals( MC.EnumerationType interfaceType, int categoryId, int entityTypeId, bool getAll = true )
		{
			MC.Enumeration e = CodesManager.GetSiteTotalsAsEnumeration( categoryId, entityTypeId, getAll );
			e.InterfaceType = interfaceType;
			e.ShowOtherValue = true;
			return e;
		}
		//
		[Obsolete]
		public MC.Enumeration GetCredentialLevel( MC.EnumerationType interfaceType, bool getAll = true )
		{
			MC.Enumeration e = CodesManager.GetEnumeration( "credentialLevel", getAll );
			e.InterfaceType = interfaceType;
			e.ShowOtherValue = true;
			return e;
		}
		//
		#endregion

		#region Condition profile related
		/// <summary>
		/// Get credential connections codes.
		/// Used by search
		/// Note: a custom type that includes is part, and part of, primarily for search filters.
		/// NOTE: ensure same one used for the editor?
		/// </summary>
		/// <param name="interfaceType"></param>
		/// <param name="getAll"></param>
		/// <returns></returns>
		//public MC.Enumeration GetCredentialConnections( MC.EnumerationType interfaceType, bool getAll = true )
		//{

		//	MC.Enumeration e = CodesManager.GetEnumeration( "conditionProfileType", getAll, true );
		//	e.ShowOtherValue = true;
		//	e.InterfaceType = interfaceType;
		//	return e;
		//}

		/// <summary>
		/// Get credential connections codes.
		/// Used by search
		/// Note: a custom type that includes is part, and part of, primarily for search filters.
		/// NOTE: ensure same one used for the editor?
		/// </summary>
		/// <param name="interfaceType"></param>
		/// <param name="getAll"></param>
		/// <returns></returns>
		public MC.Enumeration GetCredentialsConditionProfile( MC.EnumerationType interfaceType, bool getAll = true, bool useConditionManifestTitles = false )
		{

			MC.Enumeration e = CodesManager.GetCredentialsConditionProfileTypes( getAll, useConditionManifestTitles );
			e.ShowOtherValue = false;
			e.InterfaceType = interfaceType;
			return e;
		}
		public MC.Enumeration GetCommonConditionProfileTypes( MC.EnumerationType interfaceType, bool getAll = true, bool useConditionManifestTitles = false )
		{

			MC.Enumeration e = CodesManager.GetCommonConditionProfileTypes( getAll, useConditionManifestTitles );
			e.ShowOtherValue = false;
			e.InterfaceType = interfaceType;
			return e;
		}
		public MC.Enumeration GetConditionManifestConditionTypes( MC.EnumerationType interfaceType, bool getAll = true )
		{

			MC.Enumeration e = CodesManager.GetConditionManifestConditionTypes( getAll );
			e.ShowOtherValue = false;
			e.InterfaceType = interfaceType;
			return e;
		}
		//public MC.Enumeration GetAssessmentsConditionProfileTypes( MC.EnumerationType interfaceType)
		//{

		//	MC.Enumeration e = CodesManager.GetAssessmentsConditionProfileTypes();
		//	e.ShowOtherValue = false;
		//	e.InterfaceType = interfaceType;
		//	return e;
		//}
		//public MC.Enumeration GetLearningOppsConditionProfileTypes( MC.EnumerationType interfaceType )
		//{

		//	MC.Enumeration e = CodesManager.GetLearningOppsConditionProfileTypes();
		//	e.ShowOtherValue = false;
		//	e.InterfaceType = interfaceType;
		//	return e;
		//}
		#endregion

		#region agent role enums
		public MC.Enumeration GetCredentialAllAgentRoles( MC.EnumerationType interfaceType )
		{
			MC.Enumeration e = OrganizationRoleManager.GetCredentialOrg_AllRoles( false );
			e.InterfaceType = interfaceType;
			e.ShowOtherValue = true;
			return e;
		}

		
		/// <summary>
		/// Get agent roles for assessments
		/// Ex: Created By (not any more)
		/// </summary>
		/// <param name="interfaceType"></param>
		/// <returns></returns>
		public MC.Enumeration GetAssessmentAgentRoles( MC.EnumerationType interfaceType )
		{
			MC.Enumeration e = Entity_AgentRelationshipManager.GetAssessmentAgentRoles( false );
			e.InterfaceType = interfaceType;
			e.ShowOtherValue = true;
			return e;
		}
		/// <summary>
		/// Get agent roles for learning opportunities
		/// </summary>
		/// <param name="interfaceType"></param>
		/// <returns></returns>
		public MC.Enumeration GetLearningOppAgentRoles( MC.EnumerationType interfaceType )
		{
			MC.Enumeration e = Entity_AgentRelationshipManager.GetLearningOppAgentRoles( false );
			e.InterfaceType = interfaceType;
			e.ShowOtherValue = true;
			return e;
		}
		
	
		public MC.Enumeration GetAllAgentReverseRoles( MC.EnumerationType interfaceType )
		{
			MC.Enumeration e = OrganizationRoleManager.GetAgentToAgentRolesCodes( false );
			e.InterfaceType = interfaceType;
			e.ShowOtherValue = true;
			return e;
		}
        public MC.Enumeration GetAllAgentQAPerformedRoles( MC.EnumerationType interfaceType )
        {
            MC.Enumeration e = OrganizationRoleManager.GetAgentToAgentRolesCodes( true );
            e.InterfaceType = interfaceType;
            e.ShowOtherValue = true;
            return e;
        }
        public MC.Enumeration GetEntityQARoles( MC.EnumerationType interfaceType, string entityType = "Credential", bool getAll = true )
		{
			//get roles as entity to org
			MC.Enumeration e = OrganizationRoleManager.GetEntityQARoles();
			e.InterfaceType = interfaceType;
			e.ShowOtherValue = false;
			return e;
		}

		public MC.Enumeration GetEntityOfferedByRoles( MC.EnumerationType interfaceType )
		{
			//get roles as entity to org
			MC.Enumeration e = OrganizationRoleManager.GetEntityOfferedByRoles();
			e.InterfaceType = interfaceType;
			e.ShowOtherValue = false;
			return e;
		}
		/// <summary>
		/// Get only QA roles
		/// - used by editor - for 3rd party QA??
		/// </summary>
		/// <param name="interfaceType"></param>
		/// <param name="entityType"></param>
		/// <returns></returns>
		public MC.Enumeration GetCredentialAgentQAActions( MC.EnumerationType interfaceType, string entityType = "Credential", bool getAll = true )
		{
			//get roles as entity to org
			MC.Enumeration e = OrganizationRoleManager.GetEntityAgentQAActions( false, getAll, entityType );
			e.InterfaceType = interfaceType;
			e.ShowOtherValue = true;
			return e;
		}
		public MC.Enumeration GetQAPerformedFilters ( MC.EnumerationType interfaceType, string entityType, bool getAll, bool showInverseName = true)
        {
            //get roles as entity to org
            MC.Enumeration e = OrganizationRoleManager.GetEntityAgentQAActions( showInverseName, getAll, entityType );
            e.InterfaceType = interfaceType;
            e.ShowOtherValue = true;
            return e;
        }
        public MC.Enumeration GetCredentialAgentRoles( MC.EnumerationType interfaceType, string entityType = "Credential" )
		{
			//get roles as entity to org
			MC.Enumeration e = new MC.Enumeration();
			if (Utilities.UtilityManager.GetAppKeyValue("includingAllRolesForOrgRoles", false))
				e = OrganizationRoleManager.GetCredentialOrg_AllRoles( false, entityType );
			else 
				e = OrganizationRoleManager.GetCredentialOrg_NonQARoles( false, entityType );
			
			e.InterfaceType = interfaceType;
			e.ShowOtherValue = true;
			return e;
		}

		public MC.Enumeration GetCredentialOwnerAgentRoles( MC.EnumerationType interfaceType)
		{
			//get roles as entity to org
			MC.Enumeration e = new MC.Enumeration();
			e = OrganizationRoleManager.GetOwnerAgentRoles( false );
			
			e.InterfaceType = interfaceType;
			e.ShowOtherValue = true;
			return e;
		}
		public MC.Enumeration GetNonCredentialOwnerAgentRoles( MC.EnumerationType interfaceType )
		{
			//get roles as entity to org
			MC.Enumeration e = new MC.Enumeration();
			e = OrganizationRoleManager.GetOwnerAgentRoles( false, true );

			e.InterfaceType = interfaceType;
			e.ShowOtherValue = true;
			return e;
		}
		#endregion
		#region OBSOLETE ROLE METHODS
		/// <summary>
		/// OLD
		/// </summary>
		/// <param name="interfaceType"></param>
		/// <returns></returns>
		//[Obsolete]
		//private MC.Enumeration GetAllAgentRoles( MC.EnumerationType interfaceType )
		//{
		//	MC.Enumeration e = OrganizationRoleManager.GetAgentToAgentRolesCodes( true );
		//	e.InterfaceType = interfaceType;
		//	e.ShowOtherValue = true;
		//	return e;
		//}
		//[Obsolete]
		//private MC.Enumeration GetAllOrgAgentRoles( MC.EnumerationType interfaceType )
		//{
		//	MC.Enumeration e = OrganizationRoleManager.GetAgentToAgentRolesCodes( true );
		//	e.InterfaceType = interfaceType;
		//	e.ShowOtherValue = true;
		//	return e;
		//}

		/// <summary>
		/// Get INVERSE agent roles for assessments and learning opportunities
		/// Ex: Created
		/// </summary>
		/// <param name="interfaceType"></param>
		/// <returns></returns>
		//[Obsolete]
		//private MC.Enumeration GetAllOtherAgentRolesInverse( MC.EnumerationType interfaceType )
		//{
		//	MC.Enumeration e = Entity_AgentRelationshipManager.GetAllOtherAgentRoles( true );
		//	e.InterfaceType = interfaceType;
		//	e.ShowOtherValue = true;
		//	return e;
		//}
		//[Obsolete]
		//public MC.Enumeration GetEntityAgentQAActionFilters( MC.EnumerationType interfaceType, string entityType, bool getAll = true )
		//{
		//	//get roles as entity to org
		//	MC.Enumeration e = OrganizationRoleManager.GetEntityAgentQAActionFilters( false, getAll, entityType );
		//	e.InterfaceType = interfaceType;
		//	e.ShowOtherValue = false;
		//	return e;
		//}
		/// <summary>
		/// Get agent roles for assessment and learning opps
		/// </summary>
		/// <param name="interfaceType"></param>
		/// <returns></returns>
		//public MC.Enumeration GetAllLearningOppAgentRoles( MC.EnumerationType interfaceType )
		//{
		//	MC.Enumeration e = Entity_AgentRelationshipManager.GetAllOtherAgentRoles( true );
		//	e.InterfaceType = interfaceType;
		//	e.ShowOtherValue = true;
		//	return e;
		//}
		#endregion

		#region org related codes and enumurations
		public List<CodeItem> GetUserOrganizations( int userId )
		{
			return OrganizationServices.OrganizationMember_OrgsAsCodeItems( userId );
		}
		public MC.Enumeration GetOrganizationType( MC.EnumerationType interfaceType,
				bool getAll = true )
		{

			MC.Enumeration e = CodesManager.GetEnumeration( "organizationType", getAll );
			e.ShowOtherValue = true;
			e.InterfaceType = interfaceType;
			return e;
		}
		//
		public static MC.Enumeration GetOrganizationIdentifier( MC.EnumerationType interfaceType, bool getAll = true )
		{

			MC.Enumeration e = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ORGANIZATION_IDENTIFIERS, getAll );
			e.ShowOtherValue = true;
			e.InterfaceType = interfaceType;
			return e;
		}
		//
		/// <summary>
		/// Get candidate list of services for an org
		/// </summary>
		/// <param name="interfaceType"></param>
		/// <returns></returns>
		public MC.Enumeration GetOrganizationServices( MC.EnumerationType interfaceType,
				bool getAll = true )
		{
			//MC.Enumeration e = OrganizationRoleManager.GetCredentialOrgRoles( true, false, "" );
			MC.Enumeration e = OrganizationServiceManager.GetOrgServices( getAll );
			e.InterfaceType = interfaceType;
			e.ShowOtherValue = true;
			return e;
		}
	
		/// <summary>
        /// Used to retrieve managing orgs in the editor
        /// </summary>
        /// <param name="insertSelectTitle"></param>
        /// <returns></returns>
		public List<CodeItem> GetOrganizationsAsCodes(bool insertSelectTitle = false)
		{
			
			List<CodeItem> list = OrganizationManager.Organization_SelectAllAsCodes( insertSelectTitle );

			return list;
		}
		//public MC.Enumeration GetOrganizations( string schemaName, bool includeDefaultOption, bool forCurrentUser, MC.EnumerationType interfaceType )
		//{
		//	var result = new MC.Enumeration()
		//	{
		//		InterfaceType = interfaceType,
		//		Name = "Organizations",
		//		SchemaName = schemaName
		//	};

		//	AppUser user = AccountServices.GetCurrentUser();
		//	if ( ( user == null | user.Id == 0 ) )
		//		return result;
			
		//	if ( includeDefaultOption )
		//	{
		//		result.Items.Add( new MC.EnumeratedItem()
		//		{
		//			Id = 0,
		//			RowId = "",
		//			Name = "Select an Organization",
		//			Value = ""
		//		} );
		//	}

		//	//May need some overload to only get orgs for current user
		//	var organizations = OrganizationServices.OrganizationsForCredentials_Select(user.Id);
		//	foreach ( var org in organizations )
		//	{
		//		result.Items.Add( new MC.EnumeratedItem()
		//		{
		//			Id = org.Id,
		//			RowId = org.RowId.ToString(),
		//			Name = org.Name,
		//			Value = org.SubjectWebpage
		//		} );
		//	}

		//	return result;
		//}
		//
		#endregion

		#region //Temporary
		//Get a sample enumeration
		//public MC.Enumeration GetSampleEnumeration( string dataSource, string schemaName, MC.EnumerationType interfaceType )
		//{
		//	var result = CodesManager.GetSampleEnumeration( dataSource, schemaName );
		//	result.InterfaceType = interfaceType;

		//	return result;
		//}
		//
		#endregion 		//End Temporary

		#region currencies/countries
		public List<CodeItem> GetCountries()
		{
			List<CodeItem> list = CodesManager.GetCountries_AsCodes();
			return list;
		}
	
		//GetCurrencies
		public MC.Enumeration GetCurrencies( MC.EnumerationType interfaceType )
		{
			MC.Enumeration e = CodesManager.GetCurrencies();
			e.ShowOtherValue = false;
			e.InterfaceType = interfaceType;
			return e;
		}


		/// <summary>
		/// Get Languages
		/// </summary>
		/// <param name="interfaceType"></param>
		/// <param name="entityTypeId"></param>
		/// <param name="getAll"></param>
		/// <returns></returns>
		public MC.Enumeration GetLanguages( MC.EnumerationType interfaceType, int entityTypeId = 0, bool getAll = true )
		{
			MC.Enumeration e = CodesManager.GetLanguages( entityTypeId , getAll);
			e.ShowOtherValue = false;
			e.InterfaceType = interfaceType;
			return e;
		}//GetLanguages
		public MC.Enumeration GetLanguagesForEditor( MC.EnumerationType interfaceType, int entityTypeId = 0, bool getAll = true )
		{
			MC.Enumeration e = CodesManager.GetLanguagesForEditor( entityTypeId, getAll );
			e.ShowOtherValue = false;
			e.InterfaceType = interfaceType;
			return e;
		}//GetLanguages
		#endregion

		#region SOC
		public static List<CodeItem> SOC_Search( int headerId, string keyword, int pageNumber, int pageSize, ref int totalRows, bool getAll = true )
		{
			return CodesManager.SOC_Search( headerId, keyword, pageNumber, pageSize,  ref totalRows, getAll );
		}
		/// <summary>
		/// SOC Search
		/// TODO - need to include the parentId, so search will not return items already selected
		/// </summary>
		/// <param name="keyword"></param>
		/// <param name="maxTerms"></param>
		/// <returns></returns>
		//public static List<CodeItem> SOC_Search( int headerId = 0, string keyword = "", int pageNumber = 1, int maxRows = 25 )
		//{
		//	int totalRows = 0;
		//	return CodesManager.SOC_Search( headerId, keyword, pageNumber, maxRows, ref totalRows );
		//}
		public static List<CodeItem> SOC_Autocomplete( int credentialId, int headerId = 0, string keyword = "", int maxRows = 25 )
		{
			return CodesManager.SOC_Autocomplete( headerId, keyword, maxRows );
		}
		public static List<CodeItem> SOC_Categories()
		{
			return CodesManager.SOC_Categories();
		}
		public static MC.Enumeration SOC_Categories_Enumeration( bool getAll = true)
		{
			var data = new List<CodeItem>();
			if ( getAll )
				data = CodesManager.SOC_Categories();
			else
			{
				//show all until the custom one is fixed
				//data = CodesManager.SOC_Categories();
				data = CodesManager.SOC_CategoriesInUse();
			}
				
			var result = new MC.Enumeration()
			{
				Id = 11,
				Name = "Standard Occupation Codes (SOC)",
				Items = ConvertCodeItemsToEnumeratedItems( data )
			};
			return result;
		}

		#endregion
		#region NAICS
		public static List<CodeItem> NAICS_Search( int entityTypeId, int headerId, string keyword, int pageNumber, int pageSize, ref int totalRows, bool getAll = true )
		{
			//int totalRows = 0;
			if ( entityTypeId == 0)
				return CodesManager.NAICS_Search( headerId, keyword, pageNumber, pageSize, getAll, ref totalRows );
			else
				return CodesManager.NAICS_SearchInUse( entityTypeId, headerId, keyword, pageNumber, pageSize, ref totalRows );
		}
		public static List<CodeItem> NAICS_Autocomplete( int credentialId, int headerId = 0, string keyword = "", int maxRows = 25 )
		{
			//need a getAll option for this as well!
			return CodesManager.NAICS_Autocomplete( headerId, keyword, maxRows );
		}
		public static List<CodeItem> NAICS_Categories()
		{
			return CodesManager.NAICS_Categories();
		}

		/// <summary>
		/// Get all NAICS groups
		/// </summary>
		/// <returns></returns>
		public static MC.Enumeration NAICS_Categories_Enumeration()
		{
			var data = CodesManager.NAICS_Categories();
			var result = new MC.Enumeration()
			{
				Id = 10,
				Name = "North American Industry Classification System (NAICS)",
				Items = ConvertCodeItemsToEnumeratedItems( data )
			};
			return result;
		}
		public static MC.Enumeration NAICS_CategoriesInUse_Enumeration( int entityTypeId )
		{
			var data = new List<CodeItem>();
			//show all until the custom one is fixed
			//data = CodesManager.NAICS_Categories()
			
			data = CodesManager.NAICS_CategoriesInUse( entityTypeId );
			
			var result = new MC.Enumeration()
			{
				Id = 10,
				Name = "North American Industry Classification System (NAICS)",
				Items = ConvertCodeItemsToEnumeratedItems( data )
			};
			return result;
		}
		#endregion
		#region CIPS
		public static List<CodeItem> CIPS_Search( int entityTypeId, int headerId, string keyword, int pageNumber, int pageSize, ref int totalRows, bool getAll = true )
		{
			//int totalRows = 0;
			if ( entityTypeId == 0 )
				return CodesManager.CIPS_Search( headerId, keyword, pageNumber, pageSize, ref totalRows, getAll );
			else
				return CodesManager.CIPS_SearchInUse( entityTypeId, headerId, keyword, pageNumber, pageSize, ref totalRows );
		}
		public static List<CodeItem> CIPS_Search( int headerId = 0, string keyword = "", int pageNumber = 1, int maxRows = 25 )
		{
			int totalRows = 0;
			return CodesManager.CIPS_Search( headerId, keyword, pageNumber, maxRows, ref totalRows );
		}
		public static List<CodeItem> CIPS_Autocomplete( int credentialId, int headerId = 0, string keyword = "", int maxRows = 25 )
		{
			return CodesManager.CIPS_Autocomplete( headerId, keyword, maxRows );
		}
		public static List<CodeItem> CIPS_Categories()
		{
			return CodesManager.CIPS_Categories();
		}
		/// <summary>
		/// Get all CIPs groups
		/// </summary>
		/// <returns></returns>
		public static MC.Enumeration CIPS_Categories_Enumeration()
		{
			var data = CodesManager.CIPS_Categories();
			var result = new MC.Enumeration()
			{
				Id = 23,
				Name = "Classification of Instructional Programs (CIP)",
				Items = ConvertCodeItemsToEnumeratedItems( data )
			};
			return result;
		}
		public static MC.Enumeration CIPS_CategoriesInUse_Enumeration( int entityTypeId )
		{
			//show all until the custom one is fixed
			//var data1 = CodesManager.CIPS_Categories();
			var data = CodesManager.CIPS_CategoriesInUse( entityTypeId );
			
			var result = new MC.Enumeration()
			{
				Id = 23,
				Name = "Classification of Instructional Programs (CIP)",
				Items = ConvertCodeItemsToEnumeratedItems( data )
			};
			return result;
		}
		#endregion
		#region Competency framework
		//public static MC.Enumeration CompetencyFrameworks()
		//{
		//	//return CodesManager.CompetencyFrameworks_GetAll();
		//	var data = CodesManager.CompetencyFrameworks_GetAll();
		//	var result = new MC.Enumeration()
		//	{
		//		Id = 11,
		//		Name = "Competency Frameworks",
		//		Items = ConvertCodeItemsToEnumeratedItems( data )
		//	};
		//	return result;
		//}
		#endregion
		#region Helpers
		public static List<MC.EnumeratedItem> ConvertCodeItemsToEnumeratedItems( List<CodeItem> input )
		{
			var output = new List<MC.EnumeratedItem>();

			foreach ( var item in input )
			{
                output.Add( new MC.EnumeratedItem()
				{
					CodeId = item.Id,
					Id = item.Id,
					Value = item.Id.ToString(),
					Name = item.Name,
					Description = item.Description,
					SchemaName = item.SchemaName,
					SortOrder = item.SortOrder,
					//not necessary here
					//ReverseTitle = item.ReverseTitle,
					//ReverseSchemaName = item.ReverseSchemaName,
					//ReverseDescription = item.ReverseDescription,
					URL = item.URL
				} );
			}

			return output;
		}
		#endregion
	}
}