// Copyright (c) 2016 Michael B. Edwin Rickert
// Licensed to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MaulingMonkey.FlameGraphs
{
	internal class ThreadInfo
	{
		// Immutable - should be safe to access from any thread
		public readonly Thread				Thread				= Thread.CurrentThread;
		public readonly object				Mutex				= new object();

		// Thread shared - NOTE WELL: LOCK "Mutex" WHILE ACCESSING
		public readonly List<TraceEntry>	LastTrace			= new List<TraceEntry>();
		public int							LastMaxDepth		= 0;
		public bool							ConfigForceStacks	= false;

		// Thread local - NOTE WELL: ONLY ACCESS FROM THE SAME THREAD AS "Thread"
		public readonly List<TraceEntry>	CurrentTrace		= new List<TraceEntry>();
		public int							CurrentDepth		= 0;
		public int							CurrentMaxDepth		= 0;
		public bool							CurrentForceStacks	= false;
	}

	public static class Trace
	{
		readonly static object Mutex = new object();
		readonly static List<ThreadInfo> AllThreadInfos = new List<ThreadInfo>();

		[ThreadStatic] static ThreadInfo _CurrentThreadInfo;
		static ThreadInfo CurrentThreadInfo { get {
			var cti = _CurrentThreadInfo;
			if (cti == null)
			{
				lock (Mutex)
				{
					_CurrentThreadInfo = cti = new ThreadInfo();
					AllThreadInfos.Add(cti);
				}
			}
			return cti;
		}}

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
			var cti = CurrentThreadInfo;
			var traceI = cti.CurrentTrace.Count;
			var depth = cti.CurrentDepth++;
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
				Depth			= depth,
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
			var cti = CurrentThreadInfo;
			var traceI = cti.CurrentTrace.Count;
			var depth = cti.CurrentDepth++;
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
				Depth			= depth,
			};
			cti.CurrentTrace.Add(te);

			te.Start = Stopwatch.GetTimestamp();
			return new DisposeScope(cti, traceI, te);
		}

		public struct DisposeScope : IDisposable
		{
			readonly ThreadInfo	CTI;
			readonly int		TraceI;
			readonly TraceEntry	TraceEntry;

			internal DisposeScope(ThreadInfo cti, int traceI, TraceEntry te)
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

		internal static IEnumerable<ThreadInfo> Threads { get {
			lock (Mutex) return AllThreadInfos.ToArray();
		} }

		static void MaybeSyncLastTrace(ThreadInfo cti)
		{
			Debug.Assert(cti.Thread == Thread.CurrentThread);

			if (cti.CurrentDepth == 0)
			{
				lock (cti.Mutex)
				{
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
