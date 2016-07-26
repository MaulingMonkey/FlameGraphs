// Copyright (c) 2016 Michael B. Edwin Rickert
// Licensed to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using System.Diagnostics;

namespace MaulingMonkey.FlameGraphs
{
	/// <summary>
	/// Simple rectangle.  Assumes Up/Left is negative, Down/Right is positive.
	/// </summary>
	[DebuggerDisplay("{Left},{Top} - {Right},{Bottom}")]
	public struct Rect
	{
		public static Rect LTRB(int left, int top, int right, int bottom) { return LTRB(new XY(left, top), new XY(right, bottom)); }
		public static Rect LTRB(XY leftTop, XY rightBottom)
		{
			return new Rect()
			{
				LeftTop		= leftTop,
				RightBottom	= rightBottom,
			};
		}
		public static Rect LTWH(int left, int top, int width, int height) { return LTRB(new XY(left, top), new XY(left + width, top + height)); }
		public static Rect LTWH(XY leftTop, XY size) { return LTRB(leftTop, leftTop + size); }

		public bool Contains(XY pos)
		{
			return (LeftTop.X <= pos.X && pos.X < RightBottom.X)
				&& (LeftTop.Y <= pos.Y && pos.Y < RightBottom.Y);
		}

		static private bool Overlap(int min1, int max1, int min2, int max2) { return !(max1 < min2) && !(max2 < min1); }
		public bool Intersects(Rect other)
		{
			return Overlap(Left, Right, other.Left, other.Right)
				&& Overlap(Top, Bottom, other.Top, other.Bottom);
		}

		public XY LeftTop, RightBottom;
		public XY Size		{ get { return RightBottom - LeftTop; } }
		public int Left		{ get { return LeftTop.X;		} set { LeftTop.X		= value; } }
		public int Right	{ get { return RightBottom.X;	} set { RightBottom.X	= value; } }
		public int Top		{ get { return LeftTop.Y;		} set { LeftTop.Y		= value; } }
		public int Bottom	{ get { return RightBottom.Y;	} set { RightBottom.Y	= value; } }
		public int Width	{ get { return Size.X; } }
		public int Height	{ get { return Size.Y; } }

		public Rect Inflated(int padding)	{ return LTRB(Left-padding  , Top-padding  , Right+padding  , Bottom+padding  ); }
		public Rect Inflated(XY padding)	{ return LTRB(Left-padding.X, Top-padding.Y, Right+padding.X, Bottom+padding.Y); }

		public static bool operator==(Rect lhs, Rect rhs)	{ return lhs.LeftTop == rhs.LeftTop && lhs.RightBottom == rhs.RightBottom; }
		public static bool operator!=(Rect lhs, Rect rhs)	{ return !(lhs == rhs); }
		public override bool Equals(object obj)				{ return base.Equals(obj); }
		public override int GetHashCode()					{ return base.GetHashCode(); }
	}
}
