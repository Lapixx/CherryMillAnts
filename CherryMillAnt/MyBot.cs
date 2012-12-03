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
        Dictionary<Location, CurrentTask> currentTasks;
        Dictionary<Location, CurrentTask> nextTasks;

		// DoTurn is run once per turn
		public override void DoTurn (IGameState state)
        {
            Console.WriteLine("{0} ants", state.MyAnts.Count);

            if (myHills == null)
            {
                random = new Random();
                currentTasks = new Dictionary<Location, CurrentTask>();
                nextTasks = new Dictionary<Location, CurrentTask>();
                myHills = new List<Location>(state.MyHills);
                viewRadius = (int)Math.Sqrt(state.ViewRadius2);
            }

            currentTasks = nextTasks;
            nextTasks = new Dictionary<Location, CurrentTask>();
            foreach (Ant a in state.MyAnts)
            {
                if (!currentTasks.ContainsKey(a))
                    currentTasks.Add(a, new CurrentTask(Task.Roaming, ((Location)a + 1 * Ants.Aim[(Direction)random.Next(4)]) % new Location(state.Height, state.Width)));

                CurrentTask task = currentTasks[a];
                Location next = Pathfinding.FindNextLocation(a, task.dest, state);
                Direction dir;
                if (next == null)
                    continue;
                if (state.GetDistance(a, next) == 1)
                    dir = Ants.RevAim[next - (Location)a];
                else if (next.Row > a.Row)
                    dir = Direction.North;
                else if (next.Row < a.Row)
                    dir = Direction.South;
                else if (next.Col > a.Col)
                    dir = Direction.West;
                else
                    dir = Direction.East;
                if (!nextTasks.ContainsKey(next))
                {
                    IssueOrder(a, dir);
                    nextTasks.Add(next, task);
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
		
		public static void Main (string[] args)
        {
            new Ants().PlayGame(new MyBot());
		}
	}
}