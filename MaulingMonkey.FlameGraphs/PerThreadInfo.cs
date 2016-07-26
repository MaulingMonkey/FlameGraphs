// Copyright (c) 2016 Michael B. Edwin Rickert
// Licensed to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MaulingMonkey.FlameGraphs
{
	internal class PerThreadInfo
	{
		// --------------------------------------------------------------------------------------------------------------------------------------------
		// Immutable - should be safe to access from any thread
		// --------------------------------------------------------------------------------------------------------------------------------------------

		public readonly Thread				Thread				= Thread.CurrentThread;
		public readonly object				Mutex				= new object();



		// --------------------------------------------------------------------------------------------------------------------------------------------
		// Thread shared - NOTE WELL: LOCK "Mutex" WHILE ACCESSING
		// --------------------------------------------------------------------------------------------------------------------------------------------

		readonly List<TraceEntry>	_LastTrace			= new List<TraceEntry>();
		int							_LastMaxDepth		= 0;
		bool						_ConfigForceStacks	= false;

		/// <summary>
		/// Thread safety:  Lock 'Mutex' first.
		/// </summary>
		public List<TraceEntry> LastTrace {
			get { Debug.Assert(Monitor.IsEntered(Mutex), "Lock Mutex before accessing LastTrace"); return _LastTrace; }
		}

		/// <summary>
		/// Thread safety:  Lock 'Mutex' first.
		/// </summary>
		public int LastMaxDepth {
			get { Debug.Assert(Monitor.IsEntered(Mutex), "Lock Mutex before accessing LastMaxDepth"); return _LastMaxDepth; }
			set { Debug.Assert(Monitor.IsEntered(Mutex), "Lock Mutex before accessing LastMaxDepth"); _LastMaxDepth = value; }
		}

		/// <summary>
		/// Thread safety:  Lock 'Mutex' first.
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
		/// Thread safey:  Call from any thread
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
		/// Thread safey:  Call only from 'Thread' (asserted in debug)
		/// </summary>
		public List<TraceEntry> CurrentTrace {
			get { Debug.Assert(Thread == Thread.CurrentThread, "CurrentTrace should only be accessed from the Thread it belongs to!"); return _CurrentTrace; }
		}

		/// <summary>
		/// Thread safey:  Call only from 'Thread' (asserted in debug)
		/// </summary>
		public int CurrentDepth {
			get { Debug.Assert(Thread == Thread.CurrentThread, "CurrentDepth should only be accessed from the Thread it belongs to!"); return _CurrentDepth; }
			set { Debug.Assert(Thread == Thread.CurrentThread, "CurrentDepth should only be accessed from the Thread it belongs to!"); _CurrentDepth = value; }
		}

		/// <summary>
		/// Thread safey:  Call only from 'Thread' (asserted in debug)
		/// </summary>
		public int CurrentMaxDepth {
			get { Debug.Assert(Thread == Thread.CurrentThread, "CurrentMaxDepth should only be accessed from the Thread it belongs to!"); return _CurrentMaxDepth; }
			set { Debug.Assert(Thread == Thread.CurrentThread, "CurrentMaxDepth should only be accessed from the Thread it belongs to!"); _CurrentMaxDepth = value; }
		}

		/// <summary>
		/// Thread safey:  Call only from 'Thread' (asserted in debug)
		/// </summary>
		public bool CurrentForceStacks {
			get { Debug.Assert(Thread == Thread.CurrentThread, "CurrentForceStacks should only be accessed from the Thread it belongs to!"); return _CurrentForceStacks; }
			set { Debug.Assert(Thread == Thread.CurrentThread, "CurrentForceStacks should only be accessed from the Thread it belongs to!"); _CurrentForceStacks = value; }
		}

		/// <summary>
		/// Thread safey:  Call from any thread (results local to thread)
		/// </summary>
		public static PerThreadInfo CurrentThread { get {
			var cti = _CurrentThread;
			if (cti == null)
			{
				lock (StaticMutex)
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
