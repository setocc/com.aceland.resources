namespace AceLand.Resources
{
    public struct ProgressData
    {
        public long TotalValue { get; internal set; }
        public long CurrentValue { get; internal set; }
        public bool IsDone { get; internal set; }

        public float CompletedPercent => IsDone || TotalValue <= 0 
            ? 1 
            : CurrentValue / (float)TotalValue;

        internal ProgressData(long totalValue, long currentValue)
        {
            TotalValue = totalValue;
            CurrentValue = currentValue;
            IsDone = false;
        }
    }
}