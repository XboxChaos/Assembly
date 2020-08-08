using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using Blamite.Blam;
using Blamite.Blam.Util;
using Blamite.IO;
using Blamite.Util;
using Blamite.Serialization;
using System.IO;

namespace Blamite.Blam.Scripting.Context
{
    public class XMLScriptingContextLoader
    {
        private readonly ICacheFile _cache;
        private readonly IStreamManager _manager;
        private readonly EngineDescription _description;

        public XMLScriptingContextLoader (ICacheFile cache, IStreamManager manager, EngineDescription engine)
        {
            _cache = cache;
            _manager = manager;
            _description = engine;
        }

        public ScriptingContextCollection LoadContext(string contextDefinitionPath, string unitSeatMappingPath)
        {
            var result = new ScriptingContextCollection();
            LoadUnitSeatMappings(unitSeatMappingPath, result);
            LoadCacheObjects(contextDefinitionPath, result);
            return result;
        }

        public void LoadCacheObjects(string path, ScriptingContextCollection collection)
        {
            var document = XDocument.Load(path);
            LoadCacheObjects(document, collection);
        }

        public void LoadCacheObjects(XDocument document, ScriptingContextCollection collection)
        {
            var tagElement = document.Element("tags");
            var tags = tagElement.Elements("tag");
            foreach(var tagname in tags)
            {
                LoadTag(tagname, collection);
            }
        }

        public void LoadUnitSeatMappings(string path, ScriptingContextCollection context)
        {
            XDocument doc = XDocument.Load(path);
            LoadUnitSeatMappings(doc, context);
        }

        public void LoadUnitSeatMappings(XDocument document, ScriptingContextCollection context)
        {
            var mappings = document.Element("UnitSeatMappings").Elements("Mapping");

            foreach (XElement mapping in mappings)
            {
                long index = XMLUtil.GetNumericAttribute(mapping, "Index");
                long count = XMLUtil.GetNumericAttribute(mapping, "Count");
                string name = XMLUtil.GetStringAttribute(mapping, "Name");
                UnitSeatMapping seatMapping = new UnitSeatMapping((short)index, (short)count, name);
                context.AddUnitSeatMapping(seatMapping);
            }
        }

        private void LoadTag(XElement tagName, ScriptingContextCollection collection)
        {
            string group = tagName.Attribute("name").Value;
            ITag tag = _cache.Tags.FindTagByGroup(group);

            if (tag != null)
            {
                foreach (var element in tagName.Elements())
                {
                    if (element.Name == "script_object")
                    {
                        using (var reader = _manager.OpenRead())
                        {
                            LoadScriptObject(element, collection, reader, tag.MetaLocation.AsOffset());
                        }
                    }
                    else if (element.Name == "wrapper")
                    {
                        using (var reader = _manager.OpenRead())
                        {
                            LoadWrapperBlock(element, collection, reader, tag.MetaLocation.AsOffset());
                        }
                    }
                }
            }
        }

        private void LoadWrapperBlock(XElement wrapper, ScriptingContextCollection collection, IReader reader, int baseOffset)
        {
            long offset = XMLUtil.GetNumericAttribute(wrapper, "offset");

            StructureLayout blockLayout = _description.Layouts.GetLayout("tag block");
            reader.SeekTo(baseOffset + offset);
            StructureValueCollection block = StructureReader.ReadStructure(reader, blockLayout);
            uint blockPointer = (uint)block.GetInteger("pointer");
            int blockOffset = _cache.MetaArea.PointerToOffset(_cache.PointerExpander.Expand(blockPointer));

            var scriptObjects = wrapper.Elements("script_object");
            foreach(var obj in scriptObjects)
            {
                LoadScriptObject(obj, collection, reader, blockOffset);
            }
        }

        private void LoadScriptObject(XElement scriptObject, ScriptingContextCollection collection, IReader reader, int baseOffset)
        {
            string objectName = XMLUtil.GetStringAttribute(scriptObject, "name");
            long offset = XMLUtil.GetNumericAttribute(scriptObject, "offset");

            XElement nameElement = scriptObject.Element("name");
            long nameOffset = XMLUtil.GetNumericAttribute(nameElement, "offset");
            string nameType = XMLUtil.GetStringAttribute(nameElement, "type");

            var children = scriptObject.Elements("child");

            StructureLayout blockLayout = _description.Layouts.GetLayout("tag block");
            reader.SeekTo(baseOffset + offset);
            StructureValueCollection block = StructureReader.ReadStructure(reader, blockLayout);
            int blockCount = (int)block.GetInteger("entry count");
            uint blockPointer = (uint)block.GetInteger("pointer");
            long expandedPointer = _cache.PointerExpander.Expand(blockPointer);
            StructureLayout objectLayout = GenerateObjectLayout(scriptObject);
            StructureValueCollection[] objectData = TagBlockReader.ReadTagBlock(reader, blockCount, expandedPointer, objectLayout, _cache.MetaArea);
            ScriptingContextObject[] objects = objectData.Select((obj, index) => ValueCollectionToContextObject(reader, obj, index, objectName, false, children)).ToArray();

            ScriptingContextBlock result = new ScriptingContextBlock(objectName, objects);
            collection.AddObjectGroup(result);
        }

        private StructureLayout GenerateObjectLayout(XElement scriptObject)
        {
            //string objectName = XMLUtil.GetStringAttribute(scriptObject, "name");
            //long offset = XMLUtil.GetNumericAttribute(scriptObject, "offset");
            long size = XMLUtil.GetNumericAttribute(scriptObject, "size");
            XElement nameElement = scriptObject.Element("name");
            long nameOffset = XMLUtil.GetNumericAttribute(nameElement, "offset");
            string nameType = XMLUtil.GetStringAttribute(nameElement, "type");
            var children = scriptObject.Elements("child");

            StructureLayout result = new StructureLayout((int)size);

            // Add the name field.
            if(nameType == "string")
            {
                result.AddBasicField("name", StructureValueType.Asciiz, (int)nameOffset);
            }
            else if(nameType == "stringid")
            {
                result.AddBasicField("name", StructureValueType.Int32, (int)nameOffset);
            }
            else
            {
                throw new Exception($"The name attribute \"{nameType}\" of a scripting context object is invalid.");
            }

            var blockLayout = _description.Layouts.GetLayout("tag block");

            // Add a tag block for each child object block.
            foreach(var child in children)
            {
                string childName = XMLUtil.GetStringAttribute(child, "name");
                long childOffset = XMLUtil.GetNumericAttribute(child, "offset");
                result.AddStructField(childName, (int)childOffset, blockLayout);
            }

            return result;
        }

        private ScriptingContextObject ValueCollectionToContextObject(IReader reader, StructureValueCollection collection, int index, string group, bool isChild, IEnumerable<XElement> childElements)
        {
            ScriptingContextObject result = new ScriptingContextObject(GetNameFromCollection(collection), index, group, isChild);

            // Handle children blocks.
            foreach(var element in childElements)
            {
                string childName = XMLUtil.GetStringAttribute(element, "name");
                StructureValueCollection childValues = collection.GetStruct(childName);

                int blockCount = (int)childValues.GetInteger("entry count");
                uint blockPointer = (uint)childValues.GetInteger("pointer");
                long expandedPointer = _cache.PointerExpander.Expand(blockPointer);
                var layout = GenerateObjectLayout(element);
                var childData = TagBlockReader.ReadTagBlock(reader, blockCount, expandedPointer, layout, _cache.MetaArea);

                // Create the child object block.
                var test = childData.Select((data, index) => new ScriptingContextObject(GetNameFromCollection(data), index, childName, true));
                ScriptingContextBlock child = new ScriptingContextBlock(childName, test);
                result.AddChildBlock(child);
            }
            return result;
        }

        private string GetNameFromCollection(StructureValueCollection collection)
        {
            string result;
            if (collection.HasString("name"))
            {
                result = collection.GetString("name").ToLowerInvariant();
            }
            else if (collection.HasInteger("name"))
            {
                var sid = new StringID(collection.GetInteger("name"));
                result = _cache.StringIDs.GetString(sid).ToLowerInvariant();
            }
            else
            {
                throw new Exception("A scripting context object didn't have a valid name attribute.");
            }

            if (result is null || result == "")
            {
                throw new Exception("Failed to retrieve the Name for a scripting context object.");
            }

            return result;
        }
    }
}
