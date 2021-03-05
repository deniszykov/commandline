using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;

namespace deniszykov.CommandLine.Hosted
{
	internal sealed class CommandLineHostedService : BackgroundService
	{
		private readonly CommandLine commandLine;
		private readonly IHostApplicationLifetime hostApplicationLifetime;

		public CommandLineHostedService([NotNull] CommandLine commandLine, [NotNull] IHostApplicationLifetime hostApplicationLifetime)
		{
			if (commandLine == null) throw new ArgumentNullException(nameof(commandLine));
			if (hostApplicationLifetime == null) throw new ArgumentNullException(nameof(hostApplicationLifetime));

			this.commandLine = commandLine;
			this.hostApplicationLifetime = hostApplicationLifetime;
		}

		/// <inheritdoc />
		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			var executeTask = this.commandLine
				.RunAsync(stoppingToken)
				.ContinueWith(this.OnRunCompleted, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

			return executeTask;
		}

		private void OnRunCompleted(Task<int> runTask)
		{
			if (runTask.Status == TaskStatus.RanToCompletion)
			{
				Environment.ExitCode = runTask.Result;
			}
			else
			{
				Environment.ExitCode = CommandLine.DOT_NET_EXCEPTION_EXIT_CODE;
			}

			this.hostApplicationLifetime.StopApplication();
		}
	}
}
