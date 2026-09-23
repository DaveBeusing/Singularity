// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text;

namespace Singularity.Application;

internal static class StartupFailureReporter
{
	private const string LogFileName = "crash.log";
	private static int fatalFailureReported;

	internal static string LogPath =>
		Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"Singularity",
			"Logs",
			LogFileName);

	public static void Install()
	{
		System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
		System.Windows.Forms.Application.ThreadException += (_, args) =>
			ReportFatal("Unhandled UI exception", args.Exception);

		AppDomain.CurrentDomain.UnhandledException += (_, args) =>
		{
			if (args.ExceptionObject is Exception exception)
				WriteDiagnostic("Unhandled application exception", exception);
		};

		TaskScheduler.UnobservedTaskException += (_, args) =>
		{
			WriteDiagnostic("Unobserved task exception", args.Exception);
			args.SetObserved();
		};
	}

	public static void ReportStartupFailure(Exception exception) =>
		ReportFatal("Application startup failed", exception);

	internal static string BuildDiagnosticReport(string context, Exception exception)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(context);
		ArgumentNullException.ThrowIfNull(exception);

		StringBuilder builder = new();
		builder.AppendLine($"Timestamp (UTC): {DateTime.UtcNow:O}");
		builder.AppendLine($"Context: {context}");
		builder.AppendLine($"Application version: {ApplicationMetadata.Version}");
		builder.AppendLine($"Executable: {Environment.ProcessPath ?? "Unknown"}");
		builder.AppendLine($"Base directory: {AppContext.BaseDirectory}");
		builder.AppendLine($"Current directory: {Environment.CurrentDirectory}");
		builder.AppendLine($"Framework: {RuntimeInformation.FrameworkDescription}");
		builder.AppendLine($"OS: {RuntimeInformation.OSDescription}");
		builder.AppendLine($"Process architecture: {RuntimeInformation.ProcessArchitecture}");
		builder.AppendLine();
		builder.AppendLine(exception.ToString());
		return builder.ToString();
	}

	private static void ReportFatal(string context, Exception exception)
	{
		if (Interlocked.Exchange(ref fatalFailureReported, 1) != 0)
			return;

		string logPath = WriteDiagnostic(context, exception);

		try
		{
			MessageBox.Show(
				$"Singularity encountered a fatal error and must close.\n\n" +
				$"{exception.Message}\n\n" +
				$"Diagnostic log:\n{logPath}",
				"Singularity",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error);
		}
		catch
		{
			// The diagnostic log remains available even when a message box cannot be shown.
		}

		try
		{
			System.Windows.Forms.Application.Exit();
		}
		catch
		{
			// The process will terminate naturally if WinForms shutdown is unavailable.
		}
	}

	private static string WriteDiagnostic(string context, Exception exception)
	{
		string report = BuildDiagnosticReport(context, exception);
		if (TryAppend(LogPath, report))
			return LogPath;

		string fallbackPath = Path.Combine(Path.GetTempPath(), "Singularity-crash.log");
		TryAppend(fallbackPath, report);
		return fallbackPath;
	}

	private static bool TryAppend(string path, string report)
	{
		try
		{
			string? directory = Path.GetDirectoryName(path);
			if (!string.IsNullOrWhiteSpace(directory))
				Directory.CreateDirectory(directory);

			File.AppendAllText(
				path,
				$"{report}{Environment.NewLine}{new string('-', 80)}{Environment.NewLine}");
			return true;
		}
		catch
		{
			return false;
		}
	}
}
