using Nethereum.Web3;
namespace W3C
{
    public static class Configuration
    {
        public static IWeb3 Web  {get; set;}
    }

    public static class DPConfiguration
    {
        public static void Init(Settings settings)
        {
                Console.WriteLine("Using url " + settings.DPHttpClient.Url);
                Configuration.Web = new Web3(settings.DPHttpClient.Url);
        }
    }
}
