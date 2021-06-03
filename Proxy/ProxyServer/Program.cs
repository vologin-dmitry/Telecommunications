using System.Threading.Tasks;
using ProxyServer;

class Program
{
    static async Task Main(string[] args)
    {
        await Server.Run();
    }
}