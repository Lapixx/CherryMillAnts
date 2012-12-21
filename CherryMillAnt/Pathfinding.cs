using System;
using System.Collections.Generic;
using Ants;

public static class Pathfinding
{
    public static Location FindNextLocation(Location start, Location dest, IGameState state, List<Location> avoid, LayeredInfluenceMap heat)
    {
        if (start.Equals(dest) || !state.GetIsPassable(dest) || avoid.Contains(dest))
            return null;

        List<Location> list = FindPath(start, dest, state, avoid, heat);
        if (list != null)
            return list[0];
        else
            return null;
    }

    public static bool ReachableWithin(Location start, Location dest, IGameState state, int maxDepth)
    {
        return FindPath(start, dest, state, new List<Location>(), new LayeredInfluenceMap(state), maxDepth) != null;
    }
    
    // Returns a list of tiles that form the shortest path between start and dest
    public static List<Location> FindPath(Location start, Location dest, IGameState state, List<Location> avoid, LayeredInfluenceMap heat = null, int maxDepth = int.MaxValue)
    {
        if (start.Equals(dest) || !state.GetIsPassable(dest) || avoid.Contains(dest))
            return null;

        HashSet<string> closed = new HashSet<string>();
        Dictionary<string, PathfindNode> locToNode = new Dictionary<string, PathfindNode>();
        List<PathfindNode> open = new List<PathfindNode>();

        List<Location> reachable;
        
        PathfindNode first = new PathfindNode(start, null, dest, state, heat[start]);
        open.Add(first);
        locToNode.Add(MyBot.LocationToKey(first.Position), first);

        // Repeat until the destination node is reached
        PathfindNode last = null;
        while (open.Count > 0)
        {
            if (state.TimeRemaining < 10)
            {
                MyBot.LogShit("timeout.txt", "stop B - " + state.TimeRemaining);
                return null;
            }

            // Search the best available tile (lowest cost to reach from start, closest to dest)
            
            PathfindNode best = null;
            foreach (PathfindNode next in open)
            {
                if (best == null)
                    best = next;

                if (next.F < best.F)
                    best = next;
            }

            if (best.G > maxDepth)
                return null;
            
            //PathfindNode best = open.Min;

            // Move to closed list
            open.Remove(best);
            locToNode.Remove(MyBot.LocationToKey(best.Position));
            closed.Add(MyBot.LocationToKey(best.Position));

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

                if(locToNode.ContainsKey(lid))
                {
                    pfn = locToNode[lid];
                    if (best.G + 1 < pfn.G)
                        pfn.Parent = best; 
                }
                else{
                    pfn = new PathfindNode(next, best, dest, state, heat[next]);
                    open.Add(pfn);
                    locToNode.Add(lid, pfn);
                }                     
            }
        }

        if (last == null)
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

    static List<Location> GetNeighbours(Location loc, IGameState state)
    {
        List<Location> neighbours = new List<Location>();
        for (int i = 0; i < 4; i++)
            neighbours.Add(state.GetDestination(loc, (Direction)i));
        return neighbours;
    }
}

class PathfindNode
{
    public Location Position;
    public PathfindNode Parent;

    public float H; // Estimated cost to reach destination

    public float E = 0; // Added costs

    public float G // Cost to reach node from start
    {
        get
        {
            if (Parent == null)
                return 0;
            else
                return Parent.G + 1;
        }
    }

    public float F // G + H
    {
        get
        {
            return (G + H + E);
        }
    }

    public PathfindNode(Location position, PathfindNode parent, Location destination, IGameState state, float heat)
    {
        this.Position = position;
        this.Parent = parent;
        this.E = heat;
        this.H = state.GetDistance(position, destination);
    }
}