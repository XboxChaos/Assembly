using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam;
using Blamite.Blam.Resources;
using Blamite.IO;

namespace Blamite.Injection
{
    public static class TagContainerReader
    {
        public static TagContainer ReadTagContainer(IReader reader)
        {
            TagContainer tags = new TagContainer();

            ContainerReader containerFile = new ContainerReader(reader);
            if (!containerFile.NextBlock() || containerFile.BlockName != "tagc")
                throw new ArgumentException("Not a valid tag container file");

            containerFile.EnterBlock();
            ReadBlocks(reader, containerFile, tags);
            containerFile.LeaveBlock();

            return tags;
        }

        private static void ReadBlocks(IReader reader, ContainerReader containerFile, TagContainer tags)
        {
            while (containerFile.NextBlock())
            {
                switch (containerFile.BlockName)
                {
                    case "data":
                        // Data block
                        tags.AddDataBlock(ReadDataBlock(reader, containerFile.BlockVersion));
                        break;

                    case "tag!":
                        // Extracted tag
                        tags.AddTag(ReadTag(reader, containerFile.BlockVersion));
                        break;

                    case "rspg":
                        // Resource page
                        tags.AddResourcePage(ReadResourcePage(reader, containerFile.BlockVersion));
                        break;

                    case "rsrc":
                        // Resource info
                        tags.AddResource(ReadResource(reader, containerFile.BlockVersion));
                        break;
                }
            }
        }

        private static DataBlock ReadDataBlock(IReader reader, byte version)
        {
            if (version > 0)
                throw new InvalidOperationException("Unrecognized \"data\" block version");

            // Block data
            uint originalAddress = reader.ReadUInt32();
            byte[] data = ReadByteArray(reader);
            var block = new DataBlock(originalAddress, data);

            // Address fixups
            int numAddressFixups = reader.ReadInt32();
            for (int i = 0; i < numAddressFixups; i++)
            {
                uint dataAddress = reader.ReadUInt32();
                int writeOffset = reader.ReadInt32();
                block.AddressFixups.Add(new DataBlockAddressFixup(dataAddress, writeOffset));
            }

            // Tagref fixups
            int numTagFixups = reader.ReadInt32();
            for (int i = 0; i < numTagFixups; i++)
            {
                var datum = new DatumIndex(reader.ReadUInt32());
                int writeOffset = reader.ReadInt32();
                block.TagFixups.Add(new DataBlockTagFixup(datum, writeOffset));
            }

            // Resource reference fixups
            int numResourceFixups = reader.ReadInt32();
            for (int i = 0; i < numResourceFixups; i++)
            {
                var datum = new DatumIndex(reader.ReadUInt32());
                int writeOffset = reader.ReadInt32();
                block.ResourceFixups.Add(new DataBlockResourceFixup(datum, writeOffset));
            }

            return block;
        }

        private static ExtractedTag ReadTag(IReader reader, byte version)
        {
            if (version > 0)
                throw new InvalidOperationException("Unrecognized \"tag!\" block version");

            var datum = new DatumIndex(reader.ReadUInt32());
            uint address = reader.ReadUInt32();
            int tagClass = reader.ReadInt32();
            string name = reader.ReadAscii();
            return new ExtractedTag(datum, address, tagClass, name);
        }

        private static ResourcePage ReadResourcePage(IReader reader, byte version)
        {
            if (version > 0)
                throw new InvalidOperationException("Unrecognized \"rspg\" block version");

            var page = new ResourcePage();
            page.Index = reader.ReadInt32();
            page.Flags = reader.ReadByte();
            page.FilePath = reader.ReadAscii();
            if (page.FilePath.Length == 0)
                page.FilePath = null;
            page.Offset = reader.ReadInt32();
            page.UncompressedSize = reader.ReadInt32();
            page.CompressionMethod = (ResourcePageCompression)reader.ReadByte();
            page.CompressedSize = reader.ReadInt32();
            page.Checksum = reader.ReadUInt32();
            page.Hash1 = ReadByteArray(reader);
            page.Hash2 = ReadByteArray(reader);
            page.Hash3 = ReadByteArray(reader);
            page.Unknown1 = reader.ReadInt32();
            page.Unknown2 = reader.ReadInt32();
            page.Unknown3 = reader.ReadInt32();
            return page;
        }

        private static ExtractedResourceInfo ReadResource(IReader reader, byte version)
        {
            if (version > 0)
                throw new InvalidOperationException("Unrecognized \"rsrc\" block version");

            var originalIndex = new DatumIndex(reader.ReadUInt32());
            var resource = new ExtractedResourceInfo(originalIndex);
            resource.Flags = reader.ReadUInt32();
            resource.Type = reader.ReadInt32();
            resource.Info = ReadByteArray(reader);
            resource.OriginalParentTagIndex = new DatumIndex(reader.ReadUInt32());
            byte hasLocation = reader.ReadByte();
            if (hasLocation != 0)
            {
                resource.Location = new ExtractedResourcePointer();
                resource.Location.OriginalPrimaryPageIndex = reader.ReadInt32();
                resource.Location.PrimaryOffset = reader.ReadInt32();
                resource.Location.PrimaryUnknown = reader.ReadInt32();
                resource.Location.OriginalSecondaryPageIndex = reader.ReadInt32();
                resource.Location.SecondaryOffset = reader.ReadInt32();
                resource.Location.SecondaryUnknown = reader.ReadInt32();
            }
            resource.Unknown1 = reader.ReadInt32();
            resource.Unknown2 = reader.ReadInt32();
            resource.Unknown3 = reader.ReadInt32();

            int numResourceFixups = reader.ReadInt32();
            for (int i = 0; i < numResourceFixups; i++)
            {
                var fixup = new ResourceFixup();
                fixup.Offset = reader.ReadInt32();
                fixup.Address = reader.ReadUInt32();
                resource.ResourceFixups.Add(fixup);
            }

            int numDefinitionFixups = reader.ReadInt32();
            for (int i = 0; i < numDefinitionFixups; i++)
            {
                var fixup = new ResourceDefinitionFixup();
                fixup.Offset = reader.ReadInt32();
                fixup.Type = reader.ReadInt32();
                resource.DefinitionFixups.Add(fixup);
            }

            return resource;
        }

        private static byte[] ReadByteArray(IReader reader)
        {
            int size = reader.ReadInt32();
            if (size <= 0)
                return new byte[0];
            return reader.ReadBlock(size);
        }
    }
}
