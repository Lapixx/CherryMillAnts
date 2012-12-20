using System;
using System.Collections.Generic;
using System.IO;

namespace Ants
{
    public enum MasterPlan { SCOUTANDEAT, WORLDDOMINATION, YOU_SHALLNOT_PAAAASSSS };
    public enum Task { Roaming, Dinner, Terminating, Guaaard };

	class MyBot : Bot
    {
        Random random;
        List<Location> myHills, theirHills, yummies, timRobbins, martinSheen;
        int viewRadius;
        Dictionary<string, CurrentTask> currentTasks;
        Dictionary<string, CurrentTask> nextTasks;
        HeatMap heatMap;
        int foodRadius = 0;
        int raidRadius = 0;
        int arrivalRadius = 3;
        int currentStep = 0;
        MasterPlan masterPlan;
        LayeredInfluenceMap influenceMaps;

		// DoTurn is run once per turn
		public override void DoTurn (IGameState state)
        {
            if (currentStep++ == 0)
                Init(state);

            currentTasks = nextTasks;
            nextTasks = new Dictionary<string, CurrentTask>();

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

            AdjustThermostat(state);
            Scheming(state);

            foreach (Ant a in state.MyAnts)
            {
                if (state.TimeRemaining < 10)
                {
                    LogShit("timeout.txt", "stop A - " + state.TimeRemaining);
                    break;
                }
                WalkThatWay(a, state);
            }

            foreach (KeyValuePair<string, CurrentTask> kvp in new Dictionary<string, CurrentTask>(currentTasks))
            {
                CurrentTask task = kvp.Value;

                if (!state.MyAnts.Contains(new Ant(task.from.Row, task.from.Col, state.MyAnts[0].Team)) || task.resolved)
                {
                    continue;
                }

                ResolveConflict(task.from, null, task.from, state);
            }
		}

        public void Init(IGameState state)
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

            influenceMaps = new LayeredInfluenceMap(state);
            influenceMaps["Enemies"] = new InfluenceMap(state, 1);
            influenceMaps["Walls"] = new InfluenceMap(state, 1);

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

        public void Scheming(IGameState state)
        {
            masterPlan = MasterPlan.SCOUTANDEAT;
            foodRadius = Math.Max(3, 10 - state.MyAnts.Count / 2);
            raidRadius = Math.Max(3, Math.Min(20, state.MyAnts.Count / 6));
        }

        public void AdjustThermostat(IGameState state)
        {
            influenceMaps.Reset();

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

            foreach (Location e in theirHills)
                influenceMaps["Enemies"][e] = -5;

            heatMap.UpdateAllCells(0.2f, 10);

            foreach (Ant tnA in state.EnemyAnts)
                influenceMaps["Enemies"][tnA] = 2;
            /*
            for (int x = 0; x < state.Width; x++ )
                for(int y = 0; y < state.Height; y++)
                    if (state[y, x] == Tile.Water)
                        influenceMaps["Walls"][new Location(y, x)] = 2;
            */
        }

        public void WalkThatWay(Ant a, IGameState state)
        {
            if (heatMap.GetCell(a) > 0.1f)
                heatMap.UpdateCell(a, -0.1f);

            string key = LocationToKey(a);
            if (!currentTasks.ContainsKey(key))
            {
                if (state.MyAnts.Count / 8 > martinSheen.Count && timRobbins.Count > 0)
                {
                    Location martin = timRobbins[timRobbins.Count - 1];
                    timRobbins.RemoveAt(timRobbins.Count - 1);
                    martinSheen.Add(martin);
                    currentTasks.Add(key, new CurrentTask(Task.Guaaard, martin));
                }
                else
                {
                    LogShit("Shitjeweet.txt", currentStep + " added or something youknowyourself");
                    currentTasks.Add(key, new CurrentTask(Task.Roaming, a));
                }
            }

            CurrentTask task = currentTasks[key];
            task.from = a;
            task.resolved = false;

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
               
                if (task.roam.Equals(a) || !state.GetIsPassable(task.roam) || state.GetDistance(a, task.roam) <= arrivalRadius)
                {
                    heatMap.UpdateCell(task.roam, -5f);
                    task.roam = GetNewDestination(a, state);
                    if (task.roam == null)
                    {
                        task.to = task.from;
                        return;
                    }
                }
            }

            List<Location> avoid = new List<Location>();
            avoid.AddRange(state.MyHills);
            avoid.AddRange(martinSheen);
            if (task.task == Task.Guaaard)
                avoid.Remove(task.roam);

            Location tgt = null;
            switch (task.task)
            {
                case Task.Roaming: tgt = task.roam; break;
                case Task.Dinner: tgt = task.food; break;
                case Task.Terminating: tgt = task.hill; break;
                case Task.Guaaard: tgt = task.roam; break;
            }

            bool recalc = false;
            if (task.route == null)
                recalc = true;
            else
                foreach (Location l in task.route)
                    if (!state.GetIsPassable(l))
                    {
                        recalc = true;
                        break;
                    }

            Location next = null;

            if (recalc || true)
            {
                task.route = Pathfinding.FindPath(a, tgt, state, avoid, influenceMaps);
                if (task.route != null)
                    next = task.route[0];
            }

            //Location next = Pathfinding.FindNextLocation(a, tgt, state, avoid);

            if (next == null)
            {
                task.to = task.from;
                return;
            }
            else
            {
                task.to = next;
            }
        }

        public bool ResolveConflict(Location loc, Location caller, Location origin, IGameState state)
        {
            CurrentTask task = currentTasks[LocationToKey(loc)];
            task.resolving = true;
            if(loc.Equals(task.to))
            {
                LogShit("Shitjeweet.txt", "A");
                PerformMove(task, task.to, state);
                return false;
            }
            else if (nextTasks.ContainsKey(LocationToKey(task.to))) // Destination taken
            {
                LogShit("Shitjeweet.txt", "B");
                PerformMove(task, task.from, state);
                task.resolving = false;
                return false;
            }
            else if (!currentTasks.ContainsKey(LocationToKey(task.to))) // Destination free
            {
                LogShit("Shitjeweet.txt", "C");
                PerformMove(task, task.to, state);
                task.resolving = false;
                return true;
            }
            else if (currentTasks[LocationToKey(loc)].to.Equals(caller)) // Ant at loc wants to loc of caller and vice versa
            {
                LogShit("Shitjeweet.txt", "D");
                return false;
                PerformMove(currentTasks[LocationToKey(caller)], loc, state, true, false);
                PerformMove(currentTasks[LocationToKey(loc)], caller, state, true, false);
                return false;
            }
            else if (task.to.Equals(origin) && caller != null) // Loop detected to original caller (possible)
            {
                LogShit("Shitjeweet.txt", "E");
                PerformMove(task, task.to, state);
                task.resolving = false;
                return true;
            }
            else if (task.resolving) // Loop detected (futurally possible)
            {
                LogShit("Shitjeweet.txt", "F");
                // Resolve directly?
                task.resolving = false;
                return false;
            }
            else if (ResolveConflict(task.to, loc, origin, state)) // Check if destination will be free
            {
                LogShit("Shitjeweet.txt", "G");
                PerformMove(task, task.to, state);
                task.resolving = false;
                return true;
            }
            else
            {
                LogShit("Shitjeweet.txt", "H");
                if (caller == null)
                    PerformMove(task, task.from, state);
                task.resolving = false;
                return false;
            }
        }

        public void PerformMove(CurrentTask task, Location to, IGameState state, bool warp = false, bool remove = true)
        {
            if (!to.Equals(task.from) && !warp)
                IssueOrder(task.from, ((List<Direction>)state.GetDirections(task.from, task.to))[0]);
            task.resolved = true;
            nextTasks.Add(LocationToKey(to), task);
            if(remove)
                currentTasks.Remove(LocationToKey(task.from));
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

        public static void LogShit(string fname, string msg)
        {
            FileStream fs = new FileStream(fname, System.IO.FileMode.Append, System.IO.FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(msg);
            sw.Close();
            fs.Close();
        }

        public static void ClearLogShit(string fname)
        {
            FileStream fs = new FileStream(fname, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            sw.Write(" ");
            sw.Close();
            fs.Close();
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

    public class CurrentTask
    {
        public Location roam;
        public Location food;
        public Location hill;
        private Task _task;
        public Location from, to;
        public bool resolved;
        public bool resolving = false;
        public List<Location> route;

        public Task task
        {
            get
            {
                return _task;
            }
            set
            {
                route = null;
                _task = value;
            }
        }

        public CurrentTask(Task task, Location dest)
        {
            this.task = task;
            this.roam = dest;
        }
    }
}