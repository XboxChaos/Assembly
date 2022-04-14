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

			WriteSoundCodecs(tags, container, writer);
			WriteSoundPitchRanges(tags, container, writer);
			WriteSoundLanguagePitchRanges(tags, container, writer);
			WriteSoundPlaybacks(tags, container, writer);
			WriteSoundScales(tags, container, writer);
			WriteSoundPromotions(tags, container, writer);
			WriteSoundCustomPlaybacks(tags, container, writer);
			WriteSoundExtraInfos(tags, container, writer);

			container.EndBlock();
		}

		private static void WriteDataBlocks(TagContainer tags, ContainerWriter container, IWriter writer)
		{
			foreach (DataBlock dataBlock in tags.DataBlocks)
			{
				container.StartBlock("data", 8);

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

				// Model Data fixups
				writer.WriteInt32(dataBlock.InteropFixups.Count);
				foreach (DataBlockInteropFixup interop in dataBlock.InteropFixups)
				{
					writer.WriteUInt32(interop.OriginalAddress);
					writer.WriteInt32(interop.WriteOffset);
					writer.WriteInt32(interop.Type);
				}

				// Effect fixups
				writer.WriteInt32(dataBlock.EffectFixups.Count);
				foreach (DataBlockEffectFixup effectData in dataBlock.EffectFixups)
				{
					writer.WriteInt32(effectData.OriginalIndex);
					writer.WriteInt32(effectData.WriteOffset);
					writer.WriteInt32(effectData.Type);
					if (effectData.Data != null)
					{
						writer.WriteInt32(effectData.Data.Length);
						writer.WriteBlock(effectData.Data);
					}
					else
					{
						writer.WriteInt32(0);
					}
				}

				// Sound fixups
				writer.WriteInt32(dataBlock.SoundFixups.Count);
				foreach (DataBlockSoundFixup sound in dataBlock.SoundFixups)
				{
					writer.WriteInt32(sound.OriginalCodecIndex);
					writer.WriteInt32(sound.PitchRangeCount);
					writer.WriteInt32(sound.OriginalPitchRangeIndex);
					writer.WriteInt32(sound.OriginalLanguageDurationIndex);
					writer.WriteInt32(sound.OriginalPlaybackIndex);
					writer.WriteInt32(sound.OriginalScaleIndex);
					writer.WriteInt32(sound.OriginalPromotionIndex);
					writer.WriteInt32(sound.OriginalCustomPlaybackIndex);
					writer.WriteInt32(sound.OriginalExtraInfoIndex);
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
				writer.WriteInt32(tag.Group);
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
						writer.WriteInt32(expa.OriginalResourceGroup);
						writer.WriteAscii(expa.OriginalResourceName);
					}	
				}

				writer.WriteInt32(prediction.AEntries.Count);
				foreach (ExtractedResourcePredictionA expa in prediction.AEntries)
				{
					writer.WriteInt32(expa.OriginalResourceSubIndex);
					writer.WriteUInt32(expa.OriginalResourceIndex.Value);
					writer.WriteInt32(expa.OriginalResourceGroup);
					writer.WriteAscii(expa.OriginalResourceName);
				}

				container.EndBlock();
			}
		}

		private static void WriteSoundCodecs(TagContainer tags, ContainerWriter container, IWriter writer)
		{
			foreach (var codec in tags.SoundCodecs)
			{
				container.StartBlock("sndc", 0);

				writer.WriteInt32(codec.OriginalIndex);

				writer.WriteInt32(codec.Source.SampleRate);
				writer.WriteInt32(codec.Source.Encoding);
				writer.WriteInt32(codec.Source.Compression);

				container.EndBlock();
			}
		}

		private static void WriteSoundPitchRanges(TagContainer tags, ContainerWriter container, IWriter writer)
		{
			foreach (var pRange in tags.SoundPitchRanges)
			{
				container.StartBlock("snpr", 1);

				writer.WriteInt32(pRange.OriginalIndex);

				writer.WriteAscii(pRange.Name);

				if (pRange.Parameter != null)
				{
					writer.WriteByte(1);
					writer.WriteInt32(pRange.Parameter.NaturalPitch);
					writer.WriteInt32(pRange.Parameter.BendMin);
					writer.WriteInt32(pRange.Parameter.BendMax);
					writer.WriteInt32(pRange.Parameter.MaxGainPitchMin);
					writer.WriteInt32(pRange.Parameter.MaxGainPitchMax);
					writer.WriteInt32(pRange.Parameter.PlaybackPitchMin);
					writer.WriteInt32(pRange.Parameter.PlaybackPitchMax);

					if (pRange.Parameter.Distance != null)
					{
						writer.WriteByte(1);
						writer.WriteFloat(pRange.Parameter.Distance.DontPlayDistance);
						writer.WriteFloat(pRange.Parameter.Distance.AttackDistance);
						writer.WriteFloat(pRange.Parameter.Distance.MinDistance);
						writer.WriteFloat(pRange.Parameter.Distance.MaxDistance);
					}
					else
						writer.WriteByte(0);
				}
				else
					writer.WriteByte(0);

				writer.WriteByte(pRange.HasEncodedData ? (byte)1 : (byte)0);

				writer.WriteInt32(pRange.RequiredPermutationCount);

				writer.WriteInt32(pRange.Permutations.Count);
				foreach (var perm in pRange.Permutations)
				{
					writer.WriteAscii(perm.Name);

					writer.WriteInt32(perm.EncodedSkipFraction);
					writer.WriteInt32(perm.SampleSize);
					writer.WriteInt32(perm.EncodedGain);
					writer.WriteInt32(perm.EncodedPermutationInfoIndex);
					writer.WriteInt32(perm.FSBInfo);

					if (perm.Chunks != null)
					{
						writer.WriteInt32(perm.Chunks.Count);
						foreach (var chunk in perm.Chunks)
						{
							writer.WriteInt32(chunk.Source.FileOffset);
							writer.WriteInt32(chunk.Source.EncodedSizeAndFlags);
							writer.WriteInt32(chunk.Source.CacheIndex);
							writer.WriteInt32(chunk.Source.XMA2BufferStart);
							writer.WriteInt32(chunk.Source.XMA2BufferEnd);
							writer.WriteInt32(chunk.Source.Unknown);
							writer.WriteInt32(chunk.Source.Unknown1);
							writer.WriteAscii(chunk.FModBankSuffix);
						}
					}
					else
						writer.WriteInt32(0);

					if (perm.Languages != null)
					{
						writer.WriteInt32(perm.Languages.Count);
						foreach (var lang in perm.Languages)
						{
							writer.WriteInt32(lang.LanguageIndex);
							writer.WriteInt32(lang.SampleSize);

							if (lang.Chunks != null)
							{
								writer.WriteInt32(lang.Chunks.Count);
								foreach (var chunk in lang.Chunks)
								{
									writer.WriteInt32(chunk.Source.FileOffset);
									writer.WriteInt32(chunk.Source.EncodedSizeAndFlags);
									writer.WriteInt32(chunk.Source.CacheIndex);
									writer.WriteInt32(chunk.Source.XMA2BufferStart);
									writer.WriteInt32(chunk.Source.XMA2BufferEnd);
									writer.WriteInt32(chunk.Source.Unknown);
									writer.WriteInt32(chunk.Source.Unknown1);
									writer.WriteAscii(chunk.FModBankSuffix);
								}
							}
							else
								writer.WriteInt32(0);
						}
					}
					else
						writer.WriteInt32(0);

					if (perm.LayerMarkers != null)
					{
						writer.WriteInt32(perm.LayerMarkers.Count);
						foreach (var marker in perm.LayerMarkers)
							writer.WriteInt32(marker);
					}
					else
						writer.WriteInt32(0);
				}

				container.EndBlock();
			}
		}

		private static void WriteSoundLanguagePitchRanges(TagContainer tags, ContainerWriter container, IWriter writer)
		{
			foreach (var lpd in tags.SoundLanguageDurations)
			{
				container.StartBlock("snld", 0);

				writer.WriteInt32(lpd.OriginalIndex);

				writer.WriteInt32(lpd.Languages.Count);
				foreach (var lang in lpd.Languages)
				{
					writer.WriteInt32(lang.LanguageIndex);

					writer.WriteInt32(lang.Durations.Count);
					foreach (var dur in lang.Durations)
					{
						writer.WriteInt32(dur);
					}
				}

				container.EndBlock();
			}
		}

		private static void WriteSoundPlaybacks(TagContainer tags, ContainerWriter container, IWriter writer)
		{
			foreach (var playback in tags.SoundPlaybacks)
			{
				container.StartBlock("snpb", 0);

				writer.WriteInt32(playback.OriginalIndex);

				writer.WriteInt32(playback.Source.InternalFlags);
				writer.WriteFloat(playback.Source.DontObstructDistance);
				writer.WriteFloat(playback.Source.DontPlayDistance);
				writer.WriteFloat(playback.Source.AttackDistance);
				writer.WriteFloat(playback.Source.MinDistance);
				writer.WriteFloat(playback.Source.SustainBeginDistance);
				writer.WriteFloat(playback.Source.SustainEndDistance);
				writer.WriteFloat(playback.Source.MaxDistance);
				writer.WriteFloat(playback.Source.SustainDB);
				writer.WriteFloat(playback.Source.SkipFraction);
				writer.WriteFloat(playback.Source.MaxPendPerSec);

				writer.WriteFloat(playback.Source.GainBase);
				writer.WriteFloat(playback.Source.GainVariance);
				writer.WriteInt32(playback.Source.RandomPitchBoundsMin);
				writer.WriteInt32(playback.Source.RandomPitchBoundsMax);
				writer.WriteFloat(playback.Source.InnerConeAngle);
				writer.WriteFloat(playback.Source.OuterConeAngle);
				writer.WriteFloat(playback.Source.OuterConeGain);
				writer.WriteInt32(playback.Source.Flags);
				writer.WriteFloat(playback.Source.Azimuth);
				writer.WriteFloat(playback.Source.PositionalGain);
				writer.WriteFloat(playback.Source.FirstPersonGain);

				container.EndBlock();
			}
		}

		private static void WriteSoundScales(TagContainer tags, ContainerWriter container, IWriter writer)
		{
			foreach (var scale in tags.SoundScales)
			{
				container.StartBlock("snsc", 0);

				writer.WriteInt32(scale.OriginalIndex);

				writer.WriteFloat(scale.Source.GainMin);
				writer.WriteFloat(scale.Source.GainMax);
				writer.WriteInt32(scale.Source.PitchMin);
				writer.WriteInt32(scale.Source.PitchMax);
				writer.WriteFloat(scale.Source.SkipFractionMin);
				writer.WriteFloat(scale.Source.SkipFractionMax);

				container.EndBlock();
			}
		}

		private static void WriteSoundPromotions(TagContainer tags, ContainerWriter container, IWriter writer)
		{
			foreach (var promo in tags.SoundPromotions)
			{
				container.StartBlock("spro", 0);

				writer.WriteInt32(promo.OriginalIndex);

				writer.WriteInt32(promo.Source.ActivePromotionIndex);
				writer.WriteInt32(promo.Source.LastPromotionTime);
				writer.WriteInt32(promo.Source.SuppressionTimeout);

				writer.WriteInt32(promo.Source.Rules.Length);
				foreach (var rule in promo.Source.Rules)
				{
					writer.WriteInt32(rule.LocalPitchRangeIndex);
					writer.WriteInt32(rule.MaximumPlayCount);
					writer.WriteFloat(rule.SupressionTime);
					writer.WriteInt32(rule.RolloverTime);
					writer.WriteInt32(rule.ImpulseTime);
				}

				container.EndBlock();
			}
		}

		private static void WriteSoundCustomPlaybacks(TagContainer tags, ContainerWriter container, IWriter writer)
		{
			foreach (var cplayback in tags.SoundCustomPlaybacks)
			{
				container.StartBlock("scpb", 0);

				writer.WriteInt32(cplayback.OriginalIndex);

				writer.WriteByte((byte)cplayback.Version);

				writer.WriteInt32(cplayback.Flags);

				writer.WriteInt32(cplayback.Unknown);
				writer.WriteInt32(cplayback.Unknown1);

				//theres a final block that goes here

				if (cplayback.Mixes != null)
				{
					writer.WriteInt32(cplayback.Mixes.Count);
					foreach (var mix in cplayback.Mixes)
					{
						writer.WriteInt32(mix.Mixbin);
						writer.WriteFloat(mix.Gain);
					}
				}
				else
					writer.WriteInt32(0);

				if (cplayback.Filters != null)
				{
					writer.WriteInt32(cplayback.Filters.Count);
					foreach (var filter in cplayback.Filters)
					{
						writer.WriteInt32(filter.Type);
						writer.WriteInt32(filter.Width);

						writer.WriteFloat(filter.LeftFreqScaleMin);
						writer.WriteFloat(filter.LeftFreqScaleMax);
						writer.WriteFloat(filter.LeftFreqRandomBase);
						writer.WriteFloat(filter.LeftFreqRandomVariance);

						writer.WriteFloat(filter.LeftGainScaleMin);
						writer.WriteFloat(filter.LeftGainScaleMax);
						writer.WriteFloat(filter.LeftGainRandomBase);
						writer.WriteFloat(filter.LeftGainRandomVariance);

						writer.WriteFloat(filter.RightFreqScaleMin);
						writer.WriteFloat(filter.RightFreqScaleMax);
						writer.WriteFloat(filter.RightFreqRandomBase);
						writer.WriteFloat(filter.RightFreqRandomVariance);

						writer.WriteFloat(filter.RightGainScaleMin);
						writer.WriteFloat(filter.RightGainScaleMax);
						writer.WriteFloat(filter.RightGainRandomBase);
						writer.WriteFloat(filter.RightGainRandomVariance);
					}
				}
				else
					writer.WriteInt32(0);

				if (cplayback.PitchLFOs != null)
				{
					writer.WriteInt32(cplayback.PitchLFOs.Count);
					foreach (var pitchlfo in cplayback.PitchLFOs)
					{
						writer.WriteFloat(pitchlfo.DelayScaleMin);
						writer.WriteFloat(pitchlfo.DelayScaleMax);
						writer.WriteFloat(pitchlfo.DelayRandomBase);
						writer.WriteFloat(pitchlfo.DelayRandomVariance);

						writer.WriteFloat(pitchlfo.FreqScaleMin);
						writer.WriteFloat(pitchlfo.FreqScaleMax);
						writer.WriteFloat(pitchlfo.FreqRandomBase);
						writer.WriteFloat(pitchlfo.FreqRandomVariance);

						writer.WriteFloat(pitchlfo.PitchModScaleMin);
						writer.WriteFloat(pitchlfo.PitchModScaleMax);
						writer.WriteFloat(pitchlfo.PitchModRandomBase);
						writer.WriteFloat(pitchlfo.PitchModRandomVariance);
					}
				}
				else
					writer.WriteInt32(0);

				if (cplayback.FilterLFOs != null)
				{
					writer.WriteInt32(cplayback.FilterLFOs.Count);
					foreach (var filterlfo in cplayback.FilterLFOs)
					{
						writer.WriteFloat(filterlfo.DelayScaleMin);
						writer.WriteFloat(filterlfo.DelayScaleMax);
						writer.WriteFloat(filterlfo.DelayRandomBase);
						writer.WriteFloat(filterlfo.DelayRandomVariance);

						writer.WriteFloat(filterlfo.FreqScaleMin);
						writer.WriteFloat(filterlfo.FreqScaleMax);
						writer.WriteFloat(filterlfo.FreqRandomBase);
						writer.WriteFloat(filterlfo.FreqRandomVariance);

						writer.WriteFloat(filterlfo.CutoffModScaleMin);
						writer.WriteFloat(filterlfo.CutoffModScaleMax);
						writer.WriteFloat(filterlfo.CutoffModRandomBase);
						writer.WriteFloat(filterlfo.CutoffModRandomVariance);

						writer.WriteFloat(filterlfo.GainModScaleMin);
						writer.WriteFloat(filterlfo.GainModScaleMax);
						writer.WriteFloat(filterlfo.GainModRandomBase);
						writer.WriteFloat(filterlfo.GainModRandomVariance);
					}
				}
				else
					writer.WriteInt32(0);

				writer.WriteUInt32(cplayback.OriginalRadioEffect.Value);

				if (cplayback.LowpassEffects != null)
				{
					writer.WriteInt32(cplayback.LowpassEffects.Count);
					foreach (var lowpass in cplayback.LowpassEffects)
					{
						writer.WriteFloat(lowpass.Attack);
						writer.WriteFloat(lowpass.Release);
						writer.WriteFloat(lowpass.CutoffFrequency);
						writer.WriteFloat(lowpass.OutputGain);
					}
				}
				else
					writer.WriteInt32(0);

				if (cplayback.Components != null)
				{
					writer.WriteInt32(cplayback.Components.Count);
					foreach (var comp in cplayback.Components)
					{
						writer.WriteUInt32(comp.OriginalSound.Value);
						writer.WriteFloat(comp.Gain);
						writer.WriteInt32(comp.Flags);
					}
				}
				else
					writer.WriteInt32(0);

				container.EndBlock();
			}
		}

		private static void WriteSoundExtraInfos(TagContainer tags, ContainerWriter container, IWriter writer)
		{
			foreach (var extra in tags.SoundExtraInfos)
			{
				container.StartBlock("snex", 0);

				writer.WriteInt32(extra.OriginalIndex);

				if (extra.Source.PermutationSections != null)
				{
					writer.WriteInt32(extra.Source.PermutationSections.Length);
					foreach (var psection in extra.Source.PermutationSections)
					{
						writer.WriteInt32(psection.EncodedData.Length);
						writer.WriteBlock(psection.EncodedData);

						if (psection.DialogueInfos != null)
						{
							writer.WriteInt32(psection.DialogueInfos.Length);
							foreach (var dialogue in psection.DialogueInfos)
							{
								writer.WriteInt32(dialogue.MouthOffset);
								writer.WriteInt32(dialogue.MouthLength);
								writer.WriteInt32(dialogue.LipsyncOffset);
								writer.WriteInt32(dialogue.LipsyncLength);
							}
						}
						else
							writer.WriteInt32(0);

						if (psection.Unknown1s != null)
						{
							writer.WriteInt32(psection.Unknown1s.Length);
							foreach (var unk1 in psection.Unknown1s)
							{
								writer.WriteInt32(unk1.Unknown);
								writer.WriteInt32(unk1.Unknown1);
								writer.WriteInt32(unk1.Unknown2);
								writer.WriteInt32(unk1.Unknown3);
								writer.WriteInt32(unk1.Unknown4);
								writer.WriteInt32(unk1.Unknown5);
								writer.WriteInt32(unk1.Unknown6);
								writer.WriteInt32(unk1.Unknown7);
								writer.WriteInt32(unk1.Unknown8);
								writer.WriteInt32(unk1.Unknown9);
								writer.WriteInt32(unk1.Unknown10);
								writer.WriteInt32(unk1.Unknown11);

								if (unk1.Unknown12s != null)
								{
									writer.WriteInt32(unk1.Unknown12s.Length);
									foreach (var unk2 in unk1.Unknown12s)
									{
										writer.WriteFloat(unk2.Unknown);
										writer.WriteFloat(unk2.Unknown1);
										writer.WriteFloat(unk2.Unknown2);
										writer.WriteFloat(unk2.Unknown3);

										if (unk2.Unknown5s != null)
										{
											writer.WriteInt32(unk2.Unknown5s.Length);
											foreach (var unk3 in unk2.Unknown5s)
											{
												writer.WriteInt32(unk3.Unknown);
												writer.WriteInt32(unk3.Unknown1);
												writer.WriteInt32(unk3.Unknown2);
												writer.WriteInt32(unk3.Unknown3);
											}
										}
										else
											writer.WriteInt32(0);

										if (unk2.Unknown6s != null)
										{
											writer.WriteInt32(unk2.Unknown6s.Length);
											foreach (var unk4 in unk2.Unknown6s)
											{
												writer.WriteInt32(unk4.Unknown);
												writer.WriteInt32(unk4.Unknown1);
											}
										}
										else
											writer.WriteInt32(0);
									}
								}
								else
									writer.WriteInt32(0);
							}
						}
						else
							writer.WriteInt32(0);
					}
				}
				else
					writer.WriteInt32(0);

				if (extra.Source.Datums != null)
				{
					writer.WriteInt32(extra.Source.Datums.Length);
					foreach (var d in extra.Source.Datums)
						writer.WriteUInt32(d.Value);
				}
				else
					writer.WriteInt32(0);

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