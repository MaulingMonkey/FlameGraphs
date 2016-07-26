// Copyright (c) 2016 Michael B. Edwin Rickert
// Licensed to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using System.Diagnostics;

namespace MaulingMonkey.FlameGraphs
{
	[DebuggerDisplay("{X},{Y}")]
	public struct XY
	{
		public XY(int x, int y) { X = x; Y = y; }

		public int X, Y;

		public static readonly XY Zero = new XY(0,0);

		public static XY operator+(XY lhs, XY rhs) { return new XY(lhs.X+rhs.X, lhs.Y+rhs.Y); }
		public static XY operator-(XY lhs, XY rhs) { return new XY(lhs.X-rhs.X, lhs.Y-rhs.Y); }
		public static XY operator*(int s, XY xy) { return new XY(xy.X*s, xy.Y*s); }
		public static XY operator*(XY xy, int s) { return new XY(xy.X*s, xy.Y*s); }
		public static XY operator/(XY xy, int s) { return new XY(xy.X/s, xy.Y/s); }

		public static bool operator==(XY lhs, XY rhs) { return lhs.X == rhs.X && lhs.Y == rhs.Y; }
		public static bool operator!=(XY lhs, XY rhs) { return !(lhs == rhs); }
		public override bool Equals(object obj) { return base.Equals(obj); }
		public override int GetHashCode() { return base.GetHashCode(); }
	}
}
