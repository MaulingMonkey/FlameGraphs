// Copyright (c) 2016 Michael B. Edwin Rickert
// Licensed to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using System;

namespace MaulingMonkey.FlameGraphs
{
	public interface ITraceRecord
	{
		string Label { get; }
	}

	public struct TraceRecord : ITraceRecord
	{
		public TraceRecord(string label) { Label = label; }

		public string Label { get; private set; }
	}

	public struct LazyTraceRecord : ITraceRecord
	{
		public LazyTraceRecord(string format)										: this(format, 0, null, null, null, null) { }
		public LazyTraceRecord(string format, params object[] args)					: this(format,-1, args, null, null, null) { }
		public LazyTraceRecord(string format, object arg0)							: this(format, 1, null, arg0, null, null) { }
		public LazyTraceRecord(string format, object arg0, object arg1)				: this(format, 2, null, arg0, arg1, null) { }
		public LazyTraceRecord(string format, object arg0, object arg1, object arg2): this(format, 3, null, arg0, arg1, arg2) { }

		public string Label { get {
			switch (Arity)
			{
			case -1:	return string.Format(Format, Args);
			case 0:		return Format;
			case 1:		return string.Format(Format, Arg0);
			case 2:		return string.Format(Format, Arg0, Arg1);
			case 3:		return string.Format(Format, Arg0, Arg1, Arg2);
			default:	return "BUG: Invalid Arity";
			}
		} }

		LazyTraceRecord(string format, int arity, object[] args, object arg0, object arg1, object arg2)
		{
			Format	= format;
			Arity	= arity;
			Args	= args;
			Arg0	= arg0;
			Arg1	= arg1;
			Arg2	= arg2;
		}

		readonly string		Format;
		readonly int		Arity;
		readonly object[]	Args;
		readonly object		Arg0;
		readonly object		Arg1;
		readonly object		Arg2;
	}
}
