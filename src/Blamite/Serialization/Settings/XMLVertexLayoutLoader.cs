using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Blamite.Util;

namespace Blamite.Serialization.Settings
{
	/// <summary>
	///     Loads vertex layouts defined in an XML document.
	/// </summary>
	public class XMLVertexLayoutLoader : IComplexSettingLoader
	{
		/// <summary>
		///     Loads setting data from a path.
		/// </summary>
		/// <param name="path">The path to load from.</param>
		/// <returns>
		///     The loaded setting data.
		/// </returns>
		public object LoadSetting(string path)
		{
			return LoadLayouts(path);
		}

		/// <summary>
		///     Loads all of the vertex layouts defined in an XML document.
		/// </summary>
		/// <param name="document">The XML document to load vertex layouts from.</param>
		/// <param name="path">The path to the original XML. For debugging purposes.</param>
		/// <returns>The layouts that were loaded.</returns>
		public static VertexLayoutCollection LoadLayouts(XDocument document, string path)
		{
			XContainer vertexTypesContainer = document.Element("vertexTypes");
			if (document == null)
				throw new ArgumentException("Invalid vertex layout document.\r\n{path}");

			var result = new VertexLayoutCollection();
			foreach (XElement vertex in vertexTypesContainer.Elements("vertex"))
			{
				VertexLayout layout = LoadLayout(vertex);
				result.AddLayout(layout);
			}
			return result;
		}

		/// <summary>
		///     Loads all of the vertex layouts defined in an XML document.
		/// </summary>
		/// <param name="documentPath">The path to the XML document to load.</param>
		/// <returns>The layouts that were loaded.</returns>
		public static VertexLayoutCollection LoadLayouts(string documentPath)
		{
			return LoadLayouts(XDocument.Load(documentPath), documentPath);
		}

		/// <summary>
		///     Loads a VertexLayout based off of information in a vertex XML element.
		/// </summary>
		/// <param name="vertexElement">The vertex element to create the layout from.</param>
		/// <returns>The VertexLayout that was created.</returns>
		private static VertexLayout LoadLayout(XElement vertexElement)
		{
			// Vertex tags have the format:
			// <vertex type="(type index)" name="(vertex name)">(elements)</vertex>
			int type = (int)XMLUtil.GetNumericAttribute(vertexElement, "type");
			string name = XMLUtil.GetStringAttribute(vertexElement, "name");
			var result = new VertexLayout(type, name);

			result.AddElements(LoadLayoutElements(vertexElement));
			return result;
		}

		private static IEnumerable<VertexElementLayout> LoadLayoutElements(XContainer container)
		{
			// Element tags have the format:
			// <value stream="(stream)" offset="(offset)" type="(type)" usage="(usage)" usageIndex="(usage index)" />
			return (from element in container.Elements("value")
				select new VertexElementLayout(
					(int)XMLUtil.GetNumericAttribute(element, "stream"),
					(int)XMLUtil.GetNumericAttribute(element, "offset"),
					XMLUtil.GetEnumAttribute<VertexElementType>(element, "type"),
					XMLUtil.GetEnumAttribute<VertexElementUsage>(element, "usage"),
					(int)XMLUtil.GetNumericAttribute(element, "usageIndex")));
		}
	}
}