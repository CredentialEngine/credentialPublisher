﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Models.ProfileModels;
using Models.Common;
using MN = Models.Node;
using EM = Data;
using Views = Data.Views;
using ViewContext = Data.Views.CTIEntities1;
using Utilities;

using ThisEntity = Models.ProfileModels.OrganizationRoleProfile;
using DBentity = Data.Views.Entity_AgentRelationshipIdCSV;
using DBentitySummary = Data.Views.Entity_Relationship_AgentSummary;

namespace Factories
{
	public class Entity_AgentRelationshipManager : BaseFactory
	{
		/// <summary>
		/// Entity_AgentRelationshipManager
		/// The entity is acted upon by the agent. ex
		/// Credential accredited by an agent
		///		Entity: credential ??? by Agent: org
		///	Org accredits another org (entity)
		///		Entity: target org ?? by Agent: current org
		///	Org is accredited by another org
		///		Entity: current org ?? by Agent: entered org
		/// </summary>
		string thisClassname = "Entity_AgentRelationshipManager";

		#region role type constants
		public static int ROLE_TYPE_AccreditedBy = 1;
		public static int ROLE_TYPE_ApprovedBy = 2;
		public static int ROLE_TYPE_OWNER = 6;
		public static int ROLE_TYPE_OFFERED_BY = 7;
		public static int ROLE_TYPE_RecognizedBy = 10;
		public static int ROLE_TYPE_DEPARTMENT = 20;
		public static int ROLE_TYPE_SUBSIDIARY = 21;
		#endregion


		#region context valid roles constants
		//todo make table driven
		public static string VALID_ROLES_OWNER = "2,6,7,10,11,";
		public static string VALID_ROLES_QA = "1,2,10,11,";
		public static string VALID_ROLES_ORG_QA = "1,2,10,";
		public static string VALID_ROLES_OFFERED_BY = "7,";
		//Entity_AgentRelationshipManager.VALID_ROLES_OWNER
		#endregion
		#region roles persistance ==================

		public bool Agent_EntityRoles_Save( OrganizationRoleProfile profile,
					string contextRoles,
					int userId,
					ref List<string> messages )
		{
			bool isValid = true;
			string statusMessage = "";
			int msgCount = messages.Count;

			//not sure if will user isParentActor - will start with assuming parent is the recipient/acted upon
			bool isParentActor = false;
			bool isInverseRole = !isParentActor;

			if ( !IsValidGuid( profile.ParentUid ) )
			{
				messages.Add( "Error: the parent identifier was not provided." );
			}

			if ( messages.Count > msgCount )
				return false;
			//validate and get all roles
			List<OrganizationRoleProfile> list = FillAllOrgRoles( profile, ref messages, ref isValid );
			if ( messages.Count > msgCount )
				return false;

			//17-03-20 re: requirement to show all asset roles at credential level
			//	- need to handle this new approach and old approach, as still likely for orgs
			//	- need to establish the subset of vald roleIds, so as not to delete roles from other contexts
			int parentEntityId = 0;
			Entity parent = EntityManager.GetEntity( profile.ParentUid );
			//profile.ActedUponEntityId
			Entity actedUponEntity = EntityManager.GetEntity( profile.ActedUponEntityId );
			if ( actedUponEntity != null && actedUponEntity.Id > 0 )
				parentEntityId = actedUponEntity.Id;
			else
				parentEntityId = parent.Id;

			using ( var context = new EM.CTIEntities() )
			{
				//get all existing roles for the parent
				//will need some context here
				//also why it may be good to keep credential QA actions separate
				var results = GetAllRolesForAgent( profile.ParentUid, profile.ActingAgentUid, isParentActor );

				#region deletes/updates check

				var deleteList = from existing in results
								 join item in list
								 on new { existing.ActingAgentUid, existing.RoleTypeId }
								 equals new { item.ActingAgentUid, item.RoleTypeId }
								 into joinTable
								 from result in joinTable.DefaultIfEmpty( new OrganizationRoleProfile { ActingAgentId = 0, ActingAgentUid = Guid.NewGuid(), ParentId = 0, Id = 0 } )
								 select new { ActingAgentUid = existing.ActingAgentUid, DeleteId = existing.Id, ItemId = ( result.RoleTypeId ), IsInverseRole = result.IsInverseRole };

				foreach ( var v in deleteList )
				{

					if ( v.ItemId == 0 )
					{
						//the item to be deleted must be in the context
						
						//delete item
						if ( Delete( v.DeleteId, contextRoles, ref statusMessage ) == false )
						{
							messages.Add( statusMessage );
							isValid = false;
						}

					}
				}
				#endregion

				#region new items
				//should only empty ids, where not in current list, so should be adds
				var newList = from item in list
							  join existing in results
									 on new { item.ActingAgentUid, item.RoleTypeId }
								 equals new { existing.ActingAgentUid, existing.RoleTypeId }
									into joinTable
							  from addList in joinTable.DefaultIfEmpty( new OrganizationRoleProfile { Id = 0, ActingAgentId = 0, ActingAgentUid = Guid.NewGuid(), RoleTypeId = 0 } )
							  select new { ActingAgentUid = item.ActingAgentUid, RoleTypeId = item.RoleTypeId, ExistingId = addList.Id };
				foreach ( var v in newList )
				{
					if ( v.ExistingId == 0 )
					{
						bool isEmpty = false;
						if ( Add( parentEntityId,
									profile.ActingAgentUid,
									v.RoleTypeId,
									profile.ActedUponEntityId,
									isInverseRole,
									userId,
									ref messages,
									ref isEmpty ) == 0 )
						{
							if ( !isEmpty )
								isValid = false;
						}

					}
				}
				#endregion
			}
			return isValid;
		}
		/// <summary>
		/// Retrieve all existing roles for a parent and agent - only used for Agent_EntityRoles_Save
		/// </summary>
		/// <param name="pParentUid"></param>
		/// <param name="agentUid"></param>
		/// <param name="isParentActor"></param>
		/// <returns></returns>
		private static List<OrganizationRoleProfile> GetAllRolesForAgent( Guid pParentUid, Guid agentUid, bool isParentActor = false )
		{
			//If parent is actor, then this is a direct role. 
			//for ex. if called from assessments, then it is inverse, as the parent is the assessment, and the relate org is the actor
			bool isInverseRole = !isParentActor;

			OrganizationRoleProfile p = new OrganizationRoleProfile();
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
			List<Views.Entity_Relationship_AgentSummary> roles = new List<Views.Entity_Relationship_AgentSummary>();
			using ( var context = new ViewContext() )
			{
				roles = context.Entity_Relationship_AgentSummary
						.Where( s => s.SourceEntityUid == pParentUid
							&& s.ActingAgentUid == agentUid
							&& s.IsInverseRole == isInverseRole )
						.ToList();

				foreach ( Views.Entity_Relationship_AgentSummary entity in roles )
				{
					p = new OrganizationRoleProfile();
					MapTo( entity, p );

					list.Add( p );
				}

			}
			return list;

		} //
		private static void MapTo( Views.Entity_Relationship_AgentSummary from, ThisEntity to )
		{

			to.Id = from.EntityAgentRelationshipId;
			to.RowId = from.RowId;

			to.ParentId = from.EntityId;
			//to.ParentUid = from.SourceEntityUid;
			to.ParentTypeId = from.SourceEntityTypeId;
			to.ActingAgentUid = from.ActingAgentUid;
			//useful for compare when doing deletes, and New checks
			to.ActingAgentId = from.AgentRelativeId;

			to.ActingAgent = new Organization()
			{
				Id = from.AgentRelativeId,
				RowId = from.ActingAgentUid,
				Name = from.AgentName,
				SubjectWebpage = from.AgentUrl,
				Description = from.AgentDescription,
				ImageUrl = from.AgentImageUrl,
				CTID = from.CTID
			};

			to.RoleTypeId = from.RelationshipTypeId;

			string relation = "";
			if ( from.SourceToAgentRelationship != null )
			{
				relation = from.SourceToAgentRelationship;
			}
			to.IsInverseRole = from.IsInverseRole ?? false;

			to.ProfileSummary = from.AgentName;
			//can only use a detail summary where only one relationship exists!!
			//to.ProfileSummary = string.Format( "{0} is a {1} of {2}", from.AgentName, relation, from.SourceEntityName );
			to.ProfileName = to.ProfileSummary;

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;
			to.CreatedById = from.CreatedById;
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime ) from.LastUpdated;
			to.LastUpdatedById = from.LastUpdatedById;

		}

		private List<OrganizationRoleProfile> FillAllOrgRoles( OrganizationRoleProfile profile,
			ref List<string> messages,
			ref bool isValid)
		{
			isValid = false;

			OrganizationRoleProfile entity = new OrganizationRoleProfile();
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
			if ( !IsValidGuid( profile.ParentUid ) )
			{
				//roles, no agent
				messages.Add( "Invalid request, the parent entity was not provided." );
				return list;
			}
			if ( !IsGuidValid( profile.ActingAgentUid ) )
			{
				//roles, no agent
				messages.Add( "Invalid request, please select an agent for selected roles." );
				return list;
			}
			if ( profile.AgentRole == null || profile.AgentRole.Items.Count == 0 )
			{
				messages.Add( "Invalid request, please select one or more roles for this selected agent." );
				return list;
			}

			//loop thru the roles
			foreach ( EnumeratedItem e in profile.AgentRole.Items )
			{
				entity = new OrganizationRoleProfile();
				entity.ParentId = profile.ParentId;				
				entity.ActingAgentUid = profile.ActingAgentUid;
				entity.ActingAgentId = profile.ActingAgentId;
				entity.RoleTypeId = e.Id;
				entity.IsInverseRole = profile.IsInverseRole;

				list.Add( entity );
			}
		
			
			isValid = true;
			return list;
		}
		public bool Entity_AgentRole_SaveSingleRole( OrganizationRoleProfile profile,
					int userId,
					ref List<string> messages )
		{
			bool isValid = true;
			
			if ( !IsValidGuid( profile.ParentUid ) )
			{
				messages.Add( "Error: the parent identifier was not provided." );
			}

			if ( messages.Count > 0 )
				return false;

			if ( profile == null )
			{
				return false;
			}

			Entity parent = EntityManager.GetEntity( profile.ParentUid );
			using ( var context = new EM.CTIEntities() )
			{

				int roleId = profile.RoleTypeId;
				int entityId = parent.Id;
				if ( profile.Id > 0 )
				{
					EM.Entity_AgentRelationship p = context.Entity_AgentRelationship.FirstOrDefault( s => s.Id == profile.Id );
					if ( p != null && p.Id > 0 )
					{
						//p.ParentUid = parentUid;
						if ( roleId == 0 )
						{
							isValid = false;
							messages.Add( "Error: a role was not entered. Select a role and try again. " );
							return false;
						}
						if ( !IsValidGuid( profile.ActingAgentUid ) )
						{
							isValid = false;
							messages.Add( "Error: an agent was not selected. Select an agent and try again. " );
							return false;
						}
						p.RelationshipTypeId = roleId;

						//p.URL = profile.Url;
						p.Description = profile.Description;
						p.EntityId = parent.Id;

						if ( HasStateChanged( context ) )
						{
							p.LastUpdated = System.DateTime.Now;
							p.LastUpdatedById = userId;
							context.SaveChanges();
						}
					}
					else
					{
						//error should have been found
						isValid = false;
						messages.Add( string.Format( "Error: the requested role was not found: recordId: {0}", profile.Id ) );
					}
				}
				else
				{
					if ( !IsValidGuid( profile.ActingAgentUid ) && profile.ActingAgentId > 0 )
					{
						//NOTE - need to handle agent!!!
						Organization org = OrganizationManager.GetForSummary( profile.ActingAgentId );
						if ( org == null || org.Id == 0 )
						{
							isValid = false;
							messages.Add( string.Format( "Error: the selected organization was not found: {0}", profile.ActingAgentId ) );
							return false;
						}
						profile.ActingAgentUid = org.RowId;
					}
					bool isEmpty = false;
					profile.Id = Add( entityId, 
										profile.ActingAgentUid, 
										roleId,
										profile.ActedUponEntityId,
										profile.IsInverseRole, 
										userId, 
										ref messages, 
										ref isEmpty );
					if ( profile.Id == 0 )
						isValid = false;

				}
			}
			return isValid;
		}

		public int Add( int entityId, Guid agentUid
					, int roleId
					, int actedUponEntityId
					, bool isInverseRole, int userId, ref List<string> messages, 
					ref bool isEmpty )
		{
			int newId = 0;
			//assume if all empty, then ignore
			if ( entityId == 0 && !IsValidGuid( agentUid ) )
			{
				return newId;
			}
			if ( !IsValidGuid( agentUid ) && roleId == 0 )
			{
				return newId;
			}
			//
			if ( IsValidGuid( agentUid ) && roleId == 0 )
			{
				messages.Add( "Error: invalid request, please select a role." );
				return 0;
			}
			else if ( !IsValidGuid( agentUid ) && roleId > 0 )
			{
				messages.Add( "Error: invalid request, please select an agent." );
				return 0;
			}
			//TODO - update this method
			if ( AgentEntityRoleExists( entityId, agentUid, roleId ))
			{
				messages.Add( "Error: the selected relationship already exists!" );
				return 0;
			}
			//TODO - need to handle agent
			MN.ProfileLink org = OrganizationManager.Agent_GetProfileLink( agentUid );
			if ( org == null || org.Name.Length == 0 )
			{
				messages.Add( "Error: the selected agent was not found!" );
				LoggingHelper.DoTrace( 5, thisClassname + string.Format( ".Entity_AgentRole_Add the agent was not found, for entityId: {0}, AgentId:{1}, RoleId: {2}", entityId, agentUid, roleId ) );
				return 0;
			}

			using ( var context = new EM.CTIEntities() )
			{
				//add
				EM.Entity_AgentRelationship car = new EM.Entity_AgentRelationship();

				car.EntityId = entityId;

				//TODO - may not be needed anymore
				car.ActedUponEntityId = actedUponEntityId;
				car.AgentUid = agentUid;
				car.RelationshipTypeId = roleId;
				car.IsInverseRole = isInverseRole;

				car.Created = System.DateTime.Now;
				car.CreatedById = userId;
				car.LastUpdated = System.DateTime.Now;
				car.LastUpdatedById = userId;
				car.RowId = Guid.NewGuid();
				context.Entity_AgentRelationship.Add( car );

				// submit the change to database
				int count = context.SaveChanges();
				newId = car.Id;
			}

			return newId;
		}

		/// <summary>
		/// Delete a single role
		/// </summary>
		/// <param name="recordId"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int recordId, string contextRoles, ref string statusMessage )
		{
			bool isValid = true;

			using ( var context = new EM.CTIEntities() )
			{
				if ( recordId == 0 )
				{
					statusMessage = "Error - missing an identifier for the Entity-Agent Role";
					return false;
				}

				EM.Entity_AgentRelationship efEntity =
					context.Entity_AgentRelationship.SingleOrDefault( s => s.Id == recordId );
				if ( efEntity != null && efEntity.Id > 0 )
				{
					if ( contextRoles.IndexOf( efEntity.RelationshipTypeId.ToString() ) > -1 )
					{
						context.Entity_AgentRelationship.Remove( efEntity );
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							isValid = true; 
						}
					}
					else
					{
						if ( efEntity.RelationshipTypeId != 6 )
						{
							//add logging to ensure all properly covered
							LoggingHelper.DoTrace( 2, string.Format( "Entity_AgentRelationshipManager.EntityAgentRole_Delete Request to delete recordId: {0}, with roleContext of: {1}, and relationtypeId: {2} was not allowed. EntityBaseId was: {3}", recordId, contextRoles, efEntity.RelationshipTypeId, ( efEntity.Entity.EntityBaseId ?? 0 ) ) );
						}
					}
				}
				else
				{
					statusMessage = string.Format( "Agent role record was not found: {0}", recordId );
					isValid = false;
				}
			}

			return isValid;
		}

		/// <summary>
		/// Delete a single row for a parent entity and agent
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="agentUid">Handles a nullable Guid (ex for an owing organzation)</param>
		/// <param name="roleId"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( Guid parentUid, Guid? agentUid, int roleId, ref string statusMessage )
		{
			Guid current = ( Guid ) agentUid;
			return Delete( parentUid, current, roleId, ref statusMessage );
		}
		/// <summary>
		/// Delete a single row for a parent entity and agent
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="agentUid"></param>
		/// <param name="roleId"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( Guid parentUid, Guid agentUid, int roleId, ref string statusMessage )
		{
			bool isValid = false;
			Entity parent = EntityManager.GetEntity( parentUid );

			using ( var context = new EM.CTIEntities() )
			{
				if ( roleId == 0 || !IsValidGuid( parentUid ) || !IsValidGuid( agentUid ) )
				{
					statusMessage = "Error - missing identifiers, please provide proper keys.";
					return false;
				}

				EM.Entity_AgentRelationship efEntity =
					context.Entity_AgentRelationship.FirstOrDefault( s => s.EntityId == parent.Id
						&& s.AgentUid == agentUid
						&& s.RelationshipTypeId == roleId );
				if ( efEntity != null && efEntity.Id > 0 )
				{
					statusMessage = string.Format( "Removed Role of {0} from {1} #{2}", efEntity.Codes_CredentialAgentRelationship.Title, efEntity.Entity.Codes_EntityType.Title, efEntity.Entity.EntityBaseId );

					context.Entity_AgentRelationship.Remove( efEntity );

					int count = context.SaveChanges();
					if ( count > 0 )
					{
						isValid = true;
					}
				}
				else
				{
					statusMessage = string.Format( "Agent role record was not found: {0}", roleId );
					isValid = false;
				}
			}

			return isValid;
		}
		/// <summary>
		/// Delete all roles for the provided entityId (parent) and agent combination.
		/// Note: this should be inverse relationships, but we don't have direct at this time
		/// </summary>
		/// <param name="entityId"></param>
		/// <param name="agentUid"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int entityId, Guid agentUid, ref string statusMessage )
		{
			bool isValid = true;

			using ( var context = new EM.CTIEntities() )
			{
				if ( entityId == 0 || !IsValidGuid( agentUid ) )
				{
					statusMessage = "Error - missing identifiers, please provide proper keys.";
					return false;
				}

				context.Entity_AgentRelationship.RemoveRange( context.Entity_AgentRelationship.Where( s => s.EntityId == entityId && s.AgentUid == agentUid ) );
				int count = context.SaveChanges();
				if ( count > 0 )
				{
					isValid = true;
				}
				else
				{
					//this can happen initially where a role was visible, and is not longer
					statusMessage = string.Format( "Warning Delete failed, Agent role record(s) were not found for: entityId: {0}, agentUid: {1}", entityId, agentUid );
					isValid = false;
				}
			}

			return isValid;
		}
		/// <summary>
		/// Delete all records for a parent (typically due to delete of parent)
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool EntityAgentRole_DeleteAll( Guid parentUid, ref string statusMessage )
		{
			bool isValid = false;

			using ( var context = new EM.CTIEntities() )
			{
				if ( parentUid.ToString().IndexOf( "0000" ) == 0 )
				{
					statusMessage = "Error - missing an identifier for the Parent Entity";
					return false;
				}
				Entity parent = EntityManager.GetEntity( parentUid );
				List<EM.Entity_AgentRelationship> list =
					context.Entity_AgentRelationship
						.Where( s => s.EntityId == parent.Id )
						.ToList();
				//don't need this way
				if ( list != null && list.Count > 0 )
				{
					//context.Entity_AgentRelationship.Remove( efEntity );
					context.Entity_AgentRelationship.RemoveRange( context.Entity_AgentRelationship.Where( s => s.EntityId == parent.Id ) );
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						isValid = true;
					}
				}

			}

			return isValid;
		}
		#endregion


		#region == retrieval ==================
		private static bool AgentEntityRoleExists( int entityId, Guid agentUid, int roleId )
		{
			EntityAgentRelationship item = new EntityAgentRelationship();
			using ( var context = new EM.CTIEntities() )
			{
				EM.Entity_AgentRelationship entity = context.Entity_AgentRelationship.FirstOrDefault( s => s.EntityId == entityId
						&& s.AgentUid == agentUid
						&& s.RelationshipTypeId == roleId );
				if ( entity != null && entity.Id > 0 )
				{
					return true;
				}
			}
			return false;
		}
		private static bool AgentEntityRoleExists( Guid pParentUid, Guid agentUid, int roleId )
		{
			EntityAgentRelationship item = new EntityAgentRelationship();
			Entity parent = EntityManager.GetEntity( pParentUid );
			using ( var context = new EM.CTIEntities() )
			{
				EM.Entity_AgentRelationship entity = context.Entity_AgentRelationship.FirstOrDefault( s => s.EntityId == parent.Id
						&& s.AgentUid == agentUid
						&& s.RelationshipTypeId == roleId );
				if ( entity != null && entity.Id > 0 )
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Determine if any Actor roles exist for the provided agent identifier.
		/// Typically used before a delete
		/// </summary>
		/// <param name="agentUid"></param>
		/// <returns></returns>
		public static bool AgentEntityHasRoles( Guid agentUid, ref int roleCount )
		{
			roleCount = 0;
			using ( var context = new EM.CTIEntities() )
			{
				List<EM.Entity_AgentRelationship> list = context.Entity_AgentRelationship
						.Where( s => s.AgentUid == agentUid )
						.ToList();
				if ( list != null && list.Count > 0 )
				{
					roleCount = list.Count;
					return true;
				}
			}
			return false;
		}
		

		/// <summary>
		/// Get summary version of roles (using CSV) - for use in lists
		/// It will not return org to org roles like departments, and subsiduaries
		/// </summary>
		/// <param name="pParentUid"></param>
		/// <param name="isParentActor"></param>
		/// <returns></returns>
		public static List<OrganizationRoleProfile> AgentEntityRole_GetAllSummary( Guid pParentUid, bool isParentActor = false )
		{
			//If parent is actor, then this is a direct role. 
			//for ex. if called from assessments, then it is inverse, as the parent is the assessment, and the relate org is the actor
			bool isInverseRole = !isParentActor;

			OrganizationRoleProfile orp = new OrganizationRoleProfile();
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
			Entity parent = EntityManager.GetEntity( pParentUid );
			using ( var context = new ViewContext() )
			{
				List<DBentity> agentRoles = context.Entity_AgentRelationshipIdCSV
					.Where( s => s.EntityId == parent.Id
						 && s.IsInverseRole == isInverseRole )
					.ToList();
				foreach ( DBentity entity in agentRoles )
				{
					
					orp = new OrganizationRoleProfile();

					//warning for purposes of the editor, need to set the agent/object id to the orgId, and rowId from the org
					orp.Id = entity.AgentRelativeId;
					orp.RowId = ( Guid ) entity.AgentUid;

					//parent, ex credential, assessment, or org in org-to-org
					//Hmm should this be the entityId - need to be consistant
					orp.ParentId = entity.EntityId;
					//orp.ParentUid = entity.ParentUid;  //this the entityUid
					//orp.ParentTypeId = entity.ParentTypeId; //this is wrong, it is the parent of the entity

					//useful for compare when doing deletes, and New checks
					orp.ActingAgentUid = entity.AgentUid;
					orp.ActingAgentId = entity.AgentRelativeId;
					orp.ProfileName = entity.AgentName;

					//may be included now, but with addition of person, and use of agent, it won't
					
					orp.ActingAgent = new Organization()
					{
						Id = entity.AgentRelativeId,
						RowId = orp.ActingAgentUid,
						Name = entity.AgentName,
						SubjectWebpage = entity.AgentUrl,
						Description = entity.AgentDescription,
						ImageUrl = entity.AgentImageUrl
					};
					
					//don't need actual roles for summary, but including
					orp.AllRoleIds = entity.RoleIds;
					orp.AllRoles = entity.Roles.TrimEnd( ',', ' ', '\n' );
					//could include roles in profile summary??, particularly if small)

					orp.ProfileSummary = entity.AgentName + " {" + orp.AllRoles + "}";
					list.Add( orp );
				}

				if ( list.Count > 0 )
				{
					var Query = ( from roles in list.OrderBy( p => p.ProfileSummary )
								  select roles ).ToList();
					list = Query;
					//var Query = from roles in credential.OrganizationRole select roles;
					//Query = Query.OrderBy( p => p.ProfileSummary );
					//credential.OrganizationRole = Query.ToList();
				}
			}
			return list;

		} //

		/// <summary>
		/// Get All AgentEntity roles for the target entity - except where agent is the owner for the entity.
		/// Each OrganizationRoleProfile has a list of the roles, not one role per profile
		/// </summary>
		/// <param name="pParentUid"></param>
		/// <param name="owningAgentUid"></param>
		/// <param name="excludingIfOfferedOnly">If true, and the only role is offeredBy, do not include</param>
		/// <param name="isParentActor"></param>
		/// <returns>Summary list of agents, with list of roles in entity</returns>
		public static List<OrganizationRoleProfile> AgentEntityRole_GetAllExceptOwnerSummary( Guid pParentUid, Guid owningAgentUid, bool excludingIfOfferedOnly, bool isParentActor = false )
		{
			//If parent is actor, then this is a direct role. 
			//for ex. if called from assessments, then it is inverse, as the parent is the assessment, and the relate org is the actor
			bool isInverseRole = !isParentActor;

			OrganizationRoleProfile orp = new OrganizationRoleProfile();
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
			Entity parent = EntityManager.GetEntity( pParentUid );

			using ( var context = new ViewContext() )
			{
				List<DBentity> agentRoles = context.Entity_AgentRelationshipIdCSV
					.Where( s => s.EntityId == parent.Id
						 && s.AgentUid != owningAgentUid
						 && s.IsInverseRole == isInverseRole )
					.ToList();
				foreach ( DBentity entity in agentRoles )
				{

					orp = new OrganizationRoleProfile();

					//warning for purposes of the editor, need to set the agent/object id to the orgId, and rowId from the org
					orp.Id = entity.AgentRelativeId;
					orp.RowId = ( Guid ) entity.AgentUid;

					//parent, ex credential, assessment, or org in org-to-org
					//Hmm should this be the entityId - need to be consistant
					orp.ParentId = entity.EntityId;

					//useful for compare when doing deletes, and New checks
					orp.ActingAgentUid = entity.AgentUid;
					orp.ActingAgentId = entity.AgentRelativeId;
					orp.ProfileName = entity.AgentName;

					//may be included now, but with addition of person, and use of agent, it won't

					orp.ActingAgent = new Organization()
					{
						Id = entity.AgentRelativeId,
						RowId = orp.ActingAgentUid,
						Name = entity.AgentName,
						SubjectWebpage = entity.AgentUrl,
						Description = entity.AgentDescription,
						ImageUrl = entity.AgentImageUrl
					};

					//don't need actual roles for summary, but including
					//skip if only offered by
					if ( excludingIfOfferedOnly == false || entity.RoleIds != "7")
					{
						orp.AllRoleIds = entity.RoleIds;
						orp.AllRoles = entity.Roles;
						//could include roles in profile summary??, particularly if small)

						orp.ProfileSummary = entity.AgentName;
						list.Add( orp );
					} 
					
				}

				if ( list.Count > 0 )
				{
					var Query = ( from roles in list.OrderBy( p => p.ProfileSummary )
								  select roles ).ToList();
					list = Query;
					//var Query = from roles in credential.OrganizationRole select roles;
					//Query = Query.OrderBy( p => p.ProfileSummary );
					//credential.OrganizationRole = Query.ToList();
				}
			}
			return list;

		} //


		/// <summary>
		/// Get All QA Roles for all assets for a credential
		/// </summary>
		/// <param name="pParentUid"></param>
		/// <returns></returns>
		public static List<ThisEntity> CredentialAssets_GetAllQARoles( int credentialId )
		{

			OrganizationRoleProfile orp = new OrganizationRoleProfile();
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();

			using ( var context = new ViewContext() )
			{

				List<Views.Credential_Assets_AgentRelationship_Totals> agentRoles = context.Credential_Assets_AgentRelationship_Totals
					.Where( s => s.CredentialId == credentialId && s.QaCount > 0)
					.ToList();

				return CredentialAssets_AgentRelationship_Fill( agentRoles );
			}

		} //

		/// <summary>
		/// Get offered by roles for the credential, and all related assets.
		/// </summary>
		/// <param name="credentialId"></param>
		/// <returns></returns>
		public static List<OrganizationRoleProfile> CredentialAssets_GetAllOfferedByRoles( int credentialId )
		{

			OrganizationRoleProfile orp = new OrganizationRoleProfile();
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();

			using ( var context = new ViewContext() )
			{
				List<Views.Credential_Assets_AgentRelationship_Totals> agentRoles = context.Credential_Assets_AgentRelationship_Totals
					.Where( s => s.CredentialId == credentialId && s.OfferedCount > 0 )
					.ToList();

				return CredentialAssets_AgentRelationship_Fill( agentRoles );
			}
		} //
		public static List<Organization> GetAllOfferingOrgs( Guid parentUid )
		{

			Organization org = new Organization();
			List<Organization> list = new List<Organization>();

			using ( var context = new ViewContext() )
			{
				List<Views.Entity_Relationship_AgentSummary> agentRoles = context.Entity_Relationship_AgentSummary
					.Where( s => s.SourceEntityUid == parentUid
					&& s.RelationshipTypeId == ROLE_TYPE_OFFERED_BY )
					.ToList();
				foreach ( Views.Entity_Relationship_AgentSummary item in agentRoles)
				{
					org = new Organization();
					org.Id = item.AgentRelativeId;
					org.Name = item.AgentName;
					org.RowId = item.ActingAgentUid;
					org.Description = item.AgentDescription;
					org.CTID = item.CTID;
					list.Add( org );
				}
				return list;
			}
		} //



		private static List<OrganizationRoleProfile> CredentialAssets_AgentRelationship_Fill ( List<Views.Credential_Assets_AgentRelationship_Totals> agentRoles )
		{
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
			OrganizationRoleProfile orp = new OrganizationRoleProfile();
			foreach ( Views.Credential_Assets_AgentRelationship_Totals entity in agentRoles )
			{

				orp = new OrganizationRoleProfile();

				//WARNING for purposes of the editor, need to set the agent/object id to the orgId, and rowId from the org
				orp.Id = entity.AgentRelativeId;
				orp.RowId = entity.AgentUid;

				//parent, ex credential, assessment, or org in org-to-org
				//Hmm should this be the entityId - need to be consistant
				orp.ParentId = entity.AssetEntityId;
				orp.ParentType = entity.EntityType;
				orp.ParentName = entity.EntityBaseName;
				orp.ProfileName = entity.AgentName; //agent name

				orp.ActedUponEntityUid = entity.EntityUid;
				orp.ActedUponEntityId = entity.AssetEntityId;
				orp.ActedUponEntity = new Entity()
				{
					Id = entity.AssetEntityId,
					RowId = entity.EntityUid,
					EntityBaseName = entity.EntityBaseName
				};

				//useful for compare when doing deletes, and New checks
				orp.ActingAgentUid = entity.AgentUid;
				orp.ActingAgentId = entity.AgentRelativeId;
				orp.Description = entity.Description;

				//may be included now, but with addition of person, and use of agent, it won't

				orp.ActingAgent = new Organization()
				{
					Id = entity.AgentRelativeId,
					RowId = orp.ActingAgentUid,
					Name = entity.AgentName//,
										   //SubjectWebpage = entity.AgentUrl
				};

				orp.ProfileName = orp.ProfileSummary = entity.EntityType + " - " + entity.AgentName;
				list.Add( orp );
			}
			if ( list.Count > 0 )
			{
				var Query = ( from roles in list.OrderBy( p => p.ProfileSummary )
							  select roles ).ToList();
				list = Query;
				//var Query = from roles in credential.OrganizationRole select roles;
				//Query = Query.OrderBy( p => p.ProfileSummary );
				//credential.OrganizationRole = Query.ToList();
			}
		
			return list;
		}
		public static List<OrganizationRoleProfile> AgentEntityRole_GetOwnerSummary( Guid pParentUid, Guid owningAgentUid, bool isParentActor = false )
		{
			//If parent is actor, then this is a direct role. 
			//for ex. if called from assessments, then it is inverse, as the parent is the assessment, and the relate org is the actor
			bool isInverseRole = !isParentActor;

			OrganizationRoleProfile orp = new OrganizationRoleProfile();
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
			Entity parent = EntityManager.GetEntity( pParentUid );

			using ( var context = new ViewContext() )
			{
				List<DBentity> agentRoles = context.Entity_AgentRelationshipIdCSV
					.Where( s => s.EntityId == parent.Id 
							&& s.AgentUid == owningAgentUid
							&& s.IsInverseRole == isInverseRole )
					.ToList();
				foreach ( DBentity entity in agentRoles )
				{

					orp = new OrganizationRoleProfile();

					//warning for purposes of the editor, need to set the agent/object id to the orgId, and rowId from the org
					orp.Id = entity.AgentRelativeId;
					orp.RowId = ( Guid ) entity.AgentUid;

					//parent, ex credential, assessment, or org in org-to-org
					//Hmm should this be the entityId - need to be consistant
					orp.ParentId = entity.EntityId;
					//orp.ParentUid = entity.ParentUid;  //this the entityUid
					//orp.ParentTypeId = entity.ParentTypeId; //this is wrong, it is the parent of the entity

					//useful for compare when doing deletes, and New checks
					orp.ActingAgentUid = entity.AgentUid;
					orp.ActingAgentId = entity.AgentRelativeId;
					orp.ProfileName = entity.AgentName;

					//may be included now, but with addition of person, and use of agent, it won't

					orp.ActingAgent = new Organization()
					{
						Id = entity.AgentRelativeId,
						RowId = orp.ActingAgentUid,
						Name = entity.AgentName,
						SubjectWebpage = entity.AgentUrl,
						Description = entity.AgentDescription,
						ImageUrl = entity.AgentImageUrl
					};

					//don't need actual roles for summary, but including
					orp.AllRoleIds = entity.RoleIds;
					orp.AllRoles = entity.Roles;
					//could include roles in profile summary??, particularly if small)

					orp.ProfileSummary = entity.AgentName;
					list.Add( orp );
				}

				if ( list.Count > 0 )
				{
					var Query = ( from roles in list.OrderBy( p => p.ProfileSummary )
								  select roles ).ToList();
					list = Query;
					//var Query = from roles in credential.OrganizationRole select roles;
					//Query = Query.OrderBy( p => p.ProfileSummary );
					//credential.OrganizationRole = Query.ToList();
				}
			}
			return list;

		} //



		public static OrganizationRoleProfile AgentEntityRole_GetAsEnumerationFromCSV( Guid pParentUid, Guid agentUid, bool isInverseRole = true )
		{
			OrganizationRoleProfile orp = new OrganizationRoleProfile();
			
			Entity parent = EntityManager.GetEntity( pParentUid );

			using ( var context = new ViewContext() )
			{
				//there can be inconsistancies, resulting in more than one.
				//So use a list, and log/send email
				List<DBentity> agentRoles = context.Entity_AgentRelationshipIdCSV
					.Where( s => s.EntityId == parent.Id
						 && s.AgentUid == agentUid )
					.ToList();

				DBEntity_Fill( orp, agentRoles, true );

			}
			return orp;

		} //

		private static void DBEntity_Fill( OrganizationRoleProfile orp, List<DBentity> agentRoles, bool fillingEnumerations = true )
		{
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
			EnumeratedItem eitem = new EnumeratedItem();
			foreach ( DBentity entity in agentRoles )
			{

				//warning for purposes of the editor, need to set the agent/object id to the orgId, and rowId from the org
				orp.Id = entity.AgentRelativeId;
				orp.RowId = ( Guid ) entity.AgentUid;

				//parent, ex credential, assessment, or org in org-to-org
				orp.ParentId = entity.EntityId;
				//orp.ParentUid = entity.ParentUid;
				//orp.ParentTypeId = parent.EntityTypeId;

				orp.ActedUponEntityUid = entity.EntityUid;
				orp.ActedUponEntityId = entity.EntityId;
				orp.ActedUponEntity = new Entity()
				{
					Id = entity.EntityId,
					RowId = entity.EntityUid,
					EntityBaseName = entity.EntityBaseName
				};


				orp.ActingAgentUid = entity.AgentUid;
				orp.ActingAgentId = entity.AgentRelativeId;

				//TODO - do we still need this ==> YES
				orp.ActingAgent = new Organization()
				{
					Id = entity.AgentRelativeId,
					RowId = orp.ActingAgentUid,
					Name = entity.AgentName,
					SubjectWebpage = entity.AgentUrl,
					Description = entity.AgentDescription,
					ImageUrl = entity.AgentImageUrl
				};

				orp.ProfileSummary = entity.AgentName;
				orp.ProfileName = entity.AgentName;

				orp.AgentRole = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ENTITY_AGENT_ROLE );
				orp.AgentRole.ParentId = ( int ) entity.EntityBaseId;


				orp.AgentRole.Items = new List<EnumeratedItem>();
				if ( fillingEnumerations )
				{
					string[] roles = entity.RoleIds.Split( ',' );

					foreach ( string role in roles )
					{
						eitem = new EnumeratedItem();
						//??
						eitem.Id = int.Parse( role );
						//not used here
						eitem.RecordId = int.Parse( role );
						eitem.CodeId = int.Parse( role );
						eitem.Value = role.Trim();

						eitem.Selected = true;
						orp.AgentRole.Items.Add( eitem );
					}
				}
				
				//}
				if ( agentRoles.Count > 1 )
				{
					//log an exception
					//==>NO, there can be multiples with the new format, until stabalized. ex. Owned by, offered by, a QA role
					LoggingHelper.LogError( string.Format( "Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumeration. Multiple records found where one expected. entity.BaseId: {0}, entity.ParentTypeId: {1}, entity.AgentRelativeId: {2}", entity.EntityBaseId, entity.EntityTypeId, entity.AgentRelativeId ), true );
				}
				//break;
			}
		}

		/// <summary>
		/// Get all entity agent roles as an enumeration - uses the summary CSV view
		/// </summary>
		/// <param name="pParentUid"></param>
		/// <param name="isInverseRole"></param>
		/// <returns></returns>
		//public static List<OrganizationRoleProfile> AgentEntityRole_GetAll_AsEnumeration( Guid pParentUid, bool isInverseRole = true )
		//{
		//	OrganizationRoleProfile orp = new OrganizationRoleProfile();
		//	List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
		//	EnumeratedItem eitem = new EnumeratedItem();
		//	Entity parent = EntityManager.GetEntity( pParentUid );

		//	using ( var context = new ViewContext() )
		//	{
		//		List<DBentity> agentRoles = context.Entity_AgentRelationshipIdCSV
		//			.Where( s => s.EntityId == parent.Id
		//				 && s.IsInverseRole == isInverseRole )
		//			.ToList();

		//		foreach ( DBentity entity in agentRoles )
		//		{
		//			orp = new OrganizationRoleProfile();
		//			orp.Id = 0;
		//			orp.ParentId = entity.EntityId;
		//			//orp.ParentUid = entity.ParentUid;
		//			orp.ParentTypeId = parent.EntityTypeId;

		//			orp.ActedUponEntityUid = entity.EntityUid;
		//			orp.ActedUponEntityId = entity.EntityId;
		//			orp.ActedUponEntity = new Entity()
		//			{
		//				Id = entity.EntityId,
		//				RowId = entity.EntityUid,
		//				EntityBaseName = entity.EntityBaseName
		//			};

		//			orp.ActingAgentUid = entity.AgentUid;
		//			orp.ActingAgentId = entity.AgentRelativeId;
		//			orp.ActingAgent = new Organization()
		//			{
		//				Id = entity.AgentRelativeId,
		//				RowId = entity.AgentUid,
		//				Name = entity.AgentName,
		//				SubjectWebpage = entity.AgentUrl,
		//				Description = entity.AgentDescription,
		//				ImageUrl = entity.AgentImageUrl
		//			};

		//			orp.ProfileSummary = entity.EntityBaseName;

		//			orp.AgentRole = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ENTITY_AGENT_ROLE );
		//			orp.AgentRole.ParentId = (int)entity.EntityBaseId;
		//			orp.AgentRole.Items = new List<EnumeratedItem>();
		//			string[] roles = entity.RoleIds.Split( ',' );

		//			foreach ( string role in roles )
		//			{
		//				eitem = new EnumeratedItem();
		//				//??
		//				eitem.Id = int.Parse( role );
		//				//not used here
		//				eitem.RecordId = int.Parse( role );
		//				eitem.CodeId = int.Parse( role );
		//				eitem.Value = role.Trim();

		//				eitem.Selected = true;
		//				//don't have this from the csv list
		//				//if ( ( bool ) role.IsQARole )
		//				//{
		//				//	eitem.IsSpecialValue = true;
		//				//	if ( IsDevEnv() )
		//				//		eitem.Name += " (QA)";
		//				//}

		//				orp.AgentRole.Items.Add( eitem );

		//			}

		//			list.Add( orp );
		//			if ( list.Count > 0 )
		//			{
		//				var Query = ( from items in list.OrderBy( p => p.ProfileSummary )
		//							  select items ).ToList();
		//				list = Query;
		//			}
		//		}

		//	}
		//	return list;

		//} //

		/// <summary>
		/// Get all roles for an entity. 
		/// The flat roles (one entity - role - agent per record) are read and returned as enumerations - fully filled out
		/// </summary>
		/// <param name="pParentUid"></param>
		/// <param name="isInverseRole"></param>
		/// <returns></returns>
		public static List<OrganizationRoleProfile> AgentEntityRole_GetAll_ToEnumeration( Guid pParentUid, bool isInverseRole )
		{
			OrganizationRoleProfile orp = new OrganizationRoleProfile();
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
			EnumeratedItem eitem = new EnumeratedItem();
			int prevAgentId = 0;
			Entity parent = EntityManager.GetEntity( pParentUid );

			using ( var context = new ViewContext() )
			{
				//order by type, name (could have duplicate names!), then relationship
				List<DBentitySummary> agentRoles = context.Entity_Relationship_AgentSummary
					.Where( s => s.EntityId == parent.Id
						 && s.IsInverseRole == isInverseRole )
						 .OrderBy( s => s.ActingAgentEntityType )
						 .ThenBy( s => s.AgentName ).ThenBy( s => s.AgentRelativeId )
						 .ThenBy( s => s.SourceToAgentRelationship )
					.ToList();

				foreach ( DBentitySummary entity in agentRoles )
				{
					//loop until change in agent
					if ( prevAgentId != entity.AgentRelativeId )
					{
						//handle previous fill
						if ( prevAgentId > 0)
							list.Add( orp );

						prevAgentId = entity.AgentRelativeId;

						orp = new OrganizationRoleProfile();
						orp.Id = 0;
						orp.ParentId = entity.EntityId;
						//orp.ParentUid = entity.SourceEntityUid;
						orp.ParentTypeId = parent.EntityTypeId;

						orp.ActingAgentUid = entity.ActingAgentUid;
						orp.ActingAgentId = entity.AgentRelativeId;
						orp.ActingAgent = new Organization()
						{
							Id = entity.AgentRelativeId,
							RowId = entity.ActingAgentUid,
							Name = entity.AgentName,
							SubjectWebpage = entity.AgentUrl,
							Description = entity.AgentDescription,
							ImageUrl = entity.AgentImageUrl,
							CTID = entity.CTID
						};

						orp.ProfileSummary = entity.AgentName;

						orp.AgentRole = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ENTITY_AGENT_ROLE );
						orp.AgentRole.ParentId = entity.AgentRelativeId;

						orp.AgentRole.Items = new List<EnumeratedItem>();
					}
					

					eitem = new EnumeratedItem();
					//??
					eitem.Id = entity.EntityAgentRelationshipId;
					eitem.RowId = entity.RowId.ToString();
					//not used here
					eitem.RecordId = entity.EntityAgentRelationshipId;
					eitem.CodeId = entity.RelationshipTypeId;
					//???
					//eitem.Value = entity.RelationshipTypeId.ToString();
					//WARNING - the code table uses Accredited by as the title and the latter is actually the reverse (using our common context), so we need to reverse the returned values here 
					if ( !isInverseRole )
					{
						eitem.Name = entity.AgentToSourceRelationship;
						eitem.SchemaName = entity.ReverseSchemaTag;

						eitem.ReverseTitle = entity.SourceToAgentRelationship;
						eitem.ReverseSchemaName = entity.SchemaTag;
					}
					else
					{
						eitem.Name = entity.SourceToAgentRelationship;
						eitem.SchemaName = entity.SchemaTag;

						eitem.ReverseTitle = entity.AgentToSourceRelationship;
						eitem.ReverseSchemaName = entity.ReverseSchemaTag;
					}
					//TODO - if needed	
					//eitem.Description = entity.RelationshipDescription;

					eitem.Selected = true;
					if ( ( bool ) entity.IsQARole )
					{
						eitem.IsSpecialValue = true;
						if ( IsDevEnv() )
							eitem.Name += " (QA)";
					}

					orp.AgentRole.Items.Add( eitem );

				}
				//check for remaining
				if ( prevAgentId > 0 )
					list.Add( orp );

				if ( list.Count > 0 )
				{
					var Query = ( from items in list.OrderBy( p => p.ProfileSummary )
								  select items ).ToList();
					list = Query;
				}

			}
			return list;

		} //

		/// <summary>
		/// Get all QA targets for an agent
		/// </summary>
		/// <param name="agentUid"></param>
		/// <param name="isInverseRole"></param>
		/// <returns></returns>
		public static List<OrganizationRoleProfile> GetAll_QATargets_ForAgent( Guid agentUid )
		{
			OrganizationRoleProfile orp = new OrganizationRoleProfile();
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
			EnumeratedItem eitem = new EnumeratedItem();

			Guid prevTargetUid = new Guid();
			Entity agentEntity = EntityManager.GetEntity( agentUid );

			using ( var context = new ViewContext() )
			{
				List<DBentitySummary> agentRoles = context.Entity_Relationship_AgentSummary
					.Where( s => s.ActingAgentUid == agentUid
						 && s.IsQARole == true )
						 .OrderBy( s => s.SourceEntityTypeId )
						 .ThenBy( s => s.SourceEntityName )
						 .ThenBy( s => s.AgentToSourceRelationship )
					.ToList();

				foreach ( DBentitySummary entity in agentRoles )
				{
					//loop until change in entity type?
					if ( prevTargetUid != entity.SourceEntityUid )
					{
						//handle previous fill
						if ( IsGuidValid( prevTargetUid ))
							list.Add( orp );

						prevTargetUid = entity.SourceEntityUid;

						orp = new OrganizationRoleProfile();
						orp.Id = 0;
						orp.ParentId = agentEntity.Id;
						orp.ParentTypeId = agentEntity.EntityTypeId;

						//should not be necessary, but could leave?
						orp.ActingAgentUid = entity.ActingAgentUid;
						orp.ActingAgentId = entity.AgentRelativeId;
						orp.ActingAgent = new Organization()
						{
							Id = entity.AgentRelativeId,
							RowId = entity.ActingAgentUid,
							Name = entity.AgentName,
							SubjectWebpage = entity.AgentUrl,
							Description = entity.AgentDescription,
							ImageUrl = entity.AgentImageUrl,
							CTID = entity.CTID
						};
						//??
						orp.ProfileSummary = entity.AgentName;

						orp.AgentRole = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ENTITY_AGENT_ROLE );
						orp.AgentRole.ParentId = entity.AgentRelativeId;

						orp.AgentRole.Items = new List<EnumeratedItem>();

						if (entity.SourceEntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL)
						{
							orp.TargetCredential.Id = entity.SourceEntityBaseId;
							orp.TargetCredential.RowId = entity.SourceEntityUid;
							orp.TargetCredential.Name = entity.SourceEntityName;
							orp.TargetCredential.Description = entity.SourceEntityDescription;
							orp.TargetCredential.SubjectWebpage = entity.SourceEntityUrl;
							orp.TargetCredential.ImageUrl = entity.SourceEntityImageUrl;


							orp.TargetCredential.CredentialType = EntityPropertyManager.FillEnumeration( orp.TargetCredential.RowId, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_TYPE );

							orp.TargetCredential.AudienceLevelType = EntityPropertyManager.FillEnumeration( orp.TargetCredential.RowId, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL );
						}
						else if ( entity.SourceEntityTypeId == CodesManager.ENTITY_TYPE_ORGANIZATION )
						{
							orp.TargetOrganization.Id = entity.SourceEntityBaseId;
							orp.TargetOrganization.RowId = entity.SourceEntityUid;
							orp.TargetOrganization.Name = entity.SourceEntityName;
							orp.TargetOrganization.Description = entity.SourceEntityDescription;
							orp.TargetOrganization.SubjectWebpage = entity.SourceEntityUrl;
							orp.TargetOrganization.ImageUrl = entity.SourceEntityImageUrl;
						}
						else if ( entity.SourceEntityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
						{
							orp.TargetAssessment.Id = entity.SourceEntityBaseId;
							orp.TargetAssessment.RowId = entity.SourceEntityUid;
							orp.TargetAssessment.Name = entity.SourceEntityName;
							orp.TargetAssessment.Description = entity.SourceEntityDescription;
							orp.TargetAssessment.SubjectWebpage = entity.SourceEntityUrl;
							//orp.TargetAssessment.ImageUrl = entity.SourceEntityImageUrl;
						}
						else if ( entity.SourceEntityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
						{
							orp.TargetLearningOpportunity.Id = entity.SourceEntityBaseId;
							orp.TargetLearningOpportunity.RowId = entity.SourceEntityUid;
							orp.TargetLearningOpportunity.Name = entity.SourceEntityName;
							orp.TargetLearningOpportunity.Description = entity.SourceEntityDescription;
							orp.TargetLearningOpportunity.SubjectWebpage = entity.SourceEntityUrl;
							//orp.TargetLearningOpportunity.ImageUrl = entity.SourceEntityImageUrl;
						}
					}


					eitem = new EnumeratedItem();
					//??
					eitem.Id = entity.EntityAgentRelationshipId;
					eitem.RowId = entity.RowId.ToString();
					//not used here
					eitem.RecordId = entity.EntityAgentRelationshipId;
					eitem.CodeId = entity.RelationshipTypeId;
					//???
					//eitem.Value = entity.RelationshipTypeId.ToString();
					//WARNING - the code table uses Accredited by as the title and the latter is actually the reverse (using our common context), so we need to reverse the returned values here 
					
					eitem.Name = entity.AgentToSourceRelationship;
					eitem.SchemaName = entity.ReverseSchemaTag;
					
					//TODO - if needed	
					//eitem.Description = entity.RelationshipDescription;
					//??
					eitem.Selected = true;
					if ( ( bool ) entity.IsQARole )
					{
						eitem.IsSpecialValue = true;
						if ( IsDevEnv() )
							eitem.Name += " (QA)";
					}

					orp.AgentRole.Items.Add( eitem );

				}
				//check for remaining
				if ( IsGuidValid( prevTargetUid ) )
					list.Add( orp );

				if ( list.Count > 0 )
				{
					//prob not necessary
					//var Query = ( from items in list.OrderBy( p => p.ProfileSummary )
					//			  select items ).ToList();
					//list = Query;
				}

			}
			return list;

		} //

		/// <summary>
		/// Get all departments and subsiduaries for the parent org
		/// NOTE: the parent org is the agent in the relationships. The parent adds the child to the relationship, so the child is the entity, and the parent is the agent
		/// </summary>
		/// <param name="pParentUid"></param>
		/// <param name="roleTypeId">If zero, get both otherwise get specific roles</param>
		/// <returns></returns>
		public static void AgentRole_FillAllSubOrganizations( Organization parent, int roleTypeId, bool forEditView )
		{
			OrganizationRoleProfile p = new OrganizationRoleProfile();
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
			parent.OrganizationRole_Dept = new List<OrganizationRoleProfile>();
			parent.OrganizationRole_Subsidiary = new List<OrganizationRoleProfile>();
			List<Views.Entity_Relationship_AgentSummary> roles = new List<DBentitySummary>();
			List<Views.Entity_Relationship_AgentSummary> inverseRoles = new List<DBentitySummary>();
			EnumeratedItem eitem = new EnumeratedItem();
			using ( var context = new ViewContext() )
			{
				//not sure if there will be a difference
				if ( forEditView )
				{
					//SourceEntityUid is OK
					roles = context.Entity_Relationship_AgentSummary
						.Where( s => s.SourceEntityUid == parent.RowId
							 && (
									( roleTypeId == 0
									&& ( s.RelationshipTypeId == ROLE_TYPE_DEPARTMENT || s.RelationshipTypeId == ROLE_TYPE_SUBSIDIARY ) )
								|| ( s.RelationshipTypeId == roleTypeId )
								)
							 )
							 .OrderBy( s => s.RelationshipTypeId ).ThenBy( s => s.AgentName )
						.ToList();
				}
				else
				{
					roles = context.Entity_Relationship_AgentSummary
						.Where( s => s.SourceEntityUid == parent.RowId
							 && (
									( roleTypeId == 0
									&& ( s.RelationshipTypeId == ROLE_TYPE_DEPARTMENT || s.RelationshipTypeId == ROLE_TYPE_SUBSIDIARY ) )
								|| ( s.RelationshipTypeId == roleTypeId )
								)
							 )
							 .OrderBy( s => s.RelationshipTypeId ).ThenBy( s => s.AgentName )
						.ToList();

				}

				foreach ( Views.Entity_Relationship_AgentSummary entity in roles )
				{
					p = new OrganizationRoleProfile();
					p.Id = entity.EntityAgentRelationshipId;

					p.RoleTypeId = entity.RelationshipTypeId;
					string relation = "";
					if ( entity.SourceToAgentRelationship != null )
					{
						relation = entity.AgentToSourceRelationship;
					}
					p.IsInverseRole = entity.IsInverseRole ?? false;
					if ( p.IsInverseRole )
						relation = entity.SourceToAgentRelationship;

					//HACK ALERT
					//reversing the parent and agent for display
					//16-10-31 mp - works for display, but still wrong for edit.
					//for edit, the acting agent is shown, and should be the parent entity being acted upon
					if ( forEditView )
					{
						p.ParentId = entity.EntityId;
						//p.ParentUid = entity.SourceEntityUid;
						p.ParentTypeId = entity.SourceEntityTypeId;

						p.ActingAgentUid = entity.ActingAgentUid;
						p.ActingAgent = new Organization()
						{
							Id = entity.AgentRelativeId,
							RowId = entity.ActingAgentUid,
							Name = entity.AgentName,
							SubjectWebpage = entity.AgentUrl,
							Description = entity.AgentDescription,
							ImageUrl = entity.AgentImageUrl,
							CTID = entity.CTID
						};

						p.ProfileSummary = string.Format( "{0} {1} {2}", entity.SourceEntityName, relation, entity.AgentName );
					}
					else
					{


						p.ParentId = entity.EntityId;
						//p.ParentUid = entity.SourceEntityUid;
						p.ParentTypeId = entity.SourceEntityTypeId;

						p.ActingAgentUid = entity.ActingAgentUid;
						p.ActingAgent = new Organization()
						{
							Id = entity.AgentRelativeId,
							RowId = entity.ActingAgentUid,
							Name = entity.AgentName,
							SubjectWebpage = entity.AgentUrl,
							Description = entity.AgentDescription,
							ImageUrl = entity.AgentImageUrl
						};

						p.ProfileSummary = string.Format( "{0} {1} {2}", entity.SourceEntityName, relation, entity.AgentName );
					}

					if ( entity.RelationshipTypeId == ROLE_TYPE_DEPARTMENT )
					{
						parent.OrganizationRole_Dept.Add( p );
					}
					else if ( entity.RelationshipTypeId == ROLE_TYPE_SUBSIDIARY )
					{
						parent.OrganizationRole_Subsidiary.Add( p );
					}
					if ( !forEditView )
					{
						
						//OR
						p.AgentRole = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ENTITY_AGENT_ROLE );
						p.AgentRole.ParentId = entity.AgentRelativeId;

						p.AgentRole.Items = new List<EnumeratedItem>();
						eitem = new EnumeratedItem();
						eitem.Id = entity.EntityAgentRelationshipId;
						eitem.RowId = entity.RowId.ToString();
						//not used here
						eitem.RecordId = entity.EntityAgentRelationshipId;
						eitem.CodeId = entity.RelationshipTypeId;
						if ( !p.IsInverseRole )
						{
							eitem.Name = entity.AgentToSourceRelationship;
							eitem.SchemaName = entity.ReverseSchemaTag;
						}
						else
						{
							eitem.Name = entity.SourceToAgentRelationship;
							eitem.SchemaName = entity.SchemaTag;
						}
						//TODO - if needed	
						//eitem.Description = entity.RelationshipDescription;

						eitem.Selected = true;
						if ( ( bool ) entity.IsQARole )
						{
							eitem.IsSpecialValue = true;
							if ( IsDevEnv() )
								eitem.Name += " (QA)";
						}

						p.AgentRole.Items.Add( eitem );

						parent.OrganizationRole_Recipient.Add( p );
					}
					//list.Add( p );
				}

			}
			//return list;

		} //

		/// <summary>
		/// Get Parent organization
		/// The dept/subsiduaries are handled by roles. 
		/// Whereever the interface creates a relationship, the current context (ex credential) is the parent, or source, and the selected agent is the acting agent. The opposite is actually true for depts/subs, but the same appoach was used. 
		/// Any code doing retrieval must accomodate this condition
		/// 
		/// </summary>
		/// <param name="child">Child org</param>
		/// <param name="forEditView"></param>
		public static void AgentRole_GetParentOrganization( Organization child, bool forEditView )
		{
			OrganizationRoleProfile p = new OrganizationRoleProfile();
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();

			child.ParentOrganizations = new List<OrganizationRoleProfile>();

			List<Views.Entity_Relationship_AgentSummary> roles = new List<DBentitySummary>();

			EnumeratedItem eitem = new EnumeratedItem();
			using ( var context = new ViewContext() )
			{
				//not sure if there will be a difference
				if ( forEditView )
				{
					//SourceEntityUid is OK
					roles = context.Entity_Relationship_AgentSummary
						.Where( s => s.ActingAgentUid == child.RowId
							 && ( s.RelationshipTypeId == ROLE_TYPE_DEPARTMENT || s.RelationshipTypeId == ROLE_TYPE_SUBSIDIARY ) 
							 )
							 .OrderBy( s => s.RelationshipTypeId ).ThenBy( s => s.AgentName )
						.ToList();
				}
				else
				{
					roles = context.Entity_Relationship_AgentSummary
						.Where( s => s.ActingAgentUid == child.RowId
							 && ( s.RelationshipTypeId == ROLE_TYPE_DEPARTMENT || s.RelationshipTypeId == ROLE_TYPE_SUBSIDIARY )
							 )
							 .OrderBy( s => s.RelationshipTypeId ).ThenBy( s => s.AgentName )
						.ToList();

				}

				foreach ( Views.Entity_Relationship_AgentSummary entity in roles )
				{
					p = new OrganizationRoleProfile();
					p.Id = entity.EntityAgentRelationshipId;

					p.RoleTypeId = entity.RelationshipTypeId;
					string relation = "";
					//Department of 
					if ( entity.SourceToAgentRelationship != null )
					{
						relation = entity.SourceToAgentRelationship;
					}
					p.IsInverseRole = entity.IsInverseRole ?? false;
					

					//HACK ALERT???
					//reversing the parent and agent for display
					//16-10-31 mp - works for display, but still wrong for edit.
					//for edit, the acting agent is shown, and should be the parent entity being acted upon
					if ( forEditView )
					{
						p.ParentId = entity.EntityId;
						p.ParentTypeId = entity.SourceEntityTypeId;

						//ActingAgent is the parent org, but comes from the source
						p.ActingAgentUid = entity.ActingAgentUid;
						p.ActingAgent = new Organization()
						{
							Id = entity.SourceEntityBaseId,
							RowId = entity.SourceEntityUid,
							Name = entity.SourceEntityName,
							SubjectWebpage = entity.SourceEntityUrl,
							Description = entity.SourceEntityDescription,
							ImageUrl = entity.SourceEntityImageUrl,
							CTID = entity.CTID
						};

						p.ProfileSummary = string.Format( "{0} is {1} {2}", entity.AgentName, relation, p.ActingAgent.Name );
					}
					else
					{
						p.ParentId = entity.EntityId;
						p.ParentTypeId = entity.SourceEntityTypeId;

						//ActingAgent is the parent org, but comes from the source
						p.ActingAgentUid = entity.ActingAgentUid;
						p.ActingAgent = new Organization()
						{
							Id = entity.SourceEntityBaseId,
							RowId = entity.SourceEntityUid,
							Name = entity.SourceEntityName,
							SubjectWebpage = entity.SourceEntityUrl,
							Description = entity.SourceEntityDescription,
							ImageUrl = entity.SourceEntityImageUrl
						};

						p.ProfileSummary = string.Format( "{0} is {1} {2}", entity.AgentName, relation, p.ActingAgent.Name );
					}

					child.ParentOrganizations.Add( p );

				}

			}

		} //

		#endregion

		#region CREDENTIAL relationships
		/// <summary>
		/// Get total count of credentials where the provided org is the creator
		/// </summary>
		/// <param name="orgUid"></param>
		/// <returns></returns>
		public static int CredentialCount_ForOwningOrg( Guid orgUid )
		{
			int count = 0;
			using ( var context = new Data.CTIEntities() )
			{
				var creds = from cred in context.Credential
							join entity in context.Entity
							on cred.RowId equals entity.EntityUid
							join agent in context.Entity_AgentRelationship
							on entity.Id equals agent.EntityId
							where cred.StatusId <= CodesManager.ENTITY_STATUS_PUBLISHED
								&& agent.AgentUid == orgUid
								&& agent.RelationshipTypeId == 6
							select cred;
				var results = creds.ToList();

				if ( results != null && results.Count > 0 )
				{
					count = results.Count;
				}
			}

			return count;
		}


		/// <summary>
		/// Get all credentials for the organization, and relationship
		/// 17-03-24 mp -	TODO while this should still be valid, there is now a direct relationship. We hesitate as the question has again been raised 
		///					whether there can be multiple owners.
		/// </summary>
		/// <param name="orgUid"></param>
		/// <param name="relationship">Defaults to Owned by (6)</param>
		/// <returns></returns>
		public static List<Credential> Credentials_ForOwningOrg( Guid orgUid )
		{
			List<Credential> list = new List<Credential>();
			Credential credential = new Credential();
			using ( var context = new Data.CTIEntities() )
			{
				var creds = from cred in context.Credential
							join entity in context.Entity
							on cred.RowId equals entity.EntityUid
							join agent in context.Entity_AgentRelationship
							on entity.Id equals agent.EntityId
							where cred.StatusId <= CodesManager.ENTITY_STATUS_PUBLISHED
								&& agent.AgentUid == orgUid
								&& agent.RelationshipTypeId == ROLE_TYPE_OWNER
							select cred;
				var results = creds.ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( EM.Credential item in results )
					{
						credential = new Credential();

						credential.Id = item.Id;
						credential.StatusId = ( int ) ( item.StatusId ?? 1 );
						credential.RowId = item.RowId;
						credential.Name = item.Name;
						credential.AlternateName = item.AlternateName;
						credential.SubjectWebpage = item.Url;
						credential.Description = item.Description;

						credential.CredentialType = EntityPropertyManager.FillEnumeration( credential.RowId, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_TYPE );

						credential.AudienceLevelType = EntityPropertyManager.FillEnumeration( credential.RowId, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL );

						list.Add( credential );
					}
				}
			}

			return list;
		}

		/// <summary>
		/// Get total count of credentials where the provided org is the creator
		/// </summary>
		/// <param name="orgId"></param>
		/// <returns></returns>
		//public static int CredentialCount_ForCreatingOrg(Guid orgId)
		//{
		//	int count = 0;
		//	using (var context = new Data.CTIEntities())
		//	{
		//		var creds = from cred in context.Credential
		//					join entity in context.Entity
		//					on cred.RowId equals entity.EntityUid
		//					join agent in context.Entity_AgentRelationship
		//					on entity.Id equals agent.EntityId
		//					where cred.StatusId <= CodesManager.ENTITY_STATUS_PUBLISHED
		//						&& agent.AgentUid == orgId
		//						&& agent.RelationshipTypeId == 5
		//					select cred;
		//		var results = creds.ToList();

		//		if (results != null && results.Count > 0)
		//		{
		//			count = results.Count;
		//		}
		//	}

		//	return count;
		//}

		#endregion

		#region role codes retrieval ==================
		/// <summary>
		/// Get agent to agent roles
		/// </summary>
		/// <param name="isInverseRole">false - Created by, true - created</param>
		/// <returns></returns>
		public static Enumeration GetAgentToAgentRolesCodes( bool isInverseRole = true )
		{
			Enumeration entity = new Enumeration();

			using ( var context = new EM.CTIEntities() )
			{
				EM.Codes_PropertyCategory category = context.Codes_PropertyCategory
							.SingleOrDefault( s => s.Id == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE );

				if ( category != null && category.Id > 0 )
				{
					entity.Id = category.Id;
					entity.Name = category.Title;
					entity.SchemaName = category.SchemaName;
					entity.Url = category.SchemaUrl;
					entity.Items = new List<EnumeratedItem>();

					EnumeratedItem val = new EnumeratedItem();
					//var sortedList = context.Codes_CredentialAgentRelationship
					//		.Where( s => s.IsActive == true && ( qaOnlyRoles == false || s.IsQARole == true) )
					//		.OrderBy( x => x.Title )
					//		.ToList();

					var Query = from P in context.Codes_CredentialAgentRelationship
							.Where( s => s.IsActive == true && s.IsAgentToAgentRole == true )
								select P;

					Query = Query.OrderBy( p => p.Title );
					var results = Query.ToList();

					foreach ( EM.Codes_CredentialAgentRelationship item in results )
					{
						val = new EnumeratedItem();
						ToMap( item, val, isInverseRole );
						entity.Items.Add( val );
					}

				}
			}

			return entity;
		}

		/// <summary>
		/// Get agent roles for assessments and learning opportunities
		/// </summary>
		/// <param name="isInverseRole">false - Created by, true - created</param>
		/// <returns></returns>
		//public static Enumeration GetAllOtherAgentRoles( bool isInverseRole = true )
		//{
		//	Enumeration entity = new Enumeration();

		//	using ( var context = new EM.CTIEntities() )
		//	{
		//		EM.Codes_PropertyCategory category = context.Codes_PropertyCategory
		//					.SingleOrDefault( s => s.Id == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE );

		//		if ( category != null && category.Id > 0 )
		//		{
		//			entity.Id = category.Id;
		//			entity.Name = category.Title;
		//			entity.SchemaName = category.SchemaName;
		//			entity.Url = category.SchemaUrl;
		//			entity.Items = new List<EnumeratedItem>();

		//			EnumeratedItem val = new EnumeratedItem();

		//			var Query = from P in context.Codes_CredentialAgentRelationship
		//					.Where( s => s.IsActive == true 
		//						&& s.IsAssessmentAgentRole == true )
		//						select P;

		//			Query = Query.OrderBy( p => p.Title );
		//			var results = Query.ToList();

		//			foreach ( EM.Codes_CredentialAgentRelationship item in results )
		//			{
		//				val = new EnumeratedItem();
		//				ToMap( item, val, isInverseRole );
		//				entity.Items.Add( val );
		//			}

		//		}
		//	}

		//	return entity;
		//}

		/// <summary>
		/// Get agent roles for assessments
		/// </summary>
		/// <param name="isInverseRole"></param>
		/// <returns></returns>
		public static Enumeration GetAssessmentAgentRoles( bool isInverseRole = true )
		{
			Enumeration entity = new Enumeration();

			using ( var context = new EM.CTIEntities() )
			{
				EM.Codes_PropertyCategory category = context.Codes_PropertyCategory
							.SingleOrDefault( s => s.Id == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE );

				if ( category != null && category.Id > 0 )
				{
					entity.Id = category.Id;
					entity.Name = category.Title;
					entity.SchemaName = category.SchemaName;
					entity.Url = category.SchemaUrl;
					entity.Items = new List<EnumeratedItem>();

					EnumeratedItem val = new EnumeratedItem();

					var Query = from P in context.Codes_CredentialAgentRelationship
							.Where( s => s.IsActive == true
								&& s.IsAssessmentAgentRole == true )
								select P;

					Query = Query.OrderBy( p => p.Title );
					var results = Query.ToList();

					foreach ( EM.Codes_CredentialAgentRelationship item in results )
					{
						val = new EnumeratedItem();
						ToMap( item, val, isInverseRole );
						entity.Items.Add( val );
					}

				}
			}

			return entity;
		}
		public static Enumeration GetLearningOppAgentRoles( bool isInverseRole = true )
		{
			Enumeration entity = new Enumeration();

			using ( var context = new EM.CTIEntities() )
			{
				EM.Codes_PropertyCategory category = context.Codes_PropertyCategory
							.SingleOrDefault( s => s.Id == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE );

				if ( category != null && category.Id > 0 )
				{
					entity.Id = category.Id;
					entity.Name = category.Title;
					entity.SchemaName = category.SchemaName;
					entity.Url = category.SchemaUrl;
					entity.Items = new List<EnumeratedItem>();

					EnumeratedItem val = new EnumeratedItem();

					var Query = from P in context.Codes_CredentialAgentRelationship
							.Where( s => s.IsActive == true
								&& s.IsLearningOppAgentRole == true )
								select P;

					Query = Query.OrderBy( p => p.Title );
					var results = Query.ToList();

					foreach ( EM.Codes_CredentialAgentRelationship item in results )
					{
						val = new EnumeratedItem();
						ToMap( item, val, isInverseRole );
						entity.Items.Add( val );
						//val.Id = item.Id;
						//val.CodeId = item.Id;
						//val.Value = item.Id.ToString();//????
						//val.Description = item.Description;
						//val.SchemaName = item.SchemaTag;

						//if ( isInverseRole )
						//{
						//	val.Name = item.ReverseRelation;
						//}
						//else
						//{
						//	val.Name = item.Title;
						//}

						//if ( ( bool ) item.IsQARole )
						//{
						//	val.IsSpecialValue = true;
						//	if ( IsDevEnv() )
						//		val.Name += " (QA)";
						//}
						//entity.Items.Add( val );
					}

				}
			}

			return entity;

		}
		private static void ToMap( EM.Codes_CredentialAgentRelationship from, EnumeratedItem to, bool isInverseRole = true )
		{
			to.Id = from.Id;
			to.CodeId = from.Id;
			to.Value = from.Id.ToString();//????
			to.Description = from.Description;
			to.SchemaName = from.SchemaTag;

			if ( isInverseRole )
			{
				to.Name = from.ReverseRelation;
			}
			else
			{
				to.Name = from.Title;
				//val.Description = string.Format( "{0} is {1} by this Organization ", entityType, item.Title );
			}

			if ( ( bool ) from.IsQARole )
			{
				to.IsSpecialValue = true;
				if ( IsDevEnv() )
					to.Name += " (QA)";
			}
		}
		#endregion


	}
}
