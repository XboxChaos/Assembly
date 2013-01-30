using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.CodeCompletion.XML
{
    /// <summary>
    /// Represents an XML tag which can be inserted via code completion.
    /// </summary>
    public class CompletableXMLTag
    {
        private List<CompletableXMLAttribute> _attributes = new List<CompletableXMLAttribute>();

        private Dictionary<string, CompletableXMLAttribute> _attributesByName =
            new Dictionary<string, CompletableXMLAttribute>();

        /// <summary>
        /// Constructs a new CompletableXMLTag.
        /// </summary>
        /// <param name="name">The name of the XML tag.</param>
        /// <param name="description">A brief description of the tag.</param>
        public CompletableXMLTag(string name, string description)
        {
            Name = name;
            Description = description;
        }

        /// <summary>
        /// The tag's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The tag's description.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Completable attributes for the tag, ordered in the same way they were inserted.
        /// </summary>
        public IEnumerable<CompletableXMLAttribute> Attributes
        {
            get { return _attributes; }
        }

        /// <summary>
        /// Registers a completable attribute with the tag.
        /// </summary>
        /// <param name="attribute">The attribute to register.</param>
        public void RegisterAttribute(CompletableXMLAttribute attribute)
        {
            _attributes.Add(attribute);
            _attributesByName[attribute.Name] = attribute;
        }

        /// <summary>
        /// Finds a registered attribute by name and returns it.
        /// </summary>
        /// <param name="name">The name of the attribute to search for.</param>
        /// <returns>The attribute if it was found or null otherwise.</returns>
        public CompletableXMLAttribute FindAttributeByName(string name)
        {
            CompletableXMLAttribute result;
            if (_attributesByName.TryGetValue(name, out result))
                return result;
            return null;
        }
    }
}
