// Copyright (c) 2016 Michael B. Edwin Rickert
// Licensed to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace MaulingMonkey.FlameGraphs.Gdi
{
	[System.ComponentModel.DesignerCategory("")]
	public class GdiFlameGraphDisplayForm : Form
	{
		bool _AutoRefresh;

		public bool AutoRefresh
		{
			get
			{
				return _AutoRefresh;
			}
			set
			{
				if (value == _AutoRefresh) return;
				_AutoRefresh = value;
				if (value)	Application.Idle += Application_Idle;
				else		Application.Idle -= Application_Idle;
			}
		}

		public GdiFlameGraphDisplayForm()
		{
			ClientSize		= new Size(800, 600);
			DoubleBuffered	= true;
			Font			= new Font("Consolas", 8);
			Text			= "MaulingMonkey FlameGraphs";
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
			case Keys.Space:	UpdatingFlameBars ^= true; break;
			case Keys.Pause:	UpdatingFlameBars ^= true; break;
			case Keys.F1:		HelpOverlay ^= true; break;
			case Keys.F2:		FullStacks ^= true; break;
			case Keys.F3:		KeepWorst ^= true; break;
			}
			base.OnKeyDown(e);
		}

		private void Application_Idle(object sender, System.EventArgs e)
		{
			Invalidate();
		}

		new readonly Layout Layout = new Layout();

		bool UpdatingFlameBars	= true;
		bool HelpOverlay		= false;
		bool FullStacks			= false;
		bool KeepWorst			= false;
		long MaxDuration		= 1;

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

		const uint Black		= 0xFF000000u;
		const uint DarkBlue		= 0xFF000080u;

		void RenderRect(Graphics fx, Layout.TextRect rect)
		{
			var font = Font;
			var textFormatFlags = TextFormatFlags.Default;
			var textArea = rect.Area;
			if (textArea.Size == XY.Zero) {
				var s = TextRenderer.MeasureText(fx, rect.Text, font, new Size(10000,10000), textFormatFlags);
				textArea = Rect.LTWH(textArea.LeftTop, new XY(s.Width, s.Height));
			}
			var bgArea = textArea.Inflated(rect.Padding);

			if (rect.FillArgb != 0) using (var fill = new SolidBrush(Color.FromArgb(unchecked((int)rect.FillArgb)))) fx.FillRectangle(fill, bgArea.Left, bgArea.Top, bgArea.Width, bgArea.Height);
			if (rect.TextArgb != 0) TextRenderer.DrawText(fx, rect.Text, font, new Rectangle(textArea.Left, textArea.Top, textArea.Width, textArea.Height), Color.FromArgb(unchecked((int)rect.TextArgb)), Color.Transparent, textFormatFlags);
			if (rect.OutlineArgb != 0) using (var outline = new Pen(Color.FromArgb(unchecked((int)rect.OutlineArgb)))) fx.DrawRectangle(outline, new Rectangle(bgArea.Left, bgArea.Top, bgArea.Width, bgArea.Height));
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Trace.Scope("OnPaint", () =>
			{
				foreach (var thread in Trace.Threads) lock (thread.Mutex) thread.ConfigForceStacks = FullStacks; // XXX: Awkward place for this.

				using (Trace.Scope("Layout.Update")) if (UpdatingFlameBars) Layout.Update(Trace.Threads, KeepWorst ? Layout.UpdateFlags.KeepThreadWorst : Layout.UpdateFlags.Default);
				MaxDuration = Math.Max(MaxDuration, Layout.Duration);

				var cursor = PointToClient(Cursor.Position);

				var fx = e.Graphics;
				fx.Clear(Color.DarkGray);

				using (Trace.Scope("Layout.Render"))
				Layout.Render(new Layout.RenderArgs()
				{
					Cursor = new XY(cursor.X, cursor.Y),
					DurationX = 0,
					DurationW = MaxDuration,
					Target = Rect.LTWH(0,0,ClientSize.Width,ClientSize.Height),
					RenderRect = rect => RenderRect(fx, rect),
				});

				if (HelpOverlay) RenderRect(fx, new Layout.TextRect()
				{
					Text
						= "[F1]               Toggle this help display\n"
						+ "[F2]               Toggle capturing of full stack traces\n"
						+ "[F3]               Toggle between keeping the worst capture of a thread and the most recent\n"
						+ "[Space] [Pause]    Toggle updating of the flame graph\n"
						.TrimEnd('\n'),
					Area			= Rect.LTWH(10,10,0,0),
					FillArgb		= DarkBlue,
					TextArgb		= 0xFFFFFFFFu,
					OutlineArgb		= Black,
					Padding			= 5,
				});

				base.OnPaint(e);
			});
		}
	}
}
