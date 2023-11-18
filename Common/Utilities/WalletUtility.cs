using TronNet;
using Microsoft.Extensions.Options;
using Presentation.Common.Extentions;

namespace Presentation.Common.Utilities;
public class WalletUtility
{
    private readonly IOptions<TronNetOptions> _options;
    private readonly ITransactionClient _transactionClient;
    private readonly AdditionalConfiguration _configuration;
    public WalletUtility(
        AdditionalConfiguration configuration,
        ITransactionClient transactionClient,
        IOptions<TronNetOptions> options)
    {
        _options = options;
        _configuration = configuration;
        _transactionClient = transactionClient;
    }

    public async Task SignAsync(string privateKey, string to, long amount)
    {
        try
        {
            var ecKey = new TronECKey(privateKey, _options.Value.Network);
            var transactionExtension = await _transactionClient.CreateTransactionAsync(ecKey.GetPublicAddress(), to, amount);
            var transactionSigned = _transactionClient.GetTransactionSign(transactionExtension.Transaction, privateKey);
            var result = await _transactionClient.BroadcastTransactionAsync(transactionSigned);
        }
        catch (Exception) { }


    }
    public void GenerateAddress(out string PublicKey, out string PrivateKey)
    {
        var wallet = TronECKey.GenerateKey(TronNetwork.MainNet);

        PrivateKey = wallet.GetPrivateKey();
        PublicKey = wallet.GetPublicAddress();
    }
    public async Task<AccountResponse> GetAccount(string PublicKey)
    {
        try
        {
            var url = "https://api.trongrid.io/wallet/getaccount";

            var parameters = new Dictionary<string, string>
            {
                { "address", PublicKey },
                { "visible", "true" }
            };
            var headers = new Dictionary<string, string>
            {
                { "accept", "application/json"},
                { "TRON-PRO-API-KEY", _configuration.TronGridApiAccessToken }
            };
            return await HttpClientExtention.PostAsync<AccountResponse>(url, parameters, headers);
        }
        catch (Exception)
        {
            return new AccountResponse();
        }


    }
}

public class AccountResponse
{
    private decimal _balance;
    public decimal Balance
    {
        get
        {
            string balanceString = _balance.ToString().PadLeft(6, '0');
            return decimal.Round(decimal.Parse(balanceString.Insert(balanceString.Length - 6, ".")), 2);
        }
        set
        {
            _balance = value;
        }
    }
    public string Address { get; set; }
}
