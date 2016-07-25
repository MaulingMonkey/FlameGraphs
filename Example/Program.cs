// Copyright (c) 2016 Michael B. Edwin Rickert
// Licensed to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using MaulingMonkey.FlameGraphs;
using MaulingMonkey.FlameGraphs.Gdi;
using System;
using System.Threading;
using System.Windows.Forms;

namespace Example
{
	static class Program
	{
		static readonly Random PRNG = new Random();

		static void RandomlySleep1()
		{
			Trace.Scope("A", () => {
				if (PRNG.Next(100) == 0) Thread.Sleep(10);
			});
		}

		static void RandomlySleep2()
		{
			using (Trace.Scope("B")) {
				if (PRNG.Next(10) == 0) Thread.Sleep(50);
			};
		}

		static void ProfiledThread()
		{
			for (;;) using (Trace.Scope("C")) {
				for (int i=0; i<10; ++i) RandomlySleep1();
				for (int i=0; i<10; ++i) RandomlySleep2();
			}
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			var t1 = new Thread(() => ProfiledThread()) { Name = "Profiled Thread #1", IsBackground = true };
			var t2 = new Thread(() => ProfiledThread()) { Name = "Profiled Thread #2", IsBackground = true };
			var t3 = new Thread(() => ProfiledThread()) { IsBackground = true }; // Leave unnamed
			t1.Start();
			t2.Start();
			t3.Start();

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new GdiFlameGraphDisplayForm());
		}
	}
}
