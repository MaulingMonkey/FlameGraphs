// Copyright (c) 2016 Michael B. Edwin Rickert
// Licensed to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MaulingMonkey.FlameGraphs.Gdi
{
	[System.ComponentModel.DesignerCategory("")]
	public class GdiFlameGraphDisplayForm : Form
	{
		public GdiFlameGraphDisplayForm()
		{
			ClientSize		= new Size(800, 600);
			DoubleBuffered	= true;
			Font			= new Font("Consolas", 8);
			Text			= "MaulingMonkey FlameGraphs";

			Application.Idle += Application_Idle;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Application.Idle -= Application_Idle;
			}
			base.Dispose(disposing);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			switch (e.KeyData)
			{
			case Keys.Space:
			case Keys.Pause:
				UpdatingFlameBars ^= true;
				break;
			case Keys.F1:
				HelpOverlay ^= true;
				break;
			case Keys.F2:
				FullStacks ^= true;
				break;
			}
			base.OnKeyDown(e);
		}

		private void Application_Idle(object sender, System.EventArgs e)
		{
			Invalidate();
		}

		// Note: We prepare layout seperately from actually rendering it to minimize the amount of time spent with thread.Mutex locked (e.g. not having GDI rendering overhead counting against it.)
		bool UpdatingFlameBars = true;
		bool HelpOverlay = false;
		bool FullStacks = false;
		struct TextRect { public string Text; public Rectangle Area; public int Padding; public Color TextColor, FillColor, OutlineColor; public TextFormatFlags TextFormatFlags; }
		readonly List<TextRect> TextRects = new List<TextRect>();
		long MaxDuration = 1;
		void UpdateFlameBars()
		{
			if (UpdatingFlameBars)
			Trace.Scope("Update Flame Bars", () =>
			{
				var threads = Trace.Threads;
				var formPadX = 5;
				var formPadY = 5;
				var formW = ClientSize.Width-2*formPadX;
				var formH = ClientSize.Height-2*formPadY;
				var barH = 12;
				var barHPad = 4;
				var y = formPadY;

				TextRect? hover = null;

				TextRects.Clear();
				foreach (var thread in threads)
				lock (thread.Mutex)
				if (thread.LastTrace.Count > 0)
				{
					// Headers
					TextRects.Add(new TextRect() { Text = string.Format("Thread: {0}", thread.Thread == Thread.CurrentThread ? "Main Thread" : thread.Thread.Name ?? "<No Name>"), Area = new Rectangle(formPadX, y, 500, barH), FillColor = Color.DarkGray, TextColor = Color.White, OutlineColor = Color.DarkGray });
					y += barH + barHPad;

					// Bars
					var top = thread.LastTrace[0];
					var duration = MaxDuration = Math.Max(MaxDuration, top.Duration);
					y += (thread.LastMaxDepth+1) * (barH + barHPad);

					foreach (var e in thread.LastTrace)
					{
						var l = formPadX + (int)((e.Start - top.Start) * formW / duration);
						var r = formPadX + (int)((e.Stop  - top.Start) * formW / duration);
						var b =                  (y - (e.Depth * (barH + barHPad)));
						var t =                  (b - barH);

						var area			= Rectangle.FromLTRB(l,t,r,b);
						var cursorPos		= PointToClient(Cursor.Position);
						var cursorHovering	= area.Contains(cursorPos);

						var fb = new TextRect()
						{
							Text			= !string.IsNullOrEmpty(e.Caller.Member) ? string.Format("{0} ({1})", e.Label ?? "", e.Caller.Member) : e.Label,
							Area			= area,
							TextColor		= Color.Black,
							FillColor		= cursorHovering ? Color.Yellow : Color.Orange,
							OutlineColor	= cursorHovering ? Color.Orange : Color.OrangeRed,
							TextFormatFlags	= TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix,
						};
						TextRects.Add(fb);

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
								Area			= new Rectangle(cursorPos.X + Cursor.Size.Width, cursorPos.Y + Cursor.Size.Height, 0, 0),
								Padding			= 5,
								FillColor		= Color.Black,
								OutlineColor	= Color.White,
								TextColor		= Color.White,
								TextFormatFlags	= TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPrefix | TextFormatFlags.ExpandTabs,
							};
						}
					}

					y += barHPad;
				}

				if (hover != null) TextRects.Add(hover.Value);
				if (HelpOverlay) TextRects.Add(new TextRect()
				{
					Text
						= "[F1]               Toggle this help display\n"
						+ "[F2]               Toggle capturing of full stack traces\n"
						+ "[Space] [Pause]    Toggle updating of the flame graph\n"
						.TrimEnd('\n'),
					Area			= new Rectangle(10,10,0,0),
					FillColor		= Color.DarkBlue,
					TextColor		= Color.White,
					OutlineColor	= Color.Black,
					Padding			= 5,
					TextFormatFlags	= TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPrefix | TextFormatFlags.ExpandTabs,
				});
			});
		}

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

		protected override void OnPaint(PaintEventArgs e)
		{
			Trace.Scope("OnPaint", () =>
			{
				foreach (var thread in Trace.Threads) lock (thread.Mutex) thread.ConfigForceStacks = FullStacks; // XXX: Awkward place for this.

				var font = Font;

				UpdateFlameBars();
				//using (var fx = e.Graphics)
				var fx = e.Graphics;
				{
					fx.Clear(Color.DarkGray);

					foreach (var tr in TextRects)
					using (var outline	= new Pen(tr.OutlineColor))
					using (var fill		= new SolidBrush(tr.FillColor))
					{
						var textArea = tr.Area;
						if (textArea.Size.IsEmpty) {
							var s = TextRenderer.MeasureText(fx, tr.Text, font, new Size(10000,10000), tr.TextFormatFlags);
							// TextRenderer.MeasureText underestimates
							//s.Width += 5;
							//s.Height += 5;
							textArea.Size = s;
						}
						var bgArea = textArea;
						bgArea.Inflate(tr.Padding, tr.Padding);

						fx.FillRectangle(fill, bgArea);
						TextRenderer.DrawText(fx, tr.Text, font, textArea, tr.TextColor, tr.FillColor, tr.TextFormatFlags);
						fx.DrawRectangle(outline, bgArea);
					}
				}
				base.OnPaint(e);
			});
		}
	}
}
