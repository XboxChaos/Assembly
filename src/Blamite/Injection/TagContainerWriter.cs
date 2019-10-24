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
			WritePredictions(tags, container, writer);

			container.EndBlock();
		}

		private static void WriteDataBlocks(TagContainer tags, ContainerWriter container, IWriter writer)
		{
			foreach (DataBlock dataBlock in tags.DataBlocks)
			{
				container.StartBlock("data", 7);

				// Main data
				writer.WriteUInt32(dataBlock.OriginalAddress);
				writer.WriteInt32(dataBlock.EntryCount);
				writer.WriteInt32(dataBlock.Alignment);
				writer.WriteByte((byte)(dataBlock.Sortable == true ? 1 : 0));
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

				// Unicode string list fixups
				writer.WriteInt32(dataBlock.UnicListFixups.Count);
				foreach (DataBlockUnicListFixup unicList in dataBlock.UnicListFixups)
				{
					writer.WriteInt32(unicList.LanguageIndex);
					writer.WriteInt32(unicList.WriteOffset);
					writer.WriteInt32(unicList.Strings.Length);
					foreach (UnicListFixupString str in unicList.Strings)
					{
						writer.WriteAscii(str.StringID);
						writer.WriteUTF8(str.String);
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
				writer.WriteInt32(page.AssetCount);
				writer.WriteInt32(page.Unknown2);

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
				container.StartBlock("rsrc", 2);

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
					if (resource.Location.PrimarySize != null)
					{
						writer.WriteInt32(resource.Location.PrimarySize.Size);
						writer.WriteByte((byte)resource.Location.PrimarySize.Parts.Count);
						foreach (ResourceSizePart part in resource.Location.PrimarySize.Parts)
						{
							writer.WriteInt32(part.Offset);
							writer.WriteInt32(part.Size);
						}
					}
					else
						writer.WriteInt32(-1);

					writer.WriteInt32(resource.Location.OriginalSecondaryPageIndex);
					writer.WriteInt32(resource.Location.SecondaryOffset);
					if (resource.Location.SecondarySize != null)
					{
						writer.WriteInt32(resource.Location.SecondarySize.Size);
						writer.WriteByte((byte)resource.Location.SecondarySize.Parts.Count);
						foreach (ResourceSizePart part in resource.Location.SecondarySize.Parts)
						{
							writer.WriteInt32(part.Offset);
							writer.WriteInt32(part.Size);
						}
					}
					else
						writer.WriteInt32(-1);

					writer.WriteInt32(resource.Location.OriginalTertiaryPageIndex);
					writer.WriteInt32(resource.Location.TertiaryOffset);
					if (resource.Location.TertiarySize != null)
					{
						writer.WriteInt32(resource.Location.TertiarySize.Size);
						writer.WriteByte((byte)resource.Location.TertiarySize.Parts.Count);
						foreach (ResourceSizePart part in resource.Location.TertiarySize.Parts)
						{
							writer.WriteInt32(part.Offset);
							writer.WriteInt32(part.Size);
						}
					}
					else
						writer.WriteInt32(-1);
				}
				else
				{
					writer.WriteByte(0);
				}

				writer.WriteInt32(resource.ResourceBits);
				writer.WriteInt32(resource.BaseDefinitionAddress);

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

		private static void WritePredictions(TagContainer tags, ContainerWriter container, IWriter writer)
		{
			foreach (var prediction in tags.Predictions)
			{
				container.StartBlock("pdct", 0);

				writer.WriteInt32(prediction.OriginalIndex);

				writer.WriteUInt32(prediction.OriginalTagIndex.Value);

				writer.WriteInt32(prediction.Unknown1);
				writer.WriteInt32(prediction.Unknown2);

				writer.WriteInt32(prediction.CEntries.Count);
				foreach (ExtractedResourcePredictionC expc in prediction.CEntries)
				{
					writer.WriteInt32(expc.BEntry.AEntries.Count);
					foreach (ExtractedResourcePredictionA expa in expc.BEntry.AEntries)
					{
						writer.WriteInt32(expa.OriginalResourceSubIndex);
						writer.WriteUInt32(expa.OriginalResourceIndex.Value);
						writer.WriteInt32(expa.OriginalResourceClass);
						writer.WriteAscii(expa.OriginalResourceName);
					}	
				}

				writer.WriteInt32(prediction.AEntries.Count);
				foreach (ExtractedResourcePredictionA expa in prediction.AEntries)
				{
					writer.WriteInt32(expa.OriginalResourceSubIndex);
					writer.WriteUInt32(expa.OriginalResourceIndex.Value);
					writer.WriteInt32(expa.OriginalResourceClass);
					writer.WriteAscii(expa.OriginalResourceName);
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