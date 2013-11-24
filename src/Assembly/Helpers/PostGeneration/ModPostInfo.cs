using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Assembly.Helpers.PostGeneration
{
	public class ModPostInfo : INotifyPropertyChanged
	{
		#region Header

		private string _modAuthor = "";
		private string _modDescription = "";
		private string _modName = "";
		private string _modOriginalMap = "";
		private string _modPatchDownload = "";
		private string _modPreviewImage = "";

		public string ModName
		{
			get { return _modName; }
			set
			{
				_modName = value;
				NotifyPropertyChanged("ModName");
			}
		}

		public string ModDescription
		{
			get { return _modDescription; }
			set
			{
				_modDescription = value;
				NotifyPropertyChanged("ModDescription");
			}
		}

		public string ModAuthor
		{
			get { return _modAuthor; }
			set
			{
				_modAuthor = value;
				NotifyPropertyChanged("ModAuthor");
			}
		}

		public string ModOriginalMap
		{
			get { return _modOriginalMap; }
			set
			{
				_modOriginalMap = value;
				NotifyPropertyChanged("ModOriginalMap");
			}
		}

		public string ModPatchDownload
		{
			get { return _modPatchDownload; }
			set
			{
				_modPatchDownload = value;
				NotifyPropertyChanged("ModPatchDownload");
			}
		}

		public string ModPreviewImage
		{
			get { return _modPreviewImage; }
			set
			{
				_modPreviewImage = value;
				NotifyPropertyChanged("ModPreviewImage");
			}
		}

		#endregion

		#region Attributes

		private bool _aiEdits;
		private bool _barrierEdits;
		private bool _bipdEdits;
		private bool _blfMapinfoEdits;
		private bool _effectEdits;
		private bool _jmadEdits;
		private bool _lighingEdits;
		private bool _machineEdits;
		private bool _modelEdits;
		private bool _otherEdits;
		private bool _physicsEdits;
		private bool _projectileEdits;
		private bool _sceneryEdits;
		private bool _textureEdits;
		private bool _vehicleEdits;
		private bool _weaponBalances;
		private bool _weaponEdits;
		private bool _weatherEdits;

		public bool WeaponEdits
		{
			get { return _weaponEdits; }
			set
			{
				_weaponEdits = value;
				NotifyPropertyChanged("WeaponEdits");
			}
		}

		public bool WeaponBalances
		{
			get { return _weaponBalances; }
			set
			{
				_weaponBalances = value;
				NotifyPropertyChanged("WeaponBalances");
			}
		}

		public bool WeatherEdits
		{
			get { return _weatherEdits; }
			set
			{
				_weatherEdits = value;
				NotifyPropertyChanged("WeatherEdits");
			}
		}

		public bool TextureEdits
		{
			get { return _textureEdits; }
			set
			{
				_textureEdits = value;
				NotifyPropertyChanged("TextureEdits");
			}
		}

		public bool ProjectileEdits
		{
			get { return _projectileEdits; }
			set
			{
				_projectileEdits = value;
				NotifyPropertyChanged("ProjectileEdits");
			}
		}

		public bool MachineEdits
		{
			get { return _machineEdits; }
			set
			{
				_machineEdits = value;
				NotifyPropertyChanged("MachineEdits");
			}
		}

		public bool SceneryEdits
		{
			get { return _sceneryEdits; }
			set
			{
				_sceneryEdits = value;
				NotifyPropertyChanged("SceneryEdits");
			}
		}

		public bool BipdEdits
		{
			get { return _bipdEdits; }
			set
			{
				_bipdEdits = value;
				NotifyPropertyChanged("BipdEdits");
			}
		}

		public bool VehicleEdits
		{
			get { return _vehicleEdits; }
			set
			{
				_vehicleEdits = value;
				NotifyPropertyChanged("VehicleEdits");
			}
		}

		public bool LighingEdits
		{
			get { return _lighingEdits; }
			set
			{
				_lighingEdits = value;
				NotifyPropertyChanged("LighingEdits");
			}
		}

		public bool JmadEdits
		{
			get { return _jmadEdits; }
			set
			{
				_jmadEdits = value;
				NotifyPropertyChanged("JmadEdits");
			}
		}

		public bool EffectEdits
		{
			get { return _effectEdits; }
			set
			{
				_effectEdits = value;
				NotifyPropertyChanged("EffectEdits");
			}
		}

		public bool ModelEdits
		{
			get { return _modelEdits; }
			set
			{
				_modelEdits = value;
				NotifyPropertyChanged("ModelEdits");
			}
		}

		public bool PhysicsEdits
		{
			get { return _physicsEdits; }
			set
			{
				_physicsEdits = value;
				NotifyPropertyChanged("PhysicsEdits");
			}
		}

		public bool BarrierEdits
		{
			get { return _barrierEdits; }
			set
			{
				_barrierEdits = value;
				NotifyPropertyChanged("BarrierEdits");
			}
		}

		public bool AiEdits
		{
			get { return _aiEdits; }
			set
			{
				_aiEdits = value;
				NotifyPropertyChanged("AiEdits");
			}
		}

		public bool BlfMapinfoEdits
		{
			get { return _blfMapinfoEdits; }
			set
			{
				_blfMapinfoEdits = value;
				NotifyPropertyChanged("BlfMapinfoEdits");
			}
		}

		public bool OtherEdits
		{
			get { return _otherEdits; }
			set
			{
				_otherEdits = value;
				NotifyPropertyChanged("OtherEdits");
			}
		}

		#endregion

		private ObservableCollection<Image> _images = new ObservableCollection<Image>();
		private string _modVideo = "";

		private ObservableCollection<Thank> _thanks = new ObservableCollection<Thank>();

		public ObservableCollection<Image> Images
		{
			get { return _images; }
			set
			{
				_images = value;
				NotifyPropertyChanged("Images");
			}
		}

		public ObservableCollection<Thank> Thanks
		{
			get { return _thanks; }
			set
			{
				_thanks = value;
				NotifyPropertyChanged("Thanks");
			}
		}

		public string ModVideo
		{
			get { return _modVideo; }
			set
			{
				_modVideo = value;
				NotifyPropertyChanged("ModVideo");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void NotifyPropertyChanged(string info)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(info));
		}

		public class Image
		{
			public string Url { get; set; }
		}

		public class Thank
		{
			public string Alias { get; set; }
			public string Reason { get; set; }
		}
	}
}