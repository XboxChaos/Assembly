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

        public ScriptingContextCollection LoadContext(string path)
        {
            var document = XDocument.Load(path);
            return LoadContext(document);
        }

        public ScriptingContextCollection LoadContext(XDocument document)
        {
            var result = new ScriptingContextCollection();
            var tagElement = document.Element("tags");
            var tags = tagElement.Elements("tag");
            foreach(var tagname in tags)
            {
                LoadTag(tagname, result);
            }

            return result;
        }

        private void LoadTag(XElement tagName, ScriptingContextCollection collection)
        {
            string group = tagName.Attribute("name").Value;
            ITag tag = _cache.Tags.FindTagByGroup(group);

            foreach(var element in tagName.Elements())
            {
                if(element.Name == "script_object")
                {
                    using (var reader = _manager.OpenRead())
                    {
                        LoadScriptObject(element, collection, reader, tag.MetaLocation.AsOffset());
                    }
                }
                else if(element.Name == "wrapper_object")
                {
                    using (var reader = _manager.OpenRead())
                    {
                        LoadWrapperBlock(element, collection, reader, tag.MetaLocation.AsOffset());
                    }
                }
            }
        }

        private void LoadWrapperBlock(XElement wrapper, ScriptingContextCollection collection, IReader reader, int baseOffset)
        {
            //string objectName = XMLUtil.GetStringAttribute(wrapper, "name");
            long offset = XMLUtil.GetNumericAttribute(wrapper, "offset");
            //long size = XMLUtil.GetNumericAttribute(wrapper, "size");

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
            //long size = XMLUtil.GetNumericAttribute(scriptObject, "size");

            XElement nameElement = scriptObject.Element("name");
            long nameOffset = XMLUtil.GetNumericAttribute(nameElement, "offset");
            string nameType = XMLUtil.GetStringAttribute(nameElement, "type");

            var children = scriptObject.Elements("child_object");

            StructureLayout blockLayout = _description.Layouts.GetLayout("tag block");
            reader.SeekTo(baseOffset + offset);
            StructureValueCollection block = StructureReader.ReadStructure(reader, blockLayout);
            int blockCount = (int)block.GetInteger("entry count");
            uint blockPointer = (uint)block.GetInteger("pointer");
            long expandedPointer = _cache.PointerExpander.Expand(blockPointer);
            StructureLayout objectLayout = GenerateObjectLayout(scriptObject);
            StructureValueCollection[] objectData = TagBlockReader.ReadTagBlock(reader, blockCount, expandedPointer, objectLayout, _cache.MetaArea);
            var objects = objectData.Select((obj, index) => ValueCollectionToContextObject(reader, obj, index, children));

            ScriptingContextBlock result = new ScriptingContextBlock();
            result.Name = objectName;
            foreach(var obj in objects)
            {
                result.AddObject(obj);
            }

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
            var children = scriptObject.Elements("child_object");

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

        private ScriptingContextObject ValueCollectionToContextObject(IReader reader, StructureValueCollection collection, int index, IEnumerable<XElement> childElements)
        {
            ScriptingContextObject result = new ScriptingContextObject();

            if(collection.HasString("name"))
            {
                result.Name = collection.GetString("name");
            }
            else if(collection.HasInteger("name"))
            {
                result.Name = _cache.StringIDs.GetString((int)collection.GetInteger("name"));
            }
            else
            {
                throw new Exception("A scripting context object didn't have a valid name attribute.");
            }
            result.Index = index;

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
                ScriptingContextBlock child = new ScriptingContextBlock();
                child.Name = childName;

                for(int i = 0; i<childData.Length; i++)
                {
                    string name;
                    if (childData[i].HasString("name"))
                    {
                        name = childData[i].GetString("name");
                    }
                    else if (childData[i].HasInteger("name"))
                    {
                        var sid = new StringID(childData[i].GetInteger("name"));
                        name = _cache.StringIDs.GetString(sid).ToLowerInvariant();
                        if (name is null)
                        {
                            throw new Exception("Failed to retrieve the Name StringID for a scripting context object.");
                        }
                    }
                    else
                    {
                        throw new Exception("A scripting context object didn't have a valid name attribute.");
                    }

                    ScriptingContextObject childObject = new ScriptingContextObject
                    {
                        Name = name,
                        Index = i
                    };
                    child.AddObject(childObject);
                }
                result.AddChildObject(child);
            }
            return result;
        }
    }
}
