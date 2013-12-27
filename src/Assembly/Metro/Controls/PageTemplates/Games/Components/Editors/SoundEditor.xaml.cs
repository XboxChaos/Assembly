using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Assembly.Helpers;
using Assembly.Metro.Dialogs;
using Blamite.Blam;
using Blamite.Blam.Resources;
using Blamite.Blam.Resources.Sounds;
using Blamite.Blam.ThirdGen;
using Blamite.Flexibility;
using Blamite.IO;
using System.Media;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.Editors
{
	/// <summary>
	/// Interaction logic for SoundEditor.xaml
	/// </summary>
	public partial class SoundEditor
	{
		private SoundPlayer _soundPlayer;
		private readonly EngineDescription _buildInfo;
		private readonly TagEntry _tag;
		private readonly ICacheFile _cache;
		private readonly IStreamManager _streamManager;
		private readonly ResourceTable _resourceTable;
		private ResourcePage[] _resourcePages;
		private readonly Resource _soundResource;
		private readonly ISound _sound;
		private readonly ISoundResourceGestalt _soundResourceGestalt;
		private ViewModel _viewModel;
		private readonly byte[] _monoFooter = 
		{
			0x58, 0x4d, 0x41, 0x32, 0x2c, 0x00, 0x00, 0x00, 0x04, 0x01, 0x00, 0xff, 0x00, 0x00, 0x01, 0x80,
			0x00, 0x00, 0x8a, 0x00, 0x00, 0x00, 0xab, 0xD2, 0x00, 0x00, 0x10, 0xd6, 0x00, 0x00, 0x3d, 0x14,
			0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x8a, 0x00, 0x00, 0x00, 0x88, 0x80, 0x00, 0x00, 0x00, 0x01,
			0x01, 0x00, 0x00, 0x01, 0x73, 0x65, 0x65, 0x6b, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x8a, 0x00
		};
		private readonly byte[] _stereoFooter = 
		{
			0x58, 0x4d, 0x41, 0x32, 0x2c, 0x00, 0x00, 0x00, 0x04, 0x01, 0x00, 0xff, 0x00, 0x00, 0x01, 0x80, 
			0x00, 0x01, 0x0f, 0x80, 0x00, 0x00, 0xac, 0x44, 0x00, 0x00, 0x10, 0xd6, 0x00, 0x00, 0x3d, 0x14, 
			0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x10, 0x00, 0x00, 0x01, 0x0E, 0x00, 0x00, 0x00, 0x00, 0x01, 
			0x02, 0x00, 0x02, 0x01, 0x73, 0x65, 0x65, 0x6b, 0x04, 0x00, 0x00, 0x00, 0x00, 0x01, 0x10, 0x00
		};


		public SoundEditor(EngineDescription buildInfo, TagEntry tag, ICacheFile cache, IStreamManager streamManager)
		{
			InitializeComponent();

			_buildInfo = buildInfo;
			_tag = tag;
			_cache = cache;
			_streamManager = streamManager;
			_viewModel = new ViewModel();
			DataContext = _viewModel;

			if (_cache.ResourceMetaLoader.SupportsSounds)
			{
				using (var reader = _streamManager.OpenRead())
				{
					_soundResourceGestalt = _cache.LoadSoundResourceGestaltData(reader);
					_sound = _cache.ResourceMetaLoader.LoadSoundMeta(_tag.RawTag, reader);
					_resourceTable = _cache.Resources.LoadResourceTable(reader);
					_soundResource = _resourceTable.Resources.First(r => r.Index == _sound.ResourceIndex);
					_resourcePages = new ResourcePage[2];
					if (_soundResource.Location.PrimaryPage != null)
						_resourcePages[0] = _soundResource.Location.PrimaryPage;
					if (_soundResource.Location.SecondaryPage != null)
						_resourcePages[1] = _soundResource.Location.SecondaryPage;
				}

				var playback = _soundResourceGestalt.SoundPlaybacks[_sound.PlaybackIndex];

				for (var i = 0; i < playback.EncodedPermutationCount; i++)
				{
					var permutation = _soundResourceGestalt.SoundPermutations[i + playback.FirstPermutationIndex];

					_viewModel.Permutations.Add(new ViewModel.ViewPermutation
					{
						Name = _cache.StringIDs.GetString(permutation.SoundName),
						Index = i,
						SoundPermutation = permutation
					});
				}

				#region Load Resource Pages

				// Load Resource Page 1
				if (_resourcePages[0] != null)
				{
					var page = _resourcePages[0];
					
					lblResourcePage1Compression.Text = page.CompressionMethod.ToString();
					lblResourcePage1CompressedSize.Text = "0x" + page.CompressedSize.ToString("X8");
					lblResourcePage1UncompressedSize.Text = "0x" + page.UncompressedSize.ToString("X8");
					lblResourcePage1Offset.Text = "0x" + page.Offset.ToString("X8");
					lblResourcePage1FilePath.Text = page.FilePath ?? "maps\\" + _cache.InternalName + ".map";
				}
				
				// Load Resource Page 2
				if (_resourcePages[1] != null)
				{
					var page = _resourcePages[1];
					
					lblResourcePage2Compression.Text = page.CompressionMethod.ToString();
					lblResourcePage2CompressedSize.Text = "0x" + page.CompressedSize.ToString("X8");
					lblResourcePage2UncompressedSize.Text = "0x" + page.UncompressedSize.ToString("X8");
					lblResourcePage2Offset.Text = "0x" + page.Offset.ToString("X8");
					lblResourcePage2FilePath.Text = page.FilePath ?? "maps\\" + _cache.InternalName + ".map";
				}

				#endregion
				
				// Load Sound Info
				if (_sound != null)
				{
					lblSoundInfoSoundClass.Text = _sound.SoundClass.ToString(CultureInfo.InvariantCulture);
					lblSoundInfoAudioChannel.Text = _sound.SampleRate.ToString();
					lblSoundInfoEncoding.Text = _sound.Encoding.ToString();
					lblSoundInfoMaxPlaytime.Text = _sound.MaxPlaytime.ToString(CultureInfo.InvariantCulture);
				}
			}
			else
			{
				IsEnabled = false;
				MetroMessageBox.Show("Unsupported", "Assembly doesn't support sounds on this build of Halo yet.");
			}
		}

		public class ViewModel : INotifyPropertyChanged
		{
			public ViewModel()
			{
				_permutations.CollectionChanged += PermutationsOnCollectionChanged;
			}

			private ObservableCollection<ViewPermutation> _permutations = new ObservableCollection<ViewPermutation>();
			public ObservableCollection<ViewPermutation> Permutations
			{
				get { return _permutations; }
				set { SetField(ref _permutations, value, "Permutations"); }
			}

			#region Events

			private void PermutationsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
			{
				Permutations = (ObservableCollection<ViewPermutation>) sender;
			}

			#endregion
			
			#region Models

			public class ViewPermutation
			{
				public string Name { get; set; }

				public int Index { get; set; }

				public ISoundPermutation SoundPermutation { get; set; }
			}

			#endregion
			
			#region Binding Stuff

			public event PropertyChangedEventHandler PropertyChanged;
			protected virtual void OnPropertyChanged(string propertyName)
			{
				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
			protected bool SetField<T>(ref T field, T value, string propertyName)
			{
				if (EqualityComparer<T>.Default.Equals(field, value)) return false;
				field = value;
				OnPropertyChanged(propertyName);
				return true;
			}

			#endregion

		}

		private void btnPlay_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			var perm = (ViewModel.ViewPermutation)SoundPermutationListBox.SelectedValue;
			if (perm == null) return;

			_soundPlayer = new SoundPlayer(ConvertToAudioFile(ExtractRawPerm(perm.SoundPermutation)));
			_soundPlayer.Play();
		}

		private void btnExtractRawSound_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			ConvertToAudioFile(ExtractRaw());
		}

		private string ConvertToAudioFile(ICollection<byte> data, string path = null)
		{
			var tempFile = Path.GetTempFileName();

			byte[] footer;
			var codec = _soundResourceGestalt.SoundPlatformCodecs[_sound.CodecIndex];
			switch (codec.Channel)
			{
				case Channel.Mono:
					footer = _monoFooter;
					break;

				case Channel.Stereo:
					footer = _stereoFooter;
					break;

				default:
					throw new NotImplementedException();
			}

			switch (_sound.Encoding)
			{
				case Encoding.XMA:
					using (var fileStream = new FileStream(tempFile, FileMode.OpenOrCreate))
					{
						using (var writer = new EndianWriter(fileStream, Endian.BigEndian))
						{
							// Generate an XMA header
							// ADAPTED FROM wwisexmabank - I DO NOT TAKE ANY CREDIT WHATSOEVER FOR THE FOLLOWING CODE.
							// See http://hcs64.com/vgm_ripping.html for more information

							// 'riff' chunk
							writer.WriteInt32(0x52494646); // 'RIFF'
							writer.Endianness = Endian.LittleEndian;
							writer.WriteInt32(data.Count + 0x34);
							writer.Endianness = Endian.BigEndian;
							writer.WriteInt32(0x57415645);
							
							// 'data' chunk
							writer.Endianness = Endian.BigEndian;
							writer.WriteInt32(0x64617461); // 'data'
							writer.Endianness = Endian.LittleEndian;
							writer.WriteInt32(data.Count);
							writer.WriteBlock(data.ToArray());
							
							// footer
							writer.WriteBlock(footer);

							// size
							writer.SeekTo(0x04);
							writer.WriteInt32((Int32)writer.Length - 0x08);
						}
					}

					VariousFunctions.RunProgramSilently(@"A:\Xbox\Games\towav.exe",
						string.Format("\"{0}\"", Path.GetFileName(tempFile)),
						Path.GetDirectoryName(tempFile));

					if (File.Exists(tempFile))
						File.Delete(tempFile);

					tempFile = Path.ChangeExtension(tempFile, "wav");

					if (path != null)
						File.Move(tempFile, path);

					return path ?? tempFile;

				default:
					throw new NotImplementedException();
			}
		}

		private byte[] ExtractRawPerm(ISoundPermutation permutation)
		{
			var data = ExtractRaw();
			var permutationData = new List<byte>();
			
			var memoryStream = new MemoryStream(data);
			var stream = new EndianStream(memoryStream, Endian.BigEndian);

			for (var i = permutation.RawChunkIndex; 
				i < permutation.ChunkCount + permutation.RawChunkIndex; 
				i++)
			{
				var permutationChunk = _soundResourceGestalt.SoundRawChunks[i];

				stream.SeekTo(permutationChunk.Offset);
				permutationData.AddRange(stream.ReadBlock(permutationChunk.Size));
			}

			return permutationData.ToArray();
		}

		private byte[] ExtractRaw()
		{
			var outputBytes = new List<byte>();

			using (var fileStream = _streamManager.OpenRead())
			{
				_resourcePages = new ResourcePage[2];
				_resourcePages[0] = _soundResource.Location.PrimaryPage;
				_resourcePages[1] = _soundResource.Location.SecondaryPage;

				for (var i = 0; i < _resourcePages.Length; i++)
				{
					var page = _resourcePages[i];
					if (page == null || (page.CompressedSize == 0 || page.UncompressedSize == 0))
						continue;
					var resourceFile = _cache;
					Stream resourceStream = null;
					if (page.FilePath != null)
					{
						resourceStream = 
							File.OpenRead(Path.Combine(@"A:\Xbox\Games\Halo 3\Maps\Clean\", 
								Path.GetFileName(page.FilePath)));

						resourceFile = new ThirdGenCacheFile(
							new EndianReader(resourceStream, Endian.BigEndian), _buildInfo, _cache.BuildString);
					}
					
					var tmpStream = new MemoryStream();
					var extractor = new ResourcePageExtractor(resourceFile);
					var path = Path.GetTempFileName();
					var pageStream = File.Open(path, FileMode.Create, FileAccess.ReadWrite);
					extractor.ExtractPage(page, resourceStream ?? fileStream.BaseStream, tmpStream);
					pageStream.Close();

					switch (i)
					{
						case 0:
							tmpStream.Position = _soundResource.Location.PrimaryOffset;
							break;

						case 1:
							tmpStream.Position = _soundResource.Location.SecondaryOffset;
							break;
					}

					var bytes = VariousFunctions.StreamToByteArray(tmpStream);
					if (bytes.Length > 0)
						outputBytes.AddRange(new List<byte>(bytes));
				}
			}

			return outputBytes.ToArray();
		}

		private void btnExtractSelectedPerm_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			ConvertToAudioFile(ExtractRaw(), @"C:\Users\Alex\Desktop\snd\020la_300_pot-asm.wav");
		}
	}
}
