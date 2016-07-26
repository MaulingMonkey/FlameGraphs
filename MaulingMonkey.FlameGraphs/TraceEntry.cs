// Copyright (c) 2016 Michael B. Edwin Rickert
// Licensed to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MaulingMonkey.FlameGraphs.Gdi")]

namespace MaulingMonkey.FlameGraphs
{
	internal interface ITracePayload
	{
		string Label { get; }
	}

	internal struct TraceEntry
	{
		public ITracePayload	Payload			{ set { _Payload = value; } }
		public string			LabelPayload	{ set { _Payload = value; } }
		public string			Label			{ get { return _Payload == null ? null : _Payload as string ?? ((ITracePayload)_Payload).Label; } }

		object					_Payload; // null, string, or ITracePayload
		public CallSite			Caller;
		public long				Start;
		public long				Stop;
		public long				Duration { get { return Math.Max(0, Stop - Start); } }
		public int				DepthIndex;
	}
}
