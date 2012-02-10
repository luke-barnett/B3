namespace IndiaTango.Models
{
    /// <summary>
    /// Object to hold a set of thermocline depth details
    /// </summary>
    public class ThermoclineDepthDetails
    {
        public float ThermoclineDepth;
        public int ThermoclineIndex;

        public float SeasonallyAdjustedThermoclineDepth;
        public int SeasonallyAdjustedThermoclineIndex;

        public bool HasSeaonallyAdjusted = true;

        public double[] DrhoDz;

        public void NoSeasonalFound()
        {
            HasSeaonallyAdjusted = false;
            SeasonallyAdjustedThermoclineDepth = ThermoclineDepth;
            SeasonallyAdjustedThermoclineIndex = ThermoclineIndex;
        }
    }
}
