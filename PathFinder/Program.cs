using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PathFinder
{
    class Program
    {

        public Program()
        {
            State state = new State("map.txt"); // State
            Location start = new Location(1, 1);
            Location dest = new Location(3, 13);
            List<Location> avoid = new List<Location>();

            List<Location> closed, open;
            List<Location> path = Pathfinding.FindPath(start, dest, state, avoid, out closed, out open);
            //List<Location> path = Pathfinding.GetNeighbours(dest, state);

            if (path == null)
                path = new List<Location>();

           
            for (int y = 0; y < state.Height; y++)
            {
                for (int x = 0; x < state.Width; x++)
                {
                    if (start.Equals(new Location(y, x)))
                        Console.Write("S");
                    else if (dest.Equals(new Location(y, x)))
                        Console.Write("E");
                    else if (path.Contains(new Location(y, x)))
                        Console.Write("*");
                    else if (state.Map[y, x])
                        Console.Write("=");
                    else if (closed.Contains(new Location(y, x)))
                        Console.Write(" ");
                    else if (open.Contains(new Location(y, x)))
                        Console.Write("+");
                    else
                        Console.Write(".");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
            Console.Write("Length = " + path.Count.ToString());
            Console.ReadLine();
        }

        static void Main()
        {
            new Program();
        }
    }
}
