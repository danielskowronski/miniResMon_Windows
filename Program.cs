using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using InfluxDB.Collector;
using YamlDotNet.RepresentationModel;
using System.Diagnostics.Tracing;

namespace miniResMon
{
	class Program
	{
		static void exitOnProblem(string msg)
		{
			Console.WriteLine(msg);
			Thread.Sleep(10000);
			System.Environment.Exit(1);
		}
		static void Main(string[] args)
		{
			Console.WriteLine("miniResMon started");

			string url = "", db = "", user = "", pass = "";
			try
			{
				using (var reader = new System.IO.StreamReader("./config.yml"))
				{
					var cfg = new YamlStream();
					cfg.Load(reader);
					var mapping = (YamlMappingNode)cfg.Documents[0].RootNode;
					url = mapping.Children[new YamlScalarNode("url")].ToString();
					db = mapping.Children[new YamlScalarNode("db")].ToString();
					user = mapping.Children[new YamlScalarNode("user")].ToString();
					pass = mapping.Children[new YamlScalarNode("pass")].ToString();
				}
			}
			catch (Exception e)
			{
				exitOnProblem("can't read config.yml file - " + e.ToString());
			}

			try
			{
				Metrics.Collector = new CollectorConfiguration()
										.Tag.With("host", Environment.GetEnvironmentVariable("COMPUTERNAME"))
										.Batch.AtInterval(TimeSpan.FromSeconds(2))
										.WriteTo.InfluxDB(url, db, user, pass)
										.CreateCollector();

			}
			catch (Exception e)
			{
				exitOnProblem("can't connect to influxDB - " + e.ToString());
			}

			PerformanceCounter cpuCounter;
			PerformanceCounter ramCounter;

			cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
			ramCounter = new PerformanceCounter("Memory", "Available MBytes");
			while (true)
			{
				float cpu = cpuCounter.NextValue();
				long  ram = (long)(ramCounter.NextValue());
				Metrics.Increment("iterations");
				Metrics.Measure("cpu", cpu);
				Metrics.Measure("ram", ram);
				Console.WriteLine("Pushing " + cpu + "% CPU and " + ram + "MB RAM");

				Thread.Sleep(5000);
			}
		}
	}
}
