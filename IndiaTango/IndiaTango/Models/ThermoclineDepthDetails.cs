namespace IndiaTango.Models
{
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
