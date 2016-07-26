// Copyright (c) 2016 Michael B. Edwin Rickert
// Licensed to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MaulingMonkey.FlameGraphs
{
	public static class Trace
	{
		public static void Scope
			( string label
			, Action action
#if !NET20
			, [CallerMemberName]	string member = null
			, [CallerFilePath]		string file = null
			, [CallerLineNumber]	int line = 0
#endif
			)
		{
			var cti = PerThreadInfo.CurrentThread;
			var traceI = cti.CurrentTrace.Count;
			var depth = ++cti.CurrentDepth;
			if (depth > cti.CurrentMaxDepth) cti.CurrentMaxDepth = depth;
			var stackTrace = cti.CurrentForceStacks ? new StackTrace(true) : null; // We don't skip any frames in case Scope is inlined.
			var te = new TraceEntry()
			{
				LabelPayload	= label,
#if !NET20
				Caller			= new CallSite(member, file, line, stackTrace),
#else
				Caller			= new CallSite(stackTrace),
#endif
				DepthIndex			= depth-1,
			};
			cti.CurrentTrace.Add(te);

			te.Start = Stopwatch.GetTimestamp();
			action();
			te.Stop = Stopwatch.GetTimestamp();

			--cti.CurrentDepth;
			cti.CurrentTrace[traceI] = te;
			MaybeSyncLastTrace(cti);
		}

		public static DisposeScope Scope
			( string label
#if !NET20
			, [CallerMemberName]	string member = null
			, [CallerFilePath]		string file = null
			, [CallerLineNumber]	int line = 0
#endif
			)
		{
			var cti = PerThreadInfo.CurrentThread;
			var traceI = cti.CurrentTrace.Count;
			var depth = ++cti.CurrentDepth;
			if (depth > cti.CurrentMaxDepth) cti.CurrentMaxDepth = depth;
			var stackTrace = cti.CurrentForceStacks ? new StackTrace(true) : null; // We don't skip any frames in case Scope is inlined.
			var te = new TraceEntry()
			{
				LabelPayload	= label,
#if !NET20
				Caller			= new CallSite(member, file, line, stackTrace),
#else
				Caller			= new CallSite(stackTrace),
#endif
				DepthIndex			= depth-1,
			};
			cti.CurrentTrace.Add(te);

			te.Start = Stopwatch.GetTimestamp();
			return new DisposeScope(cti, traceI, te);
		}

		public struct DisposeScope : IDisposable
		{
			readonly PerThreadInfo	CTI;
			readonly int			TraceI;
			readonly TraceEntry		TraceEntry;

			internal DisposeScope(PerThreadInfo cti, int traceI, TraceEntry te)
			{
				CTI			= cti;
				TraceI		= traceI;
				TraceEntry	= te;
			}

			public void Dispose()
			{
				var stop = Stopwatch.GetTimestamp();

				var cti	= CTI;
				var te	= TraceEntry;
				te.Stop = stop;

				--cti.CurrentDepth;
				cti.CurrentTrace[TraceI] = te;
				MaybeSyncLastTrace(cti);
			}
		}

		static void MaybeSyncLastTrace(PerThreadInfo cti)
		{
			Debug.Assert(cti.Thread == Thread.CurrentThread);

			if (cti.CurrentDepth == 0)
			{
				lock (cti.Mutex)
				{
					Debug.Assert(cti.CurrentTrace.FirstOrDefault().Duration != 0);

					// Shuffle: Configuration -> CurrentTrace -> LastTrace
					cti.LastTrace.Clear();
					cti.LastTrace.AddRange(cti.CurrentTrace);
					cti.LastMaxDepth = cti.CurrentMaxDepth;

					cti.CurrentTrace.Clear();
					cti.CurrentForceStacks = cti.ConfigForceStacks;
				}
			}
		}
	}
}
