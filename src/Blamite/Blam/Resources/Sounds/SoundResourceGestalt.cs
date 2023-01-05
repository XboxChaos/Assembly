using System.Linq;
using Blamite.Blam.Resources.Sounds;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;
using System.Collections.Generic;
using System;

namespace Blamite.Blam.Resources.Sounds
{
	public class SoundResourceGestalt
	{
		private readonly MetaAllocator _allocator;
		private readonly EngineDescription _buildInfo;
		private readonly FileSegmentGroup _metaArea;
		private readonly ITag _tag;
		private IPointerExpander _expander;

		public SoundResourceGestalt(IReader reader, ITag gestalt, FileSegmentGroup metaArea, MetaAllocator allocator, EngineDescription buildInfo, IPointerExpander expander)
		{
			_tag = gestalt;
			_metaArea = metaArea;
			_allocator = allocator;
			_buildInfo = buildInfo;
			_expander = expander;
		}

		#region Codecs
		public IEnumerable<SoundCodec> LoadCodecs(IReader reader)
		{
			StructureValueCollection values = LoadTag(reader);
			StructureValueCollection[] entries = ReadBlock(values, reader, "number of codecs", "codec table address", "sound codec");
			return entries.Select(e => LoadCodec(e));
		}

		private SoundCodec LoadCodec(StructureValueCollection values)
		{
			return new SoundCodec()
			{
				SampleRate = (int)values.GetInteger("sample rate"),
				Encoding = (int)values.GetInteger("encoding"),
				Compression = (int)values.GetInteger("compression")
			};	
		}

		public void SaveCodecs(ICollection<SoundCodec> codecs, IStream stream)
		{
			StructureValueCollection values = LoadTag(stream);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound codec");

			var entries = new List<StructureValueCollection>();

			foreach (var codec in codecs)
			{
				StructureValueCollection entry = new StructureValueCollection();
				entry.SetInteger("sample rate", (uint)codec.SampleRate);
				entry.SetInteger("encoding", (uint)codec.Encoding);
				entry.SetInteger("compression", (uint)codec.Compression);
				entries.Add(entry);
			}

			int oldCount = (int)values.GetInteger("number of codecs");
			long oldAddress = _expander.Expand((uint)values.GetInteger("codec table address"));

			uint newAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(entries, oldCount, oldAddress, layout, _metaArea, _allocator, stream));

			values.SetInteger("number of codecs", (uint)entries.Count);
			values.SetInteger("codec table address", newAddress);

			SaveTag(values, stream);
		}
		#endregion

		#region Playbacks
		public IEnumerable<SoundPlayback> LoadPlaybacks(IReader reader)
		{
			StructureValueCollection values = LoadTag(reader);
			StructureValueCollection[] entries = ReadBlock(values, reader, "number of playbacks", "playback table address", "sound playback");
			return entries.Select(e => LoadPlayback(e));
		}

		private SoundPlayback LoadPlayback(StructureValueCollection values)
		{
			return new SoundPlayback()
			{
				InternalFlags = (int)values.GetInteger("internal flags"),
				DontObstructDistance = values.GetFloatOrDefault("dont obstruct distance", 0),
				DontPlayDistance = values.GetFloat("dont play distance"),
				AttackDistance = values.GetFloat("attack distance"),
				MinDistance = values.GetFloat("minimum distance"),
				SustainBeginDistance = values.GetFloatOrDefault("sustain begin distance", 0),
				SustainEndDistance = values.GetFloatOrDefault("sustain end distance", 0),
				MaxDistance = values.GetFloat("maximum distance"),
				SustainDB = values.GetFloatOrDefault("sustain db", 0),
				SkipFraction = values.GetFloat("skip fraction"),
				MaxPendPerSec = values.GetFloat("max bend per second"),
				GainBase = values.GetFloat("gain base"),
				GainVariance = values.GetFloat("gain variance"),
				RandomPitchBoundsMin = (int)values.GetInteger("random pitch bounds min"),
				RandomPitchBoundsMax = (int)values.GetInteger("random pitch bounds max"),
				InnerConeAngle = values.GetFloat("inner cone angle"),
				OuterConeAngle = values.GetFloat("outer cone angle"),
				OuterConeGain = values.GetFloat("outer cone gain"),
				Flags = (int)values.GetInteger("flags"),
				Azimuth = values.GetFloat("azimuth"),
				PositionalGain = values.GetFloat("position gain"),
				FirstPersonGain = values.GetFloat("first person gain")
			};
		}

		public void SavePlaybacks(ICollection<SoundPlayback> playbacks, IStream stream)
		{
			StructureValueCollection values = LoadTag(stream);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound playback");

			var entries = new List<StructureValueCollection>();

			foreach (var playback in playbacks)
			{
				StructureValueCollection entry = new StructureValueCollection();
				entry.SetInteger("internal flags", (uint)playback.InternalFlags);
				entry.SetFloat("dont obstruct distance", playback.DontObstructDistance);
				entry.SetFloat("dont play distance", playback.DontPlayDistance);
				entry.SetFloat("attack distance", playback.AttackDistance);
				entry.SetFloat("minimum distance", playback.MinDistance);
				entry.SetFloat("sustain begin distance", playback.SustainBeginDistance);
				entry.SetFloat("sustain end distance", playback.SustainEndDistance);
				entry.SetFloat("maximum distance", playback.MaxDistance);
				entry.SetFloat("sustain db", playback.SustainDB);
				entry.SetFloat("skip fraction", playback.SkipFraction);
				entry.SetFloat("max bend per second", playback.MaxPendPerSec);
				entry.SetFloat("gain base", playback.GainBase);
				entry.SetFloat("gain variance", playback.GainVariance);
				entry.SetInteger("random pitch bounds min", (uint)playback.RandomPitchBoundsMin);
				entry.SetInteger("random pitch bounds max", (uint)playback.RandomPitchBoundsMax);
				entry.SetFloat("inner cone angle", playback.InnerConeAngle);
				entry.SetFloat("outer cone angle", playback.OuterConeAngle);
				entry.SetFloat("outer cone gain", playback.OuterConeGain);
				entry.SetInteger("flags", (uint)playback.Flags);
				entry.SetFloat("azimuth", playback.Azimuth);
				entry.SetFloat("position gain", playback.PositionalGain);
				entry.SetFloat("first person gain", playback.FirstPersonGain);
				entries.Add(entry);
			}

			int oldCount = (int)values.GetInteger("number of playbacks");
			long oldAddress = _expander.Expand((uint)values.GetInteger("playback table address"));

			uint newAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(entries, oldCount, oldAddress, layout, _metaArea, _allocator, stream));

			values.SetInteger("number of playbacks", (uint)entries.Count);
			values.SetInteger("playback table address", newAddress);

			SaveTag(values, stream);
		}
		#endregion

		#region Scales
		public IEnumerable<SoundScale> LoadScales(IReader reader)
		{
			StructureValueCollection values = LoadTag(reader);
			StructureValueCollection[] entries = ReadBlock(values, reader, "number of scales", "scales table address", "sound scale");
			return entries.Select(e => LoadScale(e));
		}

		private SoundScale LoadScale(StructureValueCollection values)
		{
			return new SoundScale()
			{
				GainMin = values.GetFloat("gain min"),
				GainMax = values.GetFloat("gain max"),
				PitchMin = (int)values.GetInteger("pitch min"),
				PitchMax = (int)values.GetInteger("pitch max"),
				SkipFractionMin = values.GetFloat("skip fraction min"),
				SkipFractionMax = values.GetFloat("skip fraction max")
			};
		}

		public void SaveScales(ICollection<SoundScale> scales, IStream stream)
		{
			StructureValueCollection values = LoadTag(stream);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound scale");

			var entries = new List<StructureValueCollection>();

			foreach (var scale in scales)
			{
				StructureValueCollection entry = new StructureValueCollection();
				entry.SetFloat("gain min", scale.GainMin);
				entry.SetFloat("gain max", scale.GainMax);
				entry.SetInteger("pitch min", (uint)scale.PitchMin);
				entry.SetInteger("pitch max", (uint)scale.PitchMax);
				entry.SetFloat("skip fraction min", scale.SkipFractionMin);
				entry.SetFloat("skip fraction max", scale.SkipFractionMax);
				entries.Add(entry);
			}

			int oldCount = (int)values.GetInteger("number of scales");
			long oldAddress = _expander.Expand((uint)values.GetInteger("scales table address"));

			uint newAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(entries, oldCount, oldAddress, layout, _metaArea, _allocator, stream));

			values.SetInteger("number of scales", (uint)entries.Count);
			values.SetInteger("scales table address", newAddress);

			SaveTag(values, stream);
		}
		#endregion

		#region Custom Playbacks Global
		public IEnumerable<SoundCustomPlayback> LoadCustomPlaybacks(TagTable tags, IReader reader)
		{
			StructureValueCollection values = LoadTag(reader);

			if (_buildInfo.Layouts.HasLayout("sound custom playback reach"))
			{
				StructureValueCollection[] entries = ReadBlock(values, reader, "number of custom playbacks", "custom playback table address", "sound custom playback reach");
				return entries.Select(e => LoadCustomPlaybackReach(e, tags, reader));
			}
			else
			{
				StructureValueCollection[] entries = ReadBlock(values, reader, "number of custom playbacks", "custom playback table address", "sound custom playback");
				return entries.Select(e => LoadCustomPlayback(e, reader));
			}
		}

		public void SaveCustomPlaybacks(ICollection<SoundCustomPlayback> cplaybacks, IStream stream)
		{
			if (_buildInfo.Layouts.HasLayout("sound custom playback reach"))
				SaveCustomPlaybacksReach(cplaybacks, stream);
			else
				SaveCustomPlaybacksDefault(cplaybacks, stream);
		}
		#endregion

		#region Custom Playbacks (Default)
		private SoundCustomPlayback LoadCustomPlayback(StructureValueCollection values, IReader reader)
		{
			SoundCustomPlayback result = new SoundCustomPlayback();

			result.Version = SoundCustomPlaybackVersion.Default;

			result.Flags = (int)values.GetInteger("flags");

			result.Unknown = (int)values.GetInteger("unknown1");
			result.Unknown1 = (int)values.GetInteger("unknown2");

			result.Unknown2 = (int)values.GetInteger("unknown3");
			result.Unknown3 = (int)values.GetInteger("unknown4");
			result.Unknown4 = (int)values.GetInteger("unknown5");

			var mixes = ReadBlock(values, reader, "number of mixes", "mix table address", "sound custom playback mix");
			result.Mixes = mixes.Select(e => LoadCustomPlaybackMix(e)).ToArray();
		
			var filters = ReadBlock(values, reader, "number of filters", "filter table address", "sound custom playback filter");
			result.Filters = filters.Select(e => LoadCustomPlaybackFilter(e)).ToArray();

			var pitchLFOs = ReadBlock(values, reader, "number of pitch lfos", "pitch lfo table address", "sound custom playback pitch lfo");
			result.PitchLFOs = pitchLFOs.Select(e => LoadCustomPlaybackPitchLFO(e)).ToArray();

			var filterLFOs = ReadBlock(values, reader, "number of filter lfos", "filter lfo table address", "sound custom playback filter lfo");
			result.FilterLFOs = filterLFOs.Select(e => LoadCustomPlaybackFilterLFO(e)).ToArray();

			return result;
		}

		private SoundCustomPlaybackMix LoadCustomPlaybackMix(StructureValueCollection values)
		{
			return new SoundCustomPlaybackMix()
			{
				Mixbin = (int)values.GetInteger("mixbin"),
				Gain = values.GetFloat("gain")
			};
		}

		private SoundCustomPlaybackFilter LoadCustomPlaybackFilter(StructureValueCollection values)
		{
			return new SoundCustomPlaybackFilter()
			{
				Type = (int)values.GetInteger("type"),
				Width = (int)values.GetInteger("width"),

				LeftFreqScaleMin = values.GetFloat("left freq scale min"),
				LeftFreqScaleMax = values.GetFloat("left freq scale max"),
				LeftFreqRandomBase = values.GetFloat("left freq random base"),
				LeftFreqRandomVariance = values.GetFloat("left freq random variance"),
				LeftGainScaleMin = values.GetFloat("left gain scale min"),
				LeftGainScaleMax = values.GetFloat("left gain scale max"),
				LeftGainRandomBase = values.GetFloat("left gain random base"),
				LeftGainRandomVariance = values.GetFloat("left gain random variance"),
				RightFreqScaleMin = values.GetFloat("right freq scale min"),
				RightFreqScaleMax = values.GetFloat("right freq scale max"),
				RightFreqRandomBase = values.GetFloat("right freq random base"),
				RightFreqRandomVariance = values.GetFloat("right freq random variance"),
				RightGainScaleMin = values.GetFloat("right gain scale min"),
				RightGainScaleMax = values.GetFloat("right gain scale max"),
				RightGainRandomBase = values.GetFloat("right gain random base"),
				RightGainRandomVariance = values.GetFloat("right gain random variance")
			};
		}

		private SoundCustomPlaybackPitchLFO LoadCustomPlaybackPitchLFO(StructureValueCollection values)
		{
			return new SoundCustomPlaybackPitchLFO()
			{
				DelayScaleMin = values.GetFloat("delay scale min"),
				DelayScaleMax = values.GetFloat("delay scale max"),
				DelayRandomBase = values.GetFloat("delay random base"),
				DelayRandomVariance = values.GetFloat("delay random variance"),

				FreqScaleMin = values.GetFloat("freq scale min"),
				FreqScaleMax = values.GetFloat("freq scale max"),
				FreqRandomBase = values.GetFloat("freq random base"),
				FreqRandomVariance = values.GetFloat("freq random variance"),

				PitchModScaleMin = values.GetFloat("pitch mod scale min"),
				PitchModScaleMax = values.GetFloat("pitch mod scale max"),
				PitchModRandomBase = values.GetFloat("pitch mod random base"),
				PitchModRandomVariance = values.GetFloat("pitch mod random variance")
			};
		}

		private SoundCustomPlaybackFilterLFO LoadCustomPlaybackFilterLFO(StructureValueCollection values)
		{
			return new SoundCustomPlaybackFilterLFO()
			{
				DelayScaleMin = values.GetFloat("delay scale min"),
				DelayScaleMax = values.GetFloat("delay scale max"),
				DelayRandomBase = values.GetFloat("delay random base"),
				DelayRandomVariance = values.GetFloat("delay random variance"),

				FreqScaleMin = values.GetFloat("freq scale min"),
				FreqScaleMax = values.GetFloat("freq scale max"),
				FreqRandomBase = values.GetFloat("freq random base"),
				FreqRandomVariance = values.GetFloat("freq random variance"),

				CutoffModScaleMin = values.GetFloat("cutoff mod scale min"),
				CutoffModScaleMax = values.GetFloat("cutoff mod scale max"),
				CutoffModRandomBase = values.GetFloat("cutoff mod random base"),
				CutoffModRandomVariance = values.GetFloat("cutoff mod random variance"),

				GainModScaleMin = values.GetFloat("gain mod scale min"),
				GainModScaleMax = values.GetFloat("gain mod scale max"),
				GainModRandomBase = values.GetFloat("gain mod random base"),
				GainModRandomVariance = values.GetFloat("gain mod random variance")
			};
		}

		private void SaveCustomPlaybacksDefault(ICollection<SoundCustomPlayback> cplaybacks, IStream stream)
		{
			StructureValueCollection values = LoadTag(stream);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound custom playback");

			FreeCustomPlaybacks(values, stream, "number of custom playbacks", "custom playback table address", "sound custom playback");

			StructureLayout mixLayout = _buildInfo.Layouts.GetLayout("sound custom playback mix");
			StructureLayout filterLayout = _buildInfo.Layouts.GetLayout("sound custom playback filter");
			StructureLayout pitchlfoLayout = _buildInfo.Layouts.GetLayout("sound custom playback pitch lfo");
			StructureLayout filterlfoLayout = _buildInfo.Layouts.GetLayout("sound custom playback filter lfo");

			var entries = new List<StructureValueCollection>();

			foreach (var cplayback in cplaybacks)
			{
				StructureValueCollection entry = new StructureValueCollection();

				entry.SetInteger("flags", (uint)cplayback.Flags);

				entry.SetInteger("unknown1", (uint)cplayback.Unknown);
				entry.SetInteger("unknown2", (uint)cplayback.Unknown1);

				entry.SetInteger("unknown3", (uint)cplayback.Unknown2);
				entry.SetInteger("unknown4", (uint)cplayback.Unknown3);
				entry.SetInteger("unknown5", (uint)cplayback.Unknown4);

				List<StructureValueCollection> mixEntries = cplayback.Mixes.Select(f => SerializeCustomPlaybackMix(f)).ToList();
				uint mixNewAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(mixEntries.ToList(), mixLayout, _metaArea, _allocator, stream));
				entry.SetInteger("number of mixes", (uint)mixEntries.Count);
				entry.SetInteger("mix table address", mixNewAddress);

				List<StructureValueCollection> filterEntries = cplayback.Filters.Select(f => SerializeCustomPlaybackFilter(f)).ToList();
				uint filterNewAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(filterEntries.ToList(), filterLayout, _metaArea, _allocator, stream));
				entry.SetInteger("number of filters", (uint)filterEntries.Count);
				entry.SetInteger("filter table address", filterNewAddress);

				List<StructureValueCollection> pitchlfoEntries = cplayback.PitchLFOs.Select(f => SerializeCustomPlaybackPitchLFO(f)).ToList();
				uint pitchlfoNewAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(pitchlfoEntries.ToList(), pitchlfoLayout, _metaArea, _allocator, stream));
				entry.SetInteger("number of pitch lfos", (uint)pitchlfoEntries.Count);
				entry.SetInteger("pitch pfo table address", pitchlfoNewAddress);

				List<StructureValueCollection> filterlfoEntries = cplayback.Mixes.Select(f => SerializeCustomPlaybackMix(f)).ToList();
				uint filterlfoNewAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(filterlfoEntries.ToList(), filterlfoLayout, _metaArea, _allocator, stream));
				entry.SetInteger("number of filter lfos", (uint)filterlfoEntries.Count);
				entry.SetInteger("filter lfo table address", filterlfoNewAddress);

				entries.Add(entry);
			}

			uint newAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(entries, layout, _metaArea, _allocator, stream));

			values.SetInteger("number of custom playbacks", (uint)entries.Count);
			values.SetInteger("custom playback table address", newAddress);

			SaveTag(values, stream);
		}

		private StructureValueCollection SerializeCustomPlaybackMix(SoundCustomPlaybackMix mix)
		{
			var result = new StructureValueCollection();

			result.SetInteger("mixbin", (uint)mix.Mixbin);
			result.SetFloat("gain", mix.Gain);

			return result;
		}

		private StructureValueCollection SerializeCustomPlaybackFilter(SoundCustomPlaybackFilter filter)
		{
			var result = new StructureValueCollection();

			result.SetInteger("type", (uint)filter.Type);
			result.SetInteger("width", (uint)filter.Width);

			result.SetFloat("left freq scale min", filter.LeftFreqScaleMin);
			result.SetFloat("left freq scale max", filter.LeftFreqScaleMax);
			result.SetFloat("left freq random base", filter.LeftFreqRandomBase);
			result.SetFloat("left freq random variance", filter.LeftFreqRandomVariance);

			result.SetFloat("left gain scale min", filter.LeftGainScaleMin);
			result.SetFloat("left gain scale max", filter.LeftGainScaleMax);
			result.SetFloat("left gain random base", filter.LeftGainRandomBase);
			result.SetFloat("left gain random variance", filter.LeftGainRandomVariance);

			result.SetFloat("right freq scale min", filter.RightFreqScaleMin);
			result.SetFloat("right freq scale max", filter.RightFreqScaleMax);
			result.SetFloat("right freq random base", filter.RightFreqRandomBase);
			result.SetFloat("right freq random variance", filter.RightFreqRandomVariance);

			result.SetFloat("right gain scale min", filter.RightGainScaleMin);
			result.SetFloat("right gain scale max", filter.RightGainScaleMax);
			result.SetFloat("right gain random base", filter.RightGainRandomBase);
			result.SetFloat("right gain random variance", filter.RightGainRandomVariance);

			return result;
		}

		private StructureValueCollection SerializeCustomPlaybackPitchLFO(SoundCustomPlaybackPitchLFO pitchLFO)
		{
			var result = new StructureValueCollection();

			result.SetFloat("delay scale min", pitchLFO.DelayScaleMin);
			result.SetFloat("delay scale max", pitchLFO.DelayScaleMax);
			result.SetFloat("delay random base", pitchLFO.DelayRandomBase);
			result.SetFloat("delay random variance", pitchLFO.DelayRandomVariance);

			result.SetFloat("freq scale min", pitchLFO.FreqScaleMin);
			result.SetFloat("freq scale max", pitchLFO.FreqScaleMax);
			result.SetFloat("freq random base", pitchLFO.FreqRandomBase);
			result.SetFloat("freq random variance", pitchLFO.FreqRandomVariance);

			result.SetFloat("pitch mod scale min", pitchLFO.PitchModScaleMin);
			result.SetFloat("pitch mod scale max", pitchLFO.PitchModScaleMax);
			result.SetFloat("pitch mod random base", pitchLFO.PitchModRandomBase);
			result.SetFloat("pitch mod random variance", pitchLFO.PitchModRandomVariance);

			return result;
		}

		private StructureValueCollection SerializeCustomPlaybackFilterLFO(SoundCustomPlaybackFilterLFO filterLFO)
		{
			var result = new StructureValueCollection();

			result.SetFloat("delay scale min", filterLFO.DelayScaleMin);
			result.SetFloat("delay scale max", filterLFO.DelayScaleMax);
			result.SetFloat("delay random base", filterLFO.DelayRandomBase);
			result.SetFloat("delay random variance", filterLFO.DelayRandomVariance);

			result.SetFloat("freq scale min", filterLFO.FreqScaleMin);
			result.SetFloat("freq scale max", filterLFO.FreqScaleMax);
			result.SetFloat("freq random base", filterLFO.FreqRandomBase);
			result.SetFloat("freq random variance", filterLFO.FreqRandomVariance);

			result.SetFloat("cutoff mod scale min", filterLFO.CutoffModScaleMin);
			result.SetFloat("cutoff mod scale max", filterLFO.CutoffModScaleMax);
			result.SetFloat("cutoff mod random base", filterLFO.CutoffModRandomBase);
			result.SetFloat("cutoff mod random variance", filterLFO.CutoffModRandomVariance);

			result.SetFloat("gain mod scale min", filterLFO.GainModScaleMin);
			result.SetFloat("gain mod scale max", filterLFO.GainModScaleMax);
			result.SetFloat("gain mod random base", filterLFO.GainModRandomBase);
			result.SetFloat("gain mod random variance", filterLFO.GainModRandomVariance);

			return result;
		}

		private void FreeCustomPlaybacks(StructureValueCollection basevalues, IStream stream, string countName, string addressName, string layoutName)
		{
			StructureValueCollection[] entries = ReadBlock(basevalues, stream, countName, addressName, layoutName);
			foreach (StructureValueCollection entry in entries)
			{
				FreeTagBlock(entry, "number of mixes", "mix table address", "sound custom playback mix");
				FreeTagBlock(entry, "number of filters", "filter table address", "sound custom playback filter");
				FreeTagBlock(entry, "number of pitch lfos", "pitch lfo table address", "sound custom playback pitch lfo");
				FreeTagBlock(entry, "number of filter lfos", "filter lfo table address", "sound custom playback filter lfo");
			}

			FreeTagBlock(basevalues, countName, addressName, layoutName);
		}
		#endregion

		#region Custom Playbacks (Reach)
		private SoundCustomPlayback LoadCustomPlaybackReach(StructureValueCollection values, TagTable tags, IReader reader)
		{
			SoundCustomPlayback result = new SoundCustomPlayback();

			result.Version = SoundCustomPlaybackVersion.Reach;

			result.Flags = (int)values.GetInteger("flags");

			var radioTag = new DatumIndex(values.GetInteger("radio effect tag datum index"));
			result.RadioEffect = radioTag.IsValid ? tags[radioTag] : null;

			var lowpasses = ReadBlock(values, reader, "number of lowpass effects", "lowpass effect table address", "sound custom playback lowpass effect");
			result.LowpassEffects = lowpasses.Select(e => LoadCustomPlaybackLowpassEffect(e)).ToArray();

			var components = ReadBlock(values, reader, "number of components", "component table address", "sound custom playback components");
			result.Components = components.Select(e => LoadCustomPlaybackComponent(e, tags)).ToArray();

			return result;
		}

		private SoundCustomPlaybackLowpassEffect LoadCustomPlaybackLowpassEffect(StructureValueCollection values)
		{
			return new SoundCustomPlaybackLowpassEffect()
			{
				Attack = values.GetFloat("attack"),
				Release = values.GetFloat("release"),
				CutoffFrequency = values.GetFloat("cutoff frequency"),
				OutputGain = values.GetFloat("output gain"),
			};
		}

		private SoundCustomPlaybackComponent LoadCustomPlaybackComponent(StructureValueCollection values, TagTable tags)
		{
			SoundCustomPlaybackComponent result = new SoundCustomPlaybackComponent();
			var soundTag = new DatumIndex(values.GetInteger("sound tag datum index"));
			result.Sound = soundTag.IsValid ? tags[soundTag] : null;

			result.Gain = values.GetFloat("gain");
			result.Flags = (int)values.GetInteger("flags");

			return result;
		}

		private void SaveCustomPlaybacksReach(ICollection<SoundCustomPlayback> cplaybacks, IStream stream)
		{
			StructureValueCollection values = LoadTag(stream);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound custom playback reach");

			FreeCustomPlaybacksReach(values, stream, "number of custom playbacks", "custom playback table address", "sound custom playback reach");

			var entries = new List<StructureValueCollection>();

			StructureLayout lowpassLayout = _buildInfo.Layouts.GetLayout("sound custom playback lowpass effect");
			StructureLayout componentLayout = _buildInfo.Layouts.GetLayout("sound custom playback components");

			foreach (var cplayback in cplaybacks)
			{
				StructureValueCollection entry = new StructureValueCollection();


				entry.SetInteger("flags", (uint)cplayback.Flags);

				if (cplayback.RadioEffect != null)
				{
					entry.SetInteger("radio effect tag group magic", (uint)cplayback.RadioEffect.Group.Magic);
					entry.SetInteger("radio effect tag datum index", cplayback.RadioEffect.Index.Value);
				}
				else
				{
					entry.SetInteger("radio effect tag group magic", 0xFFFFFFFF);
					entry.SetInteger("radio effect tag datum index", 0xFFFFFFFF);
				}

				List<StructureValueCollection> lowpassEntries = cplayback.LowpassEffects.Select(f => SerializeCustomPlaybackLowpassEffect(f)).ToList();
				uint lowpassnewAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(lowpassEntries.ToList(), lowpassLayout, _metaArea, _allocator, stream));
				entry.SetInteger("number of lowpass effects", (uint)lowpassEntries.Count);
				entry.SetInteger("lowpass effect table address", lowpassnewAddress);

				List<StructureValueCollection> componentEntries = cplayback.Components.Select(f => SerializeCustomPlaybackComponent(f)).ToList();
				uint componentnewAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(componentEntries.ToList(), componentLayout, _metaArea, _allocator, stream));
				entry.SetInteger("number of components", (uint)componentEntries.Count);
				entry.SetInteger("component table address", lowpassnewAddress);

				entries.Add(entry);
			}

			uint newAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(entries, layout, _metaArea, _allocator, stream));

			values.SetInteger("number of custom playbacks", (uint)entries.Count);
			values.SetInteger("custom playback table address", newAddress);

			SaveTag(values, stream);
		}

		private StructureValueCollection SerializeCustomPlaybackLowpassEffect(SoundCustomPlaybackLowpassEffect lowpass)
		{
			var result = new StructureValueCollection();

			result.SetFloat("attack", lowpass.Attack);
			result.SetFloat("release", lowpass.Release);
			result.SetFloat("cutoff frequency", lowpass.CutoffFrequency);
			result.SetFloat("output gain", lowpass.OutputGain);

			return result;
		}

		private StructureValueCollection SerializeCustomPlaybackComponent(SoundCustomPlaybackComponent component)
		{
			var result = new StructureValueCollection();

			if (component.Sound != null)
			{
				result.SetInteger("sound tag group magic", (uint)component.Sound.Group.Magic);
				result.SetInteger("sound tag datum index", component.Sound.Index.Value);
			}
			else
			{
				result.SetInteger("sound tag group magic", 0xFFFFFFFF);
				result.SetInteger("sound tag datum index", 0xFFFFFFFF);
			}

			result.SetFloat("gain", component.Gain);
			result.SetInteger("flags", (uint)component.Flags);

			return result;
		}

		private void FreeCustomPlaybacksReach(StructureValueCollection basevalues, IStream stream, string countName, string addressName, string layoutName)
		{
			StructureValueCollection[] entries = ReadBlock(basevalues, stream, countName, addressName, layoutName);
			foreach (StructureValueCollection entry in entries)
			{
				FreeTagBlock(entry, "number of lowpass effects", "lowpass effect table address", "sound custom playback lowpass effect");
				FreeTagBlock(entry, "number of components", "component table address", "sound custom playback components");
			}

			FreeTagBlock(basevalues, countName, addressName, layoutName);
		}
		#endregion

		#region Extra Info Global
		public IEnumerable<SoundExtraInfo> LoadExtraInfos(IReader reader)
		{
			StructureValueCollection values = LoadTag(reader);

			if (_buildInfo.Layouts.HasLayout("sound extra info reachbeta"))
			{
				StructureValueCollection[] entries = ReadBlock(values, reader, "number of extra infos", "extra info table address", "sound extra info reachbeta");
				return entries.Select(e => LoadExtraInfoReachBeta(e, reader));
			}
			else if (_buildInfo.Layouts.HasLayout("sound extra info reach"))
			{
				StructureValueCollection[] entries = ReadBlock(values, reader, "number of extra infos", "extra info table address", "sound extra info reach");
				return entries.Select(e => LoadExtraInfoReach(e));
			}
			else
			{
				StructureValueCollection[] entries = ReadBlock(values, reader, "number of extra infos", "extra info table address", "sound extra info");
				return entries.Select(e => LoadExtraInfo(e, reader));
			}
		}

		public void SaveExtraInfos(ICollection<SoundExtraInfo> extraInfos, IStream stream)
		{
			if (_buildInfo.Layouts.HasLayout("sound extra info reachbeta"))
				SaveExtraInfosReachBeta(extraInfos, stream);
			else if (_buildInfo.Layouts.HasLayout("sound extra info reach"))
				SaveExtraInfosReach(extraInfos, stream);
			else
				SaveExtraInfosDefault(extraInfos, stream);
		}
		#endregion

		#region Extra Info (Default)
		private SoundExtraInfo LoadExtraInfo(StructureValueCollection values, IReader reader)
		{
			SoundExtraInfo result = new SoundExtraInfo();

			var permutationSections = ReadBlock(values, reader, "number of encoded permutation sections", "encoded permutation section table address", "sound encoded permutation section");
			result.PermutationSections = permutationSections.Select(e => LoadExtraInfoPermutationSection(e, reader)).ToArray();

			return result;
		}

		private SoundExtraInfoPermutationSection LoadExtraInfoPermutationSection(StructureValueCollection values, IReader reader)
		{
			SoundExtraInfoPermutationSection result = new SoundExtraInfoPermutationSection();

			result.EncodedData = LoadExtraInfoBuffer(values, reader);

			var dialogInfos = ReadBlock(values, reader, "number of dialogue infos", "dialogue infos table address", "sound dialogue info");
			result.DialogueInfos = dialogInfos.Select(e => LoadExtraInfoDialogueInfo(e)).ToArray();

			var unk1s = ReadBlock(values, reader, "number of unknown1s", "unknown1 table address", "sound extra info unknown1");
			result.Unknown1s = unk1s.Select(e => LoadExtraInfoUnknown1(e, reader)).ToArray();

			return result;
		}

		private byte[] LoadExtraInfoBuffer(StructureValueCollection values, IReader reader)
		{
			var size = (int)values.GetInteger("encoded data size");
			uint address = (uint)values.GetInteger("encoded data pointer");

			long expand = _expander.Expand(address);

			if (size <= 0 || address == 0)
				return new byte[0];

			uint offset = _metaArea.PointerToOffset(expand);
			reader.SeekTo(offset);
			return reader.ReadBlock(size);
		}

		private SoundExtraInfoDialogueInfo LoadExtraInfoDialogueInfo(StructureValueCollection values)
		{
			return new SoundExtraInfoDialogueInfo()
			{
				MouthOffset = (int)values.GetInteger("mouth data offset"),
				MouthLength = (int)values.GetInteger("mouth data length"),
				LipsyncOffset = (int)values.GetInteger("lipsync data offset"),
				LipsyncLength = (int)values.GetInteger("lipsync data length"),
			};
		}

		private SoundExtraInfoUnknown1 LoadExtraInfoUnknown1(StructureValueCollection values, IReader reader)
		{
			SoundExtraInfoUnknown1 result = new SoundExtraInfoUnknown1();

			result.Unknown = (int)values.GetIntegerOrDefault("unknown1", 0);
			result.Unknown1 = (int)values.GetIntegerOrDefault("unknown2", 0);
			result.Unknown2 = (int)values.GetIntegerOrDefault("unknown3", 0);
			result.Unknown3 = (int)values.GetIntegerOrDefault("unknown4", 0);
			result.Unknown4 = (int)values.GetIntegerOrDefault("unknown5", 0);
			result.Unknown5 = (int)values.GetIntegerOrDefault("unknown6", 0);
			result.Unknown6 = (int)values.GetIntegerOrDefault("unknown7", 0);
			result.Unknown7 = (int)values.GetIntegerOrDefault("unknown8", 0);
			result.Unknown8 = (int)values.GetIntegerOrDefault("unknown9", 0);
			result.Unknown9 = (int)values.GetIntegerOrDefault("unknown10", 0);
			result.Unknown10 = (int)values.GetIntegerOrDefault("unknown11", 0);
			result.Unknown11 = (int)values.GetIntegerOrDefault("unknown12", 0);

			var unk2s = ReadBlock(values, reader, "number of unknown2s", "unknown2 table address", "sound extra info unknown2");
			result.Unknown12s = unk2s.Select(e => LoadExtraInfoUnknown2(e, reader)).ToArray();

			return result;
		}

		private SoundExtraInfoUnknown2 LoadExtraInfoUnknown2(StructureValueCollection values, IReader reader)
		{
			SoundExtraInfoUnknown2 result = new SoundExtraInfoUnknown2();

			result.Unknown = values.GetFloat("unknown1");
			result.Unknown1 = values.GetFloat("unknown2");
			result.Unknown2 = values.GetFloat("unknown3");
			result.Unknown3 = values.GetFloat("unknown4");

			var unk3s = ReadBlock(values, reader, "number of unknown3s", "unknown3 table address", "sound extra info unknown3");
			result.Unknown5s = unk3s.Select(e => LoadExtraInfoUnknown3(e)).ToArray();

			var unk4s = ReadBlock(values, reader, "number of unknown4s", "unknown4 table address", "sound extra info unknown4");
			result.Unknown6s = unk4s.Select(e => LoadExtraInfoUnknown4(e)).ToArray();

			return result;
		}

		private SoundExtraInfoUnknown3 LoadExtraInfoUnknown3(StructureValueCollection values)
		{
			return new SoundExtraInfoUnknown3()
			{
				Unknown = (int)values.GetInteger("unknown1"),
				Unknown1 = (int)values.GetInteger("unknown2"),
				Unknown2 = (int)values.GetInteger("unknown3"),
				Unknown3 = (int)values.GetInteger("unknown4"),
			};
		}

		private SoundExtraInfoUnknown4 LoadExtraInfoUnknown4(StructureValueCollection values)
		{
			return new SoundExtraInfoUnknown4()
			{
				Unknown = (int)values.GetInteger("unknown1"),
				Unknown1 = (int)values.GetInteger("unknown2"),
			};
		}

		private void SaveExtraInfosDefault(ICollection<SoundExtraInfo> extraInfos, IStream stream)
		{
			StructureValueCollection values = LoadTag(stream);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound extra info");

			FreeExtraInfosDefault(values, stream, "number of extra infos", "extra info table address", "sound extra info");

			var entries = new List<StructureValueCollection>();

			StructureLayout permutationsectionLayout = _buildInfo.Layouts.GetLayout("sound encoded permutation section");

			foreach (var extraInfo in extraInfos)
			{
				StructureValueCollection entry = new StructureValueCollection();

				List<StructureValueCollection> permutationsectionEntries = extraInfo.PermutationSections.Select(f => SaveExtraPermutationSection(f, stream)).ToList();
				uint mixNewAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(permutationsectionEntries.ToList(), permutationsectionLayout, _metaArea, _allocator, stream));
				entry.SetInteger("number of encoded permutation sections", (uint)permutationsectionEntries.Count);
				entry.SetInteger("encoded permutation section table address", mixNewAddress);

				entries.Add(entry);
			}

			uint newAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(entries, layout, _metaArea, _allocator, stream));

			values.SetInteger("number of extra infos", (uint)entries.Count);
			values.SetInteger("extra info table address", newAddress);

			SaveTag(values, stream);
		}

		private StructureValueCollection SaveExtraPermutationSection(SoundExtraInfoPermutationSection section, IStream stream)
		{
			var result = new StructureValueCollection();

			result = SaveExtraBuffer(section.EncodedData, result, stream);

			StructureLayout dialogueinfoLayout = _buildInfo.Layouts.GetLayout("sound dialogue info");
			List<StructureValueCollection> dialogueinfoEntries = section.DialogueInfos.Select(f => SerializeExtraDialogueInfo(f)).ToList();
			uint dialogueinfoNewAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(dialogueinfoEntries.ToList(), dialogueinfoLayout, _metaArea, _allocator, stream));
			result.SetInteger("number of dialogue infos", (uint)dialogueinfoEntries.Count);
			result.SetInteger("dialogue infos table address", dialogueinfoNewAddress);

			StructureLayout unk1Layout = _buildInfo.Layouts.GetLayout("sound extra info unknown1");
			List<StructureValueCollection> unk1Entries = section.Unknown1s.Select(f => SaveExtraUnknown1(f, stream)).ToList();
			uint unk1NewAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(unk1Entries.ToList(), unk1Layout, _metaArea, _allocator, stream));
			result.SetInteger("number of unknown1s", (uint)unk1Entries.Count);
			result.SetInteger("unknown1 table address", unk1NewAddress);

			return result;
		}

		private StructureValueCollection SaveExtraBuffer(byte[] buffer, StructureValueCollection values, IStream stream)
		{
			long newAddress = 0;
			if (buffer.Length > 0)
			{
				newAddress = _allocator.Allocate((uint)buffer.Length, stream);
				stream.SeekTo(_metaArea.PointerToOffset(newAddress));
				stream.WriteBlock(buffer);
			}

			uint cont = _expander.Contract(newAddress);

			values.SetInteger("encoded data size", (uint)buffer.Length);
			values.SetInteger("encoded data pointer", cont);
			return values;
		}

		private StructureValueCollection SerializeExtraDialogueInfo(SoundExtraInfoDialogueInfo info)
		{
			var result = new StructureValueCollection();
			result.SetInteger("mouth data offset", (uint)info.MouthOffset);
			result.SetInteger("mouth data length", (uint)info.MouthLength);
			result.SetInteger("lipsync data offset", (uint)info.LipsyncOffset);
			result.SetInteger("lipsync data length", (uint)info.LipsyncLength);
			return result;
		}

		private StructureValueCollection SaveExtraUnknown1(SoundExtraInfoUnknown1 unk1, IStream stream)
		{
			var result = new StructureValueCollection();

			result.SetInteger("unknown1", (uint)unk1.Unknown);
			result.SetInteger("unknown2", (uint)unk1.Unknown1);
			result.SetInteger("unknown3", (uint)unk1.Unknown2);
			result.SetInteger("unknown4", (uint)unk1.Unknown3);
			result.SetInteger("unknown5", (uint)unk1.Unknown4);
			result.SetInteger("unknown6", (uint)unk1.Unknown5);
			result.SetInteger("unknown7", (uint)unk1.Unknown6);
			result.SetInteger("unknown8", (uint)unk1.Unknown7);
			result.SetInteger("unknown9", (uint)unk1.Unknown8);
			result.SetInteger("unknown10", (uint)unk1.Unknown9);
			result.SetInteger("unknown11", (uint)unk1.Unknown10);
			result.SetInteger("unknown12", (uint)unk1.Unknown11);

			StructureLayout unk2Layout = _buildInfo.Layouts.GetLayout("sound extra info unknown2");
			List<StructureValueCollection> unk2Entries = new List<StructureValueCollection>();
			if (unk1.Unknown12s != null)
				unk2Entries = unk1.Unknown12s.Select(f => SaveExtraUnknown2(f, stream)).ToList();
			uint unk2NewAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(unk2Entries.ToList(), unk2Layout, _metaArea, _allocator, stream));
			result.SetInteger("number of unknown2s", (uint)unk2Entries.Count);
			result.SetInteger("unknown2 table address", unk2NewAddress);

			return result;
		}

		private StructureValueCollection SaveExtraUnknown2(SoundExtraInfoUnknown2 unk2, IStream stream)
		{
			var result = new StructureValueCollection();

			result.SetFloat("unknown1", unk2.Unknown);
			result.SetFloat("unknown2", unk2.Unknown1);
			result.SetFloat("unknown3", unk2.Unknown2);
			result.SetFloat("unknown4", unk2.Unknown3);

			StructureLayout unk3Layout = _buildInfo.Layouts.GetLayout("sound extra info unknown3");
			List<StructureValueCollection> unk3Entries = unk2.Unknown5s.Select(f => SerializeExtraUnknown3(f)).ToList();
			uint unk3NewAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(unk3Entries.ToList(), unk3Layout, _metaArea, _allocator, stream));
			result.SetInteger("number of unknown3s", (uint)unk3Entries.Count);
			result.SetInteger("unknown3 table address", unk3NewAddress);

			StructureLayout unk4Layout = _buildInfo.Layouts.GetLayout("sound extra info unknown4");
			List<StructureValueCollection> unk4Entries = unk2.Unknown6s.Select(f => SerializeExtraUnknown4(f)).ToList();
			uint unk4NewAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(unk4Entries.ToList(), unk4Layout, _metaArea, _allocator, stream));
			result.SetInteger("number of unknown4s", (uint)unk4Entries.Count);
			result.SetInteger("unknown4 table address", unk4NewAddress);

			return result;
		}

		private StructureValueCollection SerializeExtraUnknown3(SoundExtraInfoUnknown3 unk)
		{
			var result = new StructureValueCollection();
			result.SetInteger("unknown1", (uint)unk.Unknown);
			result.SetInteger("unknown2", (uint)unk.Unknown1);
			result.SetInteger("unknown3", (uint)unk.Unknown2);
			result.SetInteger("unknown4", (uint)unk.Unknown3);
			return result;
		}

		private StructureValueCollection SerializeExtraUnknown4(SoundExtraInfoUnknown4 unk)
		{
			var result = new StructureValueCollection();
			result.SetInteger("unknown1", (uint)unk.Unknown);
			result.SetInteger("unknown2", (uint)unk.Unknown1);
			return result;
		}

		private void FreeExtraInfosDefault(StructureValueCollection basevalues, IStream stream, string countName, string addressName, string layoutName)
		{
			StructureValueCollection[] entries = ReadBlock(basevalues, stream, countName, addressName, layoutName);
			foreach (StructureValueCollection entry in entries)
			{
				FreeExtraInfoSections(entry, stream, "number of encoded permutation sections", "encoded permutation section table address", "sound encoded permutation section");
			}

			FreeTagBlock(basevalues, countName, addressName, layoutName);
		}
		private void FreeExtraInfoSections(StructureValueCollection basevalues, IStream stream, string countName, string addressName, string layoutName)
		{
			StructureValueCollection[] entries = ReadBlock(basevalues, stream, countName, addressName, layoutName);
			foreach (StructureValueCollection entry in entries)
			{
				// if the length is 0 then theres a good chance that the following blocks will be 0'd and have a chance to cause corruption if freed
				if ((int)entry.GetInteger("encoded data size") == 0)
					continue;

				FreeExtraInfoBuffer(entry);
				FreeTagBlock(entry, "number of dialogue infos", "dialogue infos table address", "sound dialogue info");
				FreeExtraInfoUnknown1s(entry, stream, "number of unknown1s", "unknown1 table address", "sound extra info unknown1");
			}

			FreeTagBlock(basevalues, countName, addressName, layoutName);
		}
		private void FreeExtraInfoBuffer(StructureValueCollection basevalues)
		{
			var buffsize = (uint)basevalues.GetInteger("encoded data size");
			uint buffaddr = (uint)basevalues.GetInteger("encoded data pointer");

			long expand = _expander.Expand(buffaddr);

			if (buffaddr >= 0 && buffsize > 0)
				_allocator.Free(expand, buffsize);
		}
		private void FreeExtraInfoUnknown1s(StructureValueCollection basevalues, IStream stream, string countName, string addressName, string layoutName)
		{
			StructureValueCollection[] entries = ReadBlock(basevalues, stream, countName, addressName, layoutName);
			foreach (StructureValueCollection entry in entries)
			{
				FreeExtraInfoUnknown2s(entry, stream, "number of unknown2s", "unknown2 table address", "sound extra info unknown2");
			}

			FreeTagBlock(basevalues, countName, addressName, layoutName);
		}
		private void FreeExtraInfoUnknown2s(StructureValueCollection basevalues, IStream stream, string countName, string addressName, string layoutName)
		{
			StructureValueCollection[] entries = ReadBlock(basevalues, stream, countName, addressName, layoutName);
			foreach (StructureValueCollection entry in entries)
			{
				FreeTagBlock(entry, "number of unknown3s", "unknown3 table address", "sound extra info unknown3");
				FreeTagBlock(entry, "number of unknown4s", "unknown4 table address", "sound extra info unknown4");
			}

			FreeTagBlock(basevalues, countName, addressName, layoutName);
		}
		#endregion

		#region Extra Info (Reach Beta)
		private SoundExtraInfo LoadExtraInfoReachBeta(StructureValueCollection values, IReader reader)
		{
			SoundExtraInfo result = new SoundExtraInfo();

			var datums = ReadBlock(values, reader, "number of extra info datums", "extra info datum table address", "sound extra info datum");
			result.Datums = datums.Select(e => LoadExtraInfoDatum(e)).ToArray();

			return result;
		}

		private DatumIndex LoadExtraInfoDatum(StructureValueCollection values)
		{
			return new DatumIndex(values.GetInteger("resource datum index"));
		}

		private void SaveExtraInfosReachBeta(ICollection<SoundExtraInfo> extraInfos, IStream stream)
		{
			StructureValueCollection values = LoadTag(stream);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound extra info reachbeta");

			FreeExtraInfosReachBeta(values, stream, "number of extra infos", "extra info table address", "sound extra info reachbeta");

			var entries = new List<StructureValueCollection>();

			StructureLayout datumLayout = _buildInfo.Layouts.GetLayout("sound extra info datum");

			foreach (var extraInfo in extraInfos)
			{
				StructureValueCollection entry = new StructureValueCollection();

				List<StructureValueCollection> datumEntries = extraInfo.Datums.Select(f => SerializeExtraInfoDatum(f)).ToList();
				uint datumNewAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(datumEntries.ToList(), datumLayout, _metaArea, _allocator, stream));
				entry.SetInteger("number of extra info datums", (uint)datumEntries.Count);
				entry.SetInteger("extra info datum table address", datumNewAddress);

				entries.Add(entry);
			}

			uint newAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(entries, layout, _metaArea, _allocator, stream));

			values.SetInteger("number of extra infos", (uint)entries.Count);
			values.SetInteger("extra info table address", newAddress);

			SaveTag(values, stream);
		}

		private StructureValueCollection SerializeExtraInfoDatum(DatumIndex datum)
		{
			var result = new StructureValueCollection();
			result.SetInteger("resource datum index", datum.Value);
			result.SetInteger("padding", 0);
			return result;
		}

		private void FreeExtraInfosReachBeta(StructureValueCollection basevalues, IStream stream, string countName, string addressName, string layoutName)
		{
			StructureValueCollection[] entries = ReadBlock(basevalues, stream, countName, addressName, layoutName);
			foreach (StructureValueCollection entry in entries)
			{
				FreeTagBlock(entry, "number of extra info datums", "extra info datum table address", "sound extra info datum");
			}

			FreeTagBlock(basevalues, countName, addressName, layoutName);
		}
		#endregion

		#region Extra Info (Reach)
		private SoundExtraInfo LoadExtraInfoReach(StructureValueCollection values)
		{
			SoundExtraInfo result = new SoundExtraInfo();

			result.Datums = new DatumIndex[1];

			result.Datums[0] = new DatumIndex(values.GetInteger("resource datum index"));

			return result;
		}

		private void SaveExtraInfosReach(ICollection<SoundExtraInfo> extraInfos, IStream stream)
		{
			StructureValueCollection values = LoadTag(stream);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound extra info reach");

			var entries = new List<StructureValueCollection>();

			foreach (var extraInfo in extraInfos)
			{
				StructureValueCollection entry = new StructureValueCollection();
				entry.SetInteger("resource datum index", extraInfo.Datums[0].Value);
				entry.SetInteger("padding", 0);
				entries.Add(entry);
			}

			int oldCount = (int)values.GetInteger("number of extra infos");
			long oldAddress = _expander.Expand((uint)values.GetInteger("extra info table address"));

			uint newAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(entries, oldCount, oldAddress, layout, _metaArea, _allocator, stream));

			values.SetInteger("number of extra infos", (uint)entries.Count);
			values.SetInteger("extra info table address", newAddress);

			SaveTag(values, stream);
		}
		#endregion

		#region Names
		private StringID[] LoadNames(StructureValueCollection[] entries)
		{
			return entries.Select(e => new StringID(e.GetInteger("name string id"))).ToArray();
		}

		private StructureValueCollection SerializeName(StringID name)
		{
			var result = new StructureValueCollection();

			result.SetInteger("name string id", name.Value);

			return result;
		}

		private void SaveNames(StructureValueCollection values, ICollection<StructureValueCollection> entries, IStream stream)
		{
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound name");

			int oldCount = (int)values.GetInteger("number of sound names");
			long oldAddress = _expander.Expand((uint)values.GetInteger("sound name table address"));

			uint newAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(entries, oldCount, oldAddress, layout, _metaArea, _allocator, stream));

			values.SetInteger("number of sound names", (uint)entries.Count);
			values.SetInteger("sound name table address", newAddress);
		}
		#endregion

		#region Chunks
		private SoundChunk[] LoadChunks(StructureValueCollection[] entries)
		{
			List<SoundChunk> result = new List<SoundChunk>();
			foreach (StructureValueCollection entry in entries)
			{
				SoundChunk pc = new SoundChunk();

				pc.FileOffset = (int)entry.GetInteger("offset");
				pc.EncodedSizeAndFlags = (int)entry.GetInteger("encoded size");
				pc.CacheIndex = (int)entry.GetInteger("cache index");
				pc.XMA2BufferStart = (int)entry.GetInteger("xma buffer start");
				pc.XMA2BufferEnd = (int)entry.GetInteger("xma buffer end");
				pc.Unknown = (int)entry.GetIntegerOrDefault("unknown1", 0);
				pc.Unknown1 = (int)entry.GetIntegerOrDefault("unknown2", 0);

				pc.FModBankSuffix =
					entry.HasInteger("fmod bank suffix string id") ?
					new StringID(entry.GetInteger("fmod bank suffix string id")) : StringID.Null;

				result.Add(pc);
			}
			return result.ToArray();
		}

		private StructureValueCollection SerializeChunk(SoundChunk chunk)
		{
			var result = new StructureValueCollection();

			result.SetInteger("offset", (uint)chunk.FileOffset);
			result.SetInteger("encoded size", (uint)chunk.EncodedSizeAndFlags);
			result.SetInteger("cache index", (uint)chunk.CacheIndex);
			result.SetInteger("xma buffer start", (uint)chunk.XMA2BufferStart);
			result.SetInteger("xma buffer end", (uint)chunk.XMA2BufferEnd);
			result.SetInteger("unknown1", (uint)chunk.Unknown);
			result.SetInteger("unknown2", (uint)chunk.Unknown1);
			result.SetInteger("fmod bank suffix string id", chunk.FModBankSuffix.Value);

			return result;
		}

		private void SaveChunks(StructureValueCollection values, ICollection<StructureValueCollection> entries, IStream stream)
		{
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound chunk");

			int oldCount = (int)values.GetInteger("number of chunks");
			long oldAddress = _expander.Expand((uint)values.GetInteger("chunk table address"));

			_allocator.Free(oldAddress, (uint)(oldCount * layout.Size));

			long newAddress = _allocator.Allocate((uint)(layout.Size * entries.Count), 0x10, stream);
			TagBlockWriter.WriteTagBlock(entries, newAddress, layout, _metaArea, stream);

			uint contr = _expander.Contract(newAddress);

			values.SetInteger("number of chunks", (uint)entries.Count);
			values.SetInteger("chunk table address", contr);
		}
		#endregion

		#region Language Permutations
		private Dictionary<int, SoundPermutationLanguage[]> LoadLanguagePermutations(StructureValueCollection[] entries, SoundChunk[] chunks)
		{
			var result = new Dictionary<int, SoundPermutationLanguage[]>();
			foreach (StructureValueCollection entry in entries)
			{
				List<SoundPermutationLanguage> languages = new List<SoundPermutationLanguage>();

				int parentIndex = (int)entry.GetInteger("parent permutation index");

				var langArray = entry.GetArray("languages");

				for (int i = 0; i < langArray.Length; i++)
				{
					var lang = new SoundPermutationLanguage();

					lang.LanguageIndex = i;

					lang.SampleSize = (int)langArray[i].GetInteger("sample size");

					int firstChunk = (int)langArray[i].GetInteger("first chunk");
					int chunkCount = (int)langArray[i].GetInteger("chunk count");

					lang.Chunks = new SoundChunk[chunkCount];

					for (int c = 0; c < chunkCount; c++)
						lang.Chunks[c] = chunks[firstChunk + c];

					languages.Add(lang);
				}

				result.Add(parentIndex, languages.ToArray());
			}
			return result;
		}

		private void SaveLanguagePermutations(StructureValueCollection values, ICollection<StructureValueCollection> entries, IStream stream)
		{
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound language permutation");

			int oldCount = (int)values.GetInteger("number of language permutations");
			long oldAddress = _expander.Expand((uint)values.GetInteger("language permutation table address"));

			uint newAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(entries, oldCount, oldAddress, layout, _metaArea, _allocator, stream));

			values.SetInteger("number of language permutations", (uint)entries.Count);
			values.SetInteger("language permutation table address", newAddress);
		}
		#endregion

		#region Layer Markers
		private int[] LoadLayerMarkers(StructureValueCollection[] entries)
		{
			return entries.Select(e => (int)e.GetInteger("sample offset")).ToArray();
		}

		private StructureValueCollection SerializeLayerMarker(int offset)
		{
			var result = new StructureValueCollection();

			result.SetInteger("sample offset", (uint)offset);

			return result;
		}

		private void SaveLayerMarkers(StructureValueCollection values, ICollection<StructureValueCollection> entries, IStream stream)
		{
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound layer marker");

			int oldCount = (int)values.GetInteger("number of layer markers");
			long oldAddress = _expander.Expand((uint)values.GetInteger("layer marker table address"));

			uint newAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(entries, oldCount, oldAddress, layout, _metaArea, _allocator, stream));

			values.SetInteger("number of layer markers", (uint)entries.Count);
			values.SetInteger("layer marker table address", newAddress);
		}
		#endregion

		#region Permutations
		private SoundPermutation[] LoadPermutations(StructureValueCollection[] entries, StringID[] names, SoundChunk[] chunks, Dictionary<int, SoundPermutationLanguage[]> languagePermutations, int[] layerMarkers)
		{
			var result = new List<SoundPermutation>();

			for (int i = 0; i < entries.Length; i++)
			{
				StructureValueCollection entry = entries[i];

				SoundPermutation perm = new SoundPermutation();

				perm.EncodedSkipFraction = (short)entry.GetInteger("encoded skip fraction");
				perm.SampleSize = (int)entry.GetInteger("sample size");
				perm.EncodedGain = (byte)entry.GetInteger("encoded gain");
				perm.EncodedPermutationInfoIndex = (byte)entry.GetInteger("encoded info index");
				perm.FSBInfo = (int)entry.GetIntegerOrDefault("fsb info", 0);

				int nameIndex = (short)entry.GetInteger("name index");
				perm.Name = names[nameIndex];

				int firstMarker = (short)entry.GetIntegerOrDefault("first layer marker index", 0xFFFFFFFF);
				int markerCount = (short)entry.GetIntegerOrDefault("layer marker count", 0);

				perm.LayerMarkers = new int[markerCount];
				for (int m = 0; m < markerCount; m++)
					perm.LayerMarkers[m] = layerMarkers[firstMarker + m];

				int firstChunk = (int)entry.GetInteger("first permutation chunk index");
				int chunkCount = (short)entry.GetInteger("permutation chunk count");

				perm.Chunks = new SoundChunk[chunkCount];

				for (int c = 0; c < chunkCount; c++)
					perm.Chunks[c] = chunks[firstChunk + c];

				SoundPermutationLanguage[] langs = null;
				if (languagePermutations.TryGetValue(i, out langs))
					perm.Languages = langs;
			
				result.Add(perm);
			}

			return result.ToArray();
		}

		private StructureValueCollection SerializePermutation(SoundPermutation perm, int nameIndex, int firstChunk, int firstMarker)
		{
			var result = new StructureValueCollection();

			result.SetInteger("name index", (uint)nameIndex);
			result.SetInteger("encoded skip fraction", (uint)perm.EncodedSkipFraction);
			result.SetInteger("sample size", (uint)perm.SampleSize);
			result.SetInteger("first permutation chunk index", (uint)firstChunk);
			result.SetInteger("permutation chunk count", (uint)(perm.Chunks != null ? perm.Chunks.Length : 0));
			result.SetInteger("encoded gain", (uint)perm.EncodedGain);
			result.SetInteger("encoded info index", (uint)perm.EncodedPermutationInfoIndex);
			result.SetInteger("first layer marker index", (uint)firstMarker);
			result.SetInteger("layer marker count", (uint)(perm.LayerMarkers != null ? perm.LayerMarkers.Length : 0));
			result.SetInteger("fsb info", (uint)perm.FSBInfo);

			return result;
		}

		private void SavePermutations(StructureValueCollection values, ICollection<StructureValueCollection> entries, IStream stream)
		{
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound permutation");

			int oldCount = (int)values.GetInteger("number of permutations");
			long oldAddress = _expander.Expand((uint)values.GetInteger("permutation table address"));

			uint newAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(entries, oldCount, oldAddress, layout, _metaArea, _allocator, stream));

			values.SetInteger("number of permutations", (uint)entries.Count);
			values.SetInteger("permutation table address", newAddress);
		}
		#endregion

		#region Pitch Range Distances
		private SoundPitchRangeDistance[] LoadPitchRangeDistances(StructureValueCollection[] entries)
		{
			return entries.Select(e => LoadPitchRangeDistance(e)).ToArray();
		}

		private SoundPitchRangeDistance LoadPitchRangeDistance(StructureValueCollection values)
		{
			return new SoundPitchRangeDistance()
			{
				DontObstructDistance = values.GetFloatOrDefault("dont obstruct distance", 0),
				DontPlayDistance = values.GetFloat("dont play distance"),
				AttackDistance = values.GetFloat("attack distance"),
				MinDistance = values.GetFloat("minimum distance"),
				SustainBeginDistance = values.GetFloatOrDefault("sustain begin distance", 0),
				SustainEndDistance = values.GetFloatOrDefault("sustain end distance", 0),
				MaxDistance = values.GetFloat("maximum distance"),
				SustainDB = values.GetFloatOrDefault("sustain db", 0),
			};
		}

		private StructureValueCollection SerializePitchRangeDistance(SoundPitchRangeDistance dist)
		{
			var result = new StructureValueCollection();

			result.SetFloat("dont obstruct distance", dist.DontObstructDistance);
			result.SetFloat("dont play distance", dist.DontPlayDistance);
			result.SetFloat("attack distance", dist.AttackDistance);
			result.SetFloat("minimum distance", dist.MinDistance);
			result.SetFloat("sustain begin distance", dist.SustainBeginDistance);
			result.SetFloat("sustain end distance", dist.SustainEndDistance);
			result.SetFloat("maximum distance", dist.MaxDistance);
			result.SetFloat("sustain db", dist.SustainDB);

			return result;
		}

		private void SavePitchRangeDistances(StructureValueCollection values, ICollection<StructureValueCollection> entries, IStream stream)
		{
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound pitch range distance");

			int oldCount = (int)values.GetInteger("number of pitch range distances");
			long oldAddress = _expander.Expand((uint)values.GetInteger("pitch range distance table address"));

			uint newAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(entries, oldCount, oldAddress, layout, _metaArea, _allocator, stream));

			values.SetInteger("number of pitch range distances", (uint)entries.Count);
			values.SetInteger("pitch range distance table address", newAddress);
		}
		#endregion

		#region Pitch Range Parameters
		private SoundPitchRangeParameter[] LoadPitchRangeParameters(StructureValueCollection[] entries, SoundPitchRangeDistance[] distances)
		{
			var result = new List<SoundPitchRangeParameter>();

			foreach (StructureValueCollection entry in entries)
			{
				SoundPitchRangeParameter param = new SoundPitchRangeParameter();

				param.NaturalPitch = (int)entry.GetInteger("natural pitch");
				param.BendMin = (int)entry.GetInteger("bend min");
				param.BendMax = (int)entry.GetInteger("bend max");
				param.MaxGainPitchMin = (int)entry.GetInteger("max gain pitch min");
				param.MaxGainPitchMax = (int)entry.GetInteger("max gain pitch max");
				param.PlaybackPitchMin = (int)entry.GetInteger("playback pitch min");
				param.PlaybackPitchMax = (int)entry.GetInteger("playback pitch max");

				int distanceIndex = (int)entry.GetIntegerOrDefault("distance index", 0xFFFFFFFF);
				if (distanceIndex != -1)
					param.Distance = distances[distanceIndex];

				result.Add(param);
			}

			return result.ToArray();
		}

		private StructureValueCollection SerializePitchRangeParameter(SoundPitchRangeParameter param, int distanceIndex)
		{
			var result = new StructureValueCollection();

			result.SetInteger("natural pitch", (uint)param.NaturalPitch);
			result.SetInteger("distance index", (uint)distanceIndex);
			result.SetInteger("bend min", (uint)param.BendMin);
			result.SetInteger("bend max", (uint)param.BendMax);
			result.SetInteger("max gain pitch min", (uint)param.MaxGainPitchMin);
			result.SetInteger("max gain pitch max", (uint)param.MaxGainPitchMax);
			result.SetInteger("playback pitch min", (uint)param.PlaybackPitchMin);
			result.SetInteger("playback pitch max", (uint)param.PlaybackPitchMax);

			return result;
		}

		private void SavePitchRangeParameters(StructureValueCollection values, ICollection<StructureValueCollection> entries, IStream stream)
		{
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound pitch range parameter");

			int oldCount = (int)values.GetInteger("number of pitch range parameters");
			long oldAddress = _expander.Expand((uint)values.GetInteger("pitch range parameters table address"));

			uint newAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(entries, oldCount, oldAddress, layout, _metaArea, _allocator, stream));

			values.SetInteger("number of pitch range parameters", (uint)entries.Count);
			values.SetInteger("pitch range parameters table address", newAddress);
		}
		#endregion

		#region Pitch Ranges
		public IEnumerable<SoundPitchRange> LoadPitchRanges(IReader reader)
		{
			StructureValueCollection values = LoadTag(reader);

			StringID[] names = LoadNames(ReadBlock(values, reader, "number of sound names", "sound name table address", "sound name"));
			SoundChunk[] chunks = LoadChunks(ReadBlock(values, reader, "number of chunks", "chunk table address", "sound chunk"));

			Dictionary<int, SoundPermutationLanguage[]> languagePermutations = new Dictionary<int, SoundPermutationLanguage[]>();
			if (_buildInfo.Layouts.HasLayout("sound language permutation"))
				languagePermutations = LoadLanguagePermutations(ReadBlock(values, reader, "number of language permutations", "language permutation table address", "sound language permutation"), chunks);

			int[] layerMarkers = new int[0];
			if (_buildInfo.Layouts.HasLayout("sound layer marker"))
				layerMarkers = LoadLayerMarkers(ReadBlock(values, reader, "number of layer markers", "layer marker table address", "sound layer marker"));

			SoundPermutation[] permutations = LoadPermutations(ReadBlock(values, reader, "number of permutations", "permutation table address", "sound permutation"), names, chunks, languagePermutations, layerMarkers);

			SoundPitchRangeDistance[] distances = new SoundPitchRangeDistance[0];
			if (_buildInfo.Layouts.HasLayout("sound pitch range distance"))
				distances = LoadPitchRangeDistances(ReadBlock(values, reader, "number of pitch range distances", "pitch range distance table address", "sound pitch range distance"));

			SoundPitchRangeParameter[] parameters = LoadPitchRangeParameters(ReadBlock(values, reader, "number of pitch range parameters", "pitch range parameters table address", "sound pitch range parameter"), distances);

			List<SoundPitchRange> result = new List<SoundPitchRange>();

			StructureValueCollection[] pitchRangeEntries = ReadBlock(values, reader, "number of pitch ranges", "pitch range table address", "sound pitch range");
			foreach (StructureValueCollection entry in pitchRangeEntries)
			{
				SoundPitchRange pitchRange = new SoundPitchRange();

				pitchRange.HasEncodedData = ((short)entry.GetInteger("encoded permutation data") != -1);

				int nameIndex = (short)entry.GetInteger("name index");
				pitchRange.Name = names[nameIndex];

				int parameterIndex = (short)entry.GetInteger("parameter index");
				pitchRange.Parameter = parameters[parameterIndex];

				int requiredCount = 0;
				int permCount = 0;
				int firstPerm = 0;
				SoundPitchRange.DecodeCountsAndIndex((int)entry.GetInteger("encoded permutation count and index"), out requiredCount, out permCount, out firstPerm);

				pitchRange.RequiredPermutationCount = requiredCount;

				pitchRange.Permutations = new SoundPermutation[permCount];
				for (int i = 0; i < permCount; i++)
					pitchRange.Permutations[i] = permutations[firstPerm + i];

				result.Add(pitchRange);
			}

			return result;
		}

		public void SavePitchRanges(ICollection<SoundPitchRange> pitchRanges, IStream stream)
		{
			StructureValueCollection values = LoadTag(stream);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound pitch range");

			var entries = new List<StructureValueCollection>();

			var nameEntries = new List<StructureValueCollection>();
			var chunkEntries = new List<StructureValueCollection>();
			var languagePermutationEntries = new List<StructureValueCollection>();
			var layerMarkerEntries = new List<StructureValueCollection>();
			var permutationEntries = new List<StructureValueCollection>();
			var distanceEntries = new List<StructureValueCollection>();
			var parameterEntries = new List<StructureValueCollection>();

			Dictionary<StringID, int> handledNames = new Dictionary<StringID, int>();
			Dictionary<SoundPitchRangeDistance, int> handledDistances = new Dictionary<SoundPitchRangeDistance, int>();
			Dictionary<SoundPitchRangeParameter, int> handledParameters = new Dictionary<SoundPitchRangeParameter, int>();

			int nextflagindex = 0;
			int nextvirtualbit = 0;

			foreach (var pitchRange in pitchRanges)
			{
				StructureValueCollection entry = new StructureValueCollection();

				int nameIndex = -1;
				if (!handledNames.TryGetValue(pitchRange.Name, out nameIndex))
				{
					nameIndex = nameEntries.Count;
					handledNames[pitchRange.Name] = nameIndex;
					nameEntries.Add(SerializeName(pitchRange.Name));
				}
				entry.SetInteger("name index", (uint)nameIndex);

				int parameterIndex = -1;
				if (pitchRange.Parameter != null)
				{
					if (!handledParameters.TryGetValue(pitchRange.Parameter, out parameterIndex))
					{
						int distanceIndex = -1;
						if (pitchRange.Parameter.Distance != null)
						{
							if (!handledDistances.TryGetValue(pitchRange.Parameter.Distance, out distanceIndex))
							{
								distanceIndex = distanceEntries.Count;
								handledDistances[pitchRange.Parameter.Distance] = distanceIndex;
								distanceEntries.Add(SerializePitchRangeDistance(pitchRange.Parameter.Distance));
							}
						}

						parameterIndex = parameterEntries.Count;
						handledParameters[pitchRange.Parameter] = parameterIndex;
						parameterEntries.Add(SerializePitchRangeParameter(pitchRange.Parameter, distanceIndex));
					}
				}
				entry.SetInteger("parameter index", (uint)parameterIndex);

				int flagvalue = 0;
				//how the flags value is calculated depends on if the data value is not -1;
				if (pitchRange.HasEncodedData)
				{
					//flag is not encoded at all, just an index to a starting bit
					if (pitchRange.Permutations.Length < 2)
						flagvalue = -1;
					else
					{
						flagvalue = nextvirtualbit;
						nextvirtualbit += (int)Math.Log(pitchRange.Permutations.Length - 1, 2) + pitchRange.Permutations.Length + 1;
					}

					entry.SetInteger("encoded permutation data", 0);
				}
				else
				{
					//flag is encoded with an index into the flags tag block
					flagvalue = nextflagindex << 3;
					nextflagindex += ((pitchRange.Permutations.Length + 7) & 0xFFF8) / 8;

					entry.SetInteger("encoded permutation data", (uint)0xFFFFFFFF);
				}
				entry.SetInteger("encoded runtime permutation flag index", (uint)flagvalue);

				int permutationIndex = -1;
				long permsHash = pitchRange.GetPermutationsHash();

				permutationIndex = permutationEntries.Count;

				for (int i = 0; i < pitchRange.Permutations.Length; i++)
				{
					var perm = pitchRange.Permutations[i];

					int permNameIndex = -1;
					if (!handledNames.TryGetValue(perm.Name, out permNameIndex))
					{
						nameIndex = nameEntries.Count;
						handledNames[perm.Name] = nameIndex;
						nameEntries.Add(SerializeName(perm.Name));
					}

					int chunkIndex = -1;
					long chunkHash = perm.GetChunksHash();

					chunkIndex = chunkEntries.Count;

					foreach (var chunk in perm.Chunks)
						chunkEntries.Add(SerializeChunk(chunk));

					int markerIndex = layerMarkerEntries.Count; //always the next value
					if (perm.LayerMarkers != null)
					{
						long markerHash = perm.GetMarkersHash();

						foreach (int marker in perm.LayerMarkers)
							layerMarkerEntries.Add(SerializeLayerMarker(marker));
					}

					if (perm.Languages != null && perm.Languages.Length > 0)
					{
						var langResult = new StructureValueCollection();

						langResult.SetInteger("parent permutation index", (uint)(permutationIndex + i));

						var langEntries = new StructureValueCollection[perm.Languages.Length];

						for (int l = 0; l < langEntries.Length; l++)
						{
							var langValues = new StructureValueCollection();

							var langItem = perm.Languages[l];

							langValues.SetInteger("sample size", (uint)langItem.SampleSize);

							int langChunkIndex = chunkEntries.Count;
							if (langItem.Chunks != null && langItem.Chunks.Length > 0)
							{
								long langChunkHash = langItem.GetChunksHash();

								if (langChunkHash == chunkHash)
								{
									langChunkIndex = chunkIndex;
								}
								else
								{
									langChunkIndex = chunkEntries.Count;

									foreach (var chunk in langItem.Chunks)
										chunkEntries.Add(SerializeChunk(chunk));
								}
							}

							langValues.SetInteger("first chunk", (uint)langChunkIndex);
							langValues.SetInteger("chunk count", (uint)(langItem.Chunks.Length));
							langEntries[l] = langValues;
						}

						langResult.SetArray("languages", langEntries);

						languagePermutationEntries.Add(langResult);
					}

					permutationEntries.Add(SerializePermutation(perm, nameIndex, chunkIndex, markerIndex));
				}

				entry.SetInteger("encoded permutation count and index", (uint)SoundPitchRange.EncodeCountsAndIndex(pitchRange.RequiredPermutationCount, pitchRange.Permutations.Length, permutationIndex));

				entries.Add(entry);
			}

			int oldCount = (int)values.GetInteger("number of pitch ranges");
			long oldAddress = _expander.Expand((uint)values.GetInteger("pitch range table address"));
			_allocator.Free(oldAddress, (uint)(oldCount * layout.Size));

			long newAddress = _allocator.Allocate((uint)(layout.Size * entries.Count), 0x10, stream);
			TagBlockWriter.WriteTagBlock(entries, newAddress, layout, _metaArea, stream);

			uint contr = _expander.Contract(newAddress);

			values.SetInteger("number of pitch ranges", (uint)entries.Count);
			values.SetInteger("pitch range table address", contr);

			SaveNames(values, nameEntries, stream);
			SaveChunks(values, chunkEntries, stream);

			if (_buildInfo.Layouts.HasLayout("sound language permutation"))
				SaveLanguagePermutations(values, languagePermutationEntries, stream);

			if (_buildInfo.Layouts.HasLayout("sound layer marker"))
				SaveLayerMarkers(values, layerMarkerEntries, stream);

			SavePermutations(values, permutationEntries, stream);

			if (_buildInfo.Layouts.HasLayout("sound pitch range distance"))
				SavePitchRangeDistances(values, distanceEntries, stream);

			SavePitchRangeParameters(values, parameterEntries, stream);
			SavePermutationFlags(values, nextflagindex, stream);

			SaveTag(values, stream);
		}

		#endregion

		#region Permutation Flags
		private void SavePermutationFlags(StructureValueCollection values, int count, IStream stream)
		{
			//flags are set at runtime so we just need an empty block
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound runtime permutation flag");

			int oldCount = (int)values.GetInteger("number of runtime permutation flags");
			long oldAddress = _expander.Expand((uint)values.GetInteger("runtime permutation flag table address"));

			_allocator.Free(oldAddress, (uint)(oldCount * layout.Size));

			long newAddress = _allocator.Allocate((uint)(layout.Size * count), 0x10, stream);
			stream.SeekTo(_metaArea.PointerToOffset(newAddress));
			stream.WriteBlock(new byte[layout.Size * count]);

			uint contr = _expander.Contract(newAddress);

			values.SetInteger("number of runtime permutation flags", (uint)count);
			values.SetInteger("runtime permutation flag table address", contr);
		}
		#endregion

		#region Language Durations
		public IEnumerable<SoundLanguageDuration> LoadLanguageDurations(IReader reader)
		{
			StructureValueCollection values = LoadTag(reader);

			StructureValueCollection[] entries = ReadBlock(values, reader, "number of language durations", "language duration table address", "sound language duration");
			return entries.Select(e => LoadLanguageDuration(e, reader));
		}

		private SoundLanguageDuration LoadLanguageDuration(StructureValueCollection values, IReader reader)
		{
			SoundLanguageDuration result = new SoundLanguageDuration();

			result.LanguageIndex = (int)values.GetInteger("language index");

			var durationEntries = ReadBlock(values, reader, "number of permutation durations", "permutation duration table address", "sound language permutation duration");
			var allDurations = (from dur in durationEntries select (int)dur.GetInteger("duration")).ToList();

			var pitchRangeEntries = ReadBlock(values, reader, "number of pitch ranges", "pitch range table address", "sound language pitch range");
			var pitchRanges = new List<SoundLanguagePitchRange>();
			foreach (StructureValueCollection entry in pitchRangeEntries)
			{
				SoundLanguagePitchRange pr = new SoundLanguagePitchRange();

				int firstIndex = (int)entry.GetInteger("first permutation duration index");
				int count = (int)entry.GetInteger("permutation count");

				pr.Durations = new int[count];

				for (int i = 0; i < count; i++)
					pr.Durations[i] = allDurations[firstIndex + i];

				pitchRanges.Add(pr);
			}
			result.PitchRanges = pitchRanges;

			return result;
		}

		public void SaveLanguageDurations(ICollection<SoundLanguageDuration> languageDurations, IStream stream)
		{
			StructureValueCollection values = LoadTag(stream);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound language duration");

			FreeLanguageDurations(values, stream, "number of language durations", "language duration table address", "sound language duration");

			var entries = new List<StructureValueCollection>();

			StructureLayout durationLayout = _buildInfo.Layouts.GetLayout("sound language permutation duration");
			StructureLayout pitchrangeLayout = _buildInfo.Layouts.GetLayout("sound language pitch range");

			foreach (var languageDuration in languageDurations)
			{
				StructureValueCollection entry = new StructureValueCollection();

				entry.SetInteger("language index", (uint)languageDuration.LanguageIndex);

				var durationEntries = new List<StructureValueCollection>();
				var pitchrangeEntries = new List<StructureValueCollection>();

				foreach (var pitchRange in languageDuration.PitchRanges)
				{
					StructureValueCollection pRange = new StructureValueCollection();
					pRange.SetInteger("first permutation duration index", (uint)durationEntries.Count);
					pRange.SetInteger("permutation count", (uint)pitchRange.Durations.Length);

					// should these be hashed and reused if needed?
					durationEntries.AddRange(pitchRange.Durations.Select(f => SerializeLanguageDuration(f)).ToList());

					pitchrangeEntries.Add(pRange);
				}

				uint durationNewAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(durationEntries.ToList(), durationLayout, _metaArea, _allocator, stream));
				entry.SetInteger("number of permutation durations", (uint)durationEntries.Count);
				entry.SetInteger("permutation duration table address", durationNewAddress);

				uint pitchrangeNewAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(pitchrangeEntries.ToList(), pitchrangeLayout, _metaArea, _allocator, stream));
				entry.SetInteger("number of pitch ranges", (uint)pitchrangeEntries.Count);
				entry.SetInteger("pitch range table address", pitchrangeNewAddress);

				entries.Add(entry);
			}

			uint newAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(entries, layout, _metaArea, _allocator, stream));

			values.SetInteger("number of language durations", (uint)entries.Count);
			values.SetInteger("language duration table address", newAddress);

			SaveTag(values, stream);
		}

		private StructureValueCollection SerializeLanguageDuration(int duration)
		{
			var result = new StructureValueCollection();
			result.SetInteger("duration", (uint)duration);
			return result;
		}
		
		private void FreeLanguageDurations(StructureValueCollection basevalues, IStream stream, string countName, string addressName, string layoutName)
		{
			StructureValueCollection[] entries = ReadBlock(basevalues, stream, countName, addressName, layoutName);
			foreach (StructureValueCollection entry in entries)
			{
				FreeTagBlock(entry, "number of permutation durations", "permutation duration table address", "sound language permutation duration");
				FreeTagBlock(entry, "number of pitch ranges", "pitch range table address", "sound language pitch range");
			}

			FreeTagBlock(basevalues, countName, addressName, layoutName);
		}
		#endregion

		#region Promotions
		public IEnumerable<SoundPromotion> LoadPromotions(IReader reader)
		{
			StructureValueCollection values = LoadTag(reader);

			StructureValueCollection[] entries = ReadBlock(values, reader, "number of promotions", "promotion table address", "sound promotion");

			return entries.Select(e => LoadPromotion(e, reader));
		}

		private SoundPromotion LoadPromotion(StructureValueCollection values, IReader reader)
		{
			SoundPromotion result = new SoundPromotion();

			var rules = ReadBlock(values, reader, "number of rules", "rule table address", "sound promotion rule");
			result.Rules = new SoundPromotionRule[rules.Length];
			for (int i = 0; i < rules.Length; i++)
			{
				StructureValueCollection rule = rules[i];
				SoundPromotionRule entry = new SoundPromotionRule();

				entry.LocalPitchRangeIndex = (int)rule.GetInteger("pitch range index");
				entry.MaximumPlayCount = (int)rule.GetInteger("maximum playing count");
				entry.SupressionTime = rule.GetFloat("suppression time");
				entry.RolloverTime = (int)rule.GetInteger("rollover time");
				entry.ImpulseTime = (int)rule.GetInteger("impulse time");

				result.Rules[i] = entry;
			}

			result.ActivePromotionIndex = (int)values.GetInteger("active index");
			result.LastPromotionTime = (int)values.GetInteger("last promotion time");
			result.SuppressionTimeout = (int)values.GetInteger("suppression timeout");

			return result;
		}

		public void SavePromotions(ICollection<SoundPromotion> promotions, IStream stream)
		{
			StructureValueCollection values = LoadTag(stream);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("sound promotion");

			FreePromotions(values, stream, "number of promotions", "promotion table address", "sound promotion");

			var entries = new List<StructureValueCollection>();

			StructureLayout ruleLayout = _buildInfo.Layouts.GetLayout("sound promotion rule");
			StructureLayout timerlayout = _buildInfo.Layouts.GetLayout("sound promotion runtime timer");

			foreach (var promotion in promotions)
			{
				StructureValueCollection entry = new StructureValueCollection();

				List<StructureValueCollection> ruleEntries = new List<StructureValueCollection>();
				foreach (var rule in promotion.Rules)
				{
					var ruleEntry = new StructureValueCollection();
					ruleEntry.SetInteger("pitch range index", (uint)rule.LocalPitchRangeIndex);
					ruleEntry.SetInteger("maximum playing count", (uint)rule.MaximumPlayCount);
					ruleEntry.SetFloat("suppression time", (uint)rule.SupressionTime);
					ruleEntry.SetInteger("rollover time", (uint)rule.RolloverTime);
					ruleEntry.SetInteger("impulse time", (uint)rule.ImpulseTime);

					ruleEntries.Add(ruleEntry);
				}
				uint ruleNewAddress = _expander.Contract(TagBlockWriter.WriteTagBlock(ruleEntries.ToList(), ruleLayout, _metaArea, _allocator, stream));
				entry.SetInteger("number of rules", (uint)ruleEntries.Count);
				entry.SetInteger("rule table address", ruleNewAddress);

				//timers are set at runtime so we just need an empty block the same count as rules
				uint timerlength = (uint)(timerlayout.Size * ruleEntries.Count);
				long timerNewAddress = _allocator.Allocate(timerlength, 0x10, stream);
				stream.SeekTo(_metaArea.PointerToOffset(timerNewAddress));
				stream.WriteBlock(new byte[timerlength]);

				uint timerCont = _expander.Contract(timerNewAddress);

				entry.SetInteger("number of runtime timers", (uint)ruleEntries.Count);
				entry.SetInteger("runtime timer table address", timerCont);

				entry.SetInteger("active index", (uint)promotion.ActivePromotionIndex);
				entry.SetInteger("last promotion time", (uint)promotion.LastPromotionTime);
				entry.SetInteger("suppression timeout", (uint)promotion.SuppressionTimeout);

				entries.Add(entry);
			}

			long newAddress = _allocator.Allocate((uint)(layout.Size * entries.Count), 0x10, stream);
			TagBlockWriter.WriteTagBlock(entries, newAddress, layout, _metaArea, stream);

			uint contr = _expander.Contract(newAddress);

			values.SetInteger("number of promotions", (uint)entries.Count);
			values.SetInteger("promotion table address", contr);

			SaveTag(values, stream);
		}

		private void FreePromotions(StructureValueCollection basevalues, IStream stream, string countName, string addressName, string layoutName)
		{
			StructureValueCollection[] entries = ReadBlock(basevalues, stream, countName, addressName, layoutName);
			foreach (StructureValueCollection entry in entries)
			{
				FreeTagBlock(entry, "number of rules", "rule table address", "sound promotion rule");
				FreeTagBlock(entry, "number of runtime timers", "runtime timer table address", "sound promotion runtime timer");
			}

			FreeTagBlock(basevalues, countName, addressName, layoutName);
		}
		#endregion

		private StructureValueCollection LoadTag(IReader reader)
		{
			reader.SeekTo(_tag.MetaLocation.AsOffset());
			return StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("sound resource gestalt"));
		}

		private void SaveTag(StructureValueCollection values, IWriter writer)
		{
			writer.SeekTo(_tag.MetaLocation.AsOffset());
			StructureWriter.WriteStructure(values, _buildInfo.Layouts.GetLayout("sound resource gestalt"), writer);
		}

		private void FreeTagBlock(StructureValueCollection values, string countName, string addressName, string layoutName)
		{
			var count = (int)values.GetInteger(countName);
			uint address = (uint)values.GetInteger(addressName);

			long expand = _expander.Expand(address);

			StructureLayout layout = _buildInfo.Layouts.GetLayout(layoutName);
			uint size = (uint)(count * layout.Size);
			if (expand >= 0 && size > 0)
				_allocator.Free(expand, size);
		}

		private StructureValueCollection[] ReadBlock(StructureValueCollection values, IReader reader, string countName, string addressName, string layoutName)
		{
			int count = (int)values.GetInteger(countName);
			uint address = (uint)values.GetInteger(addressName);

			long expand = _expander.Expand(address);

			var layout = _buildInfo.Layouts.GetLayout(layoutName);
			return TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
		}

	}
}

