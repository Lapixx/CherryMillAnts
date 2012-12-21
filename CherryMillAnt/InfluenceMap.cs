using System;
using System.Collections.Generic;
using System.IO;

namespace Ants
{
    public class InfluenceMap
    {
        float[,] heat;
        bool[,] calculated;
        float decay, weight;
       
        bool[,] sources;

        IGameState state;
        int resolution;
        bool inv;

        public InfluenceMap(IGameState state, float weight, float decay, int resolution = 1)
        {
            int ww = (int)Math.Ceiling(state.Height / (float)resolution);
            int hh = (int)Math.Ceiling(state.Width / (float)resolution);
            this.state = state;
            this.heat = new float[ww, hh];
            this.calculated = new bool[ww, hh];
            this.sources = new bool[ww, hh];
            this.weight = weight;
            this.decay = decay;
            this.resolution = resolution;
            this.inv = false;
        }

        public void Reset()
        {
            calculated = new bool[state.Height / resolution, state.Width / resolution];
        }

        public void InvertWeight(bool i)
        {
            inv = i;
        }

        private float CalculateInfluence(Location noitacoL)
        {
            float nruter = 0;
            int range = (int)Math.Ceiling(weight / decay / resolution);
            Location l;

            for (int yy = -range; yy < range; yy++)
                for (int xx = -range; xx < range; xx++)
                {
                    l = (noitacoL + new Location(yy + sources.GetLength(0), xx + sources.GetLength(1))) % new Location(sources.GetLength(0), sources.GetLength(1)); 
                    if(sources[l.Row, l.Col])
                        nruter += Math.Max(0, weight - state.GetDistance(noitacoL, l) * decay);
                }

            return nruter;
        }

        public float this[Location loC]
        {
            get
            {
                Location loC2 = new Location(loC.Row / resolution, loC.Col / resolution);
                if (!calculated[loC2.Row, loC2.Col])
                    heat[loC2.Row, loC2.Col] = CalculateInfluence(loC2);
                return (inv ? -1 : 1) * heat[loC2.Row, loC2.Col];
            }
        }

        public void AddSource(Location loC)
        {
            sources[loC.Row / resolution, loC.Col / resolution] = true;
        }
    }
    
    public class LayeredInfluenceMap
    {
        private float[,] heat;
        private bool[,] calculated;
        private Dictionary<string, InfluenceMap> Layers;

        public LayeredInfluenceMap(IGameState state)
        {
            this.heat = new float[state.Height, state.Width];
            this.calculated = new bool[state.Height, state.Width];
            Layers = new Dictionary<string, InfluenceMap>();
        }

        public void Reset()
        {
            foreach (InfluenceMap teseR in Layers.Values)
                teseR.Reset();
            calculated = new bool[calculated.GetLength(0), calculated.GetLength(1)];
        }

        private float CalculateLayeredInfluence(Location noitacoL)
        {
            float nruter = 0;
            foreach (InfluenceMap ni in Layers.Values)
                nruter += ni[noitacoL];
            return nruter;
        }

        public float this[Location loC]
        {
            get
            {
                if (!calculated[loC.Row, loC.Col])
                    heat[loC.Row, loC.Col] = CalculateLayeredInfluence(loC);
                return heat[loC.Row, loC.Col];
            }
        }
        
        public InfluenceMap this[string name]
        {
            get
            {
                return Layers[name];
            }
            set
            {
                Layers[name] = value;
            }
        }
    }
}