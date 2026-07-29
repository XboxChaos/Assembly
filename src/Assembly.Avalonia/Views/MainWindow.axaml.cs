using System;
using System.Linq;
using System.Threading.Tasks;
using Assembly.Avalonia.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace Assembly.Avalonia.Views
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			// Optional automated screenshot hook (see docs/dev notes).
			var shot = Environment.GetEnvironmentVariable("ASM_SHOT");
			if (!string.IsNullOrEmpty(shot))
				Opened += async (_, _) => await CaptureAsync(shot);
		}

		private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

		private MainViewModel? Vm => DataContext as MainViewModel;

		private async void OnOpenClick(object? sender, RoutedEventArgs e) => await OpenViaPickerAsync();
		private async void OnMenuOpen(object? sender, EventArgs e) => await OpenViaPickerAsync();
		private void OnMenuClose(object? sender, EventArgs e) => Vm?.CloseCache();
		private void OnMenuFocusSearch(object? sender, EventArgs e)
			=> this.FindControl<TextBox>("SearchBox")?.Focus();

		private async Task OpenViaPickerAsync()
		{
			var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = "Open Halo cache file",
				AllowMultiple = false,
				FileTypeFilter = new[]
				{
					new FilePickerFileType("Halo cache file") { Patterns = new[] { "*.map", "*.dat" } },
					FilePickerFileTypes.All
				}
			});

			if (files.FirstOrDefault()?.TryGetLocalPath() is string path && Vm != null)
				await Vm.OpenAsync(path);
		}

		private void OnTreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
		{
			if (Vm == null) return;
			// Only leaf nodes (tags) drive the meta pane; group/folder nodes clear it.
			Vm.SelectedTag = (sender as TreeView)?.SelectedItem as TagNode;
		}

		private async Task CaptureAsync(string path)
		{
			var open = Environment.GetEnvironmentVariable("ASM_OPEN");
			if (!string.IsNullOrEmpty(open) && Vm != null)
			{
				await Vm.OpenAsync(open);
				await Task.Delay(300);

				var mode = Environment.GetEnvironmentVariable("ASM_MODE");
				if (!string.IsNullOrEmpty(mode)) Vm.TreeMode = mode;

				var select = Environment.GetEnvironmentVariable("ASM_SELECT");
				if (!string.IsNullOrEmpty(select))
				{
					var tree = this.FindControl<TreeView>("TagTree")!;
					UpdateLayout();
					foreach (var c in tree.GetRealizedContainers())
						if (c is TreeViewItem tvi) tvi.IsExpanded = true;
					UpdateLayout();
					await Task.Delay(200);

					var tag = Vm.FindTag(select);
					if (tag != null) Vm.SelectedTag = tag;
				}
			}

			await Task.Delay(1200);
			try
			{
				var px = new global::Avalonia.PixelSize((int)Bounds.Width, (int)Bounds.Height);
				using var rtb = new global::Avalonia.Media.Imaging.RenderTargetBitmap(px, new global::Avalonia.Vector(96, 96));
				UpdateLayout();
				rtb.Render(this);
				rtb.Save(path);
				Console.WriteLine($"wrote {path} ({px.Width}x{px.Height})");
			}
			catch (Exception ex)
			{
				Console.WriteLine("render failed: " + ex);
			}

			Close();
		}
	}
}
