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
	public enum EffectStorageType
	{
		Effect = 0,
		Beam = 1,
		Contrail = 2,
		LightVolume = 3,
	}

	public class EffectStorage
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

		/*public List<byte[]> Effects { get; set; }

		public List<byte[]> Beams { get; set; }

		public List<byte[]> Contrails { get; set; }

		public List<byte[]> LightVolumes { get; set; }*/


		public EffectStorage(ITag scenario, IReader reader, FileSegmentGroup metaArea, MetaAllocator allocator, EngineDescription buildInfo, IPointerExpander expander)
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

		public byte[] GetData(EffectStorageType type, int index)
		{
			switch(type)
			{
				case EffectStorageType.Effect:
					return _effe[index];
				case EffectStorageType.Beam:
					return _beam[index];
				case EffectStorageType.Contrail:
					return _cntl[index];
				case EffectStorageType.LightVolume:
					return _ltvl[index];
				default:
					return null;
			}
		}

		public int AddData(EffectStorageType type, byte[] data)
		{
			switch (type)
			{
				case EffectStorageType.Effect:
					{
						if (data.Length != _effeEntrySize)
							return -1;
						_effe.Add(data);
						_effeChanged = true;
						return _effe.Count - 1;
					}
				case EffectStorageType.Beam:
					{
						if (data.Length != _beamEntrySize)
							return -1;
						_beam.Add(data);
						_beamChanged = true;
						return _beam.Count - 1;
					}
				case EffectStorageType.Contrail:
					{
						if (data.Length != _cntlEntrySize)
							return -1;
						_cntl.Add(data);
						_cntlChanged = true;
						return _cntl.Count - 1;
					}
				case EffectStorageType.LightVolume:
					{
						if (data.Length != _ltvlEntrySize)
							return -1;
						_ltvl.Add(data);
						_ltvlChanged = true;
						return _ltvl.Count - 1;
					}
				default:
					throw new ArgumentOutOfRangeException("Invalid EffectStorgeType value.");
			}
		}

		public void SaveChanges(IStream stream)
		{
			if (_effeChanged)
			{
				SaveData(stream, EffectStorageType.Effect);
				_effeChanged = false;
			}
			if (_beamChanged)
			{
				SaveData(stream, EffectStorageType.Beam);
				_beamChanged = false;
			}
			if (_cntlChanged)
			{
				SaveData(stream, EffectStorageType.Contrail);
				_cntlChanged = false;
			}
			if (_ltvlChanged)
			{
				SaveData(stream, EffectStorageType.LightVolume);
				_ltvlChanged = false;
			}



		}

		private void SaveData(IStream stream, EffectStorageType type)
		{
			long pointer;
			byte[] data;
			switch(type)
			{
				case EffectStorageType.Effect:
					pointer = _effePointer;
					data = _effe.SelectMany(a => a).ToArray();
					break;
				case EffectStorageType.Beam:
					pointer = _beamPointer;
					data = _beam.SelectMany(a => a).ToArray();
					break;
				case EffectStorageType.Contrail:
					pointer = _cntlPointer;
					data = _cntl.SelectMany(a => a).ToArray();
					break;
				case EffectStorageType.LightVolume:
					pointer = _ltvlPointer;
					data = _ltvl.SelectMany(a => a).ToArray();
					break;
				default:
					return;
			}

			var pointerLayout = _buildInfo.Layouts.GetLayout("compiled effect pointer");
			stream.SeekTo(_metaArea.PointerToOffset(pointer));
			var pointerData = StructureReader.ReadStructure(stream, pointerLayout);

			var oldSize = (int)pointerData.GetInteger("size");
			var oldAddress = (uint)pointerData.GetInteger("pointer");

			var oldExpand = _expander.Expand(oldAddress);

			if (oldExpand >= 0 && oldSize > 0)
				_allocator.Free(oldExpand, oldSize);

			long newAddress = 0;
			if (data.Length > 0)
			{
				newAddress = _allocator.Allocate(data.Length, 0x10, stream);
				stream.SeekTo(_metaArea.PointerToOffset(newAddress));
				stream.WriteBlock(data);
			}

			uint cont = _expander.Contract(newAddress);
			pointerData.SetInteger("size", (ulong)data.Length);
			pointerData.SetInteger("pointer", cont);

			stream.SeekTo(_metaArea.PointerToOffset(pointer));
			StructureWriter.WriteStructure(pointerData, pointerLayout, stream);
		}



		/*<layout for="compiled effect entry" size="0x30">
			<uint32 name = "effect pointer" offset="0x0" />
			<uint32 name = "beam pointer" offset="0xC" />
			<uint32 name = "contrail pointer" offset="0x18" />
			<uint32 name = "light volume pointer" offset="0x24" />
		</layout>
	
		<layout for="compiled effect pointer" size="0x14">
			<int32 name = "size" offset="0x0" />
			<uint32 name = "pointer" offset="0xC" />
		</layout>
		
		<layout for="effect data" size="0x7A0" />
		<layout for="beam data" size="0x740" />
		<layout for="contrail data" size="0x770" />
		<layout for="light volume data" size="0x700" />*/

		private void Load(IReader reader)
		{
			//might need to tweak once more MCC games come out

			reader.SeekTo(_scenario.MetaLocation.AsOffset());
			var scenarioLayout = _buildInfo.Layouts.GetLayout("scnr");
			var scenarioData = StructureReader.ReadStructure(reader, scenarioLayout);

			var count = (int)scenarioData.GetInteger("compiled effects count");
			var address = (uint)scenarioData.GetInteger("compiled effect address");

			long expand = _expander.Expand(address);

			reader.SeekTo(_metaArea.PointerToOffset(expand));
			var entryLayout = _buildInfo.Layouts.GetLayout("compiled effect entry");
			var pointerReflexive = StructureReader.ReadStructure(reader, entryLayout);

			var effeAddr = (uint)pointerReflexive.GetInteger("effect pointer");
			var beamAddr = (uint)pointerReflexive.GetInteger("beam pointer");
			var cntlAddr = (uint)pointerReflexive.GetInteger("contrail pointer");
			var ltvlAddr = (uint)pointerReflexive.GetInteger("light volume pointer");

			var pointerLayout = _buildInfo.Layouts.GetLayout("compiled effect pointer");

			LoadData(reader, EffectStorageType.Effect, _expander.Expand(effeAddr), pointerLayout, "effect data");
			LoadData(reader, EffectStorageType.Beam, _expander.Expand(beamAddr), pointerLayout, "beam data");
			LoadData(reader, EffectStorageType.Contrail, _expander.Expand(cntlAddr), pointerLayout, "contrail data");
			LoadData(reader, EffectStorageType.LightVolume, _expander.Expand(ltvlAddr), pointerLayout, "light volume data");
		}

		private void LoadData(IReader reader, EffectStorageType type, long pointer, StructureLayout pointerLayout, string dataLayout)
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
				case EffectStorageType.Effect:
					{
						_effeEntrySize = layout.Size;
						_effePointer = pointer;
						for (int i = 0; i < entryCount; i++)
							_effe.Add(reader.ReadBlock(layout.Size));

						break;
					}
				case EffectStorageType.Beam:
					{
						_beamEntrySize = layout.Size;
						_beamPointer = pointer;

						for (int i = 0; i < entryCount; i++)
							_beam.Add(reader.ReadBlock(layout.Size));

						break;
					}
				case EffectStorageType.Contrail:
					{
						_cntlEntrySize = layout.Size;
						_cntlPointer = pointer;

						for (int i = 0; i < entryCount; i++)
							_cntl.Add(reader.ReadBlock(layout.Size));

						break;
					}
				case EffectStorageType.LightVolume:
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
