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
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using nJupiter.Collections.Generics;

namespace nJupiter.Web.UI.Controls {

	#region Delegates
	public delegate void			PopulateNavigationItem(WebNavigationItemArgs args);
	public delegate string			GetNavigationItemName(WebNavigationPageArgs args);
	public delegate string			GetNavigationItemLink(WebNavigationPageArgs args);
	public delegate Control			GetNavigationItem(RepeaterItemEventArgs args);
	public delegate WebAnchor		GetNavigationAnchor(WebNavigationItemArgs args);
	public delegate WebNavigation	CreateWebNavigationControl(WebNavigationItemArgs args);
	#endregion

	public interface INavigationPage {
		INavigationPageId Id		{ get; }
		INavigationPageId ParentId	{ get; }
	}

	public interface INavigationPageId { }

	public class NavigationPageCollection : SortableCollection<INavigationPage> {
		public NavigationPageCollection() {}
		public NavigationPageCollection(IList<INavigationPage> list) : base(list) {}
	}

	[ParseChildren(true), PersistChildren(false)]
	public abstract class WebNavigation : Control {

		#region UI Members
		private readonly WebHeading	hedHeadline		= new WebHeading();
		private readonly WebAnchor	ancHeadline		= new WebAnchor();
		private readonly Repeater	rptNavigation	= new Repeater();
		#endregion 

		#region Members
		private int									visibleChildren;
		private int									visibleDescendants;
		private int									visibleLevels;
		private readonly NavigationPageCollection	selectedNavigationPath				= new NavigationPageCollection();
		private INavigationPage						rootPage;
		private INavigationPage						navPage;
		private INavigationPage						selectedNavigationPage;
		private IncludeChildrenOfRemovedNodesMode	includeChildrenOfRemovedNodesMode	= IncludeChildrenOfRemovedNodesMode.Never;
		private bool								selectedPageClickable;
		private bool								headingVisible;
		private bool								headingAlwaysVisible;
		private bool								headingClickable;
		private bool								autoDataBind;
		private bool								expandAll;
		private bool								showOnlyPagesInPath;
		private bool								hideSelectedPage;
		private bool								rootLevelNotClickable;
		private bool								alwaysPlaceSpanAroundNavItem;
		private bool								indicateIfNodeHasChildren;
		private bool								includeRootLevelInList;
		private int									numberOfLevels						= -1;
		private int									startLevelFromRoot					= -1;
		private int									fallbackStartLevel					= -1;
		private string								cssClass							= string.Empty;
		private PopulateNavigationItem				populateNavigationItemDelegate;
		private GetNavigationItemName				getNavigationItemNameDelegate;
		private GetNavigationItemLink				getNavigationItemLinkDelegate;
		private GetNavigationItem					getNavigationItemDelegate;
		private GetNavigationAnchor					getNavigationAnchorDelegate;
		private CreateWebNavigationControl			createWebNavigationControlDelegate;
		#endregion

		#region	Properties
		public	int									VisibleChildren						{ get { return this.visibleChildren; } }
		public	int									VisibleDescendants					{ get { return this.visibleDescendants; } }
		public	int									VisibleLevels						{ get { return this.visibleLevels; } }
		public	NavigationPageCollection			SelectedNavigationPath				{ get { return this.selectedNavigationPath; } }

		protected virtual INavigationPage			NavPage								{ get { return this.navPage; }								set { this.navPage = value; } }
		public	virtual INavigationPage				RootPage							{ get { return this.rootPage; }								set { this.rootPage = value; } }
		public	virtual INavigationPage				SelectedNavigationPage				{ get { return this.selectedNavigationPage; }				set { this.selectedNavigationPage = value; } }
		public	IncludeChildrenOfRemovedNodesMode	IncludeChildrenOfRemovedNodesMode	{ get { return this.includeChildrenOfRemovedNodesMode; }	set { this.includeChildrenOfRemovedNodesMode = value; } }
		public	bool								SelectedPageClickable				{ get { return this.selectedPageClickable; }				set { this.selectedPageClickable = value; } }
		public	bool								HeadingVisible						{ get { return this.headingVisible; }						set { this.headingVisible = value; } }
		public	bool								HeadingAlwaysVisible				{ get { return this.headingAlwaysVisible; }					set { this.headingAlwaysVisible = value; } }
		public	bool								HeadingClickable					{ get { return this.headingClickable; }						set { this.headingClickable = value; } }
		public	bool								AutoDataBind						{ get { return this.autoDataBind; }							set { this.autoDataBind = value; } }
		public	bool								ExpandAll							{ get { return this.expandAll; }							set { this.expandAll = value; } }
		public	bool								ShowOnlyPagesInPath					{ get { return this.showOnlyPagesInPath; }					set { this.showOnlyPagesInPath = value; } }
		public	bool								HideSelectedPage					{ get { return this.hideSelectedPage; }						set { this.hideSelectedPage = value; } }
		public	bool								RootLevelNotClickable				{ get { return this.rootLevelNotClickable; }				set { this.rootLevelNotClickable = value; } }
		public	bool								IndicateIfNodeHasChildren			{ get { return this.indicateIfNodeHasChildren; }			set { this.indicateIfNodeHasChildren = value; } }
		public	bool								AlwaysPlaceSpanAroundNavItem		{ get { return this.alwaysPlaceSpanAroundNavItem; }			set { this.alwaysPlaceSpanAroundNavItem = value; } }
		public	bool								IncludeRootLevelInList				{ get { return this.includeRootLevelInList; }				set { this.includeRootLevelInList = value; } }
		public	int									NumberOfLevels						{ get { return this.numberOfLevels; }						set { this.numberOfLevels = value; } }
		public	int									StartLevelFromRoot					{ get { return this.startLevelFromRoot; }					set { this.startLevelFromRoot = value; } }
		public	int									FallbackStartLevel					{ get { return this.fallbackStartLevel; }					set { this.fallbackStartLevel = value; } }
		public	string								CssClass							{ get { return this.cssClass; }								set { this.cssClass = value; } }
		public	PopulateNavigationItem				PopulateNavigationItemDelegate		{ get { return this.populateNavigationItemDelegate; }		set { this.populateNavigationItemDelegate = value; } }
		public	GetNavigationItemName				GetNavigationItemNameDelegate		{ get { return this.getNavigationItemNameDelegate; }		set { this.getNavigationItemNameDelegate = value; } }
		public	GetNavigationItemLink				GetNavigationItemLinkDelegate		{ get { return this.getNavigationItemLinkDelegate; }		set { this.getNavigationItemLinkDelegate = value; } }
		public	GetNavigationItem					GetNavigationItemDelegate			{ get { return this.getNavigationItemDelegate; }			set { this.getNavigationItemDelegate = value; } }
		public	GetNavigationAnchor					GetNavigationAnchorDelegate			{ get { return this.getNavigationAnchorDelegate; }			set { this.getNavigationAnchorDelegate = value; } }
		public	CreateWebNavigationControl			CreateWebNavigationControlDelegate	{ get { return this.createWebNavigationControlDelegate; }	set { this.createWebNavigationControlDelegate = value; } }
		public	Repeater							Repeater							{ get { return rptNavigation; } }
		
		[TemplateContainer(typeof(RepeaterItem)), PersistenceMode(PersistenceMode.InnerProperty)]
		public ITemplate HeaderTemplate {
			get {
				return rptNavigation.HeaderTemplate;
			}
			set {
				rptNavigation.HeaderTemplate = value;
			}
		}

		[TemplateContainer(typeof(RepeaterItem)), PersistenceMode(PersistenceMode.InnerProperty)]
		public ITemplate ItemTemplate {
			get {
				return rptNavigation.ItemTemplate;
			}
			set {
				rptNavigation.ItemTemplate = value;
			}
		}

		[TemplateContainer(typeof(RepeaterItem)), PersistenceMode(PersistenceMode.InnerProperty)]
		public ITemplate AlternatingItemTemplate {
			get {
				return rptNavigation.AlternatingItemTemplate;
			}
			set {
				rptNavigation.AlternatingItemTemplate = value;
			}
		}

		[TemplateContainer(typeof(RepeaterItem)), PersistenceMode(PersistenceMode.InnerProperty)]
		public ITemplate SeparatorTemplate {
			get {
				return rptNavigation.SeparatorTemplate;
			}
			set {
				rptNavigation.SeparatorTemplate = value;
			}
		}

		[TemplateContainer(typeof(RepeaterItem)), PersistenceMode(PersistenceMode.InnerProperty)]
		public ITemplate FooterTemplate {
			get {
				return rptNavigation.FooterTemplate;
			}
			set {
				rptNavigation.FooterTemplate = value;
			}
		}
		#endregion

		#region Static Holders
		private static readonly object eventDataBound	= new object();
		#endregion

		#region Abstract Methods
		protected INavigationPage GetNavigationPage(INavigationPage page) {
			INavigationPage navigationPage = null;
			if(page != null) {
				navigationPage = this.GetNavigationPage(page.Id);
			}
			return navigationPage;
		}

		protected abstract INavigationPage GetNavigationPage(INavigationPageId id);
		protected abstract NavigationPageCollection GetChildren(INavigationPage page);
		protected abstract NavigationPageCollection FilterNavigation(NavigationPageCollection childNodes, NavigationPageCollection removedChildNodes);
		public abstract string GetNavigationItemName(WebNavigationPageArgs args);
		public abstract string GetNavigationItemLink(WebNavigationPageArgs args);
		public abstract WebNavigation CreateWebNavigationControl(WebNavigationItemArgs args);
		protected abstract INavigationPage CurrentPage { get; }
		protected abstract INavigationPage SiteRoot { get; }
		#endregion

		#region Events
		public event EventHandler DataBound {
			add { base.Events.AddHandler(WebNavigation.eventDataBound, value); }
			remove { base.Events.RemoveHandler(WebNavigation.eventDataBound, value); }
		}
		#endregion

		#region Event Handlers
		/// <summary>
		/// Raises the <see cref="E:System.Web.UI.Control.Init"/> event.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		protected override void OnInit(EventArgs e) {
			hedHeadline.InnerSpan = true;
			hedHeadline.Controls.Add(ancHeadline);
			this.Controls.Add(hedHeadline);
			this.Controls.Add(rptNavigation);
			rptNavigation.ItemDataBound += this.ItemDataBound;
			base.OnInit(e);
		}

		/// <summary>
		/// Raises the <see cref="E:System.Web.UI.Control.Load"></see> event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"></see> object that contains the event data.</param>
		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);
			this.EnableViewState = false; //This control does not need viewstate.
		}

		/// <summary>
		/// Raises the <see cref="E:System.Web.UI.Control.PreRender"></see> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> object that contains the event data.</param>
		protected override void OnPreRender(EventArgs e) {
			base.OnPreRender(e);
			if(this.AutoDataBind){
				this.DataBind();
			}
		}

		protected virtual void OnItemDataBound(RepeaterItemEventArgs e) {
			if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem) {
				// Cast dataitem
				INavigationPage				pageData	= ((INavigationPage)e.Item.DataItem);
				NavigationPageCollection	navItems	= ((NavigationPageCollection)rptNavigation.DataSource);

				// check if selected
				bool isSelected				= IsPageSelected(this.RootPage, pageData, this.SelectedNavigationPage);
				bool isInSelectedPath		= IsPageInSelectedPath(this.RootPage, pageData, this.SelectedNavigationPage);
				bool isChildOfRemovedNode	= this.IncludeChildrenOfRemovedNodesMode > IncludeChildrenOfRemovedNodesMode.Never && 
					this.IsPageChildOfRemovedNode(this.RootPage, pageData);
				Control liListItem = this.GetNavigationItemDelegate(e);

				if((isSelected && this.hideSelectedPage) || (!isInSelectedPath && this.ShowOnlyPagesInPath)){
					liListItem.Visible = false;
				}else{
					bool expandingLevel = this.NumberOfLevels != 0 && (!pageData.Id.Equals(this.RootPage.Id) || this.IncludeRootLevelInList) && (this.ExpandAll || isInSelectedPath);

					if(expandingLevel) {
						PlaceHolder subNavigationPlaceHolder = new PlaceHolder();
						liListItem.Controls.Add(subNavigationPlaceHolder);
						WebNavigationItemArgs args = new WebNavigationItemArgs(liListItem,
						                          this,
						                          pageData,
						                          isSelected,
						                          isInSelectedPath,
												  e.Item.ItemIndex,
						                          e.Item.ItemIndex == 0,
						                          e.Item.ItemIndex == (navItems.Count - 1),
						                          expandingLevel,
						                          isChildOfRemovedNode,
						                          null);
						WebNavigation subNavigation = this.CreateWebNavigationControlDelegate(args);
						subNavigationPlaceHolder.Controls.Add(subNavigation);
						args.SubNavigation = subNavigation;
						this.PopulateSubNavigation(subNavigation, pageData);

						this.PopulateNavigationItemDelegate(args);

						subNavigation.DataBind();
						this.visibleLevels += subNavigation.VisibleLevels;
						this.visibleDescendants += subNavigation.VisibleDescendants;
						this.selectedNavigationPath.AddRange(subNavigation.selectedNavigationPath);
					} else { 
						this.PopulateNavigationItemDelegate(new WebNavigationItemArgs(liListItem, this, pageData, isSelected, isInSelectedPath, e.Item.ItemIndex, e.Item.ItemIndex == 0, e.Item.ItemIndex == (navItems.Count - 1), expandingLevel, isChildOfRemovedNode, null));
					}
					this.visibleChildren++;
					this.visibleDescendants++;
					if(isInSelectedPath) {
						this.selectedNavigationPath.Insert(0, pageData);
					}
				}
			}
		}

		protected virtual void PopulateSubNavigation(WebNavigation subNavigation, INavigationPage navigationPage) { 
			subNavigation.HeadingVisible = false;
			subNavigation.ExpandAll = this.ExpandAll;
			subNavigation.SelectedPageClickable = this.SelectedPageClickable;
			subNavigation.ShowOnlyPagesInPath = this.ShowOnlyPagesInPath;
			subNavigation.IndicateIfNodeHasChildren = this.IndicateIfNodeHasChildren;
			subNavigation.AlwaysPlaceSpanAroundNavItem = this.AlwaysPlaceSpanAroundNavItem;
			subNavigation.HideSelectedPage = this.HideSelectedPage;

			subNavigation.IncludeChildrenOfRemovedNodesMode = this.IncludeChildrenOfRemovedNodesMode;
			subNavigation.NumberOfLevels = this.NumberOfLevels - 1;
			if(subNavigation.RootPage == null)
				subNavigation.RootPage = GetNavigationPage(navigationPage.ParentId);
			if(subNavigation.NavPage == null)
				subNavigation.NavPage = navigationPage;
			if(subNavigation.SelectedNavigationPage == null)
				subNavigation.SelectedNavigationPage = this.SelectedNavigationPage;
			if(subNavigation.HeaderTemplate == null)
				subNavigation.HeaderTemplate = this.HeaderTemplate;
			if(subNavigation.ItemTemplate == null)
				subNavigation.ItemTemplate = this.ItemTemplate;
			if(subNavigation.FooterTemplate == null)
				subNavigation.FooterTemplate = this.FooterTemplate;
			if(subNavigation.PopulateNavigationItemDelegate == null)
				subNavigation.PopulateNavigationItemDelegate = this.PopulateNavigationItemDelegate;
			if(subNavigation.GetNavigationItemNameDelegate == null)
				subNavigation.GetNavigationItemNameDelegate = this.GetNavigationItemNameDelegate;
			if(subNavigation.GetNavigationItemLinkDelegate == null)
				subNavigation.GetNavigationItemLinkDelegate = this.GetNavigationItemLinkDelegate;
			if(subNavigation.GetNavigationItemDelegate == null)
				subNavigation.GetNavigationItemDelegate = this.GetNavigationItemDelegate;
			if(subNavigation.GetNavigationAnchorDelegate == null)
				subNavigation.GetNavigationAnchorDelegate = this.GetNavigationAnchorDelegate;
			if(subNavigation.CreateWebNavigationControlDelegate == null)
				subNavigation.CreateWebNavigationControlDelegate = this.CreateWebNavigationControlDelegate;
		}

		protected virtual void OnDataBound(EventArgs e) {
			EventHandler eventHandler = base.Events[WebNavigation.eventDataBound] as EventHandler;
			if(eventHandler != null) {
				eventHandler(this, e);
			}
		}

		private void ItemDataBound(object sender, RepeaterItemEventArgs e) {
			this.OnItemDataBound(e);
		}
		#endregion

		#region Methods
		public override void DataBind() {
		
			if(this.HeaderTemplate == null)
				this.HeaderTemplate = new DefaultHeaderTemplate(this.CssClass);
			if(this.ItemTemplate == null)
				this.ItemTemplate = new DefaultItemTemplate();
			if(this.FooterTemplate == null)
				this.FooterTemplate = new DefaultFooterTemplate();
			if(this.PopulateNavigationItemDelegate == null)
				this.PopulateNavigationItemDelegate = PopulateNavigationItem;
			if(this.GetNavigationItemNameDelegate == null)
				this.GetNavigationItemNameDelegate = GetNavigationItemName;
			if(this.GetNavigationItemLinkDelegate == null)
				this.GetNavigationItemLinkDelegate = GetNavigationItemLink;
			if(this.GetNavigationItemDelegate == null)
				this.GetNavigationItemDelegate = GetNavigationItem;
			if(this.GetNavigationAnchorDelegate == null)
				this.GetNavigationAnchorDelegate = GetNavigationAnchor;
			if(this.CreateWebNavigationControlDelegate == null)
				this.CreateWebNavigationControlDelegate = this.CreateWebNavigationControl;

			this.OnDataBinding(EventArgs.Empty);

			if(this.StartLevelFromRoot < 0)
				this.StartLevelFromRoot = this.ExpandAll ? 0 : 1;
			
			if(this.SelectedNavigationPage == null)
				this.SelectedNavigationPage = this.CurrentPage;

			if(this.RootPage == null || this.RootPage.Id == null)
				this.RootPage = this.SiteRoot;

			if(!this.HeadingVisible && !this.HeadingAlwaysVisible)
				hedHeadline.Visible = false;

			if(this.NavPage == null)
				this.NavPage = GetPageToExpand(this.RootPage, this.SelectedNavigationPage, this.StartLevelFromRoot);

			if(this.NavPage == null && this.FallbackStartLevel >= 0)
				this.NavPage = GetPageToExpand(this.RootPage, this.SelectedNavigationPage, this.FallbackStartLevel);

			if(this.NavPage != null) {
				WebNavigationPageArgs npa = new WebNavigationPageArgs(this.NavPage);
				
				ancHeadline.Text = this.GetNavigationItemNameDelegate(npa);
				if(this.HeadingClickable){
					ancHeadline.NavigateUrl = this.GetNavigationItemLinkDelegate(npa);
				}else{
					ancHeadline.NoLink = true;
				}

				NavigationPageCollection childNodes;
				if(this.IncludeRootLevelInList){
					childNodes = new NavigationPageCollection();
					childNodes.Add(this.NavPage);
				} else {
					childNodes = this.GetChildren(this.NavPage);
				}

				NavigationPageCollection removedChildNodes = this.IncludeChildrenOfRemovedNodesMode > IncludeChildrenOfRemovedNodesMode.Never	? 
					new NavigationPageCollection() : null;
				childNodes = FilterNavigation(childNodes, removedChildNodes);

				if(this.IncludeChildrenOfRemovedNodesMode > IncludeChildrenOfRemovedNodesMode.Never) {
					NavigationPageCollection nonRemovedDescendants = GetNonRemovedDescendants(removedChildNodes);
					if(this.IncludeChildrenOfRemovedNodesMode.Equals(IncludeChildrenOfRemovedNodesMode.OnlyInSelectedPath)) {
						foreach(INavigationPage descendant in nonRemovedDescendants) {
							if(this.IsPageInSelectedPath(this.RootPage, descendant, this.SelectedNavigationPage)) {
								childNodes.Add(descendant);
							}
						}
					} else {
						childNodes.AddRange(nonRemovedDescendants);
					}
				}
				rptNavigation.DataSource = childNodes;
				rptNavigation.DataBind();
			}

			if(this.visibleChildren == 0) {
				if(this.HeadingAlwaysVisible && ancHeadline.Text.Length > 0)
					rptNavigation.Visible = false;
				else
					this.Visible = false;
			} else {
				this.visibleLevels++;
			}
			OnDataBound(EventArgs.Empty);
		}

		public static void PopulateNavigationItem(WebNavigationItemArgs args) {
			if(args == null)
				throw new ArgumentNullException("args");
			string liClass	= string.Empty;
			string aClass	= string.Empty;

			if(args.IsSelected || args.IsInSelectedPath){
				aClass = liClass = args.IsSelected ? "selected" : "selected-path";
			}
			
			if(args.IsFirstItem)
				liClass = liClass + (liClass.Length > 0 ? " " : string.Empty) + "first-item";
			if(args.IsLastItem)
				liClass = liClass + (liClass.Length > 0 ? " " : string.Empty) + "last-item";
			if(args.IsChildOfRemovedNode) 
				liClass = liClass + (liClass.Length > 0 ? " " : string.Empty) + "child-of-removed-node";

			if(args.ParentNavigation.IndicateIfNodeHasChildren){
				NavigationPageCollection children = args.ParentNavigation.GetChildren(args.NavigationPage);
				NavigationPageCollection removedChildren = args.ParentNavigation.IncludeChildrenOfRemovedNodesMode > IncludeChildrenOfRemovedNodesMode.Never ? 
					new NavigationPageCollection() : null;
				children = args.ParentNavigation.FilterNavigation(children, removedChildren);

				if(args.ParentNavigation.IncludeChildrenOfRemovedNodesMode > IncludeChildrenOfRemovedNodesMode.Never) {
					NavigationPageCollection nonRemovedDescendants = args.ParentNavigation.GetNonRemovedDescendants(removedChildren);
					if(args.ParentNavigation.IncludeChildrenOfRemovedNodesMode.Equals(IncludeChildrenOfRemovedNodesMode.OnlyInSelectedPath)) {
						foreach(INavigationPage descendant in nonRemovedDescendants) {
							if(args.ParentNavigation.IsPageInSelectedPath(args.ParentNavigation.RootPage, descendant, args.ParentNavigation.SelectedNavigationPage)) {
								children.Add(descendant);
							}
						}
					} else {
						children.AddRange(nonRemovedDescendants);
					}
				}
				if(children.Count > 0){
					liClass	= liClass + (liClass.Length > 0 ? " " : string.Empty) + "has-children";
					aClass	= aClass +  (aClass.Length > 0 ? " " : string.Empty) + "has-children";
				}
			}

			WebGenericControl li = args.NavigationItem as WebGenericControl;
			if(liClass.Trim().Length > 0 && li != null) {
				li.CssClass = liClass;
			}

			// Find UI Control
			WebAnchor ancNavigationItem = args.ParentNavigation.GetNavigationAnchorDelegate(args);
			if(ancNavigationItem == null)
				throw new HttpException("The default implementation of PopulateNavigationItem requires at least one WebAnchor inside the ItemTemplate.");
			ancNavigationItem.Visible = true;
			ancNavigationItem.CssClass += " " + aClass;
			if(string.IsNullOrEmpty(ancNavigationItem.Text)) {
				ancNavigationItem.Text = args.ParentNavigation.GetNavigationItemNameDelegate(args);
			}
			if(!args.IsSelected || (args.IsSelected && args.ParentNavigation.SelectedPageClickable) || args.ParentNavigation.RootLevelNotClickable){
				if(string.IsNullOrEmpty(ancNavigationItem.NavigateUrl)) {
					if(args.ExpandingLevel && args.ParentNavigation.RootLevelNotClickable && args.SubNavigation != null) {
						args.NavigationItem.ID = HttpUtility.UrlEncode(args.SubNavigation.ClientID.Replace("_", string.Empty) + "Li");
						if(li != null)
							li.RenderId = true;
						ancNavigationItem.NavigateUrl = "#" + args.NavigationItem.ID;
						ancNavigationItem.Attributes.Add("onmousedown", "this.href=this.href+':stop';return true;");
						ancNavigationItem.Attributes.Add("onclick", "if(this.href.substring(this.href.length - 5,this.href.length)==':stop'){this.href=this.href.substring(0,this.href.length-5);return false;}else{return true;}");
					} else {
						ancNavigationItem.NavigateUrl = args.ParentNavigation.GetNavigationItemLinkDelegate(args);
					}
				}
				if(args.ParentNavigation.AlwaysPlaceSpanAroundNavItem) {
					WebGenericControl gnrNavigationItem = new WebGenericControl("span");
					args.NavigationItem.Controls.AddAt(0, gnrNavigationItem);
					gnrNavigationItem.Controls.Add(ancNavigationItem);
				} else {
					args.NavigationItem.Controls.AddAt(0, ancNavigationItem);
				}
			}else{
				ancNavigationItem.InnerSpan = true;
				ancNavigationItem.NoLink = true;
				args.NavigationItem.Controls.AddAt(0, ancNavigationItem);
			}
		}

		public static WebAnchor GetNavigationAnchor(WebNavigationItemArgs args) {
			if(args == null) {
				throw new ArgumentNullException("args");
			}
			return ControlHandler.FindFirstControlOnType(args.NavigationItem, typeof(WebAnchor)) as WebAnchor;
		}

		public static Control GetNavigationItem(RepeaterItemEventArgs args) {
			if(args == null) {
				throw new ArgumentNullException("args");
			}
			Control listControl = ControlHandler.FindFirstControlOnType(args.Item, typeof(WebGenericControl));
			if(listControl != null)
				return listControl;
			return args.Item;
		}

		protected INavigationPage GetPageToExpand(INavigationPage navigationRoot, INavigationPage currentPageReference, int startLevel) {

			if(startLevel == 0)
				return this.GetNavigationPage(navigationRoot);

			NavigationPageCollection pagePathToRoot = GetSelectedPath(navigationRoot, currentPageReference, new NavigationPageCollection());
			if(pagePathToRoot.Count > startLevel){
				return pagePathToRoot[startLevel];
			}
			return null;
		}

		protected NavigationPageCollection GetSelectedPath(INavigationPage navigationRoot, INavigationPage currentPageReference, NavigationPageCollection pageDataCollection)  {
			if(pageDataCollection == null)
				throw new ArgumentNullException("pageDataCollection");

			INavigationPage subPage = this.GetNavigationPage(currentPageReference);
			if(subPage != null){
				int count = pageDataCollection.Count;
				pageDataCollection.Insert(0, subPage);
				if(count == 0){ // Remove all non visible pages unitl if there already isn't a visible page in the path
					pageDataCollection = this.FilterNavigation(pageDataCollection, null);
				}
			}
			INavigationPage parent = subPage != null ? this.GetNavigationPage(subPage.ParentId) : null;
			if(subPage == null || subPage.Id.Equals(navigationRoot.Id) || parent == null){
				return pageDataCollection;
			}
			return this.GetSelectedPath(navigationRoot, parent, pageDataCollection);
		}

		protected bool IsPageSelected(INavigationPage navigationRoot, INavigationPage pageReference, INavigationPage currentPage) {
			if(currentPage == null)
				return false;
			if(this.IncludeRootLevelInList && currentPage.Id.Equals(navigationRoot.Id))
				return true;
			if(!this.VisibleInNavigation(currentPage)) {
				INavigationPage parent = this.GetNavigationPage(currentPage.ParentId);
				if(parent == null)
					return false;
				return this.IsPageSelected(navigationRoot, pageReference, parent);
			}
			return currentPage.Id.Equals(pageReference.Id);
		}
		
		protected bool IsPageChildOfRemovedNode(INavigationPage navigationRoot, INavigationPage currentPage) {
			if(currentPage == null) {
				throw new ArgumentNullException("currentPage");
			}
			INavigationPage page = currentPage;
			bool isVisibleFromNavigationRoot = true;
			while(page != null && !page.Id.Equals(navigationRoot.Id)) {
			    isVisibleFromNavigationRoot = this.FilterNavigation(new NavigationPageCollection(new[] { page }), null).Count.Equals(1);
			    if(!isVisibleFromNavigationRoot) {
			        break;
			    }
			    page = this.GetNavigationPage(page.ParentId);
			}
			return !isVisibleFromNavigationRoot;
		}

		protected virtual bool VisibleInNavigation(INavigationPage page) {
			if(page == null)
				throw new ArgumentNullException("page");
			
			NavigationPageCollection pdc = new NavigationPageCollection();
			pdc.Add(page);
			pdc = this.FilterNavigation(pdc, null);
			return pdc.Count > 0;
		}

		protected bool IsPageInSelectedPath(INavigationPage navigationRoot, INavigationPage navigationItemReference, INavigationPage treeReference) {
			if(this.IncludeRootLevelInList && treeReference != null && treeReference.Id.Equals(navigationRoot.Id))
				return true;
			if (treeReference == null || treeReference.Id == null || treeReference.Id.Equals(navigationRoot.Id)) 
				return false;
			if (navigationItemReference.Id.Equals(treeReference.Id)|| navigationItemReference.Id.Equals(navigationRoot.Id))
				return true;
			INavigationPage parent = this.GetNavigationPage(treeReference.ParentId);
			if(parent != null && treeReference.Id.Equals(parent.Id))
				return false;
			return this.IsPageInSelectedPath(navigationRoot, navigationItemReference, parent);
		}

		protected NavigationPageCollection GetNonRemovedDescendants(NavigationPageCollection removedNodes) {
			if(removedNodes == null) {
				throw new ArgumentNullException("removedNodes");
			}
			NavigationPageCollection nonRemovedDescendants = new NavigationPageCollection();
			foreach(INavigationPage removedNode in removedNodes) {
				NavigationPageCollection removedChildNodes = new NavigationPageCollection();
				nonRemovedDescendants.AddRange(this.GetChildren(removedNode));
				nonRemovedDescendants = this.FilterNavigation(nonRemovedDescendants, removedChildNodes);

				nonRemovedDescendants.AddRange(GetNonRemovedDescendants(removedChildNodes));
			}
			return nonRemovedDescendants;
		}
		#endregion

		#region Inner Classes
		private sealed class DefaultHeaderTemplate : ITemplate {

			private readonly string cssClass;

			public DefaultHeaderTemplate(string cssClass) {
				this.cssClass	= cssClass;
			}

			public void InstantiateIn(Control container) {
				WebPlaceHolder ulBeginTag = new WebPlaceHolder();
				string css = string.Empty;
				if(!string.IsNullOrEmpty(this.cssClass))
					css = " class=\"" + this.cssClass + "\"";

				ulBeginTag.InnerHtml = "<ul" + css + ">";
				container.Controls.Add(ulBeginTag);
			}
		}

		private sealed class DefaultFooterTemplate : ITemplate {
			public void InstantiateIn(Control container) {
				WebPlaceHolder ulEndTag = new WebPlaceHolder();
				ulEndTag.InnerHtml = "</ul>";
				container.Controls.Add(ulEndTag);
			}
		}

		private sealed class DefaultItemTemplate : ITemplate {
			public void InstantiateIn(Control container) {
				WebGenericControl li = new WebGenericControl(HtmlTag.Li);
				WebAnchor ancNavigationItem = new WebAnchor();
				li.Controls.Add(ancNavigationItem);
				container.Controls.Add(li);
			}
		}
		#endregion
	}

	#region Event Args
	public class WebNavigationPageArgs {

		private readonly INavigationPage	navigationPage;

		public INavigationPage				NavigationPage			{ get { return this.navigationPage; }  }

		public WebNavigationPageArgs(INavigationPage navigationPage) {
			if(navigationPage == null)
				throw new ArgumentNullException("navigationPage");
			this.navigationPage			= navigationPage;
		}
	}

	public class WebNavigationItemArgs : WebNavigationPageArgs {

		private readonly WebNavigation	parentNavigation;
		private readonly Control		navigationItem;
		private WebNavigation			subNavigation;
		private bool					isSelected;
		private bool					isInSelectedPath;
		private bool					isChildOfRemovedNode;
		private int						itemIndex;
		private bool					isFirstItem;
		private bool					isLastItem;
		private bool					expandingLevel;

		public Control					NavigationItem		{ get { return this.navigationItem; } }
		public WebNavigation			ParentNavigation	{ get { return this.parentNavigation; } }
		public WebNavigation			SubNavigation		{ get { return this.subNavigation; }		set { this.subNavigation = value; } }
		public bool						IsSelected			{ get { return this.isSelected; }			set { this.isSelected = value; } }
		public bool						IsInSelectedPath	{ get { return this.isInSelectedPath; }		set { this.isInSelectedPath = value; } }
		public bool						IsChildOfRemovedNode{ get { return this.isChildOfRemovedNode; }	set { this.isChildOfRemovedNode = value; } }
		public int						ItemIndex			{ get { return this.itemIndex; }			set { this.itemIndex = value; } }
		public bool						IsFirstItem			{ get { return this.isFirstItem; }			set { this.isFirstItem = value; } }
		public bool						IsLastItem			{ get { return this.isLastItem; }			set { this.isLastItem = value; } }
		public bool						ExpandingLevel		{ get { return this.expandingLevel; }		set { this.expandingLevel = value; } }

		public WebNavigationItemArgs(Control navigationItem, WebNavigation parentNavigation, INavigationPage pageData, bool isSelected, bool isInSelectedPath, int itemIndex, bool isFirstItem, bool isLastItem, bool expandingLevel, bool isChildOfRemovedNode, WebNavigation subNavigation) : base(pageData) {
			if(navigationItem == null)
				throw new ArgumentNullException("navigationItem");
			if(parentNavigation == null)
				throw new ArgumentNullException("parentNavigation");

			this.parentNavigation		= parentNavigation;
			this.subNavigation			= subNavigation;
			this.navigationItem			= navigationItem;
			this.isSelected				= isSelected;
			this.isInSelectedPath		= isInSelectedPath;
			this.isChildOfRemovedNode	= isChildOfRemovedNode;
			this.itemIndex				= itemIndex;
			this.isFirstItem			= isFirstItem;
			this.isLastItem				= isLastItem;
			this.expandingLevel			= expandingLevel;
		}
	}
	#endregion

	#region Enums
	public enum IncludeChildrenOfRemovedNodesMode {
		Never,
		Always,
		OnlyInSelectedPath
	}
	#endregion

}
