﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Configuration;
using System.Web;

using Models;
using Models.Common;
using Utilities;


namespace Factories
{
	public class BaseFactory
	{
		protected static string DEFAULT_GUID = "00000000-0000-0000-0000-000000000000";
		public string commonStatusMessage = "";

		public static bool IsDevEnv()
		{
			if ( UtilityManager.GetAppKeyValue( "envType", "no" ) == "dev" )
				return true;
			else
				return false;
		}
		#region Entity frameworks helpers
		public static bool HasStateChanged( Data.CTIEntities context )
		{
			if ( context.ChangeTracker.Entries().Any( e =>
					e.State == EntityState.Added ||
					e.State == EntityState.Modified ||
					e.State == EntityState.Deleted ) == true )
				return true;
			else
				return false;
		}

		public static string SetLastUpdatedBy( int lastUpdatedById, Data.Account accountModifier )
		{
			string lastUpdatedBy = "";
			if ( accountModifier != null )
			{
				lastUpdatedBy = accountModifier.FirstName + " " + accountModifier.LastName;
			}
			else
			{
				if ( lastUpdatedById > 0 )
				{
					AppUser user = AccountManager.AppUser_Get( lastUpdatedById );
					lastUpdatedBy = user.FullName();
				}
			}
			return lastUpdatedBy;
		}
		#endregion
		#region Database connections
		/// <summary>
		/// Get the read only connection string for the main database
		/// </summary>
		/// <returns></returns>
		public static string DBConnectionRO()
		{

			string conn = WebConfigurationManager.ConnectionStrings[ "cti_RO" ].ConnectionString;
			return conn;

		}

		#endregion

		#region data retrieval
		protected static CodeItemResult Fill_CodeItemResults( DataRow dr, string fieldName, int categoryId, bool hasSchemaName, bool hasTotals, bool hasAnIdentifer = true )
		{
			string list = GetRowPossibleColumn( dr, fieldName, "" );
			//string list = dr[ fieldName ].ToString();
			CodeItemResult item = new CodeItemResult() { CategoryId = categoryId };
			item.HasAnIdentifer = hasAnIdentifer;

			int totals = 0;
			int id = 0;
			string title = "";
			string schema = "";
			if ( !string.IsNullOrWhiteSpace( list ) )
			{

				var codeGroup = list.Split( '|' );
				foreach ( string codeSet in codeGroup )
				{
					var codes = codeSet.Split( '~' );
					schema = "";
					totals = 0;
					id = 0;
					if ( hasAnIdentifer )
					{
						Int32.TryParse( codes[ 0 ].Trim(), out id );
						title = codes[ 1 ].Trim();
						if ( hasSchemaName )
						{
							schema = codes[ 2 ];

							if ( hasTotals )
								totals = Int32.Parse( codes[ 3 ] );
						}
						else
						{
							if ( hasTotals )
								totals = Int32.Parse( codes[ 2 ] );
						}
					}
					else
					{
						//currently if no Id, assume only text value
						title = codes[ 0 ].Trim();
					}


					item.Results.Add( new Models.CodeItem() { Id = id, Title = title, SchemaName = schema, Totals = totals } );
				}
			}

			return item;
		}

		/// <summary>
		/// Helper method to retrieve a string column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRow</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>

		protected static CredentialConnectionsResult Fill_CredentialConnectionsResult( DataRow dr, string fieldName, int categoryId )
		{
			string list = dr[ fieldName ].ToString();
			CredentialConnectionsResult result = new CredentialConnectionsResult() { CategoryId = categoryId };
			CredentialConnectionItem item = new CredentialConnectionItem();
			int id = 0;

			if ( !string.IsNullOrWhiteSpace( list ) )
			{
				var codeGroup = list.Split( '|' );
				foreach ( string codeSet in codeGroup )
				{
					var codes = codeSet.Split( '~' );
					item = new CredentialConnectionItem();

					id = 0;
					Int32.TryParse( codes[ 0 ].Trim(), out id );
					item.ConnectionId = id;
					item.Connection = codes[ 1 ].Trim();
					Int32.TryParse( codes[ 2 ].Trim(), out id );
					item.CredentialId = id;
					item.Credential = codes[ 3 ].Trim();

					result.Results.Add( item );
				}
			}

			return result;
		}


		public static string GetRowColumn( DataRow row, string column, string defaultValue = "" )
		{
			string colValue = "";

			try
			{
				colValue = row[ column ].ToString();

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{
				string queryString = GetWebUrl();
				string exType = ex.GetType().ToString();
				LoggingHelper.LogError( exType + " Exception in GetRowColumn( DataRow row, string column, string defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString() + "\r\nLocation: " + queryString, true );

				colValue = defaultValue;
			}
			return colValue;

		}

		public static string GetRowPossibleColumn( DataRow row, string column, string defaultValue = "" )
		{
			string colValue = "";

			try
			{
				colValue = row[ column ].ToString();

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{

				colValue = defaultValue;
			}
			return colValue;

		}


		/// <summary>
		/// Helper method to retrieve an int column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRow</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>
		public static int GetRowColumn( DataRow row, string column, int defaultValue )
		{
			int colValue = 0;

			try
			{
				colValue = Int32.Parse( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{
				string queryString = GetWebUrl();

				LoggingHelper.LogError( "Exception in GetRowColumn( DataRow row, string column, int defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString() + "\r\nLocation: " + queryString, true );
				colValue = defaultValue;
				//throw ex;
			}
			return colValue;

		}

		public static int GetRowPossibleColumn( DataRow row, string column, int defaultValue )
		{
			int colValue = 0;

			try
			{
				colValue = Int32.Parse( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{
				colValue = defaultValue;
				//throw ex;
			}
			return colValue;

		}
		public static decimal GetRowPossibleColumn( DataRow row, string column, decimal defaultValue )
		{
			decimal colValue = 0;

			try
			{
				colValue = decimal.Parse( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{
				//string queryString = GetWebUrl();

				//LoggingHelper.LogError( "Exception in GetRowColumn( DataRow row, string column, int defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString() + "\r\nLocation: " + queryString, true );
				colValue = defaultValue;
				//throw ex;
			}
			return colValue;

		}
		public static bool GetRowColumn( DataRow row, string column, bool defaultValue )
		{
			bool colValue = false;

			try
			{
				//need to properly handle int values of 0,1, as bool
				string strValue = row[ column ].ToString();
				if ( !string.IsNullOrWhiteSpace( strValue ) && strValue.Trim().Length == 1 )
				{
					strValue = strValue.Trim();
					if ( strValue == "0" )
						return false;
					else if ( strValue == "1" )
						return true;
				}
				colValue = bool.Parse( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{
				string queryString = GetWebUrl();

				LoggingHelper.LogError( "Exception in GetRowColumn( DataRow row, string column, int defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString() + "\r\nLocation: " + queryString, true );
				colValue = defaultValue;
				//throw ex;
			}
			return colValue;

		}
		public static DateTime GetRowColumn( DataRow row, string column, DateTime defaultValue )
		{
			DateTime colValue;

			try
			{
				string strValue = row[ column ].ToString();
				if ( DateTime.TryParse( strValue, out colValue ) == false )
					colValue = defaultValue;
			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{
				string queryString = GetWebUrl();

				LoggingHelper.LogError( "Exception in GetRowColumn( DataRow row, string column, int defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString() + "\r\nLocation: " + queryString, true );
				colValue = defaultValue;
				//throw ex;
			}
			return colValue;

		}
		protected static int GetField( int? field, int defaultValue = 0 )
		{
			int value = field != null ? ( int ) field : defaultValue;

			return value;
		} // end method
		protected static decimal GetField( decimal? field, decimal defaultValue = 0 )
		{
			decimal value = field != null ? ( decimal ) field : defaultValue;

			return value;
		} // end method
		protected static Guid GetField( Guid? field, Guid defaultValue )
		{
			Guid value = field != null ? ( Guid ) field : defaultValue;

			return value;
		} // end method

		protected static string GetMessages( List<string> messages )
		{
			if ( messages == null || messages.Count == 0 )
				return "";

			return string.Join( "<br/>", messages.ToArray() );

		}
		//protected static List<string> GetArray( string messages )
		//{
		//	List<string> list = new List<string>();
		//	if ( string.IsNullOrWhiteSpace( messages) )
		//		return list;
		//	string[] array = messages.Split( ',' );

		//	return list;
		//}
		/// <summary>
		/// Split a comma separated list into a list of strings
		/// </summary>
		/// <param name="csl"></param>
		/// <returns></returns>
		public static List<string> CommaSeparatedListToStringList( string csl )
		{
			if ( string.IsNullOrWhiteSpace( csl ) )
				return new List<string>();

			try
			{
				return csl.Trim().Split( new string[] { "," }, StringSplitOptions.RemoveEmptyEntries ).ToList();
			}
			catch
			{
				return new List<string>();
			}
		}
		/// <summary>
		/// Get the current url for reporting purposes
		/// </summary>
		/// <returns></returns>
		public static string GetWebUrl()
		{
			string queryString = "n/a";

			if ( HttpContext.Current != null && HttpContext.Current.Request != null )
				queryString = HttpContext.Current.Request.RawUrl.ToString();

			return queryString;
		}


		#endregion
		#region validations, etc
		public static bool IsValidDate( DateTime date )
		{
			if ( date != null && date > new DateTime( 1492, 1, 1 ) )
				return true;
			else
				return false;
		}

		public static bool IsValidDate( DateTime? date )
		{
			if ( date != null && date > new DateTime( 1492, 1, 1 ) )
				return true;
			else
				return false;
		}
		public static bool IsValidDate( string date )
		{
			DateTime validDate;
			if ( string.IsNullOrWhiteSpace( date ) || date.Length < 8 )
				return false;

			if ( !string.IsNullOrWhiteSpace( date )
				&& DateTime.TryParse( date, out validDate )
				&& date.Length >= 8
				&& validDate > new DateTime( 1492, 1, 1 )
				)
				return true;
			else
				return false;
		}
		public static bool IsInteger( string nbr )
		{
			int validNbr = 0;
			if ( !string.IsNullOrWhiteSpace( nbr ) && int.TryParse( nbr, out validNbr ) )
				return true;
			else
				return false;
		}
		public static bool IsValid( string nbr )
		{
			int validNbr = 0;
			if ( !string.IsNullOrWhiteSpace( nbr ) && int.TryParse( nbr, out validNbr ) )
				return true;
			else
				return false;
		}

		protected bool IsValidGuid( Guid field )
		{
			if ( ( field == null || field.ToString() == DEFAULT_GUID ) )
				return false;
			else
				return true;
		}
		protected bool IsValidGuid( Guid? field )
		{
			if ( ( field == null || field.ToString() == DEFAULT_GUID ) )
				return false;
			else
				return true;
		}
		public static bool IsValidGuid( string field )
		{
			if ( string.IsNullOrWhiteSpace( field )
				|| field.Trim() == DEFAULT_GUID
				|| field.Length != 36
				)
				return false;
			else
				return true;
		}
		public static bool IsGuidValid( Guid field )
		{
			if ( ( field == null || field.ToString() == DEFAULT_GUID ) )
				return false;
			else
				return true;
		}

		public static bool IsGuidValid( Guid? field )
		{
			if ( ( field == null || field.ToString() == DEFAULT_GUID ) )
				return false;
			else
				return true;
		}
		public static string GetData( string text, string defaultValue = "" )
		{
			if ( string.IsNullOrWhiteSpace( text ) == false )
				return text.Trim();
			else
				return defaultValue;
		}
		public static int? SetData( int value, int minValue )
		{
			if ( value >= minValue )
				return value;
			else
				return null;
		}
		public static decimal? SetData( decimal value, decimal minValue )
		{
			if ( value >= minValue )
				return value;
			else
				return null;
		}
		public static DateTime? SetDate( string value )
		{
			DateTime output;
			if ( DateTime.TryParse( value, out output ) )
				return output;
			else
				return null;
		}
		public static string GetUrlData( string text, string defaultValue = "" )
		{
			if ( string.IsNullOrWhiteSpace( text ) == false )
			{
				text = text.TrimEnd( '/' );
				return text.Trim();
			}
			else
				return defaultValue;
		}
		/// <summary>
		/// Validates the format of a Url
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		//public static bool IsUrlWellFormed( string url )
		//{
		//	string responseStatus = "";

		//	if ( string.IsNullOrWhiteSpace( url ) )
		//		return true;
		//	if ( !Uri.IsWellFormedUriString( url, UriKind.Absolute ) )
		//	{
		//		responseStatus = "The URL is not in a proper format";
		//		return false;
		//	}

		//	//may need to allow ftp, and others - not likely for this context?
		//	if ( url.ToLower().StartsWith( "http" ) == false )
		//	{
		//		responseStatus = "A URL must begin with http or https";

		//		return false;
		//	}

		//	//NOTE - do NOT use the HEAD option, as many sites reject that type of request
		//	var isOk = DoesRemoteFileExists( url, ref responseStatus );
		//	//optionally try other methods, or again with GET
		//	if ( !isOk && responseStatus == "999" )
		//		isOk = true;

		//	return isOk;
		//}
		public static bool IsUrlValid( string url, ref string statusMessage )
		{
			statusMessage = "";
			if ( string.IsNullOrWhiteSpace( url ) )
				return true;

			if ( !Uri.IsWellFormedUriString( url, UriKind.Absolute ) )
			{
				statusMessage = "The URL is not in a proper format (for example, must begin with http or https).";
				return false;
			}

			//may need to allow ftp, and others - not likely for this context?
			if ( url.ToLower().StartsWith( "http" ) == false )
			{
				statusMessage = "A URL must begin with http or https";
				return false;
			}

			var isOk = DoesRemoteFileExists( url, ref statusMessage );
			//optionally try other methods, or again with GET
			if ( !isOk && statusMessage == "999" )
				isOk = true;
			//	isOk = DoesRemoteFileExists( url, ref responseStatus, "GET" );
			return isOk;
		}
		/// <summary>
		/// Checks the file exists or not.
		/// </summary>
		/// <param name="url">The URL of the remote file.</param>
		/// <returns>True : If the file exits, False if file not exists</returns>
		public static bool DoesRemoteFileExists( string url, ref string responseStatus )
		{
			bool doingLinkChecking = UtilityManager.GetAppKeyValue( "doingLinkChecking", true );
			//consider stripping off https?
			//or if not found and https, try http
			try
			{
				if ( SkippingValidation( url ) )
					return true;

				//Creating the HttpWebRequest
				HttpWebRequest request = WebRequest.Create( url ) as HttpWebRequest;
				//NOTE - do use the HEAD option, as many sites reject that type of request
				request.Method = "GET";
				//var agent = HttpContext.Current.Request.AcceptTypes;

				//request.Accept = "text/html;text/*;image/*";
				request.ContentType = "text/html;charset=\"utf-8\";image/*";
				//request.Headers.Set( const_AcceptLanguageHeaderName, const_AcceptLanguageHeader );
				request.UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_8_2) AppleWebKit/537.17 (KHTML, like Gecko) Chrome/24.0.1309.0 Safari/537.17";

				//Getting the Web Response.
				HttpWebResponse response = request.GetResponse() as HttpWebResponse;
				//Returns TRUE if the Status code == 200
				response.Close();
				if ( response.StatusCode != HttpStatusCode.OK )
				{
					if ( url.ToLower().StartsWith( "https:" ) )
					{
						url = url.ToLower().Replace( "https:", "http:" );
						LoggingHelper.DoTrace( 5, string.Format( "_____________Failed for https, trying again using http: {0}", url ) );

						return DoesRemoteFileExists( url, ref responseStatus );
					}
					else
					{
						LoggingHelper.DoTrace( 5, string.Format( "Url validation failed for: {0}, using method: GET, with status of: {1}", url, response.StatusCode ) );
					}
				}
				responseStatus = response.StatusCode.ToString();

				return ( response.StatusCode == HttpStatusCode.OK );
				//apparantly sites like Linked In have can be a  problem
				//http://stackoverflow.com/questions/27231113/999-error-code-on-head-request-to-linkedin
				//may add code to skip linked In?, or allow on fail - which the same.
				//or some update, refer to the latter link

				//
			}
			catch ( WebException wex )
			{
				responseStatus = wex.Message;
				//
				if ( wex.Message.IndexOf( "(404)" ) > 1 )
					return false;
				else if ( wex.Message.IndexOf( "Too many automatic redirections were attempted" ) > -1 )
					return false;
				else if ( wex.Message.IndexOf( "(999" ) > 1 )
					return true;
				else if ( wex.Message.IndexOf( "(400) Bad Request" ) > 1 )
					return true;
				else if ( wex.Message.IndexOf( "(401) Unauthorized" ) > 1 )
					return true;
				else if ( wex.Message.IndexOf( "(406) Not Acceptable" ) > 1 )
					return true;
				else if ( wex.Message.IndexOf( "(500) Internal Server Error" ) > 1 )
					return true;
				else if ( wex.Message.IndexOf( "Could not create SSL/TLS secure channel" ) > 1 )
				{
					//https://www.naahq.org/education-careers/credentials/certification-for-apartment-maintenance-technicians 
					return true;

				}
				else if ( wex.Message.IndexOf( "Could not establish trust relationship for the SSL/TLS secure channel" ) > -1 )
				{
					return true;
				}
				else if ( wex.Message.IndexOf( "The underlying connection was closed: An unexpected error occurred on a send" ) > -1 )
				{
					return true;
				}
				else if ( wex.Message.IndexOf( "Detail=CR must be followed by LF" ) > 1 )
				{
					return true;
				}
				//var pageContent = new StreamReader( wex.Response.GetResponseStream() )
				//		 .ReadToEnd();
				if ( !doingLinkChecking )
				{
					LoggingHelper.LogError( string.Format( "BaseFactory.DoesRemoteFileExists url: {0}. Exception Message:{1}; URL: {2}", url, wex.Message, GetWebUrl() ), true, "SKIPPING - Exception on URL Checking" );

					return true;
				}

				LoggingHelper.LogError( string.Format( "BaseFactory.DoesRemoteFileExists url: {0}. Exception Message:{1}", url, wex.Message ), true, "Exception on URL Checking" );
				responseStatus = wex.Message;
				return false;
			}
			catch ( Exception ex )
			{

				if ( ex.Message.IndexOf( "(999" ) > -1 )
				{
					//linked in scenario
					responseStatus = "999";
				}
				else if ( ex.Message.IndexOf( "Could not create SSL/TLS secure channel" ) > 1 )
				{
					//https://www.naahq.org/education-careers/credentials/certification-for-apartment-maintenance-technicians 
					return true;

				}
				else if ( ex.Message.IndexOf( "(500) Internal Server Error" ) > 1 )
				{
					return true;
				}
				else if ( ex.Message.IndexOf( "(401) Unauthorized" ) > 1 )
				{
					return true;
				}
				else if ( ex.Message.IndexOf( "Could not establish trust relationship for the SSL/TLS secure channel" ) > 1 )
				{
					return true;
				}
				else if ( ex.Message.IndexOf( "Detail=CR must be followed by LF" ) > 1 )
				{
					return true;
				}
				if ( !doingLinkChecking )
				{
					LoggingHelper.LogError( string.Format( "BaseFactory.DoesRemoteFileExists url: {0}. Exception Message:{1}", url, ex.Message ), true, "SKIPPING - Exception on URL Checking" );

					return true;
				}

				LoggingHelper.LogError( string.Format( "BaseFactory.DoesRemoteFileExists url: {0}. Exception Message:{1}", url, ex.Message ), true, "Exception on URL Checking" );
				//Any exception will returns false.
				responseStatus = ex.Message;
				return false;
			}
		}

		private static bool SkippingValidation( string url )
		{


			Uri myUri = new Uri( url );
			string host = myUri.Host;

			string exceptions = UtilityManager.GetAppKeyValue( "urlExceptions" );
			//quick method to avoid loop
			if ( exceptions.IndexOf( host ) > -1 )
				return true;


			//string[] domains = exceptions.Split( ';' );
			//foreach ( string item in domains )
			//{
			//	if ( url.ToLower().IndexOf( item.Trim() ) > 5 )
			//		return true;
			//}

			return false;
		}

		public static bool IsPhoneValid( string phone, string type, ref List<string> messages )
		{
			bool isValid = true;

			string phoneNbr = PhoneNumber.StripPhone( GetData( phone ) );

			if ( !string.IsNullOrWhiteSpace( phoneNbr ) && phoneNbr.Length < 10 )
			{
				messages.Add( string.Format( "Error - A valid {0} ({1}) must have at least 10 numbers.", type, phone ) );
				return false;
			}


			return isValid;
		}
		#endregion


		public static string FormatFriendlyTitle( string text )
		{
			if ( text == null || text.Trim().Length == 0 )
				return "";

			string title = UrlFriendlyTitle( text );

			//encode just incase
			title = HttpUtility.HtmlEncode( title );
			return title;
		}
		/// <summary>
		/// Format a title (such as for a library) to be url friendly
		/// NOTE: there are other methods:
		/// ILPathways.Utilities.UtilityManager.UrlFriendlyTitle()
		/// </summary>
		/// <param name="title"></param>
		/// <returns></returns>
		public static string UrlFriendlyTitle( string title )
		{
			if ( title == null || title.Trim().Length == 0 )
				return "";

			title = title.Trim();

			string encodedTitle = title.Replace( " - ", "-" );
			encodedTitle = encodedTitle.Replace( " ", "_" );

			//for now allow embedded periods
			//encodedTitle = encodedTitle.Replace( ".", "-" );

			encodedTitle = encodedTitle.Replace( "'", "" );
			encodedTitle = encodedTitle.Replace( "&", "-" );
			encodedTitle = encodedTitle.Replace( "#", "" );
			encodedTitle = encodedTitle.Replace( "$", "S" );
			encodedTitle = encodedTitle.Replace( "%", "percent" );
			encodedTitle = encodedTitle.Replace( "^", "" );
			encodedTitle = encodedTitle.Replace( "*", "" );
			encodedTitle = encodedTitle.Replace( "+", "_" );
			encodedTitle = encodedTitle.Replace( "~", "_" );
			encodedTitle = encodedTitle.Replace( "`", "_" );
			encodedTitle = encodedTitle.Replace( "/", "_" );
			encodedTitle = encodedTitle.Replace( "://", "/" );
			encodedTitle = encodedTitle.Replace( ":", "" );
			encodedTitle = encodedTitle.Replace( ";", "" );
			encodedTitle = encodedTitle.Replace( "?", "" );
			encodedTitle = encodedTitle.Replace( "\"", "_" );
			encodedTitle = encodedTitle.Replace( "\\", "_" );
			encodedTitle = encodedTitle.Replace( "<", "_" );
			encodedTitle = encodedTitle.Replace( ">", "_" );
			encodedTitle = encodedTitle.Replace( "__", "_" );
			encodedTitle = encodedTitle.Replace( "__", "_" );
			encodedTitle = encodedTitle.Replace( "..", "_" );
			encodedTitle = encodedTitle.Replace( ".", "_" );

			if ( encodedTitle.EndsWith( "." ) )
				encodedTitle = encodedTitle.Substring( 0, encodedTitle.Length - 1 );

			return encodedTitle;
		} //
		public static string GenerateFriendlyName( string name )
		{
			if ( name == null || name.Trim().Length == 0 )
				return "";
			//another option could be use a pattern like the following?
			//string phrase = string.Format( "{0}-{1}", Id, name );

			string str = RemoveAccent( name ).ToLower();
			// invalid chars           
			str = Regex.Replace( str, @"[^a-z0-9\s-]", "" );
			// convert multiple spaces into one space   
			str = Regex.Replace( str, @"\s+", " " ).Trim();
			// cut and trim 
			str = str.Substring( 0, str.Length <= 45 ? str.Length : 45 ).Trim();
			str = Regex.Replace( str, @"\s", "-" ); // hyphens   
			return str;
		}
		private static string RemoveAccent( string text )
		{
			byte[] bytes = System.Text.Encoding.GetEncoding( "Cyrillic" ).GetBytes( text );
			return System.Text.Encoding.ASCII.GetString( bytes );
		}
		protected string HandleDBValidationError( System.Data.Entity.Validation.DbEntityValidationException dbex, string source, string title )
		{
			string message = string.Format( "{0} DbEntityValidationException, Name: {1}", source, title );

			foreach ( var eve in dbex.EntityValidationErrors )
			{
				message += string.Format( "\rEntity of type \"{0}\" in state \"{1}\" has the following validation errors:",
					eve.Entry.Entity.GetType().Name, eve.Entry.State );
				foreach ( var ve in eve.ValidationErrors )
				{
					message += string.Format( "- Property: \"{0}\", Error: \"{1}\"",
						ve.PropertyName, ve.ErrorMessage );
				}

				LoggingHelper.LogError( message, true );
			}

			return message;
		}

		public static string FormatExceptions( Exception ex )
		{
			string message = ex.Message;

			if ( ex.InnerException != null )
			{
				message += "; \r\nInnerException: " + ex.InnerException.Message;
				if ( ex.InnerException.InnerException != null )
				{
					message += "; \r\nInnerException2: " + ex.InnerException.InnerException.Message;
				}
			}

			return message;
		}

		/// <summary>
		/// Strip off text that is randomly added that starts with jquery
		/// Will need additional check for numbers - determine actual format
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string StripJqueryTag( string text )
		{
			int pos2 = text.ToLower().IndexOf( "jquery" );
			if ( pos2 > 1 )
			{
				text = text.Substring( 0, pos2 );
			}

			return text;
		}

	}
}
