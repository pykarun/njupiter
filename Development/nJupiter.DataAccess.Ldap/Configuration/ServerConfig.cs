#region Copyright & License
// 
// 	Copyright (c) 2005-2012 nJupiter
// 
// 	Permission is hereby granted, free of charge, to any person obtaining a copy
// 	of this software and associated documentation files (the "Software"), to deal
// 	in the Software without restriction, including without limitation the rights
// 	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// 	copies of the Software, and to permit persons to whom the Software is
// 	furnished to do so, subject to the following conditions:
// 
// 	The above copyright notice and this permission notice shall be included in
// 	all copies or substantial portions of the Software.
// 
// 	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// 	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// 	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// 	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// 	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// 	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// 	THE SOFTWARE.
// 
#endregion

using System;
using System.DirectoryServices;

namespace nJupiter.DataAccess.Ldap.Configuration {
	internal class ServerConfig : IServerConfig {

		public ServerConfig() {
			TimeLimit = TimeSpan.FromSeconds(30);
			PageSize = 0;
			AuthenticationTypes = AuthenticationTypes.None;
			RangeRetrievalSupport = true;
			PropertySortingSupport = true;
		}

		public Uri Url { get; internal set; }
		public string Username { get; internal set; }
		public string Password { get; internal set; }
		public AuthenticationTypes AuthenticationTypes { get; internal set; }
		public TimeSpan TimeLimit { get; internal set; }
		public int PageSize { get; internal set; }
		public bool AllowWildcardSearch { get; internal set; }
		public bool PropertySortingSupport { get; internal set; }
		public bool RangeRetrievalSupport { get; internal set; }
	}
}