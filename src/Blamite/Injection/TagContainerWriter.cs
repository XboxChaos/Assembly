using Blamite.Blam.Resources;
using Blamite.IO;

namespace Blamite.Injection
{
	public static class TagContainerWriter
	{
		public static void WriteTagContainer(TagContainer tags, IWriter writer)
		{
			var container = new ContainerWriter(writer);
			container.StartBlock("tagc", 0);

			WriteDataBlocks(tags, container, writer);
			WriteTags(tags, container, writer);
			WriteExtractedResourcePages(tags, container, writer);
			WriteResourcePages(tags, container, writer);
			WriteResources(tags, container, writer);

			container.EndBlock();
		}

		private static void WriteDataBlocks(TagContainer tags, ContainerWriter container, IWriter writer)
		{
			foreach (DataBlock dataBlock in tags.DataBlocks)
			{
				container.StartBlock("data", 4);

				// Main data
				writer.WriteUInt32(dataBlock.OriginalAddress);
				writer.WriteInt32(dataBlock.EntryCount);
				writer.WriteInt32(dataBlock.Alignment);
				WriteByteArray(dataBlock.Data, writer);

				// Address fixups
				writer.WriteInt32(dataBlock.AddressFixups.Count);
				foreach (DataBlockAddressFixup blockRef in dataBlock.AddressFixups)
				{
					writer.WriteUInt32(blockRef.OriginalAddress);
					writer.WriteInt32(blockRef.WriteOffset);
				}

				// Tagref fixups
				writer.WriteInt32(dataBlock.TagFixups.Count);
				foreach (DataBlockTagFixup tagRef in dataBlock.TagFixups)
				{
					writer.WriteUInt32(tagRef.OriginalIndex.Value);
					writer.WriteInt32(tagRef.WriteOffset);
				}

				// Resource reference fixups
				writer.WriteInt32(dataBlock.ResourceFixups.Count);
				foreach (DataBlockResourceFixup resourceRef in dataBlock.ResourceFixups)
				{
					writer.WriteUInt32(resourceRef.OriginalIndex.Value);
					writer.WriteInt32(resourceRef.WriteOffset);
				}

				// StringID fixups
				writer.WriteInt32(dataBlock.StringIDFixups.Count);
				foreach (DataBlockStringIDFixup sid in dataBlock.StringIDFixups)
				{
					writer.WriteAscii(sid.OriginalString);
					writer.WriteInt32(sid.WriteOffset);
				}

				// Shader fixups
				writer.WriteInt32(dataBlock.ShaderFixups.Count);
				foreach (DataBlockShaderFixup shaderRef in dataBlock.ShaderFixups)
				{
					writer.WriteInt32(shaderRef.WriteOffset);
					if (shaderRef.Data != null)
					{
						writer.WriteInt32(shaderRef.Data.Length);
						writer.WriteBlock(shaderRef.Data);
					}
					else
					{
						writer.WriteInt32(0);
					}
				}

				container.EndBlock();
			}
		}

		private static void WriteTags(TagContainer tags, ContainerWriter container, IWriter writer)
		{
			foreach (ExtractedTag tag in tags.Tags)
			{
				container.StartBlock("tag!", 0);

				writer.WriteUInt32(tag.OriginalIndex.Value);
				writer.WriteUInt32(tag.OriginalAddress);
				writer.WriteInt32(tag.Class);
				writer.WriteAscii(tag.Name);

				container.EndBlock();
			}
		}

		private static void WriteResourcePages(TagContainer tags, ContainerWriter container, IWriter writer)
		{
			foreach (var page in tags.ResourcePages)
			{
				container.StartBlock("rspg", 1);

				writer.WriteInt32(page.Index);
				writer.WriteUInt16(page.Salt);
				writer.WriteByte(page.Flags);
				writer.WriteAscii(page.FilePath ?? "");
				writer.WriteInt32(page.Offset);
				writer.WriteInt32(page.UncompressedSize);
				writer.WriteByte((byte) page.CompressionMethod);
				writer.WriteInt32(page.CompressedSize);
				writer.WriteUInt32(page.Checksum);
				WriteByteArray(page.Hash1, writer);
				WriteByteArray(page.Hash2, writer);
				WriteByteArray(page.Hash3, writer);
				writer.WriteInt32(page.Unknown1);
				writer.WriteInt32(page.Unknown2);
				writer.WriteInt32(page.Unknown3);

				container.EndBlock();
			}
		}

		private static void WriteExtractedResourcePages(TagContainer tags, ContainerWriter container, IWriter writer)
		{
			foreach (var extractedPage in tags.ExtractedResourcePages)
			{
				container.StartBlock("ersp", 0);

				writer.WriteInt32(extractedPage.ResourcePageIndex);
				WriteByteArray(extractedPage.ExtractedPageData, writer);

				container.EndBlock();
			}
		}

		private static void WriteResources(TagContainer tags, ContainerWriter container, IWriter writer)
		{
			foreach (ExtractedResourceInfo resource in tags.Resources)
			{
				container.StartBlock("rsrc", 1);

				writer.WriteUInt32(resource.OriginalIndex.Value);
				writer.WriteUInt32(resource.Flags);
				if (resource.Type != null)
					writer.WriteAscii(resource.Type);
				else
					writer.WriteByte(0);
				WriteByteArray(resource.Info, writer);
				writer.WriteUInt32(resource.OriginalParentTagIndex.Value);
				if (resource.Location != null)
				{
					writer.WriteByte(1);
					writer.WriteInt32(resource.Location.OriginalPrimaryPageIndex);
					writer.WriteInt32(resource.Location.PrimaryOffset);
					writer.WriteInt32(resource.Location.PrimaryUnknown);
					writer.WriteInt32(resource.Location.OriginalSecondaryPageIndex);
					writer.WriteInt32(resource.Location.SecondaryOffset);
					writer.WriteInt32(resource.Location.SecondaryUnknown);
				}
				else
				{
					writer.WriteByte(0);
				}
				writer.WriteInt32(resource.Unknown1);
				writer.WriteInt32(resource.Unknown2);
				writer.WriteInt32(resource.Unknown3);

				writer.WriteInt32(resource.ResourceFixups.Count);
				foreach (ResourceFixup fixup in resource.ResourceFixups)
				{
					writer.WriteInt32(fixup.Offset);
					writer.WriteUInt32(fixup.Address);
				}

				writer.WriteInt32(resource.DefinitionFixups.Count);
				foreach (ResourceDefinitionFixup fixup in resource.DefinitionFixups)
				{
					writer.WriteInt32(fixup.Offset);
					writer.WriteInt32(fixup.Type);
				}

				container.EndBlock();
			}
		}

		private static void WriteByteArray(byte[] data, IWriter writer)
		{
			if (data != null)
			{
				writer.WriteInt32(data.Length);
				writer.WriteBlock(data);
			}
			else
			{
				writer.WriteInt32(0);
			}
		}
	}
}