using System;
using System.Collections;
using System.ComponentModel;
using System.DirectoryServices;
using System.Runtime.Remoting;

namespace nJupiter.DataAccess.Ldap.DirectoryServices.Abstraction {
	internal class DirectoryEntryWrapper : IDirectoryEntry {
		private readonly DirectoryEntry directoryEntry;

		public IDirectoryEntry GetDirectoryEntry() {
			return this;
		}

		public DirectoryEntry WrappedEntry { get { return directoryEntry; } }

		public DirectoryEntryWrapper(DirectoryEntry directoryEntry) {
			this.directoryEntry = directoryEntry;
		}

		public AuthenticationTypes AuthenticationType { get { return directoryEntry.AuthenticationType; }
			set { directoryEntry.AuthenticationType = value; } }

		public DirectoryEntries Children {
			get { return directoryEntry.Children; } }

		public void Close() {
			directoryEntry.Close();
		}

		public void CommitChanges() {
			directoryEntry.CommitChanges();
		}

		public IContainer Container {
			get { return directoryEntry.Container; } }

		public IEntry CopyTo(IEntry newParent) {
			return directoryEntry.CopyTo(newParent.UnWrap()).Wrap();
		}

		public IEntry CopyTo(IEntry newParent, string newName) {
			return directoryEntry.CopyTo(newParent.UnWrap(), newName).Wrap();
		}

		public ObjRef CreateObjRef(Type requestedType) {
			return directoryEntry.CreateObjRef(requestedType);
		}

		public void DeleteTree() {
			directoryEntry.DeleteTree();
		}

		public void Dispose() {
			directoryEntry.Dispose();
		}

		public event EventHandler Disposed { add { directoryEntry.Disposed += value; }
			remove { directoryEntry.Disposed -= value; } }

		public object GetLifetimeService() {
			return directoryEntry.GetLifetimeService();
		}

		public Guid Guid {
			get { return directoryEntry.Guid; } }

		public object InitializeLifetimeService() {
			return directoryEntry.InitializeLifetimeService();
		}

		public object Invoke(string methodName, params object[] args) {
			return directoryEntry.Invoke(methodName, args);
		}

		public object InvokeGet(string propertyName) {
			return directoryEntry.InvokeGet(propertyName);
		}

		public void InvokeSet(string propertyName, params object[] args) {
			directoryEntry.InvokeSet(propertyName, args);
		}

		public void MoveTo(IEntry newParent) {
			directoryEntry.MoveTo(newParent.UnWrap());
		}

		public void MoveTo(IEntry newParent, string newName) {
			directoryEntry.MoveTo(newParent.UnWrap(), newName);
		}

		public string Name {
			get { return directoryEntry.Name; } }

		public string NativeGuid {
			get { return directoryEntry.NativeGuid; } }

		public object NativeObject {
			get { return directoryEntry.NativeObject; } }

		public ActiveDirectorySecurity ObjectSecurity { get { return directoryEntry.ObjectSecurity; }
			set { directoryEntry.ObjectSecurity = value; } }

		public DirectoryEntryConfiguration Options {
			get { return directoryEntry.Options; } }

		public IEntry Parent {
			get { return directoryEntry.Parent.Wrap(); } }

		public string Password {
			set { directoryEntry.Password = value; } }

		public string Path { get { return directoryEntry.Path; }
			set { directoryEntry.Path = value; } }

		public IDictionary Properties {
			get { return directoryEntry.Properties; } }

		public void RefreshCache() {
			directoryEntry.RefreshCache();
		}

		public void RefreshCache(string[] propertyNames) {
			directoryEntry.RefreshCache(propertyNames);
		}

		public void Rename(string newName) {
			directoryEntry.Rename(newName);
		}

		public string SchemaClassName {
			get { return directoryEntry.SchemaClassName; } }

		public IEntry SchemaEntry {
			get { return directoryEntry.SchemaEntry.Wrap(); } }

		public ISite Site { get { return directoryEntry.Site; }
			set { directoryEntry.Site = value; } }

		public bool UsePropertyCache { get { return directoryEntry.UsePropertyCache; }
			set { directoryEntry.UsePropertyCache = value; } }

		public string Username { get { return directoryEntry.Username; }
			set { directoryEntry.Username = value; } }


	}
}