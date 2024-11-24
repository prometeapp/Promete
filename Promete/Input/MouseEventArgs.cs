using System;

namespace Promete.Input;

public class MouseEventArgs(VectorInt position) : EventArgs
{
    /// <summary>
    ///     Get the button position related to the event.
    /// </summary>
    public VectorInt Position { get; } = position;
}
