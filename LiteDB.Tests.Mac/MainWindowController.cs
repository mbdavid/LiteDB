using System;

using Foundation;
using AppKit;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests.Mac
{
	public class TestEntry : NSObject
	{
		#region Computed Properties
		public string Title { get; set; } = "";
		public string Result { get; set; } = "";

		#endregion

		#region Constructors
		public TestEntry()
		{
		}

		public TestEntry(string title, string result)
		{
			Title = title;
			Result = result;
		}

		public object TestClassInstance { get; set; }
		public MethodInfo Method { get; set; }

		#endregion
	}

	public class TestOutlineDataSource : NSOutlineViewDataSource
	{
		#region Public Variables
		public ObservableCollection<TestEntry> Tests = new ObservableCollection<TestEntry>();
		#endregion

		#region Constructors
		public TestOutlineDataSource()
		{
		}

		void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			
		}

	#endregion

		#region Override Methods
		public override nint GetChildrenCount(NSOutlineView outlineView, NSObject item)
		{
			if (item == null)
			{
				return Tests.Count;
			}
			return 0;
		}

		public override NSObject GetChild(NSOutlineView outlineView, nint childIndex, NSObject item)
		{
			if (item == null)
			{
				return Tests[(int)childIndex];
			}

			return null;
		}

		public override bool ItemExpandable(NSOutlineView outlineView, NSObject item)
		{
			return false;

		}
		#endregion
	}

	public class TestOutlineDelegate : NSOutlineViewDelegate
	{
		#region Constants 
		private const string CellIdentifier = "ProdCell";
		#endregion

		#region Private Variables
		private TestOutlineDataSource DataSource;
		#endregion

		#region Constructors
		public TestOutlineDelegate(TestOutlineDataSource datasource)
		{
			this.DataSource = datasource;

			
			
		}
		#endregion

		#region Override Methods

		public override NSView GetView(NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
		{
			// This pattern allows you reuse existing views when they are no-longer in use.
			// If the returned view is null, you instance up a new view
			// If a non-null view is returned, you modify it enough to reflect the new data
			NSTextField view = (NSTextField)outlineView.MakeView(CellIdentifier, this);
			if (view == null)
			{
				view = new NSTextField();
				view.Identifier = CellIdentifier;
				view.BackgroundColor = NSColor.Clear;
				view.Bordered = false;
				view.Selectable = false;
				view.Editable = false;
			}

			// Cast item
			var product = item as TestEntry;

			// Setup view based on the column selected
			switch (tableColumn.Identifier)
			{
				case "TestColumn":
					view.StringValue = product.Title;
					break;
				case "ResultColumn":
					view.StringValue = product.Result;
					break;
			}

			return view;
		}
		#endregion
	}

	public partial class MainWindowController : NSWindowController
	{
		public MainWindowController(IntPtr handle) : base(handle)
		{
		}

		[Export("initWithCoder:")]
		public MainWindowController(NSCoder coder) : base(coder)
		{
		}

		public MainWindowController() : base("MainWindow")
		{
		}

		public async  override void AwakeFromNib()
		{
			base.AwakeFromNib();

			// Create data source and populate
			var DataSource = new TestOutlineDataSource();


			// Populate the outline
			TestOutlineView.DataSource = DataSource;
			TestOutlineView.Delegate = new TestOutlineDelegate(DataSource);

			DataSource.Tests.CollectionChanged += OnCollectionChanged;
		
			var classes = this.GetType().Assembly
	.GetTypes()
	.Where(y => y.GetCustomAttributes(true).OfType<TestFixtureAttribute>().Any())
	.ToDictionary(z => z.Name);

			var testListToShow = new List<TestEntry>();

			foreach (var test in classes)
			{
				var allmethods = test.Value.GetMethods().ToList();
				var methods = allmethods
				                  .Where(y => y.GetCustomAttributes(typeof(TestMethodAttribute)).Any())
				                  .ToList();

				foreach (var method in methods)
				{
					var testEntry = new TestEntry(method.Name, "")
					{
						//TestClassInstance = 
						Method = method
					};

					try
					{
						testEntry.TestClassInstance = test.Value.GetConstructor(new Type[] { }).Invoke(new object[] { });
					}
					catch (Exception ex)
					{
						testEntry.Result = ex.Message;
					}

					DataSource.Tests.Add(testEntry);
				}
			}

			await Task.Delay(1500);

			foreach (var test in DataSource.Tests)
			{
				try
				{
					await Task.Run(() => test.Method.Invoke(test.TestClassInstance, new object[] { }));

					test.Result = "OK!";
				}
				catch (Exception ex)
				{
					test.Result = ex.Message;
				}

				TestOutlineView.ReloadData();

				await Task.Delay(1000);
			}

		}

		void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			TestOutlineView.ReloadData();
		}

		public new MainWindow Window
		{
			get { return (MainWindow)base.Window; }
		}
	}
}
