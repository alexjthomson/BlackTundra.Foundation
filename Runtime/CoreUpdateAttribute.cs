using System;

namespace BlackTundra.Foundation {

    /// <summary>
    /// Attribute that marks a method to be executed on every frame.
    /// Methods decorated with this attribute can either have no parameters or take in a float parameter for
    /// the difference in time since the update was last invoked (delta-time).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class CoreUpdateAttribute : Attribute { }

}