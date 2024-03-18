using Atlas.Core;
using NUnit.Framework;

namespace Atlas.Serialize.Test;

[Category("Serialize")]
public class TestDataRepo : TestSerializeBase
{
	private DataRepo _dataRepo = new DataRepo(TestPath, "Test");
	private Call _call = new();

	[OneTimeSetUp]
	public void BaseSetup()
	{
		Initialize("TestDataRepo");
	}

	[SetUp]
	public void Setup()
	{
	}

	[Test, Description("Serialize int Save Load")]
	public void SerializeInt()
	{
		string keyId = "int";
		int input = 1;
		_dataRepo.Save(keyId, input, _call);
		int output = _dataRepo.Load<int>(keyId, _call);

		Assert.AreEqual(input, output);
	}

	[Test, Description("DataInstance int Save Load")]
	public void TestDataInstanceInt()
	{
		string groupId = "PagingTest";
		string keyId = "int";
		int input = 1;
		var instance = _dataRepo.Open<int>(groupId);
		instance.Save(_call, keyId, input);
		int output = instance.Load(_call, keyId);

		Assert.AreEqual(input, output);
	}

	[Test, Description("DataInstance paging")]
	public void TestDataInstancePaging()
	{
		string groupId = "PagingTest";
		int pageSize = 2;
		var instance = _dataRepo.Open<int>(groupId);
		instance.DeleteAll(_call);
		for (int i = 0; i < 5; i++)
		{
			instance.Save(_call, i.ToString(), i);
		}
		var pageView = instance.LoadPageView(_call, true)!;
		pageView.PageSize = pageSize;
		var results = pageView.Next().ToList();

		Assert.AreEqual(results.Count, pageSize);
	}

	[Test, Description("DataInstance paging")]
	public void TestDataInstancePagingIndex()
	{
		string groupId = "PagingIndexTest";
		int pageSize = 2;
		var instance = _dataRepo.Open<int>(groupId);
		instance.DeleteAll(_call);
		instance.AddIndex();
		for (int i = 0; i < 5; i++)
		{
			instance.Save(_call, i.ToString(), i);
		}
		var pageView = instance.LoadPageView(_call, true)!;
		pageView.PageSize = pageSize;
		var results = pageView.Next().ToList();

		Assert.AreEqual(results.Count, pageSize);
	}
}
