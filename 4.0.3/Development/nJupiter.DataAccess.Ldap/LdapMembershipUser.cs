﻿using System;
using System.Collections;
using System.Web.Security;

namespace nJupiter.DataAccess.Ldap {
	public class LdapMembershipUser : MembershipUser {
		internal LdapMembershipUser(string providerName,
									string name,
									object providerUserKey,
									string email,
									string passwordQuestion,
									string comment,
									bool isApproved,
									bool isLockedOut,
									DateTime creationDate,
									DateTime lastLoginDate,
									DateTime lastActivityDate,
									DateTime lastPasswordChangedDate,
									DateTime lastLockoutDate,
									IDictionary propertyCollection,
									string path
									)
									: base(providerName,
											name,
											providerUserKey,
											email,
											passwordQuestion,
											comment,
											isApproved,
											isLockedOut,
											creationDate,
											lastLoginDate,
											lastActivityDate,
											lastPasswordChangedDate,
											lastLockoutDate) {
			attributes = new AttributeCollection(propertyCollection);
			this.path = path;
		}

		private readonly AttributeCollection attributes;
		private readonly string path;

		public AttributeCollection Attributes {
			get {
				return attributes;
			}
		}

		public string Path {
			get {
				return path;
			}
		}


	}
}