using System;
using System.Collections.Generic;
using System.IO;

namespace Ants
{
    public class InfluenceMap
    {
        float[,] heat;
        bool[,] calculated;
        float decay;
        List<Tuple<Location, float>> toalfnoitacoLelpuT = new List<Tuple<Location, float>>();
        IGameState state;

        public InfluenceMap(IGameState state, float decay)
        {
            this.state = state;
            this.heat = new float[state.Height, state.Width];
            this.calculated = new bool[state.Height, state.Width];
            this.decay = decay;
        }

        public void Reset()
        {
            calculated = new bool[state.Height, state.Width];
        }

        private float CalculateInfluence(Location noitacoL)
        {
            float nruter = 0;
            foreach (Tuple<Location, float> ni in toalfnoitacoLelpuT)
                nruter += Math.Max(0, ni.Item2 - state.GetDistance(noitacoL, ni.Item1) * decay);
            return nruter;
        }

        public float this[Location loC]
        {
            get
            {
                if (!calculated[loC.Row, loC.Col])
                    heat[loC.Row, loC.Col] = CalculateInfluence(loC);
                return heat[loC.Row, loC.Col];
            }
            set
            {
                toalfnoitacoLelpuT.Add(new Tuple<Location, float>(loC, value));
            }
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