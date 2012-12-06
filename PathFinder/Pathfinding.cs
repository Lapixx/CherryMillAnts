using System;
using System.Collections.Generic;

namespace PathFinder
{
    public static class Pathfinding
    {
        public static Location FindNextLocation(Location start, Location dest, State state, List<Location> avoid)
        {
            if (start.Equals(dest))
                return null;
            if (!state.GetIsPassable(dest))
                return null;
            if (avoid.Contains(dest))
                return null;
            List<Location> asfasf,asfasf2;
            List<Location> list = FindPath(start, dest, state, avoid, out asfasf, out asfasf2);
            if (list != null)
                return list[0];
            else
                return null;
        }

        // Returns a list of tiles that form the shortest path between start and dest
        public static List<Location> FindPath(Location start, Location dest, State state, List<Location> avoid, out List<Location> closedOut, out List<Location> openOut)
        {
            closedOut = new List<Location>();
            openOut = new List<Location>();

            /*
            List<PathfindNode> open = new List<PathfindNode>();
            List<PathfindNode> closed = new List<PathfindNode>();
            */

            //LinkedList<PathfindNode> open = new LinkedList<PathfindNode>();
            //HashSet<PathfindNode> open = new HashSet<PathfindNode>();
            HashSet<string> closed = new HashSet<string>();
            Dictionary<string, PathfindNode> locToNode = new Dictionary<string, PathfindNode>();
            List<PathfindNode> open = new List<PathfindNode>();

            List<Location> reachable;
            // Starting node

            /*
            closed.Add(first);

            // Add all reachable tiles to the Open list.

            foreach (Location next in reachable)
            {
                if (state.GetIsPassable(next)) // Check if tile is free
                    open.Add(new PathfindNode(next, first, dest));
            }
            */
            PathfindNode first = new PathfindNode(start, null, dest, state);
            open.Add(first);
            openOut.Add(start);
            locToNode.Add(MyBot.LocationToKey(first.Position), first);

            // Repeat until the destination node is reached
            PathfindNode last = null;
            while (open.Count > 0)
            {


                for (int y = 0; y < state.Height; y++)
                {
                    for (int x = 0; x < state.Width; x++)
                    {
                        if (start.Equals(new Location(y, x)))
                            Console.Write("S");
                        else if (dest.Equals(new Location(y, x)))
                            Console.Write("E");
                        else if (state.Map[y, x])
                            Console.Write("=");
                        else if (closedOut.Contains(new Location(y, x)))
                            Console.Write(" ");
                        else if (openOut.Contains(new Location(y, x)))
                            Console.Write("+");
                        else
                            Console.Write(".");
                    }
                    Console.WriteLine();
                }
                Console.WriteLine(); Console.WriteLine(); Console.ReadLine();



                // Search the best available tile (lowest cost to reach from start, closest to dest)
                
                PathfindNode best = null;
                foreach (PathfindNode next in open)
                {
                    if (best == null)
                        best = next;

                    if (next.F < best.F)
                        best = next;
                }

                foreach (PathfindNode q in open)
                {
                    if (best == q)
                        Console.Write("+");
                    else
                        Console.Write(" ");
                    Console.WriteLine(" [" + q.Position.Row + ", "+ q.Position.Col + "] " + q.G + ", " + q.H + " ("+q.F+")");
                }
                

                //PathfindNode best = open.Min;

                // Move to closed list
                Console.WriteLine("open.count - = " + open.Count);
                open.Remove(best);
                Console.WriteLine("open.count - = " + open.Count);
                locToNode.Remove(MyBot.LocationToKey(best.Position));
                openOut.Remove(best.Position);
                closed.Add(MyBot.LocationToKey(best.Position));
                closedOut.Add(best.Position);

                Console.WriteLine(best.G + " + " + best.H + " = " + best.F);


                if (best.Position.Equals(dest)) // Destination added to closed list - almost done!
                {
                    last = best;
                    break;
                }

                // Find tiles adjacent to this tile
                reachable = GetNeighbours(best.Position, state);
                string lid;
                PathfindNode pfn;
                foreach (Location next in reachable)
                {
                    if (!state.GetIsPassable(next) || avoid.Contains(next)) // Check if tile is blocked
                        continue;

                    lid = MyBot.LocationToKey(next);

                    if (closed.Contains(lid))
                        continue;

                    if (locToNode.ContainsKey(lid))
                    {
                        pfn = locToNode[lid];
                        if (best.G + 1 < pfn.G)
                            pfn.Parent = best;
                    }
                    else
                    {
                        pfn = new PathfindNode(next, best, dest, state);
                        Console.WriteLine("open.count + = " + open.Count);
                        open.Add(pfn);
                        Console.WriteLine("open.count + = " + open.Count);
                        locToNode.Add(lid, pfn);
                        openOut.Add(pfn.Position);
                    }
                }
            }

            if (last == null)//(!Location.Equals(last.Position, dest))
                return null;
            // Trace the route from destination to start (using each node's parent property)
            List<PathfindNode> route = new List<PathfindNode>();
            while (last != first && last != null)
            {
                route.Add(last);
                last = last.Parent;
            }

            // Reverse route and convert to Points
            List<Location> path = new List<Location>();
            for (int i = route.Count - 1; i >= 0; i--)
            {
                path.Add(route[i].Position);
            }

            // Return the list of Points
            return path;
        }

        public static List<Location> GetNeighbours(Location loc, State state)
        {
            List<Location> neighbours = new List<Location>();
            for (int i = 3; i >= 0; i--)
                neighbours.Add(state.GetDestination(loc, (Direction)i));
            return neighbours;
        }
    }

    class PathfindNode : IComparable<PathfindNode>
    {
        public Location Position;
        public PathfindNode Parent;

        public int H; // Estimated cost to reach destination

        public int G // Cost to reach node from start
        {
            get
            {
                if (Parent == null)
                    return 0;
                else
                    return (Parent.G + 1);
            }
        }

        public int F // G + H
        {
            get
            {
                return (G + H);
            }
        }

        public PathfindNode(Location position, PathfindNode parent, Location destination, State state)
        {
            this.Position = position;
            this.Parent = parent;
            this.H = state.GetDistance(position, destination);
        }



        public int CompareTo(PathfindNode pfn2)
        {
            if (F < pfn2.F)
                return -1;
            else if (pfn2.F < F)
                return 1;
            return 0;
        }

        public override bool Equals(Object pfn)
        {
            PathfindNode pfn2 = pfn as PathfindNode;
            return (Position.Row == pfn2.Position.Row && Position.Col == pfn2.Position.Col);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Position.Row * 397) ^ Position.Col;
            }
        }
    }
}