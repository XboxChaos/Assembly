using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Backend.CodeCompletion.XML
{
    /// <summary>
    /// Represents an XML attribute value which can be inserted via code completion.
    /// </summary>
    public class CompletableXMLValue
    {
        /// <summary>
        /// Constructs a new CompletableXMLAttribute.
        /// </summary>
        /// <param name="name">The name of the value.</param>
        /// <param name="description">A brief description of the value.</param>
        public CompletableXMLValue(string name, string description)
        {
            Name = name;
            Description = description;
        }

        /// <summary>
        /// The value's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The value's description.
        /// </summary>
        public string Description { get; private set; }
    }
}
