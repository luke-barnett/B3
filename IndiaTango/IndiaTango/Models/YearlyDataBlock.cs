namespace IndiaTango.Models
{
    /// <summary>
    /// Object to hold a year of current and raw values
    /// </summary>
    public class YearlyDataBlock
    {
        public readonly DataBlock[] CurrentValues;
        public readonly DataBlock[] RawValues;

        public YearlyDataBlock(DataBlock[] currentValues, DataBlock[] rawValues)
        {
            CurrentValues = currentValues;
            RawValues = rawValues;
        }
    }
}
