
// ReSharper disable once CheckNamespace
namespace System
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class HelpTextAttribute : Attribute
    {
        public string Description { get; private set; }

        public HelpTextAttribute(string description)
        {
            if (description == null) throw new ArgumentNullException("description");

            this.Description = description;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.Description;
        }
    }
}
