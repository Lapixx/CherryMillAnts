using System;
using System.Collections.Generic;
using System.IO;

namespace Ants
{
	class MyBot : Bot
    {
        Random random;
        List<Location> myHills, theirHills, yummies, timRobbins, martinSheen;
        int viewRadius;
        Dictionary<string, CurrentTask> currentTasks;
        Dictionary<string, CurrentTask> nextTasks;
        Dictionary<string, List<CurrentTask>> conflictingTasks;
        HeatMap heatMap;
        int foodRadius = 0;
        int raidRadius = 0;
        int arrivalRadius = 3;
        int currentStep = 0;

		// DoTurn is run once per turn
		public override void DoTurn (IGameState state)
        {
            //Console.WriteLine("{0} ants", state.MyAnts.Count);

            currentStep++;

            if (myHills == null)
            {
                random = new Random();
                currentTasks = new Dictionary<string, CurrentTask>();
                nextTasks = new Dictionary<string, CurrentTask>();
                myHills = new List<Location>(state.MyHills);
                theirHills = new List<Location>();
                yummies = new List<Location>();
                timRobbins = new List<Location>();
                martinSheen = new List<Location>();
                viewRadius = (int)Math.Sqrt(state.ViewRadius2);
                heatMap = new HeatMap(viewRadius, state);

                foreach (Location h in myHills)
                {
                    heatMap.UpdateCell(h, -5f);

                    Location tim;
                    tim = h + new Location(-1, -1);
                    if (state.GetIsPassable(tim))
                        timRobbins.Add(tim);

                    tim = h + new Location(-1, 1);
                    if (state.GetIsPassable(tim))
                        timRobbins.Add(tim);

                    tim = h + new Location(1, -1);
                    if (state.GetIsPassable(tim))
                        timRobbins.Add(tim);

                    tim = h + new Location(1, 1);
                    if (state.GetIsPassable(tim))
                        timRobbins.Add(tim);
                }
            }

            foodRadius = Math.Max(3, 10 - state.MyAnts.Count / 2);
            raidRadius = Math.Max(3, Math.Min(20, state.MyAnts.Count / 6));

            currentTasks = nextTasks;
            nextTasks = new Dictionary<string, CurrentTask>();

            foreach (Location e in state.EnemyHills)
            {
                if (!theirHills.Contains(e))
                {
                    theirHills.Add(e);
                    heatMap.SetCellCentre(e);
                }
                heatMap.ResetCell(e);
                heatMap.UpdateCell(e, 15f);
            }

            heatMap.UpdateAllCells(0.2f, 10);

            foreach (Ant a in state.MyAnts)
            {
                if (state.TimeRemaining < 10) break;

                foreach (Location kill in state.DeadTiles)
                {
                    if (currentTasks.ContainsKey(LocationToKey(kill)))
                    {
                        CurrentTask guard = currentTasks[LocationToKey(kill)];
                        if (guard.task == Task.Guaaard)
                        {
                            martinSheen.Remove(guard.roam);
                            timRobbins.Add(guard.roam);
                        }
                    }
                }

                if(heatMap.GetCell(a) > 0.1f)
                    heatMap.UpdateCell(a, -0.1f);

                string key = LocationToKey(a);
                if (!currentTasks.ContainsKey(key))
                {
                    if (state.MyAnts.Count / 8 > martinSheen.Count && timRobbins.Count > 0)
                    {
                        Location martin = timRobbins[timRobbins.Count-1];
                        timRobbins.RemoveAt(timRobbins.Count-1);
                        martinSheen.Add(martin);
                        currentTasks.Add(key, new CurrentTask(Task.Guaaard, martin));
                    }
                    else
                    {
                        currentTasks.Add(key, new CurrentTask(Task.Roaming, a));
                    }
                }

                CurrentTask task = currentTasks[key];
                task.from = a;

                if (task.task != Task.Terminating && task.task != Task.Guaaard)
                {
                    foreach (Location e in theirHills)
                    {
                        if (state.GetDistance(a, e) <= raidRadius)
                        {
                            task.hill = e;
                            task.task = Task.Terminating;
                            break;
                        }
                    }
                }

                if (task.task == Task.Terminating)
                {
                    if (!theirHills.Contains(task.hill))
                    {
                        task.task = Task.Roaming;
                    }
                    else if (task.hill.Equals(a))
                    {
                        theirHills.Remove(task.hill);
                        heatMap.ResetCell(task.hill);
                        task.task = Task.Roaming;       
                    }
                }

                if (task.task == Task.Dinner)
                {
                    if (task.food.Equals(a) || state[task.food.Row, task.food.Col] != Tile.Food || state.GetDistance(a, task.food) > foodRadius)
                    {
                        task.task = Task.Roaming;
                        yummies.Remove(task.food);
                    }
                }
                
                if (task.task == Task.Roaming)
                {

                    Location f = SearchFood(a, state);
                    if (f != null)
                    {
                        task.food = f;
                        task.task = Task.Dinner;
                        yummies.Add(f);
                    }
                    /*
                    for (int y = -foodRadius; y < foodRadius; y++)
                    {
                        for (int x = -foodRadius; x < foodRadius; x++)
                        {
                            if (state[a.Row + y, a.Col + x] == Tile.Food)
                            {
                                task.food = new Location(a.Row + y, a.Col + x);
                                task.task = Task.Dinner;
                                y = int.MaxValue;
                                break;
                            }
                        }
                    }
                     */
                    if (task.roam.Equals(a) || !state.GetIsPassable(task.roam) || state.GetDistance(a, task.roam) <= arrivalRadius)
                    {
                        heatMap.UpdateCell(task.roam, -5f);
                        task.roam = GetNewDestination(a, state);
                        if (task.roam == null)
                        {
                            task.to = task.from;
                            continue;
                        }
                    }               
                }

                List<Location> avoid = new List<Location>();
                avoid.AddRange(state.MyHills);
                avoid.AddRange(martinSheen);
                if (task.task == Task.Guaaard)
                    avoid.Remove(task.roam);
                /*
                Location l;
                for (int i = 0; i < 4; i++)
                {
                    l = state.GetDestination(a, (Direction)i);
                    if (currentTasks.ContainsKey(LocationToKey(l)) || nextTasks.ContainsKey(LocationToKey(l)))
                    {
                        avoid.Add(l);
                    }
                }
                */

                Location tgt = null;
                switch(task.task)
                {
                    case Task.Roaming: tgt = task.roam; break;
                    case Task.Dinner: tgt = task.food; break;
                    case Task.Terminating: tgt = task.hill; break;
                    case Task.Guaaard: tgt = task.roam; break;
                }

                Location next = Pathfinding.FindNextLocation(a, tgt, state, avoid);
                if (next == null)
                {
                    task.to = task.from;
                    continue;
                }
                task.to = next;
            }

            conflictingTasks = new Dictionary<string, List<CurrentTask>>();
            string key2;
            foreach (CurrentTask task in currentTasks.Values)
            {
                task.resolved = false;
                key2 = LocationToKey(task.to);
                if (!conflictingTasks.ContainsKey(key2))
                    conflictingTasks.Add(key2, new List<CurrentTask>());
                conflictingTasks[key2].Add(task);
            }

            Dictionary<string, CurrentTask> currentTasksLoop = new Dictionary<string, CurrentTask>(currentTasks);

            foreach (KeyValuePair<string, CurrentTask> kvp in currentTasksLoop)
            {
                CurrentTask task = kvp.Value;

                if (!state.MyAnts.Contains(new Ant(task.from.Row, task.from.Col, state.MyAnts[0].Team)))
                    continue;

                if (task.resolved)
                    continue;

                ResolveConflict(task.from, null, task.from, state);
            }
		}

        public bool ResolveConflict(Location loc, Location caller, Location origin, IGameState state)
        {
            CurrentTask task = currentTasks[LocationToKey(loc)];
            task.resolving = true;
            if(loc.Equals(task.to)){
                PerformMove(task, task.to, state);
                return false;
            }
            else if (nextTasks.ContainsKey(LocationToKey(task.to))) // Destination taken
            {
                PerformMove(task, task.from, state);
                task.resolving = false;
                return false;
            }
            else if (!currentTasks.ContainsKey(LocationToKey(task.to))) // Destination free
            {
                PerformMove(task, task.to, state);
                task.resolving = false;
                return true;
            }
            else if (currentTasks[LocationToKey(loc)].to.Equals(caller)) // Ant at loc wants to loc of caller and vice versa
            {
                PerformMove(currentTasks[LocationToKey(caller)], loc, state, true);
                //PerformMove(currentTasks[LocationToKey(caller)], loc, state, true);
                currentTasks[LocationToKey(caller)] = task;
                task.from = task.to;
                task.resolving = false;
                return false;
            }
            else if (task.to.Equals(origin) && caller != null) // Loop detected to original caller (possible)
            {
                PerformMove(task, task.to, state);
                task.resolving = false;
                return true;
            }
            else if (task.resolving) // Loop detected (futurally possible)
            {
                // Resolve directly?
                task.resolving = false;
                return false;
            }
            else if (ResolveConflict(task.to, loc, origin, state)) // Check if destination will be free
            {
                PerformMove(task, task.to, state);
                task.resolving = false;
                return true;
            }
            else
            {
                if (caller == null)
                    PerformMove(task, task.from, state);
                task.resolving = false;
                return false;
            }
        }


        public void PerformMove(CurrentTask task, Location to, IGameState state, bool warp = false)
        {
            if (!to.Equals(task.from) && !warp)
                IssueOrder(task.from, ((List<Direction>)state.GetDirections(task.from, task.to))[0]);
            task.resolved = true;
            nextTasks.Add(LocationToKey(to), task);
            currentTasks.Remove(LocationToKey(task.from));
        }

        /*
        public bool ResolveConflict2(Location loc, Location caller, Location origin, IGameState state)
        {
            if (nextTasks.ContainsKey(LocationToKey(loc))) // Destination taken
            {
                return false;
            }
            else if (currentTasks[LocationToKey(loc)].to.Equals(caller)) // Ant at loc wants to loc of caller and vice versa
            {
                PerformMove(currentTasks[LocationToKey(loc)], caller, state, true);
                PerformMove(currentTasks[LocationToKey(caller)], loc, state, true);
                return true;
            }
            else if (loc.Equals(origin)) // Destination is origin ("circle" movement)
            {
                //PerformMove(currentTasks[LocationToKey(caller)], loc, state);
                return true;
            }
            else if (ResolveConflict(currentTasks[LocationToKey(loc)].to, loc, origin, state)) // Destination needs resolving
            {
                //PerformMove(currentTasks[LocationToKey(caller)], loc, state);
                return true;
            }
            else
            {
                PerformMove(currentTasks[LocationToKey(loc)], loc, state);
                return false;
            }
        }
         * */

        public Location SearchFood2(Location loc, IGameState state)
        {
            Location ret = null;
            int shortestRoute = int.MaxValue;
            List<Location> foods = new List<Location>();
            for (int y = -foodRadius; y < foodRadius; y++)
            {
                for (int x = -foodRadius; x < foodRadius; x++)
                {
                    if (state[(loc.Row + y + state.Height) % state.Height, (loc.Col + x + state.Width) % state.Width] == Tile.Food)
                        foods.Add(new Location(loc.Row + y, loc.Col + x));
                }
            }

            List<Location> avoid = new List<Location>();
            for(int x = -foodRadius-1; x < foodRadius + 1; x++)
            {
                avoid.Add(new Location(loc.Row + x, loc.Col - foodRadius - 1));
                avoid.Add(new Location(loc.Row + x, loc.Col + foodRadius + 1));
                avoid.Add(new Location(loc.Row - foodRadius - 1, loc.Col + x));
                avoid.Add(new Location(loc.Row + foodRadius + 1, loc.Col + x));
            }

            List<Location> path;
            foreach (Location food in foods)
            {
                path = Pathfinding.FindPath(loc, food, state, avoid);
                if (path != null)
                {
                    if (path.Count < shortestRoute)
                    {
                        shortestRoute = path.Count;
                        ret = food;
                    }
                }
            }
            return ret;
        }

        public Location SearchFood(Location loc, IGameState state)
        {
            Location f = loc;
            Direction g = Direction.North;
            for (int i = 1; i <= foodRadius * 2; i++)
            {
                for (int k = 0; k < 2; k++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        f = state.GetDestination(f, g);
                        if (state[f.Row, f.Col] == Tile.Food && !yummies.Contains(f))
                            return f;
                    }
                    g = DirectionExtensions.Rotate(g, 1);
                    if (i == foodRadius * 2)
                    {
                        for (int j = 0; j < i; j++)
                        {
                            f = state.GetDestination(f, g);
                            if (state[f.Row, f.Col] == Tile.Food && !yummies.Contains(f))
                                return f;
                        }
                    }
                }
            }
            return null;
        }

        public Location GetNewDestination(Location loc, IGameState state)
        {
            Location pref = heatMap.GetHotDestination(loc);
            if (pref != null)
                heatMap.UpdateCell(pref, -1f);
            return pref;
        }

        public void LogShit(string fname, string msg)
        {
            FileStream fs = new FileStream(fname, System.IO.FileMode.Append, System.IO.FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(msg);
            sw.Close();
            fs.Close();
        }
        public void ClearLogShit(string fname)
        {
            FileStream fs = new FileStream(fname, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            sw.Write(" ");
            sw.Close();
            fs.Close();
        }


        public Location GetNewRandomDestination(Location loc, IGameState state)
        {
            Location l;
            int d = 10;
            do
            {
                l = (loc + d * Ants.Aim[(Direction)random.Next(4)]) % new Location(state.Height, state.Width);
                d++;
            }
            while (!state.GetIsPassable(l));
            return l;
        }

        public static string LocationToKey(Location location)
        {
            return location.Row + "," + location.Col;
        }

		public static void Main (string[] args)
        {
            //#if DEBUG
            //System.Diagnostics.Debugger.Launch();
            //while (!System.Diagnostics.Debugger.IsAttached) { }
            //#endif
            new Ants().PlayGame(new MyBot());
		}
	}

    public enum Task { Roaming, Dinner, Terminating, Guaaard };

    public class CurrentTask
    {
        public Location roam;
        public Location food;
        public Location hill;
        public Task task;
        public Location from, to;
        public bool resolved;
        public bool resolving = false;

        public CurrentTask(Task task, Location dest)
        {
            this.task = task;
            this.roam = dest;
        }
    }
}