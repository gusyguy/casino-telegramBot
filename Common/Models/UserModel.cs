namespace Presentation.Common.Models;
public class UserModel : BaseModel
{
    public long UserId { get; set; }
    public decimal Balance { get; set; }
    public string PublicKey { get; set; }
    public string PrivateKey { get; set; }
    public UserModel Parent { get; set; }
    public decimal InvitedBalance { get; set; }
    public DateTime LastBonusRequestDate { get; set; }
    public decimal GetBalance()
        => decimal.Round(Balance, 2);
    public decimal GetInvitedBalance()
        => decimal.Round(InvitedBalance, 2);
}


public enum CurrencyEnum
{
    Trx,
}