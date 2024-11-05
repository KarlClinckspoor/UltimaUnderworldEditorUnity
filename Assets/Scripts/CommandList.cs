using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandList<T>{

	List<T> list;
	int size;

	public CommandList(int size)
	{
		list = new List<T>();
		this.size = size;
	}

	public void Add(T item)
	{
		list.Add(item);
		if(list.Count > size)
		{
			for (int i = 1; i < list.Count; i++)
			{
				list[i - 1] = list[i];
			}
			list.RemoveAt(list.Count - 1);
		}
	}

	public void Remove()
	{
		if (list.Count > 0)
			list.RemoveAt(list.Count - 1);
	}

	public T Pop()
	{
		if (list.Count == 0)
			return default(T);
		T last = list[list.Count - 1];
		Remove();
		return last;
	}
}
