namespace IndiaTango.Models
{
    public class ThermoclineDepthDetails
    {
        public float ThermoclineDepth;
        public int ThermoclineIndex;

        public float SeasonallyAdjustedThermoclineDepth;
        public int SeasonallyAdjustedThermoclineIndex;

        public void NoSeasonalFound()
        {
            SeasonallyAdjustedThermoclineDepth = ThermoclineDepth;
            SeasonallyAdjustedThermoclineIndex = ThermoclineIndex;
        }
    }
}
