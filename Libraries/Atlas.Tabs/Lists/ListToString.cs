﻿using Atlas.Core;
using System.Collections;

namespace Atlas.Tabs
{
	public class ListToString
	{
		private const int MaxItems = 200000;

		[InnerValue]
		public object Object;
		
		public string Value { get; set; }

		[DataKey]
		public string DataKey;

		[DataValue]
		public object DataValue;

		public override string ToString() => Value;

		public ListToString(object obj)
		{
			Object = obj;
			if (obj == null)
				return;

			Value = obj.ToString();
			DataKey = TabUtils.GetDataKey(obj);
			DataValue = TabUtils.GetDataValue(obj);
		}

		public static ItemCollection<ListToString> Create(IEnumerable enumerable, int limit = MaxItems)
		{
			var list = new ItemCollection<ListToString>();
			foreach (object obj in enumerable)
			{
				list.Add(new ListToString(obj));
				if (list.Count > limit)
					break;
			}
			return list;
		}
	}
}
