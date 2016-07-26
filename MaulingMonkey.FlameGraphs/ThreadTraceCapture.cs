// Copyright (c) 2016 Michael B. Edwin Rickert
// Licensed to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MaulingMonkey.FlameGraphs
{
	/// <summary>
	/// Represents a snapshot or capture of another thread.
	/// 
	/// Avoid mutating (except through Reset, which exists mainly as a minor optimization in reusing Trace's allocation)
	/// </summary>
	[DebuggerDisplay("{ThreadName} - {Trace.Count} trace")]
	internal class ThreadTraceCapture
	{
		public IEnumerable<TraceEntry>		Trace		{ get { return _Trace.AsReadOnly(); } }
		readonly List<TraceEntry>			_Trace = new List<TraceEntry>();
		public string						ThreadName	{ get; private set; }
		public Thread						Thread		{ get; private set; }
		public int							MaxDepth	{ get; private set; }

		public ThreadTraceCapture(PerThreadInfo info)
		{
			Reset(info);
		}

		public void Reset(PerThreadInfo info)
		{
			lock (info.Mutex)
			{
				_Trace.Clear();
				_Trace.AddRange(info.LastTrace);
				ThreadName	= info.Thread.Name;
				Thread		= info.Thread;
				MaxDepth	= info.LastMaxDepth;
			}
		}
	}
}
