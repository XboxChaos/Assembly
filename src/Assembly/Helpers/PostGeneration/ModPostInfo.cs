using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Assembly.Helpers.PostGeneration
{
	public class ModPostInfo : INotifyPropertyChanged
	{
		#region Header
		private string _modName = "";
		public string ModName
		{
			get { return _modName; }
			set { _modName = value; NotifyPropertyChanged("ModName"); }
		}

		private string _modDescription = "";
		public string ModDescription
		{
			get { return _modDescription; }
			set { _modDescription = value; NotifyPropertyChanged("ModDescription"); }
		}

		private string _modAuthor = "";
		public string ModAuthor
		{
			get { return _modAuthor; }
			set { _modAuthor = value; NotifyPropertyChanged("ModAuthor"); }
		}

		private string _modOriginalMap = "";
		public string ModOriginalMap
		{
			get { return _modOriginalMap; }
			set { _modOriginalMap = value; NotifyPropertyChanged("ModOriginalMap"); }
		}

		private string _modPatchDownload = "";
		public string ModPatchDownload
		{
			get { return _modPatchDownload; }
			set { _modPatchDownload = value; NotifyPropertyChanged("ModPatchDownload"); }
		}

		private string _modPreviewImage = "";
		public string ModPreviewImage
		{
			get { return _modPreviewImage; }
			set { _modPreviewImage = value; NotifyPropertyChanged("ModPreviewImage"); }
		}
		#endregion

		#region Attributes
		private bool _weaponEdits;
		public bool WeaponEdits
		{
			get { return _weaponEdits; }
			set { _weaponEdits = value; NotifyPropertyChanged("WeaponEdits"); }
		}

		private bool _weaponBalances;
		public bool WeaponBalances
		{
			get { return _weaponBalances; }
			set { _weaponBalances = value; NotifyPropertyChanged("WeaponBalances"); }
		}

		private bool _weatherEdits;
		public bool WeatherEdits
		{
			get { return _weatherEdits; }
			set { _weatherEdits = value; NotifyPropertyChanged("WeatherEdits"); }
		}

		private bool _textureEdits;
		public bool TextureEdits
		{
			get { return _textureEdits; }
			set { _textureEdits = value; NotifyPropertyChanged("TextureEdits"); }
		}

		private bool _projectileEdits;
		public bool ProjectileEdits
		{
			get { return _projectileEdits; }
			set { _projectileEdits = value; NotifyPropertyChanged("ProjectileEdits"); }
		}

		private bool _machineEdits;
		public bool MachineEdits
		{
			get { return _machineEdits; }
			set { _machineEdits = value; NotifyPropertyChanged("MachineEdits"); }
		}

		private bool _sceneryEdits;
		public bool SceneryEdits
		{
			get { return _sceneryEdits; }
			set { _sceneryEdits = value; NotifyPropertyChanged("SceneryEdits"); }
		}

		private bool _bipdEdits;
		public bool BipdEdits
		{
			get { return _bipdEdits; }
			set { _bipdEdits = value; NotifyPropertyChanged("BipdEdits"); }
		}

		private bool _vehicleEdits;
		public bool VehicleEdits
		{
			get { return _vehicleEdits; }
			set { _vehicleEdits = value; NotifyPropertyChanged("VehicleEdits"); }
		}

		private bool _lighingEdits;
		public bool LighingEdits
		{
			get { return _lighingEdits; }
			set { _lighingEdits = value; NotifyPropertyChanged("LighingEdits"); }
		}

		private bool _jmadEdits;
		public bool JmadEdits
		{
			get { return _jmadEdits; }
			set { _jmadEdits = value; NotifyPropertyChanged("JmadEdits"); }
		}

		private bool _effectEdits;
		public bool EffectEdits
		{
			get { return _effectEdits; }
			set { _effectEdits = value; NotifyPropertyChanged("EffectEdits"); }
		}

		private bool _modelEdits;
		public bool ModelEdits
		{
			get { return _modelEdits; }
			set { _modelEdits = value; NotifyPropertyChanged("ModelEdits"); }
		}

		private bool _physicsEdits;
		public bool PhysicsEdits
		{
			get { return _physicsEdits; }
			set { _physicsEdits = value; NotifyPropertyChanged("PhysicsEdits"); }
		}

		private bool _barrierEdits;
		public bool BarrierEdits
		{
			get { return _barrierEdits; }
			set { _barrierEdits = value; NotifyPropertyChanged("BarrierEdits"); }
		}

		private bool _aiEdits;
		public bool AiEdits
		{
			get { return _aiEdits; }
			set { _aiEdits = value; NotifyPropertyChanged("AiEdits"); }
		}

		private bool _blfMapinfoEdits;
		public bool BlfMapinfoEdits
		{
			get { return _blfMapinfoEdits; }
			set { _blfMapinfoEdits = value; NotifyPropertyChanged("BlfMapinfoEdits"); }
		}

		private bool _otherEdits;
		public bool OtherEdits
		{
			get { return _otherEdits; }
			set { _otherEdits = value; NotifyPropertyChanged("OtherEdits"); }
		}
		#endregion

		public class Image
		{
			public string Url { get; set; }
		}
		private ObservableCollection<Image> _images = new ObservableCollection<Image>();
		public ObservableCollection<Image> Images
		{
			get { return _images; }
			set { _images = value; NotifyPropertyChanged("Images"); }
		}

		public class Thank
		{
			public string Alias { get; set; }
			public string Reason { get; set; }
		}
		private ObservableCollection<Thank> _thanks = new ObservableCollection<Thank>();
		public ObservableCollection<Thank> Thanks
		{
			get { return _thanks; }
			set { _thanks = value; NotifyPropertyChanged("Thanks"); }
		}

		private string _modVideo = "";
		public string ModVideo
		{
			get { return _modVideo; }
			set { _modVideo = value; NotifyPropertyChanged("ModVideo"); }
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public void NotifyPropertyChanged(string info)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(info));
		}
	}
}