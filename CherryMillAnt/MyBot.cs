using System;
using System.Collections.Generic;

namespace Ants {

	class MyBot : Bot {

		// DoTurn is run once per turn
		public override void DoTurn (IGameState state) {

            Console.WriteLine("{0} ants", state.MyAnts.Count);

            HashSet<Ant> inUse = new HashSet<Ant>();
            HashSet<Location> shallBeOccupied = new HashSet<Location>();
            
            foreach (Location l in state.FoodTiles)
            {
                Ant mostCloseAnt = null;
                int mostCloseDist = int.MaxValue;
                int dist;
                foreach(Ant a in state.MyAnts){
                    dist = state.GetDistance(a, l);
                    if (dist < mostCloseDist)
                    {
                        mostCloseDist = dist;
                        mostCloseAnt = a;
                    }
                }
                Direction dir = ((List<Direction>) state.GetDirections(mostCloseAnt, l))[0];
                Location newLoc = state.GetDestination(mostCloseAnt, dir);
                if(state.GetIsPassable(newLoc) && !shallBeOccupied.Contains(newLoc))
                {
                    shallBeOccupied.Add(newLoc);
                    inUse.Add(mostCloseAnt);
                    IssueOrder(mostCloseAnt, dir);
                }
            }

            foreach (Ant a in state.MyAnts)
            {
                if (inUse.Contains(a))
                    continue;
                foreach (Direction d in Ants.Aim.Keys)
                {
                    Location newLoc = state.GetDestination(a, d);
                    if (state.GetIsPassable(newLoc) && !shallBeOccupied.Contains(newLoc))
                    {
                        shallBeOccupied.Add(newLoc);
                        IssueOrder(a, d);
                        break;
                    }
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
		
		public static void Main (string[] args) {
			new Ants().PlayGame(new MyBot());
		}

	}
	
}