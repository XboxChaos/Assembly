using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Assembly.Helpers;
using Assembly.Metro.Dialogs;
using Blamite.Blam;
using Blamite.Blam.Resources;
using Blamite.Blam.Resources.Sounds;
using Blamite.Blam.ThirdGen;
using Blamite.Serialization;
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
		private ResourcePage[] _resourcePages;
		private readonly EngineDescription _buildInfo;
		private readonly TagEntry _tag;
		private readonly ICacheFile _cache;
		private readonly string _cacheLocation;
		private readonly IStreamManager _streamManager;
		private readonly Resource _soundResource;
		private readonly CacheSound _sound;
		private readonly SoundResourceTable _soundResourceTable;

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


		public SoundEditor(EngineDescription buildInfo, string cacheLocation, TagEntry tag, ICacheFile cache, IStreamManager streamManager)
		{
			InitializeComponent();

			/*
			This was been fixed up to support the changes that came from sound injection, but has not really been tested
			but it still isn't great/complete and was mainly only done so I didn't leave in a control where most of its code has been commented out and broken
			not to mention this is all moot with MCC using external proprietary files for sounds
			if i had to complete it, id probably add extra methods to SoundResourceTable and SoundGestalt to grab pitch ranges on demand instead of loading in everything every time
				-Zedd
			*/

			_buildInfo = buildInfo;
			_cacheLocation = cacheLocation;
			_tag = tag;
			_cache = cache;
			_streamManager = streamManager;

			var viewModel = new ViewModel();
			DataContext = viewModel;
			viewModel.TagName = _tag.TagFileName;
			viewModel.Sound = _sound;

			if (!_cache.ResourceMetaLoader.SupportsSounds)
			{
				IsEnabled = false;
				MetroMessageBox.Show("Unsupported", "Assembly doesn't support sounds on this build of Halo yet.");
				return;
			}

			using (var reader = _streamManager.OpenRead())
			{
				// load gestalt
				if (_cache.SoundGestalt != null)
					_soundResourceTable = _cache.SoundGestalt.LoadSoundResourceTable(reader);

				_sound = _cache.ResourceMetaLoader.LoadSoundMeta(_tag.RawTag, reader);
				var resourceTable = _cache.Resources.LoadResourceTable(reader);
				_soundResource = resourceTable.Resources.First(r => r.Index == _sound.ResourceIndex);
				_resourcePages = new []
				{
					_soundResource.Location.PrimaryPage,
					_soundResource.Location.SecondaryPage,
					_soundResource.Location.TertiaryPage,
				};

				viewModel.ResourcePages = 
					new ObservableCollection<ResourcePage>(_resourcePages.ToList());
			}

			for (int i = 0; i < _sound.PitchRangeCount; i++)
			{
				var pitchrange = _soundResourceTable.PitchRanges[_sound.FirstPitchRangeIndex + i];

				foreach (var permutation in pitchrange.Permutations)
				{
					viewModel.Permutations.Add(new ViewModel.ViewPermutation
					{
						Name = _cache.StringIDs.GetString(permutation.Name),
						Index = i,
						SoundPermutation = permutation
					});
				}
				
			}
			
		}

		private void btnPlay_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			var perm = (ViewModel.ViewPermutation)SoundPermutationListBox.SelectedValue;
			if (perm == null) return;

			string tempLocation = ConvertToAudioFile(ExtractRawPerm(perm.SoundPermutation));

			if (tempLocation == null)
				return;

			_soundPlayer = new SoundPlayer(tempLocation);
			_soundPlayer.Play();
		}

		private void StopAudioButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (_soundPlayer != null)
				_soundPlayer.Stop();
		}

		private void btnExtractSelectedPerm_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			var perm = SoundPermutationListBox.SelectedItem as ViewModel.ViewPermutation;
			if (perm == null)
				return;

			var sfd = new SaveFileDialog
			{
				FileName = perm.Name + ".wav"
			};

			if (sfd.ShowDialog() == DialogResult.OK)
				ConvertToAudioFile(ExtractRawPerm(perm.SoundPermutation), sfd.FileName);
		}

		private void btnExtractRawSound_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			var sfd = new SaveFileDialog
			{
				FileName = _tag.TagFileName + ".wav"
			};

			if (sfd.ShowDialog() == DialogResult.OK)
				ConvertToAudioFile(ExtractRaw(), sfd.FileName);
		}

		#region Sound Helpers

		private string ConvertToAudioFile(ICollection<byte> data, string path = null)
		{
			string towav = Path.Combine(VariousFunctions.GetApplicationLocation(), "helpers", "towav.exe");
			if (!File.Exists(towav))
			{
				MetroMessageBox.Show("Cannot Convert Sound", "Sounds cannot be converted because towav.exe is not present. Copy it to the \\helpers folder inside your Assembly installation.");
				return null;
			}

			var tempFile = Path.GetTempFileName();

			byte[] footer;
			var codec = _soundResourceTable.Codecs[_sound.CodecIndex];
			switch ((SoundEncoding)codec.Encoding)
			{
				case SoundEncoding.Mono:
					footer = _monoFooter;
					break;

				case SoundEncoding.Stereo:
					footer = _stereoFooter;
					break;

				default:
					throw new NotImplementedException();
			}

			switch ((SoundCompression)codec.Compression)
			{
				case SoundCompression.XMA2:
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

					VariousFunctions.RunProgramSilently(towav,
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

		private byte[] ExtractRawPerm(SoundPermutation permutation)
		{
			var data = ExtractRaw();
			var permutationData = new List<byte>();
			
			var memoryStream = new MemoryStream(data);
			var stream = new EndianStream(memoryStream, Endian.BigEndian);

			foreach (var chunk in permutation.Chunks)
			{
				stream.SeekTo(chunk.FileOffset);
				permutationData.AddRange(stream.ReadBlock(chunk.DecodedSize));
			}

			return permutationData.ToArray();
		}

		private byte[] ExtractRaw()
		{
			var outputBytes = new List<byte>();

			using (var fileStream = _streamManager.OpenRead())
			{
				_resourcePages = new ResourcePage[3];
				_resourcePages[0] = _soundResource.Location.PrimaryPage;
				_resourcePages[1] = _soundResource.Location.SecondaryPage;
				_resourcePages[2] = _soundResource.Location.TertiaryPage;

				for (var i = 0; i < _resourcePages.Length; i++)
				{
					var page = _resourcePages[i];
					if (page == null || (page.CompressedSize == 0 || page.UncompressedSize == 0))
						continue;
					var resourceFile = _cache;
					Stream resourceStream = null;
					if (page.FilePath != null)
					{
						var resourceCacheInfo =
						App.AssemblyStorage.AssemblySettings.HalomapResourceCachePaths.FirstOrDefault(
							r => r.EngineName == _buildInfo.Name);

						var resourceCachePath = (resourceCacheInfo != null && resourceCacheInfo.ResourceCachePath != "")
							? resourceCacheInfo.ResourceCachePath : Path.GetDirectoryName(_cacheLocation);

						resourceCachePath = Path.Combine(resourceCachePath ?? "", Path.GetFileName(page.FilePath));

						if (!File.Exists(resourceCachePath))
						{
							MetroMessageBox.Show("Unable to extract tag",
								string.Format("Unable to extract tag, because a resource it relies on is stored in an external cache, \"{0}\" which could not be found.\r\nCheck Assembly's settings and set the file path to resource caches, or verify that the missing cache is in the same folder as the open cache file.",
								Path.GetFileName(resourceCachePath)));
							return null;
						}

						resourceStream =
							File.OpenRead(resourceCachePath);
						resourceFile = new ThirdGenCacheFile(new EndianReader(resourceStream, Endian.BigEndian), _buildInfo, resourceCachePath);
					}
					
					var tmpStream = new MemoryStream();
					var extractor = new ResourcePageExtractor(resourceFile);
					var path = Path.GetTempFileName();
					var pageStream = File.Open(path, FileMode.Create, FileAccess.ReadWrite);
					extractor.ExtractPage(page, resourceStream ?? fileStream.BaseStream, tmpStream);
					pageStream.Close();
					File.Delete(path);

					switch (i)
					{
						case 0:
							tmpStream.Position = _soundResource.Location.PrimaryOffset;
							break;

						case 1:
							tmpStream.Position = _soundResource.Location.SecondaryOffset;
							break;

						case 2:
							tmpStream.Position = _soundResource.Location.TertiaryOffset;
							break;
					}

					var bytes = VariousFunctions.StreamToByteArray(tmpStream);
					if (bytes.Length > 0)
						outputBytes.AddRange(new List<byte>(bytes));
				}
			}

			return outputBytes.ToArray();
		}

		#endregion

		public class ViewModel : INotifyPropertyChanged
		{
			public ViewModel()
			{
				_permutations.CollectionChanged += PermutationsOnCollectionChanged;
				_resourcePages.CollectionChanged += ResourcePagesOnCollectionChanged;
			}

			public ObservableCollection<ViewPermutation> Permutations
			{
				get { return _permutations; }
				set { SetField(ref _permutations, value, "Permutations"); }
			}
			private ObservableCollection<ViewPermutation> _permutations = new ObservableCollection<ViewPermutation>();

			public ObservableCollection<ResourcePage> ResourcePages
			{
				get { return _resourcePages; }
				set { SetField(ref _resourcePages, value, "ResourcePages"); }
			}
			private ObservableCollection<ResourcePage> _resourcePages = new ObservableCollection<ResourcePage>();

			public string TagName
			{
				get { return _tagName; }
				set { SetField(ref _tagName, value, "TagName"); }
			}
			private string _tagName;

			public CacheSound Sound
			{
				get { return _sound; }
				set { SetField(ref _sound, value, "Sound"); }
			}
			private CacheSound _sound;

			public ResourcePage PrimaryResourcePage { get { return _resourcePages[0]; } }
			public ResourcePage SecondaryResourcePage { get { return _resourcePages[1]; } }

			#region Events

			private void PermutationsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
			{
				Permutations = (ObservableCollection<ViewPermutation>)sender;
			}
			private void ResourcePagesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
			{
				ResourcePages = (ObservableCollection<ResourcePage>) sender;
			}

			#endregion

			#region Models

			public class ViewPermutation
			{
				public string Name { get; set; }

				public int Index { get; set; }

				public SoundPermutation SoundPermutation { get; set; }
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
	}
}
