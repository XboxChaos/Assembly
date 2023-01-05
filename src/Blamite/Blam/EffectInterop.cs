using Blamite.Blam.Util;
using Blamite.IO;
using Blamite.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam
{
	public enum EffectInteropType
	{
		Effect = 0,
		Beam = 1,
		Contrail = 2,
		LightVolume = 3,
	}

	public class EffectInterop
	{
		private bool _effeChanged = false;
		private bool _beamChanged = false;
		private bool _cntlChanged = false;
		private bool _ltvlChanged = false;

		private ITag _scenario;
		private FileSegmentGroup _metaArea;
		private MetaAllocator _allocator;
		private EngineDescription _buildInfo;
		private IPointerExpander _expander;

		private long _effePointer;
		private long _beamPointer;
		private long _cntlPointer;
		private long _ltvlPointer;

		private int _effeEntrySize;
		private int _beamEntrySize;
		private int _cntlEntrySize;
		private int _ltvlEntrySize;

		private List<byte[]> _effe;
		private List<byte[]> _beam;
		private List<byte[]> _cntl;
		private List<byte[]> _ltvl;

		public EffectInterop(ITag scenario, IReader reader, FileSegmentGroup metaArea, MetaAllocator allocator, EngineDescription buildInfo, IPointerExpander expander)
		{
			_scenario = scenario;
			_metaArea = metaArea;
			_allocator = allocator;
			_buildInfo = buildInfo;
			_expander = expander;

			_effe = new List<byte[]>();
			_beam = new List<byte[]>();
			_cntl = new List<byte[]>();
			_ltvl = new List<byte[]>();

			Load(reader);
		}

		public byte[] GetData(EffectInteropType type, int index)
		{
			switch(type)
			{
				case EffectInteropType.Effect:
					return _effe[index];
				case EffectInteropType.Beam:
					return _beam[index];
				case EffectInteropType.Contrail:
					return _cntl[index];
				case EffectInteropType.LightVolume:
					return _ltvl[index];
				default:
					return null;
			}
		}

		public int AddData(EffectInteropType type, byte[] data)
		{
			switch (type)
			{
				case EffectInteropType.Effect:
					{
						if (data.Length != _effeEntrySize)
							return -1;
						_effe.Add(data);
						_effeChanged = true;
						return _effe.Count - 1;
					}
				case EffectInteropType.Beam:
					{
						if (data.Length != _beamEntrySize)
							return -1;
						_beam.Add(data);
						_beamChanged = true;
						return _beam.Count - 1;
					}
				case EffectInteropType.Contrail:
					{
						if (data.Length != _cntlEntrySize)
							return -1;
						_cntl.Add(data);
						_cntlChanged = true;
						return _cntl.Count - 1;
					}
				case EffectInteropType.LightVolume:
					{
						if (data.Length != _ltvlEntrySize)
							return -1;
						_ltvl.Add(data);
						_ltvlChanged = true;
						return _ltvl.Count - 1;
					}
				default:
					throw new ArgumentOutOfRangeException("Invalid EffectInteropType value.");
			}
		}

		public void SaveChanges(IStream stream)
		{
			if (_effeChanged)
			{
				SaveData(stream, EffectInteropType.Effect);
				_effeChanged = false;
			}
			if (_beamChanged)
			{
				SaveData(stream, EffectInteropType.Beam);
				_beamChanged = false;
			}
			if (_cntlChanged)
			{
				SaveData(stream, EffectInteropType.Contrail);
				_cntlChanged = false;
			}
			if (_ltvlChanged)
			{
				SaveData(stream, EffectInteropType.LightVolume);
				_ltvlChanged = false;
			}
		}

		private void SaveData(IStream stream, EffectInteropType type)
		{
			long pointer;
			byte[] data;
			switch(type)
			{
				case EffectInteropType.Effect:
					pointer = _effePointer;
					data = _effe.SelectMany(a => a).ToArray();
					break;
				case EffectInteropType.Beam:
					pointer = _beamPointer;
					data = _beam.SelectMany(a => a).ToArray();
					break;
				case EffectInteropType.Contrail:
					pointer = _cntlPointer;
					data = _cntl.SelectMany(a => a).ToArray();
					break;
				case EffectInteropType.LightVolume:
					pointer = _ltvlPointer;
					data = _ltvl.SelectMany(a => a).ToArray();
					break;
				default:
					return;
			}

			var pointerLayout = _buildInfo.Layouts.GetLayout("data reference");
			stream.SeekTo(_metaArea.PointerToOffset(pointer));
			var pointerData = StructureReader.ReadStructure(stream, pointerLayout);

			var oldSize = (uint)pointerData.GetInteger("size");
			var oldAddress = (uint)pointerData.GetInteger("pointer");

			var oldExpand = _expander.Expand(oldAddress);

			if (oldExpand >= 0 && oldSize > 0)
				_allocator.Free(oldExpand, oldSize);

			long newAddress = 0;
			if (data.Length > 0)
			{
				newAddress = _allocator.Allocate((uint)data.Length, 0x10, stream);
				stream.SeekTo(_metaArea.PointerToOffset(newAddress));
				stream.WriteBlock(data);
			}

			uint cont = _expander.Contract(newAddress);
			pointerData.SetInteger("size", (ulong)data.Length);
			pointerData.SetInteger("pointer", cont);

			stream.SeekTo(_metaArea.PointerToOffset(pointer));
			StructureWriter.WriteStructure(pointerData, pointerLayout, stream);
		}

		private void Load(IReader reader)
		{
			//might need to tweak once more MCC games come out

			reader.SeekTo(_scenario.MetaLocation.AsOffset());
			var scenarioLayout = _buildInfo.Layouts.GetLayout("scnr");
			var scenarioData = StructureReader.ReadStructure(reader, scenarioLayout);

			var count = (int)scenarioData.GetInteger("structured effect interops count");
			var address = (uint)scenarioData.GetInteger("structured effect interop address");

			long expand = _expander.Expand(address);

			reader.SeekTo(_metaArea.PointerToOffset(expand));
			var entryLayout = _buildInfo.Layouts.GetLayout("structured effect interop element");
			var pointerBlock = StructureReader.ReadStructure(reader, entryLayout);

			var effeAddr = (uint)pointerBlock.GetInteger("effect pointer");
			var beamAddr = (uint)pointerBlock.GetInteger("beam pointer");
			var cntlAddr = (uint)pointerBlock.GetInteger("contrail pointer");
			var ltvlAddr = (uint)pointerBlock.GetInteger("light volume pointer");

			var pointerLayout = _buildInfo.Layouts.GetLayout("data reference");

			LoadData(reader, EffectInteropType.Effect, _expander.Expand(effeAddr), pointerLayout, "effect data");
			LoadData(reader, EffectInteropType.Beam, _expander.Expand(beamAddr), pointerLayout, "beam data");
			LoadData(reader, EffectInteropType.Contrail, _expander.Expand(cntlAddr), pointerLayout, "contrail data");
			LoadData(reader, EffectInteropType.LightVolume, _expander.Expand(ltvlAddr), pointerLayout, "light volume data");
		}

		private void LoadData(IReader reader, EffectInteropType type, long pointer, StructureLayout pointerLayout, string dataLayout)
		{
			reader.SeekTo(_metaArea.PointerToOffset(pointer));
			var layout = _buildInfo.Layouts.GetLayout(dataLayout);

			var pointerData = StructureReader.ReadStructure(reader, pointerLayout);

			var size = (int)pointerData.GetInteger("size");
			var address = (uint)pointerData.GetInteger("pointer");

			long expand = _expander.Expand(address);

			reader.SeekTo(_metaArea.PointerToOffset(expand));

			int entryCount = size / layout.Size;

			switch (type)
			{
				case EffectInteropType.Effect:
					{
						_effeEntrySize = layout.Size;
						_effePointer = pointer;
						for (int i = 0; i < entryCount; i++)
							_effe.Add(reader.ReadBlock(layout.Size));

						break;
					}
				case EffectInteropType.Beam:
					{
						_beamEntrySize = layout.Size;
						_beamPointer = pointer;

						for (int i = 0; i < entryCount; i++)
							_beam.Add(reader.ReadBlock(layout.Size));

						break;
					}
				case EffectInteropType.Contrail:
					{
						_cntlEntrySize = layout.Size;
						_cntlPointer = pointer;

						for (int i = 0; i < entryCount; i++)
							_cntl.Add(reader.ReadBlock(layout.Size));

						break;
					}
				case EffectInteropType.LightVolume:
					{
						_ltvlEntrySize = layout.Size;
						_ltvlPointer = pointer;

						for (int i = 0; i < entryCount; i++)
							_ltvl.Add(reader.ReadBlock(layout.Size));

						break;
					}
			}

		}

	}
}
