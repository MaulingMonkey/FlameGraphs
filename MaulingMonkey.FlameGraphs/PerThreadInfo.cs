// Copyright (c) 2016 Michael B. Edwin Rickert
// Licensed to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MaulingMonkey.FlameGraphs
{
	/// <summary><para>
	/// Track various information per-thread for supporting the generation of flamegarph traces.
	/// This is accessed from multiple threads - typically the UI thread for sampling and rendering, and the 'Thread' this PerThreadInfo corresponds to for trace collection.
	/// Pay special attention to the thread safety requirements of the different fields.
	/// </para>
	/// <para>PerThreadInfo.Config*  generally represents the desired trace collection settings for this thread.  These may or may not take effect immediately.</para>
	/// <para>PerThreadInfo.Last*    generally represents the last full trace completed on the thread, which is ready for reading/display by the UI or other tools.</para>
	/// <para>PerThreadInfo.Current* generally represents the currently ongoing collection of tracing information of the thread, and should only be accessed by the Thread this PerThreadInfo belongs to.</para>
	/// </summary>
	internal class PerThreadInfo
	{
		// --------------------------------------------------------------------------------------------------------------------------------------------
		// Immutable - should be safe to access from any thread
		// --------------------------------------------------------------------------------------------------------------------------------------------

		/// <summary>
		/// <para>The thread that all this information corresponds to.</para>
		/// 
		/// <para>Thread safety:  Immutable.</para>
		/// </summary>
		public readonly Thread				Thread				= Thread.CurrentThread;

		/// <summary>
		/// <para>Used to protect Last* and Config* access.</para>
		/// 
		/// <para>Thread safety:  Immutable.  Lock from any thread.</para>
		/// </summary>
		public readonly object				Mutex				= new object();



		// --------------------------------------------------------------------------------------------------------------------------------------------
		// Thread shared - NOTE WELL: LOCK "Mutex" WHILE ACCESSING
		// --------------------------------------------------------------------------------------------------------------------------------------------

		readonly List<TraceEntry>	_LastTrace			= new List<TraceEntry>();
		int							_LastMaxDepth		= 0;
		bool						_ConfigForceStacks	= false;

		/// <summary>
		/// <para>The most recently completed trace of Thread, for display in your UI etc.</para>
		/// 
		/// <para>Thread safety:  Lock 'Mutex' first.</para>
		/// </summary>
		public List<TraceEntry> LastTrace {
			get { Debug.Assert(Monitor.IsEntered(Mutex), "Lock Mutex before accessing LastTrace"); return _LastTrace; }
		}

		/// <summary>
		/// <para>Currently the maximum number of nested scopes of LastTrace and earlier traces - e.g. if you had a scope inside a scope inside a scope at some point, this would be 3.</para>
		/// 
		/// <para>Thread safety:  Lock 'Mutex' first.</para>
		/// </summary>
		public int LastMaxDepth {
			get { Debug.Assert(Monitor.IsEntered(Mutex), "Lock Mutex before accessing LastMaxDepth"); return _LastMaxDepth; }
			set { Debug.Assert(Monitor.IsEntered(Mutex), "Lock Mutex before accessing LastMaxDepth"); _LastMaxDepth = value; }
		}

		/// <summary>
		/// <para>Wheither or not to collect a full stack trace for each scope event.  Applies to the next whole trace taken.  May be replaced with an optional TimeSpan in the future.</para>
		/// 
		/// <para>Thread safety:  Lock 'Mutex' first.</para>
		/// </summary>
		public bool ConfigForceStacks {
			get { Debug.Assert(Monitor.IsEntered(Mutex), "Lock Mutex before accessing ConfigForceStacks"); return _ConfigForceStacks; }
			set { Debug.Assert(Monitor.IsEntered(Mutex), "Lock Mutex before accessing ConfigForceStacks"); _ConfigForceStacks = value; }
		}



		// --------------------------------------------------------------------------------------------------------------------------------------------
		// Thread shared - NOTE WELL: LOCK "StaticMutex" WHILE ACCESSING
		// --------------------------------------------------------------------------------------------------------------------------------------------

		readonly static object				StaticMutex = new object();
		readonly static List<PerThreadInfo>	_All = new List<PerThreadInfo>();

		/// <summary>
		/// <para>Collect the PerThreadInfo s for all threads ever collected.  (Used e.g. by the UI to display all flame graphs.)</para>
		/// 
		/// <para>Thread safey:  Call from any thread.  Returned IEnumerable is local to thread - but you must still respect the thread safety rules of accessing the individual PerThreadInfo s.</para>
		/// </summary>
		public static IEnumerable<PerThreadInfo> All { get {
			lock (StaticMutex) return _All.ToArray(); // We create a new array that can be local to the calling thread.
		} }



		// --------------------------------------------------------------------------------------------------------------------------------------------
		// Thread local - NOTE WELL: ONLY ACCESS FROM THE SAME THREAD AS "Thread"
		// --------------------------------------------------------------------------------------------------------------------------------------------

		readonly List<TraceEntry>			_CurrentTrace		= new List<TraceEntry>();
		int									_CurrentDepth		= 0;
		int									_CurrentMaxDepth	= 0;
		bool								_CurrentForceStacks	= false;
		[ThreadStatic] static PerThreadInfo _CurrentThread;

		/// <summary>
		/// <para>The accumulating scope events, for the partial currently-in-progress trace, for 'Thread'.</para>
		/// 
		/// <para>Thread safey:  Call only from 'Thread' (asserted in debug)</para>
		/// </summary>
		public List<TraceEntry> CurrentTrace {
			get { Debug.Assert(Thread == Thread.CurrentThread, "CurrentTrace should only be accessed from the Thread it belongs to!"); return _CurrentTrace; }
		}

		/// <summary>
		/// <para>How many Trace.Scope()s 'Thread' is in, right now.</para>
		/// 
		/// <para>Thread safey:  Call only from 'Thread' (asserted in debug)</para>
		/// </summary>
		public int CurrentDepth {
			get { Debug.Assert(Thread == Thread.CurrentThread, "CurrentDepth should only be accessed from the Thread it belongs to!"); return _CurrentDepth; }
			set { Debug.Assert(Thread == Thread.CurrentThread, "CurrentDepth should only be accessed from the Thread it belongs to!"); _CurrentDepth = value; }
		}

		/// <summary>
		/// <para>Currently the maximum number of nested scopes of CurrentTrace and earlier traces - e.g. if you had a scope inside a scope inside a scope at some point, this would be 3.</para>
		/// 
		/// <para>Thread safey:  Call only from 'Thread' (asserted in debug)</para>
		/// </summary>
		public int CurrentMaxDepth {
			get { Debug.Assert(Thread == Thread.CurrentThread, "CurrentMaxDepth should only be accessed from the Thread it belongs to!"); return _CurrentMaxDepth; }
			set { Debug.Assert(Thread == Thread.CurrentThread, "CurrentMaxDepth should only be accessed from the Thread it belongs to!"); _CurrentMaxDepth = value; }
		}

		/// <summary>
		/// <para>Wheither or not to collect a full stack trace for each scope event.  Applies to the currently capturing 'CurrentTrace'.  Syncronize to 'ConfigureForceStacks' at the end of the trace, or the beginning of the next one.</para>
		/// 
		/// <para>Thread safey:  Call only from 'Thread' (asserted in debug)</para>
		/// </summary>
		public bool CurrentForceStacks {
			get { Debug.Assert(Thread == Thread.CurrentThread, "CurrentForceStacks should only be accessed from the Thread it belongs to!"); return _CurrentForceStacks; }
			set { Debug.Assert(Thread == Thread.CurrentThread, "CurrentForceStacks should only be accessed from the Thread it belongs to!"); _CurrentForceStacks = value; }
		}

		/// <summary>
		/// <para>Get a PerThreadInfo corresponding to the thread you're currently calling this on.</para>
		/// 
		/// <para>Thread safey:  Call from any thread (results local to thread)</para>
		/// </summary>
		public static PerThreadInfo CurrentThread { get {
			var cti = _CurrentThread;
			if (cti == null)
			{
				lock (StaticMutex) // Needed for _All
				{
					cti = new PerThreadInfo();
					_CurrentThread = cti;
					_All.Add(cti);
				}
			}
			return cti;
		}}
	}
}
