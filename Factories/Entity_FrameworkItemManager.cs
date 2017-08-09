﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Models;
using Models.Common;
using MC = Models.Common;
using Models.ProfileModels;
using Data;
using Utilities;
using DBentity = Data.Entity_FrameworkItem;
using DBRefentity = Data.Entity_Reference;
using Data.Views;
using ViewContext = Data.Views.CTIEntities1;
namespace Factories
{
	public class Entity_FrameworkItemManager : BaseFactory
	{
		static string thisClassName = "Entity_FrameworkItemManager";

		/// <summary>
		/// Add a Entity framework Item
		/// </summary>
		/// <param name="parentEntityId"></param>
		/// <param name="categoryId"></param>
		/// <param name="codeID"></param>
		/// <param name="userId"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public int ItemAdd( int parentEntityId, int categoryId, int codeID, int userId, ref string statusMessage )
		{

			DBentity efEntity = new DBentity();
			//Entity_Summary e = EntityManager.GetDBEntityByBaseId( parentId, entityTypeId );

			using ( var context = new Data.CTIEntities() )
			{
				try
				{
					//first ensure not a duplicate (until interface/search prevents dups)
					EnumeratedItem entity = ItemGet( parentEntityId, categoryId, codeID );
					if ( entity != null && entity.Id > 0 )
					{
						statusMessage = "Error: the selected code already exists!";
						return 0;
					}
					efEntity.EntityId = parentEntityId;
					efEntity.CategoryId = categoryId;
					efEntity.CodeId = codeID;
					efEntity.CreatedById = userId;

					efEntity.Created = System.DateTime.Now;

					context.Entity_FrameworkItem.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						statusMessage = "successful";
						//TODO could get the entity for use in activity logging, etc?
						return efEntity.Id;
					}
					else
					{
						//?no info on error
						statusMessage = "Error - the add was not successful. ";
						string message = string.Format( thisClassName + ".ItemAdd Failed", "Attempted to add a credential framework item. The process appeared to not work, but was not an exception, so we have no message, or no clue. parentId: {0}, createdById: {1}, CategoryId: {2}, CodeId: {3}", parentEntityId, userId, categoryId, codeID );
						EmailManager.NotifyAdmin( thisClassName + ".ItemAdd Failed", message );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".ItemAdd(), parentId: {0}, createdById: {1}, CategoryId: {2}, CodeId: {3}", parentEntityId, userId, categoryId, codeID ) );
					statusMessage = ex.Message + ( ex.InnerException != null && ex.InnerException.InnerException != null ? " - " + ex.InnerException.InnerException.Message : "" );
				}
			}

			return efEntity.Id;
		}

		public bool ItemDelete( int recordId, ref string statusMessage )
		{
			bool isOK = true;
			using ( var context = new Data.CTIEntities() )
			{
				DBentity p = context.Entity_FrameworkItem.FirstOrDefault( s => s.Id == recordId );
				if ( p != null && p.Id > 0 )
				{
					statusMessage = thisClassName + string.Format( ".ItemDelete. Deleting codeID: {0} of categoryID: {1} from ParentId: {2}", p.CodeId, p.CategoryId, p.EntityId );
					context.Entity_FrameworkItem.Remove( p );
					int count = context.SaveChanges();
				}
				else
				{
					statusMessage = string.Format( "Entity_FrameworkItem record was not found: {0}", recordId );
					isOK = false;
				}
			}
			return isOK;

		}

		/// <summary>
		/// Get a list of framework items generically
		/// </summary>
		/// <param name="recordIds"></param>
		/// <returns></returns>
		public static List<EnumeratedItem> ItemsGet( List<int> recordIds )
		{
			var items = new List<EnumeratedItem>();
			using ( var context = new ViewContext() )
			{
				List<Entity_FrameworkItemSummary> entities = context.Entity_FrameworkItemSummary
					.Where( s => recordIds.Contains( s.Id ) )
					.OrderBy( s => s.CategoryId).ThenBy(s => s.Title)
					.ToList();
				foreach ( var entity in entities )
				{
					items.Add( new EnumeratedItem()
					{
						Id = entity.Id,
						ParentId = (int)entity.ParentId,
						CodeId = entity.CodeId,
						URL = entity.URL,
						Value = entity.FrameworkCode,
						Name = entity.Title,
						Description = entity.Description,
						SchemaName = entity.SchemaName,
						ItemSummary = entity.FrameworkCode + " - " + entity.Title
					} );
				}
			}
			return items;
		}


		/// <summary>
		/// Get a generic version of a framework item
		/// </summary>
		/// <param name="recordId"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public static EnumeratedItem ItemGet( int recordId )
		{
			EnumeratedItem item = new EnumeratedItem();
			using ( var context = new ViewContext() )
			{
				Entity_FrameworkItemSummary entity = context.Entity_FrameworkItemSummary.FirstOrDefault( s => s.Id == recordId );
				if ( entity != null && entity.Id > 0 )
				{
					item.Id = entity.Id;
					item.ParentId = (int) entity.ParentId;
					item.CodeId = entity.CodeId;
					item.URL = entity.URL;
					item.Value = entity.FrameworkCode;
					item.Name = entity.Title;
					item.Description = entity.Description;
					item.SchemaName = entity.SchemaName;

					item.ItemSummary = entity.FrameworkCode + " - " + item.Name;
				}
				else
				{
					item.Name = "Not Found";
				}

			}
			return item;

		}

		public static EnumeratedItem ItemGet( int parentEntityId, int categoryId, int codeID )
		{
			EnumeratedItem item = new EnumeratedItem();
			using ( var context = new ViewContext() )
			{
				Entity_FrameworkItemSummary entity = context.Entity_FrameworkItemSummary.FirstOrDefault( s => s.EntityId == parentEntityId
							&& s.CategoryId == categoryId 
							&& s.CodeId == codeID );
				if ( entity != null && entity.Id > 0 )
				{
					item.Id = entity.Id;
					item.ParentId = ( int ) entity.ParentId;
					item.CodeId = entity.CodeId;
					item.URL = entity.URL;
					item.Value = entity.FrameworkCode;
					item.Name = entity.Title;
					item.Description = entity.Description;
					item.SchemaName = entity.SchemaName;

					item.ItemSummary = entity.FrameworkCode + " - " + item.Name;
				}
				else
				{
					item.Name = "Not Found";
				}

			}
			return item;

		}

		//public static void Item_FillOccupations( Models.Common.Credential to )
		//{
		//	to.Occupation = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_SOC );

		//	to.Occupation.ParentId = to.Id;


		//	to.Occupation.Items = new List<EnumeratedItem>();
		//	EnumeratedItem item = new EnumeratedItem();

		//	using ( var context = new ViewContext() )
		//	{
		//		List<Credential_OccupationCodesSummary> results = context.Credential_OccupationCodesSummary
		//			.Where( s => s.CredentialId == to.Id && s.CategoryId == CodesManager.PROPERTY_CATEGORY_SOC )
		//							.OrderBy( s => s.OnetSocCode )
		//							.ToList();

		//		if ( results != null && results.Count > 0 )
		//		{
		//			foreach ( Credential_OccupationCodesSummary entity in results )
		//			{
		//				item = new EnumeratedItem();
		//				item.Id = entity.Id;
		//				item.ParentId = entity.CredentialId;
		//				item.CodeId = entity.CodeId;
		//				item.URL = entity.URL;
		//				item.Value = entity.OnetSocCode;
		//				item.Name = entity.Title;
		//				item.Description = entity.Description;
		//				item.SchemaName = entity.SchemaName;

		//				item.Selected = true;

		//				item.ItemSummary = item.Name + " (" + entity.OnetSocCode + ")";
		//				to.Occupation.Items.Add( item );

		//			}
		//		}

		//		//Other parts
		//	}

		//}

		#region Entity property read ===================
		public static Enumeration FillEnumeration( Guid parentUid, int categoryId )
		{
			Enumeration entity = new Enumeration();
			entity = CodesManager.GetEnumeration( categoryId );

			entity.Items = new List<EnumeratedItem>();
			EnumeratedItem item = new EnumeratedItem();

			using ( var context = new ViewContext() )
			{
				List<Entity_FrameworkItemSummary> results = context.Entity_FrameworkItemSummary
					.Where( s => s.EntityUid == parentUid
						&& s.CategoryId == categoryId )
					.OrderBy( s => s.FrameworkCode )
					.ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( Entity_FrameworkItemSummary prop in results )
					{

						item = new EnumeratedItem();
						item.Id = prop.Id;
						item.ParentId = (int)prop.ParentId;
						if ( entity.ParentId == 0 )
							entity.ParentId = item.ParentId;

						item.CodeId = prop.CodeId;
						item.URL = prop.URL;
						item.Value = prop.FrameworkCode;
						item.Name = prop.Title;
						item.Description = prop.Description;
						item.SchemaName = prop.SchemaName;

						item.Selected = true;

						item.ItemSummary = item.Name + " (" + item.Value + ")";
						entity.Items.Add( item );

					}
				}
				
				return entity;
			}
		}

		public static List<TextValueProfile> GetAll_Other( Guid parentUid, int categoryId )
		{
			TextValueProfile entity = new TextValueProfile();
			List<TextValueProfile> list = new List<TextValueProfile>();
			MC.Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}
			try
			{
				using ( var context = new Data.CTIEntities() )
				{
					var query = from Q in context.Entity_Reference
							.Where( s => s.EntityId == parent.Id && s.CategoryId == categoryId )
								select Q;
					if ( categoryId == CodesManager.PROPERTY_CATEGORY_SUBJECT
					  || categoryId == CodesManager.PROPERTY_CATEGORY_KEYWORD )
					{
						query = query.OrderBy( p => p.TextValue );
					}
					else
					{
						query = query.OrderBy( p => p.Id );
					}
					var count = query.Count();
					var results = query.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBRefentity item in results )
						{
							entity = new TextValueProfile();
							ToMap( item, entity );

							list.Add( entity );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Entity_GetAll" );
			}
			return list;
		}//
		private static void ToMap( DBRefentity from, TextValueProfile to )
		{
			to.Id = from.Id;
			to.ParentId = from.EntityId;
			to.TextTitle = from.Title;
			to.CategoryId = from.CategoryId;
			if ( to.CategoryId == CodesManager.PROPERTY_CATEGORY_PHONE_TYPE )
			{
				to.TextValue = PhoneNumber.DisplayPhone( from.TextValue );
			}
			else if ( to.CategoryId == CodesManager.PROPERTY_CATEGORY_ORGANIZATION_IDENTIFIERS )
			{
				to.TextValue = from.TextValue;
			}
			else
			{
				to.TextValue = from.TextValue;
			}

			//if ( from.Codes_PropertyValue != null)
			//	to.CodeTitle = from.Codes_PropertyValue.Title;
			to.CodeId = ( int ) ( from.PropertyValueId ?? 0 );

			to.ProfileSummary = to.TextTitle + " - " + to.TextValue;
			if ( from.Codes_PropertyValue != null
				&& from.Codes_PropertyValue.Title != null )
			{
				to.CodeTitle = from.Codes_PropertyValue.Title;
				to.CodeSchema = from.Codes_PropertyValue.SchemaName ?? "";

				//to.ProfileSummary += " (" + from.Codes_PropertyValue.Title + ")";
				to.ProfileSummary = from.Codes_PropertyValue.Title + " - " + to.TextValue;
				//to.TextTitle = from.Codes_PropertyValue.Title;
				//just in case
				if ( to.CodeId == 0 )
					to.CodeId = from.Codes_PropertyValue.Id;
			}


			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;
			to.CreatedById = from.CreatedById == null ? 0 : ( int ) from.CreatedById;
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime ) from.LastUpdated;
			to.LastUpdatedById = from.LastUpdatedById == null ? 0 : ( int ) from.LastUpdatedById;


		}

		#endregion
	}
}
