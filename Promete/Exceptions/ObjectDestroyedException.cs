namespace Promete.Exceptions;

[System.Serializable]
public class ObjectDestroyedException : System.Exception
{
	public ObjectDestroyedException() : base("You can not add the destroyed node or component.")
	{
	}

	public ObjectDestroyedException(string message) : base(message)
	{
	}

	public ObjectDestroyedException(string message, System.Exception inner) : base(message, inner)
	{
	}
}
