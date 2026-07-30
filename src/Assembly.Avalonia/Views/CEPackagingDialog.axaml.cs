using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assembly.Avalonia.Services;
using Assembly.Avalonia.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Blamite.Serialization;

namespace Assembly.Avalonia.Views
{
	/// <summary>Which of the two packaging workflows a <see cref="CEPackagingDialog" /> is showing.</summary>
	public enum CEPackagingMode
	{
		Unpack,
		Repack
	}

	/// <summary>
	///     A dialog that walks a user through unpacking a Campaign Evolved container set to a directory of tag files,
	///     or repacking a directory of tag files back into a copy of the container set they came from.
	/// </summary>
	/// <remarks>
	///     Both workflows share one shape - pick input path(s), preview what will happen, run it with a real progress
	///     report, show a specific result - so one window with a mode switch, rather than two near-identical windows,
	///     is what this is. All of the actual work is delegated to <see cref="CEPackagingService" />; this class is
	///     purely presentation and the async/cancellation plumbing around it.
	/// </remarks>
	public partial class CEPackagingDialog : Window
	{
		private readonly EngineDatabase _db;
		private CancellationTokenSource? _runCts;

		public CEPackagingDialog()
		{
			// Parameterless constructor required by AvaloniaXamlLoader; never used to actually show a
			// dialog (see the real constructor below), so the engine database argument is allowed to
			// be missing here.
			InitializeComponent();
		}

		public CEPackagingDialog(CEPackagingMode mode, EngineDatabase db, string? initialSourcePath)
		{
			_db = db;
			InitializeComponent();
			Vm = new CEPackagingDialogViewModel(mode) { SourcePath = initialSourcePath ?? "" };
			DataContext = Vm;
			Title = mode == CEPackagingMode.Unpack ? "Assembly - Unpack Campaign Evolved Tags" : "Assembly - Repack Campaign Evolved Tags";
		}

		private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

		internal CEPackagingDialogViewModel Vm { get; private set; } = new(CEPackagingMode.Unpack);

		// ---- path pickers ----

		private async void OnBrowseSourceClick(object? sender, RoutedEventArgs e)
		{
			var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = "Choose the folder holding the .utoc/.ucas container set"
			});
			if (folders.FirstOrDefault()?.TryGetLocalPath() is string path)
			{
				Vm.SourcePath = path;
				Vm.InvalidatePreview();
			}
		}

		private async void OnBrowseTagsDirectoryClick(object? sender, RoutedEventArgs e)
		{
			var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = "Choose the folder of .ubulk tag files (an unpack's \"tags\" folder, or your own)"
			});
			if (folders.FirstOrDefault()?.TryGetLocalPath() is string path)
			{
				Vm.TagsDirectory = path;
				Vm.InvalidatePreview();
			}
		}

		private async void OnBrowseOutputClick(object? sender, RoutedEventArgs e)
		{
			var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = "Choose (or create) the output folder",
				AllowMultiple = false
			});
			if (folders.FirstOrDefault()?.TryGetLocalPath() is string path)
			{
				Vm.OutputDirectory = path;
				Vm.InvalidatePreview();
			}
		}

		// ---- preview ----

		private async void OnPreviewClick(object? sender, RoutedEventArgs e) => await RunPreviewAsync();

		private async Task RunPreviewAsync()
		{
			Vm.PreviewError = null;
			Vm.HasPreview = false;
			Vm.IsPreviewing = true;
			try
			{
				if (Vm.Mode == CEPackagingMode.Unpack)
				{
					var preview = await Task.Run(() => CEPackagingService.PreviewUnpack(Vm.SourcePath, _db));
					Vm.ApplyUnpackPreview(preview);
				}
				else
				{
					var preview = await Task.Run(() => CEPackagingService.PreviewRepack(Vm.SourcePath, Vm.TagsDirectory, _db));
					Vm.ApplyRepackPreview(preview);
				}
				Vm.HasPreview = true;
			}
			catch (Exception ex)
			{
				Vm.PreviewError = ex.Message;
			}
			finally
			{
				Vm.IsPreviewing = false;
			}
		}

		// ---- run ----

		private async void OnRunClick(object? sender, RoutedEventArgs e)
		{
			if (!Vm.HasPreview) await RunPreviewAsync();
			if (!Vm.CanRun) return;

			Vm.IsRunning = true;
			Vm.HasResult = false;
			Vm.WasCancelled = false;
			_runCts = new CancellationTokenSource();

			var progress = new Progress<CEPackagingProgress>(p =>
			{
				Vm.ProgressStage = p.Stage.ToString();
				Vm.ProgressDetail = p.Detail;
				Vm.ProgressTotal = p.Total;
				Vm.ProgressCompleted = p.Completed;
			});

			try
			{
				if (Vm.Mode == CEPackagingMode.Unpack)
				{
					var result = await Task.Run(() => CEPackagingService.Unpack(
						Vm.SourcePath, Vm.OutputDirectory, _db, Vm.IncludeRawChunks, progress, _runCts.Token), _runCts.Token);
					Vm.ApplyUnpackResult(result);
				}
				else
				{
					var result = await Task.Run(() => CEPackagingService.Repack(
						Vm.SourcePath, Vm.TagsDirectory, Vm.OutputDirectory, _db, progress, _runCts.Token), _runCts.Token);
					Vm.ApplyRepackResult(result);
				}
			}
			catch (OperationCanceledException)
			{
				Vm.WasCancelled = true;
				Vm.ApplyCancelled();
			}
			catch (Exception ex)
			{
				Vm.ApplyUnexpectedFailure(ex);
			}
			finally
			{
				Vm.IsRunning = false;
				Vm.HasResult = true;
				_runCts?.Dispose();
				_runCts = null;
			}
		}

		private void OnCancelRunClick(object? sender, RoutedEventArgs e) => _runCts?.Cancel();

		private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
	}

	/// <summary>Presentation state for <see cref="CEPackagingDialog" />, updated from its code-behind.</summary>
	internal sealed class CEPackagingDialogViewModel : ObservableObject
	{
		public CEPackagingDialogViewModel(CEPackagingMode mode)
		{
			Mode = mode;
		}

		public CEPackagingMode Mode { get; }
		public bool IsUnpack => Mode == CEPackagingMode.Unpack;
		public bool IsRepack => Mode == CEPackagingMode.Repack;

		private string _sourcePath = "";
		public string SourcePath { get => _sourcePath; set => Set(ref _sourcePath, value); }

		private string _tagsDirectory = "";
		public string TagsDirectory { get => _tagsDirectory; set => Set(ref _tagsDirectory, value); }

		private string _outputDirectory = "";
		public string OutputDirectory { get => _outputDirectory; set => Set(ref _outputDirectory, value); }

		private bool _includeRawChunks;
		public bool IncludeRawChunks { get => _includeRawChunks; set => Set(ref _includeRawChunks, value); }

		// ---- preview state ----
		private bool _hasPreview;
		public bool HasPreview { get => _hasPreview; set { Set(ref _hasPreview, value); Raise(nameof(CanRun)); } }

		private bool _isPreviewing;
		public bool IsPreviewing { get => _isPreviewing; set => Set(ref _isPreviewing, value); }

		private string? _previewError;
		public string? PreviewError { get => _previewError; set { Set(ref _previewError, value); Raise(nameof(HasPreviewError)); } }
		public bool HasPreviewError => !string.IsNullOrEmpty(PreviewError);

		public string PreviewSummary { get; private set; } = "";
		public ObservableCollection<string> PreviewLines { get; } = new();
		public ObservableCollection<string> PreviewWarnings { get; } = new();

		private bool _previewCanProceed = true;
		public bool PreviewCanProceed { get => _previewCanProceed; set { Set(ref _previewCanProceed, value); Raise(nameof(CanRun)); } }

		private string _blockingReason = "";
		public string BlockingReason { get => _blockingReason; set => Set(ref _blockingReason, value); }

		public bool CanRun => HasPreview && PreviewCanProceed && !IsRunning;

		// ---- run state ----
		private bool _isRunning;
		public bool IsRunning { get => _isRunning; set { Set(ref _isRunning, value); Raise(nameof(CanRun)); } }

		private string _progressStage = "";
		public string ProgressStage { get => _progressStage; set => Set(ref _progressStage, value); }

		private string _progressDetail = "";
		public string ProgressDetail { get => _progressDetail; set => Set(ref _progressDetail, value); }

		private int _progressCompleted;
		public int ProgressCompleted { get => _progressCompleted; set { Set(ref _progressCompleted, value); Raise(nameof(ProgressFraction)); Raise(nameof(ProgressIsIndeterminate)); } }

		private int _progressTotal;
		public int ProgressTotal { get => _progressTotal; set { Set(ref _progressTotal, value); Raise(nameof(ProgressFraction)); Raise(nameof(ProgressIsIndeterminate)); } }

		public double ProgressFraction => ProgressTotal > 0 ? 100.0 * ProgressCompleted / ProgressTotal : 0;
		public bool ProgressIsIndeterminate => ProgressTotal <= 0;

		public bool WasCancelled { get; set; }

		// ---- result state ----
		private bool _hasResult;
		public bool HasResult { get => _hasResult; set => Set(ref _hasResult, value); }

		public bool ResultSuccess { get; private set; }
		public string ResultHeadline { get; private set; } = "";
		public ObservableCollection<string> ResultLines { get; } = new();
		public ObservableCollection<string> ResultWarnings { get; } = new();

		private bool _resultByteIdentical;
		public bool ResultByteIdentical { get => _resultByteIdentical; set => Set(ref _resultByteIdentical, value); }

		/// <summary>Discards a computed preview, e.g. because a path changed, so a stale preview cannot be run against.</summary>
		public void InvalidatePreview()
		{
			HasPreview = false;
			PreviewError = null;
			PreviewLines.Clear();
			PreviewWarnings.Clear();
		}

		public void ApplyUnpackPreview(CEUnpackPreview preview)
		{
			PreviewLines.Clear();
			PreviewWarnings.Clear();
			foreach (var t in preview.Tags.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
				PreviewLines.Add($"[{t.Group}] {t.Name}   {t.PayloadSize:N0} bytes");
			foreach (var w in preview.MountWarnings)
				PreviewWarnings.Add(w);

			PreviewSummary = $"{preview.ContainerCount} container(s) mounted -> {preview.Tags.Count} tag(s), {preview.TotalTagBytes:N0} bytes total.";
			Raise(nameof(PreviewSummary));
			PreviewCanProceed = true;
			BlockingReason = "";
		}

		public void ApplyRepackPreview(CERepackPreview preview)
		{
			PreviewLines.Clear();
			PreviewWarnings.Clear();
			foreach (var f in preview.TagFiles.OrderBy(f => f.LogicalName, StringComparer.OrdinalIgnoreCase))
			{
				string status = !f.MatchedExistingTag ? "UNMATCHED - no tag by this name in the source"
					: !f.ParsesCleanly ? $"INVALID - {f.ParseProblem}"
					: f.IdenticalToSource ? "unchanged"
					: "CHANGED";
				PreviewLines.Add($"[{f.Group}] {f.LogicalName}   {f.FileSize:N0} bytes   {status}");
			}
			foreach (var w in preview.MountWarnings)
				PreviewWarnings.Add(w);

			PreviewSummary = $"{preview.ContainerFiles.Count} file(s) to copy from the source; " +
				$"{preview.MatchedCount} tag file(s) matched ({preview.ChangedCount} changed, {preview.MatchedCount - preview.ChangedCount} identical), " +
				$"{preview.UnmatchedCount} unmatched, {preview.InvalidCount} invalid. " +
				$"{preview.UntouchedSourceTagCount} tag(s) in the source have no file here and will be left untouched.";
			Raise(nameof(PreviewSummary));

			PreviewCanProceed = preview.CanProceed;
			BlockingReason = preview.CanProceed
				? ""
				: $"{preview.InvalidCount} tag file(s) do not parse as valid Blam tags. Fix or remove them before repacking - see the list above.";
		}

		public void ApplyUnpackResult(CEUnpackResult result)
		{
			ResultSuccess = result.Success;
			ResultLines.Clear();
			ResultWarnings.Clear();
			if (result.Success)
			{
				ResultHeadline = $"Unpacked {result.TagsWritten} tag(s), {result.TagBytesWritten:N0} bytes, in {result.Elapsed.TotalSeconds:0.0}s.";
				ResultLines.Add($"Output: {result.OutputDirectory}");
				if (result.ChunksWritten > 0)
					ResultLines.Add($"Also wrote {result.ChunksWritten} raw chunk(s) under containers/ (every chunk of every mounted container, tag payloads included).");
			}
			else
			{
				ResultHeadline = "Unpack failed.";
				ResultLines.Add(result.Error ?? "(no further detail)");
			}
			foreach (var w in result.Warnings) ResultWarnings.Add(w);
			ResultByteIdentical = false; // unpack doesn't write a container; the statement doesn't apply
			Raise(nameof(ResultHeadline));
			Raise(nameof(ResultSuccess));
		}

		public void ApplyRepackResult(CERepackResult result)
		{
			ResultSuccess = result.Success;
			ResultLines.Clear();
			ResultWarnings.Clear();
			if (result.Success)
			{
				ResultHeadline = result.ByteIdenticalToSource
					? $"Repacked to {result.OutputDirectory} - byte-identical to the source (nothing was changed)."
					: $"Repacked to {result.OutputDirectory} - {result.TagsChanged} container(s) rewritten, {result.TagsUnchanged} tag(s) unchanged.";
				ResultLines.Add($"Files copied: {result.FilesCopied}");
				if (result.ChangedContainers.Count > 0)
					ResultLines.Add("Rewritten containers: " + string.Join(", ", result.ChangedContainers));
				ResultLines.Add($"Elapsed: {result.Elapsed.TotalSeconds:0.0}s");
			}
			else
			{
				ResultHeadline = "Repack failed - nothing was written.";
				ResultLines.Add(result.Error ?? "(no further detail)");
			}
			foreach (var w in result.Warnings) ResultWarnings.Add(w);
			ResultByteIdentical = result.Success && result.ByteIdenticalToSource;
			Raise(nameof(ResultHeadline));
			Raise(nameof(ResultSuccess));
		}

		public void ApplyCancelled()
		{
			ResultSuccess = false;
			ResultHeadline = "Cancelled. Nothing further was written once the cancellation was noticed, but anything already " +
				"copied or rewritten before then is still on disk - re-run to finish, or delete the output and start over.";
			ResultLines.Clear();
			ResultWarnings.Clear();
			Raise(nameof(ResultHeadline));
			Raise(nameof(ResultSuccess));
		}

		public void ApplyUnexpectedFailure(Exception ex)
		{
			ResultSuccess = false;
			ResultHeadline = "Failed unexpectedly - nothing more was written once this happened.";
			ResultLines.Clear();
			ResultLines.Add($"{ex.GetType().Name}: {ex.Message}");
			Raise(nameof(ResultHeadline));
			Raise(nameof(ResultSuccess));
		}
	}
}
