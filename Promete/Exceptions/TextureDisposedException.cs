using System;
using System.Runtime.Serialization;

namespace Promete.Exceptions;

public class TextureDisposedException : System.Exception
{
	public TextureDisposedException() : base("You can not use the disposed texture.")
	{
	}

	public TextureDisposedException(string? message) : base(message)
	{
	}

	public TextureDisposedException(string? message, Exception? innerException) : base(message, innerException)
	{
	}
}
