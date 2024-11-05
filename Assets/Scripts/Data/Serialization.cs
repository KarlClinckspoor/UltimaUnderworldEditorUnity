using System.Runtime.Serialization;
using UnityEngine;

[System.Serializable]
public struct SerializableVector2
{
	public float x;
	public float y;

	public SerializableVector2(float x, float y)
	{
		this.x = x;
		this.y = y;
	}

	public static implicit operator Vector2(SerializableVector2 v2)
	{
		return new Vector2(v2.x, v2.y);
	}
	public static implicit operator SerializableVector2(Vector2 v2)
	{
		return new SerializableVector2(v2.x, v2.y);
	}
}
[System.Serializable]
public struct SerializableVector3
{
	public float x;
	public float y;
	public float z;

	public SerializableVector3(float x, float y, float z)
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public static implicit operator Vector3(SerializableVector3 v3)
	{
		return new Vector3(v3.x, v3.y, v3.z);
	}
	public static implicit operator SerializableVector3(Vector3 v3)
	{
		return new SerializableVector3(v3.x, v3.y, v3.z);
	}
}
sealed class Vector3SerializationSurrogate : ISerializationSurrogate
{

	// Method called to serialize a Vector3 object
	public void GetObjectData(System.Object obj,
							  SerializationInfo info, StreamingContext context)
	{

		Vector3 v3 = (Vector3)obj;
		info.AddValue("x", v3.x);
		info.AddValue("y", v3.y);
		info.AddValue("z", v3.z);
		//Debug.Log(v3);
	}

	// Method called to deserialize a Vector3 object
	public System.Object SetObjectData(System.Object obj,
									   SerializationInfo info, StreamingContext context,
									   ISurrogateSelector selector)
	{

		Vector3 v3 = (Vector3)obj;
		v3.x = (float)info.GetValue("x", typeof(float));
		v3.y = (float)info.GetValue("y", typeof(float));
		v3.z = (float)info.GetValue("z", typeof(float));
		obj = v3;
		return obj;   // Formatters ignore this return value //Seems to have been fixed!
	}
}

sealed class Vector2SerializationSurrogate : ISerializationSurrogate
{

	// Method called to serialize a Vector3 object
	public void GetObjectData(System.Object obj,
							  SerializationInfo info, StreamingContext context)
	{

		Vector2 v2 = (Vector2)obj;
		info.AddValue("x", v2.x);
		info.AddValue("y", v2.y);
		//Debug.Log(v3);
	}

	// Method called to deserialize a Vector3 object
	public System.Object SetObjectData(System.Object obj,
									   SerializationInfo info, StreamingContext context,
									   ISurrogateSelector selector)
	{

		Vector3 v2 = (Vector3)obj;
		v2.x = (float)info.GetValue("x", typeof(float));
		v2.y = (float)info.GetValue("y", typeof(float));
		obj = v2;
		return obj;   // Formatters ignore this return value //Seems to have been fixed!
	}
}
