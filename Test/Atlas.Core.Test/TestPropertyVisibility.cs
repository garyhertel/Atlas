using Atlas.Extensions;
using Atlas.Tabs;
using NUnit.Framework;
using System.Linq;
using System.Reflection;

namespace Atlas.Core.Test;

[Category("Core")]
public class TestPropertyVisibility : TestBase
{
	[OneTimeSetUp]
	public void BaseSetup()
	{
		Initialize("Core");
	}

	private PropertyInfo GetPropertyInfo(object obj, string propertyName)
	{
		return obj.GetType()
			.GetProperties()
			.Where(p => p.Name == propertyName)
			.Single();
	}

	private ListProperty GetListProperty(object obj, string propertyName)
	{
		PropertyInfo propertyInfo = GetPropertyInfo(obj, propertyName);
		return new ListProperty(obj, propertyInfo);
	}

	public bool Default { get; set; }

	[Test]
	public void TestDefault()
	{
		PropertyInfo propertyInfo = GetPropertyInfo(this, nameof(Default));
		Assert.IsTrue(propertyInfo.IsRowVisible());
	}


	[Hidden]
	public bool Hidden { get; set; }

	[Test]
	public void TestHidden()
	{
		PropertyInfo propertyInfo = GetPropertyInfo(this, nameof(Hidden));
		Assert.IsFalse(propertyInfo.IsRowVisible());
	}

	[HiddenRow]
	public bool HiddenRow { get; set; }

	[Test]
	public void TestHiddenRow()
	{
		PropertyInfo propertyInfo = GetPropertyInfo(this, nameof(HiddenRow));
		Assert.IsFalse(propertyInfo.IsRowVisible());
	}

	[HiddenColumn]
	public bool HiddenColumn { get; set; }

	[Test]
	public void TestHiddenColumn()
	{
		PropertyInfo propertyInfo = GetPropertyInfo(this, nameof(HiddenColumn));
		Assert.IsFalse(propertyInfo.IsColumnVisible());
	}

	[Hide(null)]
	public bool? HideNull { get; set; }

	[Test]
	public void TestHideNull()
	{
		ListProperty listProperty = GetListProperty(this, nameof(HideNull));
		Assert.IsTrue(listProperty.IsPropertyVisible);
		Assert.IsFalse(listProperty.IsRowVisible());
	}

	[Hide(null)]
	public bool? HideNullShow { get; set; } = true;

	[Test]
	public void TestHideNullShow()
	{
		ListProperty listProperty = GetListProperty(this, nameof(HideNullShow));
		Assert.IsTrue(listProperty.IsPropertyVisible);
		Assert.IsTrue(listProperty.IsRowVisible());
	}

	[HideRow(null)]
	public bool? HideRowNull { get; set; }

	[Test]
	public void TestHideRowNull()
	{
		ListProperty listProperty = GetListProperty(this, nameof(HideRowNull));
		Assert.IsTrue(listProperty.IsPropertyVisible);
		Assert.IsFalse(listProperty.IsRowVisible());
	}

	[HideRow(null)]
	public bool? HideRowNullShow { get; set; } = true;

	[Test]
	public void TestHideRowNullShow()
	{
		ListProperty listProperty = GetListProperty(this, nameof(HideRowNullShow));
		Assert.IsTrue(listProperty.IsPropertyVisible);
		Assert.IsTrue(listProperty.IsRowVisible());
	}

	[HideColumn(null)]
	public bool? HideColumnNull { get; set; }

	[Test]
	public void TestHideColumnNull()
	{
		ListProperty listProperty = GetListProperty(this, nameof(HideColumnNull));
		Assert.IsTrue(listProperty.IsPropertyVisible);
		Assert.IsFalse(listProperty.IsColumnVisible());
	}

	[HideColumn(null)]
	public bool? HideColumnNullShow { get; set; } = true;

	[Test]
	public void TestHideColumnNullShow()
	{
		ListProperty listProperty = GetListProperty(this, nameof(HideColumnNullShow));
		Assert.IsTrue(listProperty.IsPropertyVisible);
		Assert.IsTrue(listProperty.IsColumnVisible());
	}
}
