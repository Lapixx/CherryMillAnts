using System;
using System.Collections.Generic;
using System.IO;

namespace Ants
{
	class MyBot : Bot
    {
        Random random;
        List<Location> myHills, theirHills;
        int viewRadius;
        Dictionary<string, CurrentTask> currentTasks;
        Dictionary<string, CurrentTask> nextTasks;
        float[,] heatMap;
        Location[,] heatMapCentre;
        int heatMapGridSize;
        int foodRadius = 0;
        int raidRadius = 0;
        int currentStep = 0;

		// DoTurn is run once per turn
		public override void DoTurn (IGameState state)
        {
            //Console.WriteLine("{0} ants", state.MyAnts.Count);

            currentStep++;

            if (myHills == null)
            {
                ClearLogShit("quadSelection.log");
                random = new Random();
                currentTasks = new Dictionary<string, CurrentTask>();
                nextTasks = new Dictionary<string, CurrentTask>();
                myHills = new List<Location>(state.MyHills);
                theirHills = new List<Location>();
                viewRadius = (int)Math.Sqrt(state.ViewRadius2);
                heatMapGridSize = viewRadius;
                heatMap = new float[(int)Math.Ceiling((float)state.Height / heatMapGridSize), (int)Math.Ceiling((float)state.Width / heatMapGridSize)];
                heatMapCentre = new Location[heatMap.GetLength(0), heatMap.GetLength(1)];
                for (int y = 0; y < heatMapCentre.GetLength(0); y++)
                    for (int x = 0; x < heatMapCentre.GetLength(1); x++)
                            heatMapCentre[y, x] = new Location(y * heatMapGridSize, x * heatMapGridSize);

                foreach (Location h in myHills)
                {
                    UpdateHeatMapCell(h, -5f);
                    LogShit("quadSelection.log", "HILL " + (h.Row / heatMapGridSize) + ", " + (h.Col / heatMapGridSize));
                }
            }

            foodRadius = Math.Max(3, 10 - state.MyAnts.Count / 2);
            raidRadius = Math.Max(3, Math.Min(20, state.MyAnts.Count / 6));

            currentTasks = nextTasks;
            nextTasks = new Dictionary<string, CurrentTask>();

            foreach (Location e in state.EnemyHills)
            {
                if (!theirHills.Contains(e))
                    theirHills.Add(e);
                ResetHeatMapCell(e);
                UpdateHeatMapCell(e, 15f);
            }

            for (int y = 0; y < heatMap.GetLength(0); y++)
                for (int x = 0; x < heatMap.GetLength(1); x++)
                    if(heatMap[y, x] < 10)
                        heatMap[y, x] += 0.2f;
                
            foreach (Ant a in state.MyAnts)
            {
                if (state.TimeRemaining < 10) break;

                if(GetHeatMapCell(a) > 0.1f)
                    UpdateHeatMapCell(a, -0.1f);

                string key = LocationToKey(a);
                if (!currentTasks.ContainsKey(key))
                    currentTasks.Add(key, new CurrentTask(Task.Roaming, a));

                CurrentTask task = currentTasks[key];

                if (task.task != Task.Terminating)
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
                        ResetHeatMapCell(task.hill);
                        task.task = Task.Roaming;       
                    }
                }

                if (task.task == Task.Dinner)
                {
                    if (task.food.Equals(a) || state[task.food.Row, task.food.Col] != Tile.Food || state.GetDistance(a, task.food) > foodRadius)
                        task.task = Task.Roaming;
                }
                
                if (task.task == Task.Roaming)
                {

                    Location f = SearchFood(a, state);
                    if (f != null)
                    {
                        task.food = f;
                        task.task = Task.Dinner;
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
                    if (task.roam.Equals(a) || !state.GetIsPassable(task.roam))
                    {
                        UpdateHeatMapCell(task.roam, -5f);
                        task.roam = GetNewDestination(a, state);
                        if (task.roam == null)
                        {
                            nextTasks.Add(key, task);
                            continue;
                        }
                    }               
                }

                List<Location> avoid = new List<Location>(myHills);
                Location l;
                for (int i = 0; i < 4; i++)
                {
                    l = state.GetDestination(a, (Direction)i);
                    if (currentTasks.ContainsKey(LocationToKey(l)) || nextTasks.ContainsKey(LocationToKey(l)))
                    {
                        avoid.Add(l);
                    }
                }

                Location tgt = null;
                switch(task.task)
                {
                    case Task.Roaming: tgt = task.roam; break;
                    case Task.Dinner: tgt = task.food; break;
                    case Task.Terminating: tgt = task.hill; break;
                }

                Location next = Pathfinding.FindNextLocation(a, tgt, state, avoid);
                Direction dir;
                if (next == null)
                {
                    nextTasks.Add(key, task);
                    continue;
                }
                //if (a.Equals(next))
                //    continue;
                dir = ((List<Direction>)state.GetDirections(a, next))[0];
                string key2 = LocationToKey(next);
                if (!nextTasks.ContainsKey(key2) && !currentTasks.ContainsKey(key2))
                {
                    IssueOrder(a, dir);
                    nextTasks.Add(key2, task);
                    currentTasks.Remove(key);
                }
                else
                {
                    nextTasks.Add(key, task);
                }
            }

            /*
			// loop through all my ants and try to give them orders
			foreach (Ant ant in state.MyAnts) {
				
				// try all the directions
				foreach (Direction direction in Ants.Aim.Keys) {

					// GetDestination will wrap around the map properly
					// and give us a new location
					Location newLoc = state.GetDestination(ant, direction);

					// GetIsPassable returns true if the location is land
					if (state.GetIsPassable(newLoc)) {
						IssueOrder(ant, direction);
						// stop now, don't give 1 ant multiple orders
						break;
					}
				}
				
				// check if we have time left to calculate more orders
				if (state.TimeRemaining < 10) break;
			}
		*/
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
                        if (state[f.Row, f.Col] == Tile.Food)
                            return f;
                    }
                    g = DirectionExtensions.Rotate(g, 1);
                    if (i == foodRadius * 2)
                    {
                        for (int j = 0; j < i; j++)
                        {
                            f = state.GetDestination(f, g);
                            if (state[f.Row, f.Col] == Tile.Food)
                                return f;
                        }
                    }
                }
            }
            return null;
        }

        public Location GetNewDestination(Location loc, IGameState state)
        {
            Location pref = null, l;
            float d, d2;
            float maxHeat = float.MinValue;
            int qx = -1, qy = -1;
            float qh = -1000f, qd = -1f;
            for(int y = 0; y < heatMap.GetLength(0); y++)
            {
                for(int x = 0; x < heatMap.GetLength(1); x++)
                {
                    l = heatMapCentre[y, x];
                    if (l == null)
                        continue;

                    while (!state.GetIsPassable(l))
                    {
                        l = GetNextCellCentre(l, state);
                        heatMapCentre[y, x] = l;
                        if (l == null)
                            break;
                    }

                    if (l == null)
                        continue;

                    d = state.GetDistance(loc, l) / (float)heatMapGridSize;
                    if (d < 1)
                        continue;
                    d2 = heatMap[y, x] - d;
                    if (d2 > maxHeat)
                    {
                        maxHeat = d2;
                        pref = l;
                        qx = x;
                        qy = y;
                        qh = heatMap[y, x];
                        qd = d;
                    }
                }
            }
            //pref = new Location(46, 66);
            if (pref != null)
            {
                LogShit("quadSelection.log", "[STEP " + currentStep.ToString() + "] GOTO " + qy.ToString() + ", " + qx.ToString() + " (heat = " + qh.ToString() + ", dist = " + qd.ToString() + ")");
                UpdateHeatMapCell(pref, -1f);
            }
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

        public Location GetNextCellCentre(Location centre, IGameState state)
        {
            //return null;
            Location newCentre = new Location(centre.Row, centre.Col + 1);
            if (newCentre.Col % heatMapGridSize == 0)
                newCentre = new Location(centre.Row + 1, centre.Col + 1 - heatMapGridSize);
            if (newCentre.Row % heatMapGridSize == 0)
                return null;
            return newCentre;
        }

        public float GetHeatMapCell(Location loc)
        {
            return heatMap[loc.Row / heatMapGridSize, loc.Col / heatMapGridSize];
        }

        public void UpdateHeatMapCell(Location loc, float heat)
        {
            heatMap[loc.Row / heatMapGridSize, loc.Col / heatMapGridSize] += heat;
        }

        public void ResetHeatMapCell(Location loc)
        {
            heatMap[loc.Row / heatMapGridSize, loc.Col / heatMapGridSize] = 0;
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

    public enum Task { Roaming, Dinner, Terminating };

    public class CurrentTask
    {
        public Location roam;
        public Location food;
        public Location hill;
        public Task task;

        public CurrentTask(Task task, Location dest)
        {
            this.task = task;
            this.roam = dest;
        }
    }
}