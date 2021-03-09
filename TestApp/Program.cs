using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace TestApp
{
	class Program
	{
		private static double _realLoad;
		private static readonly BlockingCollection<float> CpuLoadValues = new BlockingCollection<float>();
		private const int AnalysisLimit = 3000;

		static void Main(string[] args)
		{
			Console.WriteLine("Enter a desired CPU load in percentage:");
			var loadString = Console.ReadLine();
			int load;
			while (!int.TryParse(loadString, out load) || load > 100)
			{
				Console.WriteLine("Invalid load format, try again:");
				loadString = Console.ReadLine();
			}

			Console.WriteLine("Preparing...");
			PrepareVariables(load);
			Console.WriteLine("System ready!");

			load = Math.Abs(load - (int)Math.Round(_realLoad));
			Console.WriteLine("Stressing the CPU on {0}% load...", loadString);
			
			for (var cpu = 0; cpu <= Environment.ProcessorCount; cpu++)
			{
				new Thread(() => Sleep(load)).Start();
			}

			for (; ; ) ;
		}

		private static void PrepareVariables(int target)
		{
			var producer = Task.Factory.StartNew(() =>
			{
				for (var cpu = 0; cpu <= Environment.ProcessorCount; cpu++)
				{
					var thread = new Thread(() => Analyze(target));
					thread.Start();
					while (thread.IsAlive)
					{
					}
				}
				CpuLoadValues.CompleteAdding();
			});
			var consumer = Task.Factory.StartNew(() =>
			{
				foreach (var loadValue in CpuLoadValues.GetConsumingEnumerable())
				{
					_realLoad = Math.Round((_realLoad + loadValue) / 2);
				}
			});
			Task.WaitAll(producer, consumer);
		}

		private static void Analyze(int target)
		{
			var limit = new Stopwatch();
			limit.Start();
			var watch = new Stopwatch();
			watch.Start();
			while (limit.ElapsedMilliseconds < AnalysisLimit)
			{
				CpuLoadValues.Add(GetCpuLoad());
				if (watch.ElapsedMilliseconds == target)
				{
					Thread.Sleep(100 - target);
					watch.Reset();
					watch.Start();
				}
			}
		}

		private static float GetCpuLoad()
		{
			var cpuCounter = new PerformanceCounter
			{
				CategoryName = "Processor",
				CounterName = "% Processor Time",
				InstanceName = "_Total"
			};
			cpuCounter.NextValue();
			Thread.Sleep(1000);

			return cpuCounter.NextValue();
		}

		public static void Sleep(int x)
		{
			var watch = new Stopwatch();
			watch.Start();
			while (true)
			{
				if (watch.ElapsedMilliseconds > x)
				{
					Thread.Sleep(100 - x);
					watch.Reset();
					watch.Start();
				}
			}
		}

		public static void ConsumeCpu(int percentage)
		{
			if (percentage < 0 || percentage > 100)
				throw new ArgumentException("percentage");
			Stopwatch watch = new Stopwatch();
			watch.Start();
			while (true)
			{
				// Make the loop go on for "percentage" milliseconds then sleep the 
				// remaining percentage milliseconds. So 40% utilization means work 40ms and sleep 60ms
				if (watch.ElapsedMilliseconds > percentage)
				{
					Thread.Sleep(100 - percentage);
					watch.Reset();
					watch.Start();
				}
			}
		}

		public static int Fac(int n)
		{
			if (n != 1)
			{
				return n * Fac(n - 1);
			}
			else
			{
				return 1;
			}
		}
		public static int CpuTime()
		{
			return (int)Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds;
		}

		public static long WallTime()
		{
			return DateTime.Now.Ticks / 10000;
		}
		public static void Load(double target)
		{
			int n = 1000000;
			int cpu = CpuTime();
			long wall = WallTime();
			int dcpu;
			long dwall;
			for (; ; )
			{
				for (int i = 0; i < n; i++)
				{
					if (Fac(10) != 3628800)
					{
						Environment.Exit(1);
					}
				}
				Thread.Sleep(100);
				int cpu2 = CpuTime();
				long wall2 = WallTime();
				dcpu = cpu2 - cpu;
				dwall = wall2 - wall;
				n = (int)(n * ((target * dwall) / dcpu));
				cpu = cpu2;
				wall = wall2;
			}
		}

	}
}
