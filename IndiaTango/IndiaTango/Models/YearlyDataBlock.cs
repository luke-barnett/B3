namespace IndiaTango.Models
{
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
