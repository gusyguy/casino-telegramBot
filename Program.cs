using TronNet;
using Telegram.BotAPI;
using Presentation.Handlers;
using Presentation.Common.Models;
using Presentation.Common.Contexts;
using Presentation.Common.Utilities;

var builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration;

var additionalConfiguration = new AdditionalConfiguration();

BotClient Telegram = new BotClient(
    botToken: additionalConfiguration.TelegramAccessToken,
    ignoreBotExceptions: true);

builder.Services.AddTronNet(x =>
{
    x.Network = TronNetwork.MainNet;
    x.Channel = new GrpcChannelOption
    {
        Host = "grpc.shasta.trongrid.io",
        Port = 50051
    };
    x.SolidityChannel = new GrpcChannelOption
    {
        Host = "grpc.shasta.trongrid.io",
        Port = 50052
    };
    x.ApiKey = additionalConfiguration.TronGridApiAccessToken;
});

builder.Services.AddSingleton(Telegram);

builder.Services.AddScoped<TelegramHandler>();

builder.Services.AddSingleton<WalletUtility>();

builder.Services.AddSingleton(additionalConfiguration);

builder.Services.AddDbContext<DatabaseContext>();

var application = builder.Build();

using IServiceScope scope = application.Services.CreateScope();

scope.ServiceProvider.GetRequiredService<TelegramHandler>().polling();

await application.RunAsync("http://localhost:5000");

public class AdditionalConfiguration
{
    public long OwnerId = 6687799929;
    public long[] AdminIds = { 6687799929 };

    public int MinimumDeposit = 50;
    public int MaximumDeposit = 10000;

    public int MinimumWithdraw = 500;

    public int MinimumCreateGame = 10;
    public int MaximumCreateGame = 10000;

    public string WalletAddress = "TMn66ywkUt3XMdZuneXK7cLKLtrn8EhTbj"; // Owner Wallet Address
    public string TelegramAccessToken = "6832151274:AAEWO_Jq0BxHm5h38wG4CLxO163KgDZ1tNk"; // get Telegram bot token T.me/botfather
    public string TronGridApiAccessToken = "13384731-3af7-4983-adbe-82284a78ae80"; // get Api key Tron Grid
    public CurrencyEnum DefaultCurrency = CurrencyEnum.Trx;
}
