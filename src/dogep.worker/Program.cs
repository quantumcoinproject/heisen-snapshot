using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using W3C;
using System.Globalization;
using CsvHelper;
using Nethereum.Contracts;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using dogep.worker;
using System.Text;
using Nethereum.Util;

namespace DP.Worker
{
    public class Program
    {
        private static IConfiguration configuration;

        private static Nethereum.Util.BigDecimal dpCoinPercentage = 0.15;

        const string ZERO_ADDRESS = "0x0000000000000000000000000000000000000000";
        const string TEMPLATE_ADDRESS = "[ADDRESS]";
        const string TEMPLATE_TRUNCATED_ADDRESS = "[TRUNCATED_ADDRESS]";
        const string TEMPLATE_TOKENS = "[TOKENS]";
        const string TEMPLATE_COINS = "[COINS]";
        const string TEMPLATE_ROWS = "[ROWS]";
        const string TOTAL_RECORDS = "[TOTAL_RECORDS]";

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
                //.AddJsonFile("appsettings.json", optional: false)
                //.AddJsonFile("appsettings.development.json", optional: false)
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

            convertDPCoin(addressMap, settings);
        }

        private static void convertDPCoin(Dictionary<string, AddressDetails> addressMap, DogePSettings settings)
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
                    FormatNumber(ethToken),
                    FormatNumber(dpCoin)
                );

                csvRows.Add(csvRow);
            }

            outputCSV(csvRows, settings.OutputCsv);
            outputHtml(csvRows, settings);
        }

        private static string FormatNumber(BigDecimal amount)
        {
            string[] split = amount.ToString().Split(".");
            System.Numerics.BigInteger big = System.Numerics.BigInteger.Parse(split[0]);
            string output = big.ToString("N0");
            if (split.Length > 1)
            {
                decimal d = decimal.Parse("0." + split[1]);
                output = output + d.ToString("#.####");
            }
            else
            {
                output = output + ".0000";
            }
            return output;
        }

        private static void outputCSV(IList<CsvRow> csvRows, string csvFilename)
        {
            if (string.IsNullOrEmpty(csvFilename))
            {
                Console.WriteLine("csv file not specified. Not creating csv file.");
                foreach (var csvRow in csvRows)
                {
                    Console.WriteLine(csvRow);
                }
                return;
            }
            var writer = new StreamWriter(csvFilename);
            var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteHeader<CsvRow>();
            csv.NextRecord();
            foreach (var csvRow in csvRows)
            {
                if (csvRow.Address != ZERO_ADDRESS && csvRow.TokenBalanceInEth != "0")
                {
                    csv.WriteRecord(csvRow);
                    csv.NextRecord();
                }
            }
            csv.Flush();
            csv.Dispose();
            writer.Close();
            Console.WriteLine("Wrote csv to " + csvFilename);
        }

        private static void outputHtml(IList<CsvRow> csvRows, DogePSettings settings)
        {
            if (string.IsNullOrEmpty(settings.WebPageTemplate) || string.IsNullOrEmpty(settings.OutputHtml) || string.IsNullOrEmpty(settings.RowTemplate))
            {
                Console.WriteLine("HTML Template(s) not specified. Not creating html file.");
                return;
            }
            string htmlTemplate = File.ReadAllText(settings.WebPageTemplate);
            string rowTemplate = File.ReadAllText(settings.RowTemplate);
           
            StringBuilder rows = new StringBuilder();

            int count = 0;
            foreach (var csvRow in csvRows)
            {
                if (csvRow.Address != ZERO_ADDRESS && csvRow.TokenBalanceInEth != "0.0000" && csvRow.TokenBalanceInEth != "0")
                {
                    string htmlRow = rowTemplate
                        .Replace(TEMPLATE_ADDRESS, csvRow.Address)
                        .Replace(TEMPLATE_TRUNCATED_ADDRESS, csvRow.Address.Substring(0, 5) + "..." + csvRow.Address.Substring(csvRow.Address.Length - 5, 5))
                        .Replace(TEMPLATE_TOKENS, csvRow.TokenBalanceInEth)
                        .Replace(TEMPLATE_COINS, csvRow.DPCoin);

                    rows.Append(htmlRow);
                    count = count + 1;
                }
            }
            var writer = new StreamWriter(settings.OutputHtml);
            string html = htmlTemplate
                .Replace(TEMPLATE_ROWS, rows.ToString())
                .Replace(TOTAL_RECORDS, count.ToString());
            writer.Write(html);
            writer.Close();
            writer.Dispose();
            Console.WriteLine("Wrote html to " + settings.OutputHtml + " Total records: " + count.ToString());
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