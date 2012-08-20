#region Copyright & License
/*
	Copyright (c) 2005-2010 nJupiter

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/
#endregion

using System;
using System.Web;
using System.Text;
using System.Collections.Specialized;
using System.Globalization;

namespace nJupiter.Web {

	public static class UrlHandler {
		public static string AddQueryKeyValue(string url, string key, string value, bool encodeValue) {
			if(url == null) {
				throw new ArgumentNullException("url");
			}
			if(key == null) {
				throw new ArgumentNullException("key");
			}
			if(value == null) {
				throw new ArgumentNullException("value");
			}
			if(encodeValue)
				value = HttpUtility.UrlEncode(value);
			return AddQueryKeyValue(url, key, value);
		}

		public static string AddQueryKeyValue(string url, string key, string value) {
			if(url == null) {
				throw new ArgumentNullException("url");
			}
			if(key == null) {
				throw new ArgumentNullException("key");
			}
			if(value == null) {
				throw new ArgumentNullException("value");
			}
			return AddQueryParams(url, key + "=" + value);
		}

		public static string ReplaceQueryKeyValue(string url, string key, string value, bool encodeValue) {
			if(url == null) {
				throw new ArgumentNullException("url");
			}
			if(key == null) {
				throw new ArgumentNullException("key");
			}
			if(value == null) {
				throw new ArgumentNullException("value");
			}
			if(encodeValue)
				value = HttpUtility.UrlEncode(value);
			url = RemoveQueryKey(url, key);
			return AddQueryKeyValue(url, key, value);
		}

		public static string ReplaceQueryKeyValue(string url, string key, string value) {
			if(url == null) {
				throw new ArgumentNullException("url");
			}
			if(key == null) {
				throw new ArgumentNullException("key");
			}
			if(value == null) {
				throw new ArgumentNullException("value");
			}
			url = RemoveQueryKey(url, key);
			return AddQueryParams(url, key + "=" + value);
		}

		public static string AddQueryParams(string url, params string[] parameters){
			if(url == null) {
				throw new ArgumentNullException("url");
			}
			if(parameters == null) {
				throw new ArgumentNullException("parameters");
			}

			if(parameters.Length > 0){
				StringBuilder queryString = new StringBuilder(url);
				int hashPos = url.IndexOf("#");
				int hashPosFromEnd = hashPos >= 0 ? url.Length - hashPos : hashPos;
				if(hashPosFromEnd >= 0) { 
					queryString.Insert(queryString.Length - hashPosFromEnd, url.IndexOf("?") > 0 ? "&" : "?");
				} else {
					queryString.Append(url.IndexOf("?") > 0 ? "&" : "?");
				}
				for(int i = 0; i < parameters.Length; i++){
					if(parameters[i].Length > 0) {
						if(hashPosFromEnd >= 0) {
							queryString.Insert(queryString.Length - hashPosFromEnd, parameters[i]);
							if(i != parameters.Length - 1)
								queryString.Insert(queryString.Length - hashPosFromEnd, "&");
						} else {
							queryString.Append(parameters[i]);
							if(i != parameters.Length - 1)
								queryString.Append("&");
						}
					}
				}
				if(queryString.Length > url.Length + 1) // has any query parameter been added?
					return queryString.ToString();
			}
			return url;
		}
		public static string AddQueryParams(string url, NameValueCollection queryStringCollection, bool encodeValues){
			if(url == null) {
				throw new ArgumentNullException("url");
			}
			if(queryStringCollection == null) {
				throw new ArgumentNullException("queryStringCollection");
			}
			string[] parameters = new string[queryStringCollection.Count];
			int i = 0;
			foreach(string name in queryStringCollection) {
				parameters[i++] = string.Format(CultureInfo.InvariantCulture,"{0}={1}", name, (encodeValues ? HttpUtility.UrlEncode(queryStringCollection[name]) : queryStringCollection[name]));
			}
			return AddQueryParams(url, parameters);
		}
		public static string AddQueryParams(string url, NameValueCollection queryStringCollection){
			return AddQueryParams(url, queryStringCollection, false);
		}

		public static NameValueCollection GetQueryString(string url) {
			if(url == null) {
				throw new ArgumentNullException("url");
			}
			NameValueCollection nvc = new NameValueCollection();
			if(url.IndexOf('?') > 0)
				url = url.Substring(url.IndexOf('?') + 1);
			string[] queryParams = url.Split('&');
			foreach(string queryParam in queryParams) {
				if(queryParam.IndexOf('=') > 0) {
					string[] q = queryParam.Split('=');
					if(q.Length == 2)
						nvc.Add(q[0], q[1]);
					else if(q.Length == 1)
						nvc.Add(q[0], string.Empty);
				}
			}
			return nvc;
		}

		public static string GetQueryString(string url, bool encodeValues, params string[] parametersToRemove) {
			return GetQueryString(GetQueryString(url), encodeValues, parametersToRemove);
		}

		public static string GetQueryString(string url, params string[] parametersToRemove) {
			return GetQueryString(GetQueryString(url), parametersToRemove);
		}

		public static string GetQueryString(NameValueCollection queryStringCollection, bool encodeValues, params string[] parametersToRemove) {
			return GetQueryString(RemoveQueryParams(queryStringCollection, parametersToRemove), encodeValues);
		}
		public static string GetQueryString(NameValueCollection queryStringCollection, params string[] parametersToRemove) {
			return GetQueryString(RemoveQueryParams(queryStringCollection, parametersToRemove));
		}

		public static string GetQueryString(NameValueCollection queryStringCollection) {
			return GetQueryString(queryStringCollection, false);
		}

		public static string GetQueryString(NameValueCollection queryStringCollection, bool encodeValues) {
			if(queryStringCollection == null) {
				throw new ArgumentNullException("queryStringCollection");
			}
			StringBuilder sb = new StringBuilder();
			foreach(string name in queryStringCollection) {
				string value = (encodeValues ? HttpUtility.UrlEncode(queryStringCollection[name]) : queryStringCollection[name]);
				sb.AppendFormat("{0}={1}&", name, value);
			}
			if(sb.Length == 0)
				return string.Empty;
			return sb.ToString(0, sb.Length - 1);
		}

		public static NameValueCollection RemoveQueryParams(NameValueCollection queryStringCollection, params string[] parametersToRemove) {
			if(queryStringCollection == null) {
				throw new ArgumentNullException("queryStringCollection");
			}
			if(parametersToRemove == null) {
				throw new ArgumentNullException("parametersToRemove");
			}
			queryStringCollection = new NameValueCollection(queryStringCollection);
			for(int i = 0; i < parametersToRemove.Length; i++){
				queryStringCollection.Remove(parametersToRemove[i]);
			}
			return queryStringCollection;
		}

		public static string RemoveQueryKeys(string url, params string[] keys) {
			string path = url;
			foreach(string key in keys) {
				path = RemoveQueryKey(path, key);
			}
			return path;
		}
		public static string RemoveQueryKey(string url, string key) {
			if(url == null) {
				throw new ArgumentNullException("url");
			}
			if(key == null) {
				throw new ArgumentNullException("key");
			}
			int queryStringSeparatorPos = url.IndexOf("?");
			int hashSeparatorPos = url.IndexOf("#");
			string path = url;
			if(queryStringSeparatorPos > -1) {
				if(hashSeparatorPos > -1) 
					path = path.Substring(0, queryStringSeparatorPos) + path.Substring(hashSeparatorPos);
				else 
					path = path.Substring(0, queryStringSeparatorPos);
			}
			return AddQueryParams(path, UrlHandler.GetQueryString(url, key)); 
		}

		// Fix so even non epi 4.6 components works correctly with friendly url
		public static string CurrentPath {
			get {
				if(HttpContext.Current.Items["FriendlyUrlModule.CurrentFriendlyUrl"] != null)
					return HttpContext.Current.Items["FriendlyUrlModule.CurrentFriendlyUrl"].ToString();
				return HttpContext.Current.Request.Path;
			}
		}

		// Fix so even non epi 4.6 components works correctly with friendly url
		public static NameValueCollection CurrentQueryString {
			get {
				if(HttpContext.Current.Items["nJupiter.Web.UrlHandler.CurrentQueryString"] == null) {
					if(HttpContext.Current.Items["FriendlyUrlModule.CurrentFriendlyUrl"] != null) {
						HttpContext.Current.Items["nJupiter.Web.UrlHandler.CurrentQueryString"] = UrlHandler.RemoveQueryParams(HttpContext.Current.Request.QueryString, "id", "epslanguage");
					} else {
						return HttpContext.Current.Request.QueryString;
					}
				}
				return HttpContext.Current.Items["nJupiter.Web.UrlHandler.CurrentQueryString"] as NameValueCollection;
			}
		}

		// Fix so even non epi 4.6 components works correctly with friendly url
		public static string CurrentPathAndQuery {
			get {
				NameValueCollection queryString = CurrentQueryString;
				if(queryString != null && queryString.Count > 0)
					return AddQueryParams(CurrentPath, GetQueryString(CurrentQueryString));
				return CurrentPath;
			}
		}

		// Fix so even non epi 4.6 components works correctly with friendly url
		public static NameValueCollection CurrentForm {
			get {
				if(HttpContext.Current.Items["nJupiter.Web.UrlHandler.CurrentForm"] == null) {
					if(HttpContext.Current.Items["FriendlyUrlModule.CurrentFriendlyUrl"] != null) {
						HttpContext.Current.Items["nJupiter.Web.UrlHandler.CurrentForm"] = UrlHandler.RemoveQueryParams(HttpContext.Current.Request.Form, "id", "epslanguage");
					} else {
						return HttpContext.Current.Request.QueryString;
					}
				}
				return HttpContext.Current.Items["nJupiter.Web.UrlHandler.CurrentForm"] as NameValueCollection;
			}
		}

	}
}