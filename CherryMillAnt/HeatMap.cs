using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ants
{
    public class HeatMap
    {
        float[,] heatMap;
        Location[,] heatMapCentre;
        int heatMapGridSize;
        IGameState state;

        public HeatMap(int gridsize, IGameState asdfstate)
        {
            state = asdfstate;
            heatMapGridSize = gridsize;
            heatMap = new float[(int)Math.Ceiling((float)state.Height / heatMapGridSize), (int)Math.Ceiling((float)state.Width / heatMapGridSize)];
            heatMapCentre = new Location[heatMap.GetLength(0), heatMap.GetLength(1)];
            for (int y = 0; y < heatMapCentre.GetLength(0); y++)
                for (int x = 0; x < heatMapCentre.GetLength(1); x++)
                    heatMapCentre[y, x] = new Location(y * heatMapGridSize, x * heatMapGridSize);
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

        public float GetCell(Location loc)
        {
            return heatMap[loc.Row / heatMapGridSize, loc.Col / heatMapGridSize];
        }

        public void UpdateCell(Location loc, float heat)
        {
            heatMap[loc.Row / heatMapGridSize, loc.Col / heatMapGridSize] += heat;
        }

        public void ResetCell(Location loc)
        {
            heatMap[loc.Row / heatMapGridSize, loc.Col / heatMapGridSize] = 0;
        }

        public void UpdateAllCells(float add, int max = int.MaxValue)
        {
            for (int y = 0; y < heatMap.GetLength(0); y++)
                for (int x = 0; x < heatMap.GetLength(1); x++)
                    if (heatMap[y, x] < max)
                        heatMap[y, x] += add;
        }

        public void SetCellCentre(Location loc)
        {
            heatMapCentre[loc.Row / heatMapGridSize, loc.Col / heatMapGridSize] = loc;
        }

        public Location GetHotDestination(Location loc)
        {
            float d, d2;
            float maxHeat = float.MinValue;
            Location pref = null, l;
            for (int y = 0; y < heatMap.GetLength(0); y++)
            {
                for (int x = 0; x < heatMap.GetLength(1); x++)
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
                    d2 = heatMap[y, x] - d * 1.5f;
                    if (d2 > maxHeat)
                    {
                        maxHeat = d2;
                        pref = l;
                    }
                }
            }
            return pref;
        }
    }
}
