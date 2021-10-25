using System;

namespace BlackTundra.Foundation {

    /// <summary>
    /// Attribute that has an order associated with it. This can help with order specific method execution.
    /// </summary>
    public class OrderedAttribute : Attribute {
        public readonly int order;
        protected OrderedAttribute(in int order) => this.order = order;
    }

    /// <summary>
    /// Marks a static method should be invoked when the application is initialised.
    /// </summary>
    [MethodImplements()]
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class CoreInitialiseAttribute : OrderedAttribute {
        public CoreInitialiseAttribute() : base(0) { }
        public CoreInitialiseAttribute(int order) : base(order) { }
    }

    /// <summary>
    /// Marks a static method should be invoked when the applcation is terminated.
    /// </summary>
    [MethodImplements()]
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class CoreTerminateAttribute : OrderedAttribute {
        public CoreTerminateAttribute() : base(0) { }
        public CoreTerminateAttribute(int order) : base(order) { }
    }

    /// <summary>
    /// Attribute that marks a method to be executed on every frame.
    /// Methods decorated with this attribute can either have no parameters or take in a float parameter for
    /// the difference in time since the update was last invoked (delta-time).
    /// </summary>
    [MethodImplements()]
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class CoreUpdateAttribute : OrderedAttribute {
        public CoreUpdateAttribute() : base(0) { }
        public CoreUpdateAttribute(int order) : base(order) { }
    }

    /// <summary>
    /// Indicates a static method is used as a validation method.
    /// </summary>
    [MethodImplements()]
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class CoreValidateAttribute : Attribute { }

}