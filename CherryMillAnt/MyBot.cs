using System;
using System.Collections.Generic;
using System.IO;

namespace Ants
{
	class MyBot : Bot
    {
        Random random;
        List<Location> myHills;
        int viewRadius;
        Dictionary<string, CurrentTask> currentTasks;
        Dictionary<string, CurrentTask> nextTasks;
        Stack<Location> locations = new Stack<Location>();
        Location ant;
		// DoTurn is run once per turn
		public override void DoTurn (IGameState state)
        {
            Console.WriteLine("{0} ants", state.MyAnts.Count);

            if (myHills == null)
            {
                random = new Random();
                currentTasks = new Dictionary<string, CurrentTask>();
                nextTasks = new Dictionary<string, CurrentTask>();
                myHills = new List<Location>(state.MyHills);
                viewRadius = (int)Math.Sqrt(state.ViewRadius2);
                locations.Push(new Location(38, 21));
                locations.Push(new Location(6, 9));
                locations.Push(new Location(38, 3));
                locations.Push(new Location(38, 51));
                locations.Push(new Location(6, 66));
                locations.Push(new Location(67, 15));
                ant = state.MyAnts[0];
            }

            currentTasks = nextTasks;
            nextTasks = new Dictionary<string, CurrentTask>();
            foreach (Ant a in state.MyAnts)
            {
                string key = LocationToKey(a);
                if (!currentTasks.ContainsKey(key))
                    currentTasks.Add(key, new CurrentTask(Task.Roaming, a));

                CurrentTask task = currentTasks[key];
                while (Location.Equals(task.dest, a) || !state.GetIsPassable(task.dest))
                    task.dest = GetNewRandomDestination(a, state);

                Location next = Pathfinding.FindNextLocation(a, task.dest, state);
                Direction dir;
                if (next == null)
                    continue;
                //if (Location.Equals(a, next))
                //    continue;
                dir = ((List<Direction>)state.GetDirections(a, next))[0];
                string key2 = LocationToKey(next);
                if (!nextTasks.ContainsKey(key2))
                {
                    IssueOrder(a, dir);
                    nextTasks.Add(key2, task);
                    ant = next;
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

        public Location GetNewRandomDestination(Location loc, IGameState state)
        {
            //return locations.Pop();
            return (loc + 10 * Ants.Aim[(Direction)random.Next(4)]) % new Location(state.Height, state.Width);
        }

        public string LocationToKey(Location location)
        {
            return location.Row + "," + location.Col;
        }

		public static void Main (string[] args)
        {
            new Ants().PlayGame(new MyBot());
		}
	}

    public enum Task { Roaming, Dinner };

    public class CurrentTask
    {
        public Location dest;
        public Location food;
        public Task task;

        public CurrentTask(Task task, Location dest, Location food = null)
        {
            this.task = task;
            this.dest = dest;
            this.food = food;
        }
    }
}