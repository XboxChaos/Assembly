using System;
using Blamite.Blam;
using Blamite.Blam.Resources;
using Blamite.Blam.Resources.Sounds;
using Blamite.IO;

namespace Blamite.Injection
{
	public static class TagContainerReader
	{
		public static TagContainer ReadTagContainer(IReader reader)
		{
			var tags = new TagContainer();

			var containerFile = new ContainerReader(reader);
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

					case "ersp":
						// Extracted Raw Resource Page
						tags.AddExtractedResourcePage(ReadExtractedResourcePage(reader, containerFile.BlockVersion));
						break;

					case "rspg":
						// Resource page
						tags.AddResourcePage(ReadResourcePage(reader, containerFile.BlockVersion));
						break;

					case "rsrc":
						// Resource info
						tags.AddResource(ReadResource(reader, containerFile.BlockVersion));
						break;

					case "pdct":
						// Prediction info
						tags.AddPrediction(ReadPrediction(reader, containerFile.BlockVersion));
						break;

					case "sndc":
						// Sound platform codec
						tags.AddSoundCodec(ReadSoundCodec(reader, containerFile.BlockVersion));
						break;

					case "snpr":
						// Sound pitch range
						tags.AddSoundPitchRange(ReadSoundPitchRange(reader, containerFile.BlockVersion));
						break;

					case "snld":
						// Sound language pitch range
						tags.AddSoundLanguageDuration(ReadSoundLanguageDuration(reader, containerFile.BlockVersion));
						break;

					case "snpb":
						// Sound playback parameter
						tags.AddSoundPlayback(ReadSoundPlayback(reader, containerFile.BlockVersion));
						break;

					case "snsc":
						// Sound scale
						tags.AddSoundScale(ReadSoundScale(reader, containerFile.BlockVersion));
						break;

					case "spro":
						// Sound promotion
						tags.AddSoundPromotion(ReadSoundPromotion(reader, containerFile.BlockVersion));
						break;

					case "scpb":
						// Sound custom playback
						tags.AddSoundCustomPlayback(ReadSoundCustomPlayback(reader, containerFile.BlockVersion));
						break;

					case "snex":
						// Sound extra info
						tags.AddSoundExtraInfo(ReadSoundExtraInfo(reader, containerFile.BlockVersion));
						break;
				}
			}
		}

		private static DataBlock ReadDataBlock(IReader reader, byte version)
		{
			if (version > 8)
				throw new InvalidOperationException("Unrecognized \"data\" block version");

			// Block data
			uint originalAddress = reader.ReadUInt32();
			int entryCount = (version >= 1) ? reader.ReadInt32() : 1;
			int align = (version >= 3) ? reader.ReadInt32() : 4;
			bool sort = false;
			if (version >= 7)
			{
				byte sortval = reader.ReadByte();
				sort = sortval > 0;
			}
			byte[] data = ReadByteArray(reader);
			var block = new DataBlock(originalAddress, entryCount, align, sort, data);

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

			if (version >= 2)
			{
				// StringID fixups
				int numSIDFixups = reader.ReadInt32();
				for (int i = 0; i < numSIDFixups; i++)
				{
					string str = reader.ReadAscii();
					int writeOffset = reader.ReadInt32();
					block.StringIDFixups.Add(new DataBlockStringIDFixup(str, writeOffset));
				}
			}

			if (version >= 4)
			{
				// Shader fixups
				int numShaderFixups = reader.ReadInt32();
				for (int i = 0; i < numShaderFixups; i++)
				{
					int writeOffset = reader.ReadInt32();
					int shaderDataSize = reader.ReadInt32();
					byte[] shaderData = reader.ReadBlock(shaderDataSize);
					block.ShaderFixups.Add(new DataBlockShaderFixup(writeOffset, shaderData));
				}
			}

			if (version >= 5)
			{
				// Unicode string list fixups
				int numUnicListFixups = reader.ReadInt32();
				for (int i = 0; i < numUnicListFixups; i++)
				{
					// Version 5 is buggy and doesn't include a language index :x
					int languageIndex = i;
					if (version >= 6)
						languageIndex = reader.ReadInt32();

					int writeOffset = reader.ReadInt32();
					int numStrings = reader.ReadInt32();
					UnicListFixupString[] strings = new UnicListFixupString[numStrings];
					for (int j = 0; j < numStrings; j++)
					{
						string stringId = reader.ReadAscii();
						string str = reader.ReadUTF8();
						strings[j] = new UnicListFixupString(stringId, str);
					}
					block.UnicListFixups.Add(new DataBlockUnicListFixup(languageIndex, writeOffset, strings));
				}
			}

			if (version >= 7)
			{
				int numInteropFixups = reader.ReadInt32();
				for (int i = 0; i < numInteropFixups; i++)
				{
					uint dataAddress = reader.ReadUInt32();
					int writeOffset = reader.ReadInt32();
					int type = reader.ReadInt32();
					block.InteropFixups.Add(new DataBlockInteropFixup(type, dataAddress, writeOffset));
				}

				int numEffectFixups = reader.ReadInt32();
				for (int i = 0; i < numEffectFixups; i++)
				{
					int index = reader.ReadInt32();
					int writeOffset = reader.ReadInt32();
					int type = reader.ReadInt32();
					int effectDataSize = reader.ReadInt32();
					byte[] effectData = reader.ReadBlock(effectDataSize);
					block.EffectFixups.Add(new DataBlockEffectFixup(type, index, writeOffset, effectData));
				}
			}

			if (version >= 8)
			{
				int numSoundFixups = reader.ReadInt32();

				for (int i = 0; i < numSoundFixups; i++)
				{
					int codec = reader.ReadInt32();
					int prcount = reader.ReadInt32();
					int pr = reader.ReadInt32();
					int lpr = reader.ReadInt32();
					int pb = reader.ReadInt32();
					int sc = reader.ReadInt32();
					int promo = reader.ReadInt32();
					int cpb = reader.ReadInt32();
					int ex = reader.ReadInt32();
					block.SoundFixups.Add(new DataBlockSoundFixup(codec, prcount, pr, lpr, pb, sc, promo, cpb, ex));
				}
			}

			return block;
		}

		private static ExtractedTag ReadTag(IReader reader, byte version)
		{
			if (version > 0)
				throw new InvalidOperationException("Unrecognized \"tag!\" block version");

			var datum = new DatumIndex(reader.ReadUInt32());
			uint address = reader.ReadUInt32();
			int tagGroup = reader.ReadInt32();
			string name = reader.ReadAscii();
			return new ExtractedTag(datum, address, tagGroup, name);
		}

		private static ResourcePage ReadResourcePage(IReader reader, byte version)
		{
			if (version > 1)
				throw new InvalidOperationException("Unrecognized \"rspg\" block version");

			var page = new ResourcePage();
			page.Index = reader.ReadInt32();
			if (version > 0)
				page.Salt = reader.ReadUInt16();
			page.Flags = reader.ReadByte();
			page.FilePath = reader.ReadAscii();
			if (page.FilePath.Length == 0)
				page.FilePath = null;
			page.Offset = reader.ReadInt32();
			page.UncompressedSize = reader.ReadInt32();
			page.CompressionMethod = (ResourcePageCompression) reader.ReadByte();
			page.CompressedSize = reader.ReadInt32();
			page.Checksum = reader.ReadUInt32();
			page.Hash1 = ReadByteArray(reader);
			page.Hash2 = ReadByteArray(reader);
			page.Hash3 = ReadByteArray(reader);
			page.Unknown1 = reader.ReadInt32();
			page.AssetCount = reader.ReadInt32();
			page.Unknown2 = reader.ReadInt32();
			return page;
		}

		private static ExtractedPage ReadExtractedResourcePage(IReader reader, byte version)
		{
			if (version > 0)
				throw new InvalidOperationException("Unrecognized \"ersp\" block version");

			return new ExtractedPage
			{
				ResourcePageIndex = reader.ReadInt32(),
				ExtractedPageData = ReadByteArray(reader)
			};
		}

		private static ExtractedResourceInfo ReadResource(IReader reader, byte version)
		{
			if (version > 2)
				throw new InvalidOperationException("Unrecognized \"rsrc\" block version");

			var originalIndex = new DatumIndex(reader.ReadUInt32());
			var resource = new ExtractedResourceInfo(originalIndex);
			resource.Flags = reader.ReadUInt32();
			resource.Type = reader.ReadAscii();
			if (string.IsNullOrEmpty(resource.Type))
				resource.Type = null;
			resource.Info = ReadByteArray(reader);
			resource.OriginalParentTagIndex = new DatumIndex(reader.ReadUInt32());
			byte hasLocation = reader.ReadByte();
			if (hasLocation != 0)
			{
				resource.Location = new ExtractedResourcePointer();
				resource.Location.OriginalPrimaryPageIndex = reader.ReadInt32();
				resource.Location.PrimaryOffset = reader.ReadInt32();
				if (version > 1)
				{
					var size = reader.ReadInt32();
					if (size != -1)
					{
						ResourceSize newSize = new ResourceSize();
						newSize.Size = size;
						byte partCount = reader.ReadByte();
						for (int i = 0; i < partCount; i++)
						{
							ResourceSizePart newPart = new ResourceSizePart();
							newPart.Offset = reader.ReadInt32();
							newPart.Size = reader.ReadInt32();
							newSize.Parts.Add(newPart);
						}
						resource.Location.PrimarySize = newSize;
					}
					else
						resource.Location.PrimarySize = null;
				}
				else
				{
					resource.Location.PrimarySize = null;
					reader.Skip(4);
				}
					

				resource.Location.OriginalSecondaryPageIndex = reader.ReadInt32();
				resource.Location.SecondaryOffset = reader.ReadInt32();
				if (version > 1)
				{
					var size = reader.ReadInt32();
					if (size != -1)
					{
						ResourceSize newSize = new ResourceSize();
						newSize.Size = size;
						byte partCount = reader.ReadByte();
						for (int i = 0; i < partCount; i++)
						{
							ResourceSizePart newPart = new ResourceSizePart();
							newPart.Offset = reader.ReadInt32();
							newPart.Size = reader.ReadInt32();
							newSize.Parts.Add(newPart);
						}
						resource.Location.SecondarySize = newSize;
					}
					else
						resource.Location.SecondarySize = null;
				}
				else
				{
					resource.Location.SecondarySize = null;
					reader.Skip(4);
				}

				if (version > 1)
				{
					resource.Location.OriginalTertiaryPageIndex = reader.ReadInt32();
					resource.Location.TertiaryOffset = reader.ReadInt32();
					var size = reader.ReadInt32();
					if (size != -1)
					{
						ResourceSize newSize = new ResourceSize();
						newSize.Size = size;
						byte partCount = reader.ReadByte();
						for (int i = 0; i < partCount; i++)
						{
							ResourceSizePart newPart = new ResourceSizePart();
							newPart.Offset = reader.ReadInt32();
							newPart.Size = reader.ReadInt32();
							newSize.Parts.Add(newPart);
						}
						resource.Location.TertiarySize = newSize;
					}
					else
						resource.Location.TertiarySize = null;
				}
			}
			if (version == 1)
			{
				reader.BaseStream.Position += 4;
				resource.ResourceBits = reader.ReadUInt16();
				reader.BaseStream.Position += 2;
			}
			else
			{
				resource.ResourceBits = reader.ReadInt32();
			}
			resource.BaseDefinitionAddress = reader.ReadInt32();

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

		private static ExtractedResourcePredictionD ReadPrediction(IReader reader, byte version)
		{
			if (version > 0)
				throw new InvalidOperationException("Unrecognized \"pdct\" block version");

			var prediction = new ExtractedResourcePredictionD();

			prediction.OriginalIndex = reader.ReadInt32();
			prediction.OriginalTagIndex = new DatumIndex(reader.ReadUInt32());

			prediction.Unknown1 = reader.ReadInt32();
			prediction.Unknown2 = reader.ReadInt32();

			int cCount = reader.ReadInt32();
			for (int c = 0; c < cCount; c++)
			{
				var expc = new ExtractedResourcePredictionC();

				expc.BEntry = new ExtractedResourcePredictionB();

				int baCount = reader.ReadInt32();
				for (int a = 0; a < baCount; a++)
				{
					ExtractedResourcePredictionA expa = new ExtractedResourcePredictionA();
					expa.OriginalResourceSubIndex = reader.ReadInt32();
					expa.OriginalResourceIndex = new DatumIndex(reader.ReadUInt32());
					expa.OriginalResourceGroup = reader.ReadInt32();
					expa.OriginalResourceName = reader.ReadAscii();
					expc.BEntry.AEntries.Add(expa);
				}
				prediction.CEntries.Add(expc);
			}

			int aCount = reader.ReadInt32();
			for (int a = 0; a < aCount; a++)
			{
				ExtractedResourcePredictionA expa = new ExtractedResourcePredictionA();
				expa.OriginalResourceSubIndex = reader.ReadInt32();
				expa.OriginalResourceIndex = new DatumIndex(reader.ReadUInt32());
				expa.OriginalResourceGroup = reader.ReadInt32();
				expa.OriginalResourceName = reader.ReadAscii();
				prediction.AEntries.Add(expa);
			}

			return prediction;
		}

		private static ExtractedSoundCodec ReadSoundCodec(IReader reader, byte version)
		{
			if (version > 0)
				throw new InvalidOperationException("Unrecognized \"sndc\" block version");

			var codec = new SoundCodec();

			int originalIndex = reader.ReadInt32();

			codec.SampleRate = reader.ReadInt32();
			codec.Encoding = reader.ReadInt32();
			codec.Compression = reader.ReadInt32();

			return new ExtractedSoundCodec(originalIndex, codec);
		}

		private static ExtractedSoundPitchRange ReadSoundPitchRange(IReader reader, byte version)
		{
			if (version > 1)
				throw new InvalidOperationException("Unrecognized \"snpr\" block version");

			int originalIndex = reader.ReadInt32();

			string name = reader.ReadAscii();

			SoundPitchRangeParameter param = null;

			if (reader.ReadByte() == 1)
			{
				param = new SoundPitchRangeParameter();
				param.NaturalPitch = reader.ReadInt32();
				param.BendMin = reader.ReadInt32();
				param.BendMax = reader.ReadInt32();
				param.MaxGainPitchMin = reader.ReadInt32();
				param.MaxGainPitchMax = reader.ReadInt32();
				param.PlaybackPitchMin = reader.ReadInt32();
				param.PlaybackPitchMax = reader.ReadInt32();

				if (reader.ReadByte() == 1)
				{
					SoundPitchRangeDistance dist = new SoundPitchRangeDistance();
					dist.DontPlayDistance = reader.ReadFloat();
					dist.AttackDistance = reader.ReadFloat();
					dist.MinDistance = reader.ReadFloat();
					dist.MaxDistance = reader.ReadFloat();
					param.Distance = dist;
				}
			}

			bool hasdata = (reader.ReadByte() == 1);

			int reqcount = reader.ReadInt32();

			System.Collections.Generic.List<ExtractedSoundPermutation> perms = new System.Collections.Generic.List<ExtractedSoundPermutation>();

			int permcount = reader.ReadInt32();
			for (int p = 0; p < permcount; p++)
			{
				ExtractedSoundPermutation perm = new ExtractedSoundPermutation();
				perm.Name = reader.ReadAscii();
				perm.EncodedSkipFraction = reader.ReadInt32();
				perm.SampleSize = reader.ReadInt32();
				perm.EncodedGain = reader.ReadInt32();
				perm.EncodedPermutationInfoIndex = reader.ReadInt32();
				perm.FSBInfo = reader.ReadInt32();

				int chunkcount = reader.ReadInt32();
				perm.Chunks = new System.Collections.Generic.List<ExtractedSoundChunk>();
				for (int c = 0; c < chunkcount; c++)
				{
					SoundChunk chunk = new SoundChunk();
					chunk.FileOffset = reader.ReadInt32();
					chunk.EncodedSizeAndFlags = reader.ReadInt32();
					chunk.CacheIndex = reader.ReadInt32();
					chunk.XMA2BufferStart = reader.ReadInt32();
					chunk.XMA2BufferEnd = reader.ReadInt32();
					chunk.Unknown = reader.ReadInt32();
					chunk.Unknown1 = reader.ReadInt32();

					string bankSuffix = null;
					if (version >= 1)
						bankSuffix = reader.ReadAscii();

					perm.Chunks.Add(new ExtractedSoundChunk(chunk, bankSuffix));
				}

				int langcount = reader.ReadInt32();
				perm.Languages = new System.Collections.Generic.List<ExtractedSoundLanguagePermutation>();
				for (int l = 0; l < langcount; l++)
				{
					ExtractedSoundLanguagePermutation lang = new ExtractedSoundLanguagePermutation();
					lang.LanguageIndex = reader.ReadInt32();
					lang.SampleSize = reader.ReadInt32();

					int lchunkcount = reader.ReadInt32();
					lang.Chunks = new System.Collections.Generic.List<ExtractedSoundChunk>();
					for (int c = 0; c < lchunkcount; c++)
					{
						SoundChunk chunk = new SoundChunk();
						chunk.FileOffset = reader.ReadInt32();
						chunk.EncodedSizeAndFlags = reader.ReadInt32();
						chunk.CacheIndex = reader.ReadInt32();
						chunk.XMA2BufferStart = reader.ReadInt32();
						chunk.XMA2BufferEnd = reader.ReadInt32();
						chunk.Unknown = reader.ReadInt32();
						chunk.Unknown1 = reader.ReadInt32();

						string bankSuffix = null;
						if (version >= 1)
							bankSuffix = reader.ReadAscii();

						lang.Chunks.Add(new ExtractedSoundChunk(chunk, bankSuffix));
					}

					perm.Languages.Add(lang);
				}

				int markercount = reader.ReadInt32();
				perm.LayerMarkers = new System.Collections.Generic.List<int>();
				for (int m = 0; m < markercount; m++)
				{
					perm.LayerMarkers.Add(reader.ReadInt32());
				}

				perms.Add(perm);
			}

			return new ExtractedSoundPitchRange(originalIndex, name, param, hasdata, reqcount, perms);
		}

		private static ExtractedSoundLanguageDuration ReadSoundLanguageDuration(IReader reader, byte version)
		{
			if (version > 0)
				throw new InvalidOperationException("Unrecognized \"snld\" block version");

			int originalIndex = reader.ReadInt32();
			var lpd = new ExtractedSoundLanguageDuration(originalIndex);

			int langcount = reader.ReadInt32();
			for (int l = 0; l < langcount; l++)
			{
				ExtractedSoundLanguageDurationInfo info = new ExtractedSoundLanguageDurationInfo();
				info.Durations = new System.Collections.Generic.List<int>();
				info.LanguageIndex = reader.ReadInt32();

				int durcount = reader.ReadInt32();
				for (int d = 0; d < durcount; d++)
				{
					info.Durations.Add(reader.ReadInt32());
				}
				lpd.Languages.Add(info);
			}

			return lpd;
		}

		private static ExtractedSoundPlayback ReadSoundPlayback(IReader reader, byte version)
		{
			if (version > 0)
				throw new InvalidOperationException("Unrecognized \"snpb\" block version");

			var playback = new SoundPlayback();

			int originalIndex = reader.ReadInt32();

			playback.InternalFlags = reader.ReadInt32();
			playback.DontObstructDistance = reader.ReadFloat();
			playback.DontPlayDistance = reader.ReadFloat();
			playback.AttackDistance = reader.ReadFloat();
			playback.MinDistance = reader.ReadFloat();
			playback.SustainBeginDistance = reader.ReadFloat();
			playback.SustainEndDistance = reader.ReadFloat();
			playback.MaxDistance = reader.ReadFloat();
			playback.SustainDB = reader.ReadFloat();
			playback.SkipFraction = reader.ReadFloat();
			playback.MaxPendPerSec = reader.ReadFloat();

			playback.GainBase = reader.ReadFloat();
			playback.GainVariance = reader.ReadFloat();
			playback.RandomPitchBoundsMin = reader.ReadInt32();
			playback.RandomPitchBoundsMax = reader.ReadInt32();
			playback.InnerConeAngle = reader.ReadFloat();
			playback.OuterConeAngle = reader.ReadFloat();
			playback.OuterConeGain = reader.ReadFloat();
			playback.Flags = reader.ReadInt32();
			playback.Azimuth = reader.ReadFloat();
			playback.PositionalGain = reader.ReadFloat();
			playback.FirstPersonGain = reader.ReadFloat();

			return new ExtractedSoundPlayback(originalIndex, playback);
		}

		private static ExtractedSoundScale ReadSoundScale(IReader reader, byte version)
		{
			if (version > 0)
				throw new InvalidOperationException("Unrecognized \"snsc\" block version");

			int originalIndex = reader.ReadInt32();

			var scale = new SoundScale();

			scale.GainMin = reader.ReadFloat();
			scale.GainMax = reader.ReadFloat();
			scale.PitchMin = reader.ReadInt32();
			scale.PitchMax = reader.ReadInt32();
			scale.SkipFractionMin = reader.ReadFloat();
			scale.SkipFractionMax = reader.ReadFloat();

			return new ExtractedSoundScale(originalIndex, scale);
		}

		private static ExtractedSoundPromotion ReadSoundPromotion(IReader reader, byte version)
		{
			if (version > 0)
				throw new InvalidOperationException("Unrecognized \"spro\" block version");

			int originalIndex = reader.ReadInt32();

			SoundPromotion promo = new SoundPromotion();

			promo.ActivePromotionIndex = reader.ReadInt32();
			promo.LastPromotionTime = reader.ReadInt32();
			promo.SuppressionTimeout = reader.ReadInt32();

			var rules = new System.Collections.Generic.List<SoundPromotionRule>();

			int rulecount = reader.ReadInt32();
			for (int i = 0; i < rulecount; i++)
			{
				SoundPromotionRule rule = new SoundPromotionRule();

				rule.LocalPitchRangeIndex = reader.ReadInt32();
				rule.MaximumPlayCount = reader.ReadInt32();
				rule.SupressionTime = reader.ReadFloat();
				rule.RolloverTime = reader.ReadInt32();
				rule.ImpulseTime = reader.ReadInt32();
				rules.Add(rule);
			}

			promo.Rules = rules.ToArray();

			return new ExtractedSoundPromotion(originalIndex, promo);
		}

		private static ExtractedSoundCustomPlayback ReadSoundCustomPlayback(IReader reader, byte version)
		{
			if (version > 0)
				throw new InvalidOperationException("Unrecognized \"scpb\" block version");

			var cplayback = new ExtractedSoundCustomPlayback();

			cplayback.OriginalIndex = reader.ReadInt32();

			cplayback.Version = (SoundCustomPlaybackVersion)reader.ReadByte();

			cplayback.Flags = reader.ReadInt32();

			cplayback.Unknown = reader.ReadInt32();
			cplayback.Unknown1 = reader.ReadInt32();

			cplayback.Mixes = new System.Collections.Generic.List<SoundCustomPlaybackMix>();
			int mixcount = reader.ReadInt32();
			for (int i = 0; i < mixcount; i++)
			{
				SoundCustomPlaybackMix mix = new SoundCustomPlaybackMix();

				mix.Mixbin = reader.ReadInt32();
				mix.Gain = reader.ReadFloat();

				cplayback.Mixes.Add(mix);
			}

			cplayback.Filters = new System.Collections.Generic.List<SoundCustomPlaybackFilter>();
			int filtercount = reader.ReadInt32();
			for (int i = 0; i < filtercount; i++)
			{
				SoundCustomPlaybackFilter filter = new SoundCustomPlaybackFilter();

				filter.Type = reader.ReadInt32();
				filter.Width = reader.ReadInt32();

				filter.LeftFreqScaleMin = reader.ReadFloat();
				filter.LeftFreqScaleMax = reader.ReadFloat();
				filter.LeftFreqRandomBase = reader.ReadFloat();
				filter.LeftFreqRandomVariance = reader.ReadFloat();

				filter.LeftGainScaleMin = reader.ReadFloat();
				filter.LeftGainScaleMax = reader.ReadFloat();
				filter.LeftGainRandomBase = reader.ReadFloat();
				filter.LeftGainRandomVariance = reader.ReadFloat();

				filter.RightFreqScaleMin = reader.ReadFloat();
				filter.RightFreqScaleMax = reader.ReadFloat();
				filter.RightFreqRandomBase = reader.ReadFloat();
				filter.RightFreqRandomVariance = reader.ReadFloat();

				filter.RightGainScaleMin = reader.ReadFloat();
				filter.RightGainScaleMax = reader.ReadFloat();
				filter.RightGainRandomBase = reader.ReadFloat();
				filter.RightGainRandomVariance = reader.ReadFloat();

				cplayback.Filters.Add(filter);
			}

			cplayback.PitchLFOs = new System.Collections.Generic.List<SoundCustomPlaybackPitchLFO>();
			int pitchlfocount = reader.ReadInt32();
			for (int i = 0; i < pitchlfocount; i++)
			{
				SoundCustomPlaybackPitchLFO pitchlfo = new SoundCustomPlaybackPitchLFO();

				pitchlfo.DelayScaleMin = reader.ReadFloat();
				pitchlfo.DelayScaleMax = reader.ReadFloat();
				pitchlfo.DelayRandomBase = reader.ReadFloat();
				pitchlfo.DelayRandomVariance = reader.ReadFloat();

				pitchlfo.FreqScaleMin = reader.ReadFloat();
				pitchlfo.FreqScaleMax = reader.ReadFloat();
				pitchlfo.FreqRandomBase = reader.ReadFloat();
				pitchlfo.FreqRandomVariance = reader.ReadFloat();

				pitchlfo.PitchModScaleMin = reader.ReadFloat();
				pitchlfo.PitchModScaleMax = reader.ReadFloat();
				pitchlfo.PitchModRandomBase = reader.ReadFloat();
				pitchlfo.PitchModRandomVariance = reader.ReadFloat();

				cplayback.PitchLFOs.Add(pitchlfo);
			}

			cplayback.FilterLFOs = new System.Collections.Generic.List<SoundCustomPlaybackFilterLFO>();
			int filterlfocount = reader.ReadInt32();
			for (int i = 0; i < filterlfocount; i++)
			{
				SoundCustomPlaybackFilterLFO filterlfo = new SoundCustomPlaybackFilterLFO();

				filterlfo.DelayScaleMin = reader.ReadFloat();
				filterlfo.DelayScaleMax = reader.ReadFloat();
				filterlfo.DelayRandomBase = reader.ReadFloat();
				filterlfo.DelayRandomVariance = reader.ReadFloat();

				filterlfo.FreqScaleMin = reader.ReadFloat();
				filterlfo.FreqScaleMax = reader.ReadFloat();
				filterlfo.FreqRandomBase = reader.ReadFloat();
				filterlfo.FreqRandomVariance = reader.ReadFloat();

				filterlfo.CutoffModScaleMin = reader.ReadFloat();
				filterlfo.CutoffModScaleMax = reader.ReadFloat();
				filterlfo.CutoffModRandomBase = reader.ReadFloat();
				filterlfo.CutoffModRandomVariance = reader.ReadFloat();

				filterlfo.GainModScaleMin = reader.ReadFloat();
				filterlfo.GainModScaleMax = reader.ReadFloat();
				filterlfo.GainModRandomBase = reader.ReadFloat();
				filterlfo.GainModRandomVariance = reader.ReadFloat();

				cplayback.FilterLFOs.Add(filterlfo);
			}

			//bug fix because I released this with the writer only writing half the datum, so the tag should be thrown out if it predates this commit
			cplayback.OriginalRadioEffect = new DatumIndex(reader.ReadUInt32());
			if (cplayback.OriginalRadioEffect.Salt == 0)
				cplayback.OriginalRadioEffect = DatumIndex.Null;

			cplayback.LowpassEffects = new System.Collections.Generic.List<SoundCustomPlaybackLowpassEffect>();
			int lowpasscount = reader.ReadInt32();
			for (int i = 0; i < lowpasscount; i++)
			{
				SoundCustomPlaybackLowpassEffect lp = new SoundCustomPlaybackLowpassEffect();

				lp.Attack = reader.ReadFloat();
				lp.Release = reader.ReadFloat();
				lp.CutoffFrequency = reader.ReadFloat();
				lp.OutputGain = reader.ReadFloat();

				cplayback.LowpassEffects.Add(lp);
			}

			cplayback.Components = new System.Collections.Generic.List<ExtractedSoundCustomPlaybackComponent>();
			int compcount = reader.ReadInt32();
			for (int i = 0; i < compcount; i++)
			{
				ExtractedSoundCustomPlaybackComponent c = new ExtractedSoundCustomPlaybackComponent();

				//bug fix because I released this with the writer only writing half the datum, so the tag should be thrown out if it predates this commit
				c.OriginalSound = new DatumIndex(reader.ReadUInt32());
				if (c.OriginalSound.Salt == 0)
					c.OriginalSound = DatumIndex.Null;

				c.Gain = reader.ReadFloat();
				c.Flags = reader.ReadInt32();

				cplayback.Components.Add(c);
			}

			return cplayback;
		}

		private static ExtractedSoundExtraInfo ReadSoundExtraInfo(IReader reader, byte version)
		{
			if (version > 0)
				throw new InvalidOperationException("Unrecognized \"snex\" block version");

			int originalIndex = reader.ReadInt32();

			var extra = new SoundExtraInfo();

			var permSections = new System.Collections.Generic.List<SoundExtraInfoPermutationSection>();

			int permsectioncount = reader.ReadInt32();
			for (int i = 0; i < permsectioncount; i++)
			{
				var permsection = new SoundExtraInfoPermutationSection();

				var dialogInfos = new System.Collections.Generic.List<SoundExtraInfoDialogueInfo>();
				var unk1s = new System.Collections.Generic.List<SoundExtraInfoUnknown1>();

				int datalength = reader.ReadInt32();
				byte[] data = reader.ReadBlock(datalength);
				permsection.EncodedData = data;

				int dialogueinfocount = reader.ReadInt32();
				for (int d = 0; d < dialogueinfocount; d++)
				{
					var dialogueinfo = new SoundExtraInfoDialogueInfo();

					dialogueinfo.MouthOffset = reader.ReadInt32();
					dialogueinfo.MouthLength = reader.ReadInt32();
					dialogueinfo.LipsyncOffset = reader.ReadInt32();
					dialogueinfo.LipsyncLength = reader.ReadInt32();

					dialogInfos.Add(dialogueinfo);
				}

				int unk1count = reader.ReadInt32();
				for (int a = 0; a < unk1count; a++)
				{
					var unk1 = new SoundExtraInfoUnknown1();
					var unk2s = new System.Collections.Generic.List<SoundExtraInfoUnknown2>();

					unk1.Unknown = reader.ReadInt32();
					unk1.Unknown1 = reader.ReadInt32();
					unk1.Unknown2 = reader.ReadInt32();
					unk1.Unknown3 = reader.ReadInt32();
					unk1.Unknown4 = reader.ReadInt32();
					unk1.Unknown5 = reader.ReadInt32();
					unk1.Unknown6 = reader.ReadInt32();
					unk1.Unknown7 = reader.ReadInt32();
					unk1.Unknown8 = reader.ReadInt32();
					unk1.Unknown9 = reader.ReadInt32();
					unk1.Unknown10 = reader.ReadInt32();
					unk1.Unknown11 = reader.ReadInt32();

					int unk2count = reader.ReadInt32();
					for (int b = 0; b < unk2count; b++)
					{
						var unk2 = new SoundExtraInfoUnknown2();
						var unk5s = new System.Collections.Generic.List<SoundExtraInfoUnknown3>();
						var unk6s = new System.Collections.Generic.List<SoundExtraInfoUnknown4>();

						unk2.Unknown = reader.ReadFloat();
						unk2.Unknown1 = reader.ReadFloat();
						unk2.Unknown2 = reader.ReadFloat();
						unk2.Unknown3 = reader.ReadFloat();

						int unk3count = reader.ReadInt32();
						for (int c = 0; c < unk3count; c++)
						{
							var unk3 = new SoundExtraInfoUnknown3();

							unk3.Unknown = reader.ReadInt32();
							unk3.Unknown1 = reader.ReadInt32();

							if (version >= 1)
							{
								unk3.Unknown2 = reader.ReadInt32();
								unk3.Unknown3 = reader.ReadInt32();
							}

							unk5s.Add(unk3);
						}

						int unk4count = reader.ReadInt32();
						for (int c = 0; c < unk4count; c++)
						{
							var unk4 = new SoundExtraInfoUnknown4();

							unk4.Unknown = reader.ReadInt32();
							unk4.Unknown1 = reader.ReadInt32();
							unk6s.Add(unk4);
						}

						unk2.Unknown5s = unk5s.ToArray();
						unk2.Unknown6s = unk6s.ToArray();

						unk2s.Add(unk2);
						unk1.Unknown12s = unk2s.ToArray();
					}

					unk1s.Add(unk1);

					permsection.DialogueInfos = dialogInfos.ToArray();
					permsection.Unknown1s = unk1s.ToArray();
				}

				permSections.Add(permsection);
			}

			extra.PermutationSections = permSections.ToArray();

			int datumscount = reader.ReadInt32();
			var datums = new System.Collections.Generic.List<DatumIndex>();
			for (int i = 0; i < datumscount; i++)
				datums.Add(new DatumIndex(reader.ReadUInt32()));

			extra.Datums = datums.ToArray();

			return new ExtractedSoundExtraInfo(originalIndex, extra);
		}

		private static byte[] ReadByteArray(IReader reader)
		{
			var size = reader.ReadInt32();
			return size <= 0 ? new byte[0] : reader.ReadBlock(size);
		}
	}
}