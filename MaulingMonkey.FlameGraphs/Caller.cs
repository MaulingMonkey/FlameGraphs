// Copyright (c) 2016 Michael B. Edwin Rickert
// Licensed to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

namespace MaulingMonkey.FlameGraphs
{
	internal struct CallSite
	{
		public readonly string		Member;		// Likely "unknown" (should never be empty unless not created via constructor)
		public readonly string		File;		// Likely "" (should never be null unless not created via constructor)
		public readonly int			Line;		// Likely 0 (trace may not have full source info)
		public readonly int			Column;		// Likely 0 (only captured via trace, which may not have full source info)
		public readonly StackTrace	StackTrace;	// Likely null (Unless ConfigForceStacks is enabled, a trace is not needed - [Caller*Attribute] will gather enough information.

		static readonly Assembly SelfAssembly = Assembly.GetExecutingAssembly();

		/// <summary>
		/// Meant for use with System.Runtime.CompilerServices.Caller*Attribute
		/// </summary>
		/// <param name="member">Pass [CallerMemberName]</param>
		/// <param name="file">Pass [CallerFilePath]</param>
		/// <param name="line">Pass [CallerLineNumber]</param>
		/// <param name="trace">Optional stack trace for additional information.</param>
		public CallSite(string member, string file, int line, StackTrace trace)
		{
			Member		= member ?? "unknown";
			File		= file ?? "";
			Line		= line;
			Column		= 0;
			StackTrace	= trace;

			// Only infer Member/File/Line/Column from trace if all the values were bad/default (e.g. the compiler consuming our library doesn't recognize/support [Caller*])
			if (member == null && file == null && line == 0 && trace != null)
			{
				var n = trace.FrameCount;
				for (var i=0; i<n; ++i)
				{
					var frame = trace.GetFrame(i);
					var method = frame.GetMethod();
					if (method.DeclaringType.Assembly == SelfAssembly) continue; // Skip over this frame - belongs to MaulingMonkey.FlameGraphs

					Member	= string.Format("{0} + {1}", method.Name ?? "unknown", frame.GetNativeOffset());
					File	= frame.GetFileName() ?? "";
					Line	= frame.GetFileLineNumber();
					Column	= frame.GetFileColumnNumber();
					return;
				}
			}
		}

		public CallSite(StackTrace trace)
		{
			Member		= "unknown";
			File		= "";
			Line		= 0;
			Column		= 0;
			StackTrace	= trace;

			if (trace != null)
			{
				var n = trace.FrameCount;
				for (var i=0; i<n; ++i)
				{
					var frame = trace.GetFrame(i);
					var method = frame.GetMethod();
					if (method.DeclaringType.Assembly == SelfAssembly) continue; // Skip over this frame - belongs to MaulingMonkey.FlameGraphs

					Member	= string.Format("{0} + {1}", method.Name ?? "unknown", frame.GetNativeOffset());
					File	= frame.GetFileName() ?? "";
					Line	= frame.GetFileLineNumber();
					Column	= frame.GetFileColumnNumber();
					return;
				}
			}
		}
	}
}
