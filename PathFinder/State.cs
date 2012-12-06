using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PathFinder
{
    public class State
    {
        public int Width;
        public int Height;

        public bool[,] Map;

        public State(String fname)
        {
            StreamReader sr = new StreamReader(fname);
            List<string> lines = new List<string>();
            string line;
            while((line = sr.ReadLine()) != null)
            {
                lines.Add(line);
            }
            Height = lines.Count;
            Width = lines[0].Length;
            Map = new bool[Height, Width];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (lines[y][x] != '.')
                        Map[y, x] = true;
                }
            }
        }

        public bool GetIsPassable(Location loc)
        {
            return !Map[loc.Row, loc.Col];
        }

        public static readonly Location North = new Location(-1, 0);
        public static readonly Location South = new Location(1, 0);
        public static readonly Location West = new Location(0, -1);
        public static readonly Location East = new Location(0, 1);

        public static IDictionary<Direction, Location> Aim = new Dictionary<Direction, Location> {
			{ Direction.North, North},
			{ Direction.East, East},
			{ Direction.South, South},
			{ Direction.West, West}
		};

        public Location GetDestination(Location location, Direction direction)
        {
            Location delta = State.Aim[direction];

            int row = (location.Row + delta.Row) % Height;
            if (row < 0) row += Height; // because the modulo of a negative number is negative

            int col = (location.Col + delta.Col) % Width;
            if (col < 0) col += Width;

            return new Location(row, col);
        }

        public int GetDistance(Location loc1, Location loc2)
        {
            int d_row = Math.Abs(loc1.Row - loc2.Row);
            d_row = Math.Min(d_row, Height - d_row);

            int d_col = Math.Abs(loc1.Col - loc2.Col);
            d_col = Math.Min(d_col, Width - d_col);

            return d_row + d_col;
        }
    }
}
