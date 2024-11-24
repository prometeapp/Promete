using System;

namespace Promete.Exceptions;

[Serializable]
public class ObjectDestroyedException : Exception
{
    public ObjectDestroyedException() : base("You can not add the destroyed node or component.")
    {
    }

    public ObjectDestroyedException(string message) : base(message)
    {
    }

    public ObjectDestroyedException(string message, Exception inner) : base(message, inner)
    {
    }
}
