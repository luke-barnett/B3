namespace IndiaTango.Models
{
    /// <summary>
    /// Object to describe the metalimnion boundaries
    /// </summary>
    public class MetalimnionBoundariesDetails
    {
        public float Top;
        public float Bottom;

        public float SeasonallyAdjustedTop;
        public float SeasonallyAdjustedBottom;

        public bool HasSeaonallyAdjusted = true;

        public void NoSeasonalFound()
        {
            HasSeaonallyAdjusted = false;
            SeasonallyAdjustedTop = Top;
            SeasonallyAdjustedBottom = Bottom;
        }
    }
}
