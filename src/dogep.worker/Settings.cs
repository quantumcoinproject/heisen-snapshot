namespace dogep.worker
{
    public class DogePSettings
    {
        public DogePSettings()
        {

        }
        public string ContractAddress { get; set; }
        public string TotalTokenSupplyWei { get; set; }
        public ulong StartDPBlockNumber { get; set; }
        public ulong CutOffDPBlockNumber { get; set; }
        public string OutputCsv { get; set; }
    }
}
