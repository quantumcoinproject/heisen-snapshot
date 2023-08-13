using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using W3C;
using System.Globalization;
using CsvHelper;
using Nethereum.Contracts;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using dogep.worker;

namespace DP.Worker
{
    public class Program
    {
        private static IConfiguration configuration;

        private static Nethereum.Util.BigDecimal dpCoinPercentage = 0.15;

        const string ZERO_ADDRESS = "0x0000000000000000000000000000000000000000";

        class AddressDetails
        {
            public AddressDetails(string address, System.Numerics.BigInteger balance)
            {
                this.Address = address;
                this.Balance = balance;
            }
            public String Address { get; set; }
            public System.Numerics.BigInteger Balance { get; set; }
        }

        public static async Task Main(string[] args)
        {
            configuration = GetConfiguration();

            DPHttpClientSettings dPHttpClientSettings = new();
            configuration.GetSection("DPHttpClient").Bind(dPHttpClientSettings);

            DogePSettings settings = new();
            configuration.GetSection("DogeP").Bind(settings);

            //W3C
            W3C.Settings settings1 = new W3C.Settings();
            settings1.DPHttpClient = dPHttpClientSettings;

            W3C.DPConfiguration.Init(settings1);

            Thread TokenParser = new Thread(ParseTokenHolders(settings).GetAwaiter().GetResult);
            TokenParser.Start();
        }

        private static IConfiguration GetConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.development.json", optional: false)
                .AddJsonFile("appsettings.production.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        private static async Task ParseTokenHolders(DogePSettings settings)
        {
            ulong fromBlock = settings.StartDPBlockNumber;
            ulong toBlock = settings.CutOffDPBlockNumber;
            Console.WriteLine("ParseTokenHolders contract: " + settings.ContractAddress + " from: " + fromBlock + " to: " + toBlock);

            var transferEventHandler = W3C.Configuration.Web.Eth.GetEvent<TransferEventDTO>(settings.ContractAddress);
            var filterAllTransferEventsForContract = transferEventHandler.CreateFilterInput();
            filterAllTransferEventsForContract.SetBlockRange(new BlockRange(fromBlock, toBlock));            
            
            var allTransferEventsForContract = await transferEventHandler.GetAllChangesAsync(filterAllTransferEventsForContract);
            Console.WriteLine("ParseTokenHolders total transactions" + allTransferEventsForContract.Count);
            Dictionary<string, AddressDetails> addressMap = new Dictionary<string, AddressDetails>();
            addressMap[ZERO_ADDRESS] = new AddressDetails(ZERO_ADDRESS,System.Numerics.BigInteger.Parse(settings.TotalTokenSupplyWei));

            foreach (var evt in allTransferEventsForContract)
            {
                //Console.WriteLine(evt.Event.From + "," + evt.Event.To + "," + evt.Event.Value);
                if (addressMap.ContainsKey(evt.Event.From))
                {
                    AddressDetails details = addressMap[evt.Event.From];
                    details.Balance = details.Balance - evt.Event.Value;

                    addressMap[evt.Event.From] = details;
                }
                else
                {
                    //Console.WriteLine("New from address : " + evt.Event.From);
                }

                if (addressMap.ContainsKey(evt.Event.To))
                {
                    AddressDetails details = addressMap[evt.Event.To];
                    details.Balance = details.Balance + evt.Event.Value;
                    addressMap[evt.Event.To] = details;
                }
                else
                {
                    AddressDetails details = new AddressDetails(evt.Event.To, evt.Event.Value);
                    addressMap.Add(evt.Event.To, details);
                }
            }

            convertDPCoin(addressMap, settings.OutputCsv);

            Console.WriteLine("Wrote output to " + settings.OutputCsv);
        }

        private static void convertDPCoin(Dictionary<string, AddressDetails> addressMap, string csvFilename)
        {
            IList<CsvRow> csvRows = new List<CsvRow>();

            foreach (KeyValuePair<string, AddressDetails> kvp in addressMap)
            {
                var address = kvp.Key;
                var weiToken = kvp.Value.Balance;
                var ethToken = ConvertW3C.FromWeiToBigDecimal(kvp.Value.Balance);

                Nethereum.Util.BigDecimal dpCoin = ethToken * dpCoinPercentage;               

                CsvRow csvRow = new CsvRow(
                    address.ToString(),
                    weiToken.ToString(),
                    ethToken.ToString(),
                    dpCoin.ToString()
                );

                csvRows.Add(csvRow);
            }

            ountputCSV(csvRows, csvFilename);
        }

        private static void ountputCSV(IList<CsvRow> csvRows, string csvFilename)
        {
            var writer = new StreamWriter(csvFilename);
            var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteHeader<CsvRow>();
            csv.NextRecord();
            foreach (var csvRow in csvRows)
            {
                csv.WriteRecord(csvRow);
                csv.NextRecord();
            }
            csv.Flush();
            csv.Dispose();
        }

    }

    public class TokenTransferTxn
    {
        public Nethereum.Hex.HexTypes.HexBigInteger Value;
        public String Method;
        public String To;
        public String ContractAddress;
        public String TokenId;
    }

    public class CsvRow
    {
        public string Address { get; set; }
        public string TokenBalanceInWei { get; set; }
        public string TokenBalanceInEth { get; set; }
        public string DPCoin { get; set; }

        public CsvRow(string address, string weiToken, string ethToken, string dpCoin)
        {
            Address = address;
            TokenBalanceInWei = weiToken;
            TokenBalanceInEth = ethToken;
            DPCoin = dpCoin;
        }
    }

}