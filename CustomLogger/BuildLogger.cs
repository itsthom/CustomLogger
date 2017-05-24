using Microsoft.Build.Utilities;
using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Build.Framework;

namespace CustomLogger
{
	public class BuildLogger : Logger
	{
		private readonly ConcurrentQueue<string> _errors = new ConcurrentQueue<string>();
		private readonly ConcurrentQueue<string> _warnings = new ConcurrentQueue<string>();
		private int _projectSucceededCount;
		private int _projectFailedCount;
		private DateTime _startTime;
		private DateTime _finishTime;

		public override void Initialize(IEventSource eventSource)
		{
			if (eventSource == null)
				return;

			eventSource.BuildStarted += EventSource_BuildStarted;
			eventSource.BuildFinished += EventSource_BuildFinished;
			eventSource.ProjectFinished += EventSource_ProjectFinished;
			eventSource.ErrorRaised += EventSource_ErrorRaised;
			eventSource.WarningRaised += EventSource_WarningRaised;
		}

		private void EventSource_BuildStarted(object sender, BuildStartedEventArgs e)
		{
			_startTime = DateTime.Now;
			Console.WriteLine($"\n{_startTime.ToLongTimeString()} > {e.Message}");
		}
		private void EventSource_BuildFinished(object sender, BuildFinishedEventArgs e)
		{
			_finishTime = DateTime.Now;
			PrintSummary(e.Succeeded);
		}
		private void EventSource_ProjectFinished(object sender, ProjectFinishedEventArgs e)
		{
			if (e.Succeeded)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write(".");
				Interlocked.Increment(ref _projectSucceededCount);
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write("X");
				Interlocked.Increment(ref _projectFailedCount);
			}
		}
		private void EventSource_ErrorRaised(object sender, BuildErrorEventArgs e)
		{
			_errors.Enqueue("ERROR " + FormatErrorOrWarning(e.Code, e.ProjectFile, e.File, e.LineNumber, e.ColumnNumber, e.Message));
		}
		private void EventSource_WarningRaised(object sender, BuildWarningEventArgs e)
		{
			_warnings.Enqueue("Warning " + FormatErrorOrWarning(e.Code, e.ProjectFile, e.File, e.LineNumber, e.ColumnNumber, e.Message));
		}
		private void PrintSummary(bool buildSuccess)
		{
			Console.ResetColor();
			Console.WriteLine(
				$"\n\n{DateTime.Now.ToLongTimeString()} > Build {(buildSuccess ? "finished successfully" : "failed")} ({_finishTime - _startTime}).");
			Console.WriteLine(
				$"\t- {_projectSucceededCount} of {_projectSucceededCount + _projectFailedCount} projects succeeded, {_errors.Count} {(_errors.Count == 1 ? "error" : "errors")}, {_warnings.Count} {(_warnings.Count == 1 ? "warning" : "warnings")}");
			PrintErrorsAndWarnings();
		}
		private void PrintErrorsAndWarnings()
		{
			if (_errors.Count == 0 && _warnings.Count == 0)
				return;

			Console.WriteLine();
			foreach (var warning in _warnings) Console.WriteLine(warning);
			foreach (var error in _errors) Console.WriteLine(error);
			Console.WriteLine();
		}
		private static string FormatErrorOrWarning(string code, string project, string file, int line, int col, string message)
		{
			return $"{code}:\n\t{project}\n\t{file}; ({line}|{col}).\n\t{message}\n";
		}
	}
}
