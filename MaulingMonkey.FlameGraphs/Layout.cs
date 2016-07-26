// Copyright (c) 2016 Michael B. Edwin Rickert
// Licensed to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace MaulingMonkey.FlameGraphs
{
	public class Layout
	{
		[Flags]
		internal enum UpdateFlags
		{
			Default			= 0x0,
			None			= 0x0,
			CullThreads		= 0x1,
			KeepThreadWorst	= 0x2,
		}

		readonly Dictionary<Thread,ThreadTraceCapture> Captures = new Dictionary<Thread, ThreadTraceCapture>();

		internal void Update(IEnumerable<ThreadInfo> infos, UpdateFlags flags) {
		using (Trace.Scope("Layout.Update"))
		{
			var keepThreads = new HashSet<Thread>();
			foreach (var info in infos)
			{
				ThreadTraceCapture capture;
				if (Captures.TryGetValue(info.Thread, out capture))
				{
					if ((flags & UpdateFlags.KeepThreadWorst) == UpdateFlags.None)
					{
						capture.Reset(info);
					}
					else if (capture.Trace.FirstOrDefault().Duration < info.LastTrace.FirstOrDefault().Duration)
					{
						capture.Reset(info);
					}
				}
				else
				{
					capture = new ThreadTraceCapture(info);
					Captures.Add(info.Thread, capture);
				}

				keepThreads.Add(info.Thread);
			}

			if ((flags & UpdateFlags.CullThreads) != UpdateFlags.None) foreach (var t in Captures.Keys.Except(keepThreads).ToArray()) Captures.Remove(t);
		}}

		public struct TextRect
		{
			public string Text;
			public Rect Area;
			public int Padding;
			public uint TextArgb, FillArgb, OutlineArgb;
		}

		public struct RenderArgs
		{
			public Rect					Target;
			public XY?					Cursor;
			public Action<TextRect>		RenderRect;
			public long					DurationX;
			public long					DurationW;
		}

		const uint White		= 0xFFFFFFFFu;
		const uint Yellow		= 0xFFFFFF00u;
		const uint Orange		= 0xFFFF8000u;
		const uint OrangeRed	= 0xFFFF4000u;
		const uint Black		= 0xFF000000u;
		const uint DarkBlue		= 0xFF000080u;
		const uint Transparent	= 0;

		public long Duration { get { return Captures.Values.Max(c => c.Trace.FirstOrDefault().Duration); } }

		public void Render(RenderArgs args) {
		using (Trace.Scope("Layout.Render"))
		{
			var target = args.Target;
			var cursor = args.Cursor ?? new XY(-10000,-10000);

			var formPad = new XY(5,5);
			var formSize = target.Size - 2*formPad;
			var barH = 12;
			var barHPad = 4;
			var y = formPad.Y;

			Action<TextRect> rect = r => {
				if (!target.Intersects(r.Area)) return;
				if (r.Area.Size != XY.Zero && (r.Area.Width == 0 || r.Area.Height == 0)) return;

				args.RenderRect(r);
			};

			TextRect? hover = null;
			foreach (var capture in Captures)
			if (capture.Value.Trace.Count > 0)
			{
				var thread = capture.Key;
				var trace = capture.Value.Trace;

				// Headers
				rect(new TextRect()
				{
					Text			= string.Format("Thread: {0}", thread == Thread.CurrentThread ? "Main Thread" : thread.Name ?? "<No Name>"),
					Area			= Rect.LTWH(formPad.X, y, 500, barH),
					FillArgb		= 0,
					TextArgb		= 0xFFFFFFFFu,
					OutlineArgb		= 0
				});
				y += barH + barHPad;

				// Bars
				var top = trace.FirstOrDefault();
				var duration = args.DurationW;
				y += (capture.Value.MaxDepth+1) * (barH + barHPad);

				foreach (var e in trace)
				{
					var l = formPad.X + (int)((e.Start - top.Start - args.DurationX) * formSize.X / args.DurationW);
					var r = formPad.X + (int)((e.Stop  - top.Start - args.DurationX) * formSize.X / args.DurationW);
					var b =                  (y - (e.Depth * (barH + barHPad)));
					var t =                  (b - barH);

					var area			= Rect.LTRB(l,t,r,b);
					var cursorHovering	= area.Contains(cursor);

					var fb = new TextRect()
					{
						Text			= !string.IsNullOrEmpty(e.Caller.Member) ? string.Format("{0} ({1})", e.Label ?? "", e.Caller.Member) : e.Label,
						Area			= area,
						TextArgb		= White,
						FillArgb		= cursorHovering ? Yellow : Orange,
						OutlineArgb		= cursorHovering ? Orange : OrangeRed,
					};
					rect(fb);

					if (cursorHovering)
					{
						var desc = new StringBuilder();

													desc.AppendFormat("Label:     {0}\n",			e.Label);
													desc.AppendFormat("Caller:    {0}\n",			e.Caller.Member);
						if (e.Caller.Column != 0)	desc.AppendFormat("File:      {0}({1},{2})\n",	e.Caller.File, e.Caller.Line, e.Caller.Column);
						else						desc.AppendFormat("File:      {0}({1})\n",		e.Caller.File, e.Caller.Line);
						if (e.Start != e.Stop)		desc.AppendFormat("Duration:  {0}\n",			ToShortTime(e.Duration));

						if (e.Caller.StackTrace != null)
						{
							desc.AppendLine();
							desc.AppendLine("Trace:");
							foreach (var frame in e.Caller.StackTrace.GetFrames())
							{
								var m	= string.Format("{0}.{1}+{2}", frame.GetMethod().DeclaringType.FullName, frame.GetMethod().Name, frame.GetNativeOffset());
								var cs	= string.IsNullOrEmpty(frame.GetFileName()) ? "<unknown>" : string.Format("{0}({1},{2})", frame.GetFileName(), frame.GetFileLineNumber(), frame.GetFileColumnNumber());
								desc.AppendFormat("    {0} @ {1}\n", m.PadRight(60, ' '), cs);
							}
							desc.AppendLine();
						}

						hover = new TextRect()
						{
							Text			= desc.ToString().TrimEnd('\r', '\n'),
							Area			= Rect.LTWH(cursor.X + 32, cursor.Y + 32, 0, 0),
							Padding			= 5,
							FillArgb		= 0xFF000000u,
							OutlineArgb		= 0xFFFFFFFFu,
							TextArgb		= 0xFFFFFFFFu,
						};
					}
				}

				y += barHPad;
			}

			if (hover != null) rect(hover.Value);
		}}

		static string ToShortTime(long ticks)
		{
			var freq = Stopwatch.Frequency;
			var s  = ticks * 1 / freq;
			var ms = ticks * 1000 / freq;
			var us = ticks * 1000000 / freq;
			var ns = ticks * 1000000000 / freq;

			if (s  > 9)	return (s .ToString("N0")+"s ").PadLeft(5+2, ' ');
			if (ms > 9)	return (ms.ToString("N0")+"ms").PadLeft(5+2, ' ');
			if (us > 9)	return (us.ToString("N0")+"us").PadLeft(5+2, ' ');
						return (ns.ToString("N0")+"ns").PadLeft(5+2, ' ');
		}
	}
}
