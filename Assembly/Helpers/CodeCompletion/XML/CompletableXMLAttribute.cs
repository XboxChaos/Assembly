using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.CodeCompletion.XML
{
    /// <summary>
    /// Represents an XML attribute which can be inserted via code completion.
    /// </summary>
    public class CompletableXMLAttribute
    {
        private List<CompletableXMLValue> _values = new List<CompletableXMLValue>();

        /// <summary>
        /// Constructs a new CompletableXMLAttribute.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="description">A brief description of the attribute.</param>
        public CompletableXMLAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }

        /// <summary>
        /// The attribute's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The attribute's description.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Values registered with the attribute.
        /// </summary>
        public IEnumerable<CompletableXMLValue> Values
        {
            get { return _values; }
        }

        /// <summary>
        /// Whether or not the attribute has any values registered with it.
        /// </summary>
        public bool HasValues
        {
            get { return _values.Count > 0; }
        }

        /// <summary>
        /// Registers a completable value with the attribute.
        /// </summary>
        /// <param name="value">The value to register.</param>
        public void RegisterValue(CompletableXMLValue value)
        {
            _values.Add(value);
        }
    }
}
