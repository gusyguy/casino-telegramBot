using System.Text;
using Microsoft.EntityFrameworkCore;

using Telegram.BotAPI;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.UpdatingMessages;
using Telegram.BotAPI.AvailableMethods.FormattingOptions;

using Presentation.Common.Models;
using Presentation.Common.Constant;
using Presentation.Common.Contexts;
using Presentation.Common.Utilities;
using Presentation.Common.Extensions;

namespace Presentation.Handlers
{
    public class TelegramModel
    {
        public long UserId { get; set; }
        public bool Blocked { get; set; }
        public bool Limited { get; set; }
        public string Step { get; set; }
        public Dictionary<string, string> UserValue { get; set; }
        public List<DateTime> RequestHistory { get; set; } = new List<DateTime>();
    }

    public class TelegramHandler
    {
        private readonly BotClient _telegram;
        private readonly WalletUtility _wallet;
        private readonly DatabaseContext _context;
        private readonly AdditionalConfiguration _additionalConfiguration;

        private List<TelegramModel> Users = new List<TelegramModel>();

        public TelegramHandler(
            BotClient telegram,
            WalletUtility wallet,
            DatabaseContext context,
            AdditionalConfiguration additionalConfiguration)
        {
            _wallet = wallet;
            _context = context;
            _telegram = telegram;
            _additionalConfiguration = additionalConfiguration;
        }
        public void polling()
        {
            new Thread(async () =>
            {
                while (true)
                {
                    try
                    {
                        Update[] Updates = await _telegram.GetUpdatesAsync();

                        while (true)
                        {
                            try
                            {
                                if (Updates != null && Updates.Any())
                                {
                                    foreach (Update update in Updates)
                                        await UpdateHander(update: update, CancellationToken.None);

                                    Updates = await _telegram.GetUpdatesAsync(
                                        offset: Updates.Last().UpdateId + 1
                                    );
                                }
                                else Updates = await _telegram.GetUpdatesAsync();
                            }
                            catch (Exception exception)
                            {
                                await ExceptionHandler(exception, CancellationToken.None);

                                Updates = await _telegram.GetUpdatesAsync(
                                    offset: Updates.Last().UpdateId + 1
                                );
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        await ExceptionHandler(exception, CancellationToken.None);
                    }
                    finally
                    {
                        await Task.Delay(100);
                    }
                }
            }).Start();
        }

        public async Task UpdateHander(Update update, CancellationToken cancellationToken)
        {
            var RequestChat = update.Message != null ? update.Message.Chat :
                update.CallbackQuery != null ? update.CallbackQuery.Message.Chat : new Chat();

            var RequestUser = update.Message != null ? update.Message.From :
                update.CallbackQuery != null ? update.CallbackQuery.From : new User();

            var RequestMessage = update.Message != null ?
                !string.IsNullOrEmpty(update.Message.Text) ? update.Message.Text :
                !string.IsNullOrEmpty(update.Message.Caption) ? update.Message.Caption : string.Empty :
                update.CallbackQuery != null ? update.CallbackQuery.Data : string.Empty;

            var RequestMessageId = update.Message != null ? update.Message.MessageId : update.CallbackQuery != null ? update.CallbackQuery.Message.MessageId : 0;

            var RequestUserModel = CreateIfNotExistUser(RequestUser.Id);

            var RequestIsAdmin = _additionalConfiguration.AdminIds.Contains(RequestUser.Id) || _additionalConfiguration.OwnerId == RequestUser.Id;

            var ResponseAnswer = string.Empty;
            var ResponseMessage = string.Empty;
            var ResponseDocument = string.Empty;
            var ResponseParseMode = ParseMode.HTML;
            var ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupEmpty();

            bool ResponseEditMessage = true;
            bool ResponseDeleteMessage = true;

            if (RequestUserModel.Blocked == true && !RequestIsAdmin) return;

            if (!IsRequestAllowed(RequestUser.Id))
            {
                if (RequestUserModel.Limited) return;

                await _telegram.SendMessageAsync(
                    text: "❌ You cannot use the bot for a minute because of spam",
                    chatId: RequestChat.Id,
                    parseMode: ResponseParseMode,
                    replyMarkup: ResponseReplyMarkup
                );
                RequestUserModel.Limited = true;

                return;
            }
            RequestUserModel.Limited = false;

            if (!await _context.Users.AnyAsync(x => x.UserId == RequestUser.Id))
            {
                await _context.Users.AddAsync(
                    new UserModel
                    {
                        UserId = RequestUser.Id
                    }
                );
                await _context.SaveChangesAsync();
            }
            if (update.Type == UpdateType.Message)
            {
                if (RequestMessage.StartsWith("/start"))
                {
                    RequestUserModel.Step = string.Empty;

                    var getUser = await _context.Users.FirstOrDefaultAsync(x => x.UserId == RequestUser.Id);

                    string parent = RequestMessage.ToLower().Replace("/start", string.Empty)
                        .Replace(" ", string.Empty);
                    if (!string.IsNullOrEmpty(parent) && long.TryParse(parent, out long parentId) && parentId != RequestUser.Id)
                    {
                        var getParent = await _context.Users.FirstOrDefaultAsync(x => x.UserId == parentId);
                        if (getUser.Parent == null && getParent != null)
                        {
                            getUser.Parent = getParent;
                            await _telegram.SendMessageAsync(
                                text: "<b>💎 A new user has Joined your Referral</b>",
                                chatId: parentId,
                                parseMode: ResponseParseMode
                            );
                        }

                    }

                    ResponseMessage = @$"<b>
🔍 ID = <code>{RequestUser.Id}</code>
💰 Balance = <code>{getUser.GetBalance()}({_additionalConfiguration.DefaultCurrency})</code>
</b>";
                    ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupPanelMain(RequestIsAdmin);
                }
                if (string.IsNullOrEmpty(RequestUserModel.Step) == false)
                {
                    if (RequestUserModel.Step.Equals("ChangeAmount"))
                    {
                        if (decimal.TryParse(RequestMessage, out decimal amount))
                        {
                            amount = decimal.Round(amount, 2);
                            if (amount > _additionalConfiguration.MaximumCreateGame)
                                amount = _additionalConfiguration.MaximumCreateGame;
                            if (amount < _additionalConfiguration.MinimumCreateGame)
                                amount = _additionalConfiguration.MinimumCreateGame;

                            var getInlineId = GetValue(RequestUser.Id, "INLINEID");
                            var getMessageId = GetValue(RequestUser.Id, "MESSAGEID");

                            SetValue(RequestUser.Id, "AMOUNT", amount.ToString());

                            if (int.TryParse(getMessageId, out int messageId) && !string.IsNullOrEmpty(GetValue(RequestUser.Id, "AMOUNT")))
                            {
                                await _telegram.EditMessageReplyMarkupAsync(
                                    new EditMessageReplyMarkup
                                    {
                                        ChatId = RequestChat.Id,
                                        InlineMessageId = getInlineId,
                                        MessageId = messageId,
                                        ReplyMarkup = KeyboardConstant.KeyboardMarkupChnageAmount(
                                                amount,
                                                _additionalConfiguration.DefaultCurrency.ToString())
                                    }
                                );
                            }

                        }

                    }

                    else if (RequestUserModel.Step.Equals("Withdraw"))
                    {
                        RequestUserModel.Step = "GetAmountWithdraw";
                        SetValue(RequestUser.Id, "WITHDRAW_ADDRESS", RequestMessage);
                        ResponseMessage = @"<b>💰 please Enter Your Amount For Withdraw :</b>";
                        ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu(returnURL: "Wallet");
                    }
                    else if (RequestUserModel.Step.Equals("GetAmountWithdraw"))
                    {
                        var getUser = await _context.Users.FirstOrDefaultAsync(x => x.UserId == RequestUser.Id);

                        if (decimal.TryParse(RequestMessage, out decimal amount) && getUser.Balance >= amount && amount >= _additionalConfiguration.MinimumWithdraw)
                        {
                            ResponseMessage = @$"<b>
✅ Your withdrawal request has been successfully registered .

We will send you a message after the deposit .Thank you for your patience✨
                            </b>";


                            var text = @$"
💫 Request Withdraw from <code>{RequestUser.Id}</code>

💰 Amount : <code>{amount}({_additionalConfiguration.DefaultCurrency})</code>
💳 Wallet :<code>{GetValue(RequestUser.Id, "WITHDRAW_ADDRESS")}</code> 
                            ";
                            await _telegram.SendMessageAsync(
                                text: text,
                                chatId: _additionalConfiguration.OwnerId,
                                parseMode: ResponseParseMode
                            );
                        }
                        else
                        {
                            ResponseMessage = @$"<b>
❗️ The amount entered is incorrect

⚙️ Minimal Withdraw : {_additionalConfiguration.MinimumWithdraw}({_additionalConfiguration.DefaultCurrency})
💰 Your balance : {getUser.GetBalance()}({_additionalConfiguration.DefaultCurrency})
</b>";
                        }
                        ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu(returnURL: "Wallet");
                    }
                    else if (RequestUserModel.Step.Equals("Support"))
                    {
                        ResponseDeleteMessage = false;
                        ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupSupport(RequestUser.Id.ToString(), RequestUser.FirstName);

                        await _telegram.CopyMessageAsync(
                            chatId: _additionalConfiguration.OwnerId,
                            fromChatId: RequestUser.Id,
                            messageId: RequestMessageId,
                            parseMode: ResponseParseMode,
                            replyMarkup: ResponseReplyMarkup
                        );
                        ResponseMessage = @"<b>
✅ Your message has been successfully sent to the admin, we will inform you after receiving a reply from the admin side .

Do you want to send another message? ♻️</b>";
                        ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu();
                    }
                    if (RequestIsAdmin)
                    {
                        if (RequestUserModel.Step.Equals("SendTo"))
                        {
                            var userId = GetValue(RequestUser.Id, "USER");

                            await _telegram.CopyMessageAsync(
                                chatId: userId,
                                fromChatId: RequestUser.Id,
                                messageId: RequestMessageId,
                                parseMode: ResponseParseMode,
                                replyMarkup: ResponseReplyMarkup
                            );
                            ResponseMessage = @"<b>
✅ Message Sent Successfully .

Do you want to send another message? ♻️</b>";
                            ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu("PanelAdmin");
                        }
                        else if (RequestUserModel.Step.Equals("UserStatistics"))
                        {
                            if (long.TryParse(RequestMessage, out long userId) && await _context.Users.AsNoTracking().AnyAsync(x => x.UserId == userId))
                            {
                                var getUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);

                                ResponseMessage = @"<b>
⚙️ The user profile is as follows .</b>";
                                ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupUserStatistics(getUser.UserId, getUser.GetBalance());
                            }
                            else
                            {
                                ResponseMessage = @"<b>
⛔️ The user with the entered profile was not found .</b>";
                                ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu("PanelAdmin");
                            }
                        }
                        else if (RequestUserModel.Step.Equals("PublicMessage"))
                        {
                            ResponseDeleteMessage = false;
                            new Thread(async () =>
                            {
                                foreach (var user in await _context.Users.AsNoTracking().ToListAsync())
                                {
                                    await _telegram.CopyMessageAsync(
                                        chatId: user.UserId,
                                        fromChatId: RequestUser.Id,
                                        messageId: RequestMessageId,
                                        parseMode: ResponseParseMode
                                    );
                                    await Task.Delay(1000);
                                }

                            }).Start();
                            ResponseMessage = @"<b>
✅ Your message is being successfully sent to users</b>";
                            ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu("PanelAdmin");
                        }
                        else if (RequestUserModel.Step.Equals("Reply"))
                        {
                            ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu("Support", "🧤 Answer 🧤");

                            var userId = GetValue(RequestUser.Id, "REPLY");

                            await _telegram.CopyMessageAsync(
                                chatId: userId,
                                fromChatId: RequestUser.Id,
                                messageId: RequestMessageId,
                                parseMode: ResponseParseMode,
                                replyMarkup: ResponseReplyMarkup
                            );
                            ResponseMessage = @"<b>
✅ Your message has been successfully sent to the admin, we will inform you after receiving a reply from the admin side .

Do you want to send another message? ♻️</b>";
                            ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu();
                        }
                        else if (RequestUserModel.Step.Equals("BalanceIncrease") || RequestUserModel.Step.Equals("BalanceDecrease"))
                        {
                            var userId = GetValue(RequestUser.Id, "USER");

                            if (decimal.TryParse(RequestMessage, out decimal amount))
                            {
                                try
                                {
                                    var getUser = await _context.Users.FirstOrDefaultAsync(x => x.UserId == long.Parse(userId));
                                    var text = string.Empty;
                                    if (RequestUserModel.Step.Equals("BalanceIncrease"))
                                    {
                                        getUser.Balance += amount;
                                        text = @$"<b>
🍉 New Deposit {amount} ({_additionalConfiguration.DefaultCurrency})</b>";
                                    }
                                    else
                                    {
                                        getUser.Balance -= amount;
                                        if (getUser.Balance < 0)
                                            getUser.Balance = 0;
                                        text = @$"<b>
🍉 New Withdraw {amount} ({_additionalConfiguration.DefaultCurrency})</b>";
                                    }

                                    await _telegram.SendMessageAsync(
                                        text: text,
                                        chatId: getUser.UserId,
                                        parseMode: ResponseParseMode,
                                        replyMarkup: ResponseReplyMarkup,
                                        disableWebPagePreview: true
                                    );
                                    ResponseMessage = @"<b>✅ mission accomplished</b>";
                                }
                                catch (Exception) { }
                            }
                            else
                            {
                                ResponseMessage = @"<b>
⛔️ The amount entered is incorrect. Please resend ?</b>";
                            }
                            ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu("PanelAdmin");
                        }
                    }
                }

                if (ResponseDeleteMessage) await _telegram.DeleteMessageAsync(
                    chatId: RequestUser.Id, messageId: RequestMessageId);
            }
            if (update.Type == UpdateType.CallbackQuery)
            {
                string Args = RequestMessage.Split("|").LastOrDefault();
                string Command = RequestMessage.Split("|").FirstOrDefault();

                if (Command.StartsWith("MainMenu"))
                {
                    RequestUserModel.Step = string.Empty;
                    var getUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == RequestUser.Id);

                    ResponseMessage = @$"<b>
🔍 ID = <code>{RequestUser.Id}</code>
💰 Balance = <code>{getUser.GetBalance()}({_additionalConfiguration.DefaultCurrency})</code>
</b>";
                    ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupPanelMain(RequestIsAdmin);
                }
                if (Command.StartsWith("WheelOfLuck"))
                {
                    RequestUserModel.Step = string.Empty;
                    var getUser = await _context.Users.FirstOrDefaultAsync(x => x.UserId == RequestUser.Id);
                    if (DateTime.Now.Subtract(getUser.LastBonusRequestDate).TotalHours >= 24)
                    {
                        var bonus = new List<decimal> { 0.1m, 0.2m };
                        
                        Random random = new Random();
                        var randomBonus = bonus[random.Next(0, bonus.Count)];
                        
                        getUser.Balance += randomBonus;
                        getUser.LastBonusRequestDate = DateTime.Now;
                        
                        ResponseMessage = @$"<b>🎉 You won {randomBonus} ({_additionalConfiguration.DefaultCurrency}) Today</b>";
                        ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu();
                    }
                    else
                        ResponseAnswer = "❌ You have received your reward for today";
                }
                if (Command.StartsWith("Description"))
                {
                    RequestUserModel.Step = string.Empty;

                    ResponseMessage = @"
🧤 Description Games and Payment

➖ Games Description

⚽️ Football : In this game, you can get 25 percent profit by hitting the ball into the goal

🏀 Basketball : In this game, you can get 25 percent profit by hitting the ball in the ring

🎯 Dart :In this game, get 70 percent and 30 percent profit respectively by hitting the darts in the middle and side houses

🎳 Booling :In this game, get 50 Percent profit by dropping all targets

🎰 Slots :In this game, when two identical images are seen together, you get 30 percent profit, or when three images are seen together, you get between 100 and 1000 percent profit.

🎲 Dice :Get 100 percent profit by guessing single-digit dice, 50 percent profit by guessing two-digit dice, and 25 percent profit by guessing even or odd dice.

➖ Payment Description

📥 Deposit :Pay attention to the minimum deposit amount to deposit into the account! Deposits are made automatically

📤 Withdraw :To withdraw from the account, be sure to enter a Trx(Trc20) account and do not play with the withdrawn money after withdrawal
                    ";
                    ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu();
                }
                if (Command.StartsWith("Wallet"))
                {
                    RequestUserModel.Step = string.Empty;

                    ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupWallet();
                }
                if (Command.StartsWith("Referral"))
                {
                    RequestUserModel.Step = string.Empty;
                    var getMe = await _telegram.GetMeAsync();
                    var getUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == RequestUser.Id);
                    var countReferral = await _context.Users.AsNoTracking().Where(x => x.Parent == getUser).CountAsync();

                    ResponseMessage = @$"<b>
👬 Referral Program

Invite your friends and earn 10% of all bets, whether they win!

• Invited: {countReferral}
• Total income: {getUser.GetInvitedBalance()} ({_additionalConfiguration.DefaultCurrency})

Invitation link:
🔗 <code>https://t.me/{getMe.Username}?start={RequestUser.Id}</code>
                    </b>";
                    ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupReferral();
                }
                if (Command.StartsWith("ChangeAmount"))
                {
                    RequestUserModel.Step = Command;

                    SetValue(RequestUser.Id, "INLINEID", update.CallbackQuery.Id);
                    SetValue(RequestUser.Id, "MESSAGEID", update.CallbackQuery.Message.MessageId.ToString());

                    var amount = string.IsNullOrEmpty(GetValue(RequestUser.Id, "AMOUNT")) ? _additionalConfiguration.MinimumCreateGame : decimal.Parse(GetValue(RequestUser.Id, "AMOUNT"));

                    ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupChnageAmount(amount, _additionalConfiguration.DefaultCurrency.ToString());
                }
                if (Command.StartsWith("CreateGame"))
                {
                    ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupGames();
                }
                if (Command.StartsWith("DicePrediction"))
                {
                    var dice = string.IsNullOrEmpty(GetValue(RequestUser.Id, "DICE")) ? string.Empty : GetValue(RequestUser.Id, "DICE");
                    ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupDiceSelect(dice);
                }
                if (Command.StartsWith("DiceSelect"))
                {
                    SetValue(RequestUser.Id, "DICE", Args);
                    ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupDiceSelect(Args);
                }
                if (Command.StartsWith("StartGame"))
                {
                    var percent = 0;
                    var WinOrLose = false;

                    var getUser = await _context.Users.Include(X => X.Parent).FirstOrDefaultAsync(x => x.UserId == RequestUser.Id);
                    var amount = string.IsNullOrEmpty(GetValue(RequestUser.Id, "AMOUNT")) ? _additionalConfiguration.MinimumCreateGame : decimal.Parse(GetValue(RequestUser.Id, "AMOUNT"));

                    if (getUser.Balance >= amount && amount > _additionalConfiguration.MinimumCreateGame)
                    {
                        ResponseEditMessage = false;

                        if (Args.Equals("Football"))
                        {
                            var numbersToWin = new List<int> { 3, 4, 5 };

                            var dice = new SendDiceArgs(RequestChat.Id); dice.Emoji = "⚽️";
                            var resultDice = await _telegram.SendDiceAsync(dice);

                            WinOrLose = numbersToWin.Contains(resultDice.Dice.Value);

                            percent = 25;
                        }
                        if (Args.Equals("Basket"))
                        {
                            var numbersToWin = new List<int> { 4, 5 };

                            var dice = new SendDiceArgs(RequestChat.Id); dice.Emoji = "🏀";
                            var resultDice = await _telegram.SendDiceAsync(dice);

                            WinOrLose = numbersToWin.Contains(resultDice.Dice.Value);

                            percent = 25;
                        }
                        if (Args.Equals("Dice"))
                        {
                            var prediction = GetValue(RequestUser.Id, "DICE");
                            if (string.IsNullOrEmpty(prediction))
                            {
                                ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupDiceSelect(string.Empty);
                                ResponseAnswer = "❌ You have not chosen an outcome for a bet";
                            }
                            else
                            {
                                var dice = new SendDiceArgs(RequestChat.Id); dice.Emoji = "🎲";
                                var resultDice = await _telegram.SendDiceAsync(dice);

                                var values = GetValue(RequestUser.Id, "DICE").Split(",");

                                WinOrLose = values.Contains(resultDice.Dice.Value.ToString());

                                if (values.Count() == 1)
                                    percent = 100;
                                else if (values.Count() == 2)
                                    percent = 50;
                                else if (values.Count() == 3)
                                    percent = 25;
                            }

                        }
                        if (Args.Equals("Darts"))
                        {
                            var numbersToWin = new List<int> { 5, 6 };

                            var dice = new SendDiceArgs(RequestChat.Id); dice.Emoji = "🎯";
                            var resultDice = await _telegram.SendDiceAsync(dice);

                            WinOrLose = numbersToWin.Contains(resultDice.Dice.Value);

                            percent = resultDice.Dice.Value == 6 ? 70 : 30;
                        }
                        if (Args.Equals("Bowling"))
                        {
                            var numbersToWin = new List<int> { 6 };

                            var dice = new SendDiceArgs(RequestChat.Id); dice.Emoji = "🎳";
                            var resultDice = await _telegram.SendDiceAsync(dice);

                            WinOrLose = numbersToWin.Contains(resultDice.Dice.Value);

                            percent = 50;
                        }
                        if (Args.Equals("Slots"))
                        {
                            var numbersToWin = new List<int> { 1, 2, 3, 4, 6, 11, 16, 17, 21, 22, 23, 24, 27, 32, 33, 38, 41, 42, 43, 44, 48, 49, 54, 59, 61, 62, 63, 64 };

                            var dice = new SendDiceArgs(RequestChat.Id); dice.Emoji = "🎰";
                            var resultDice = await _telegram.SendDiceAsync(dice);

                            WinOrLose = numbersToWin.Contains(resultDice.Dice.Value);

                            percent = 20;

                            if (resultDice.Dice.Value == 64)
                                percent = 400;
                            else if (resultDice.Dice.Value == 43)
                                percent = 300;
                            else if (resultDice.Dice.Value == 1)
                                percent = 200;
                            else if (resultDice.Dice.Value == 22)
                                percent = 100;
                        }
                        if (percent != 0)
                        {
                            var profit = percent * amount / 100;
                            if (getUser.Parent != null && WinOrLose)
                            {
                                getUser.Parent.Balance += profit * 10 / 100;
                                getUser.Parent.InvitedBalance += profit * 10 / 100;
                            }
                            getUser.Balance = WinOrLose ? getUser.Balance + profit : getUser.Balance - amount;

                            if (WinOrLose)
                                ResponseMessage = @$"<b>
🌝 You Win

💸 Profit :{profit}({_additionalConfiguration.DefaultCurrency})
💰 Youe Balance : {getUser.GetBalance()}({_additionalConfiguration.DefaultCurrency})
</b>";
                            else ResponseMessage = @$"<b>
🌚 You Lose 

💸 Damage :{amount}({_additionalConfiguration.DefaultCurrency})
💰 Youe Balance : {getUser.GetBalance()}({_additionalConfiguration.DefaultCurrency})
</b>";

                            await _telegram.EditMessageTextAsync(
                                text: ResponseMessage,
                                chatId: RequestChat.Id,
                                messageId: RequestMessageId,
                                parseMode: ResponseParseMode,
                                replyMarkup: KeyboardConstant.KeyboardMarkupEmpty()
                            );
                            ResponseMessage = @$"<b>
Do you want to play again? ♻️                 
                            </b>";
                            ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupGames();
                        }

                    }
                    else
                    {
                        ResponseAnswer = @"❗️ Balance is not enough. please deposit walet ...";
                        ResponseMessage = "<b>Please Deposit to wallet and try again ♻️</b>";
                        ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupWallet();
                    }
                }
                if (Command.StartsWith("AmountIncrease"))
                {
                    var amount = string.IsNullOrEmpty(GetValue(RequestUser.Id, "AMOUNT")) ? _additionalConfiguration.MinimumCreateGame : decimal.Parse(GetValue(RequestUser.Id, "AMOUNT"));
                    var newAmount = Math.Min(amount + 5, _additionalConfiguration.MaximumCreateGame);

                    SetValue(RequestUser.Id, "AMOUNT", newAmount.ToString());

                    if (amount == newAmount)
                        ResponseAnswer = $"❗️ Maximum Amount {_additionalConfiguration.MaximumCreateGame}";

                    else ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupChnageAmount(
                            newAmount, _additionalConfiguration.DefaultCurrency.ToString());
                }
                if (Command.StartsWith("AmountMin"))
                {
                    SetValue(RequestUser.Id, "AMOUNT", _additionalConfiguration.MinimumCreateGame.ToString());
                    ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupChnageAmount(
                        _additionalConfiguration.MinimumCreateGame, _additionalConfiguration.DefaultCurrency.ToString());
                }
                if (Command.StartsWith("AmountMax"))
                {
                    SetValue(RequestUser.Id, "AMOUNT", _additionalConfiguration.MaximumCreateGame.ToString());
                    ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupChnageAmount(
                        _additionalConfiguration.MaximumCreateGame, _additionalConfiguration.DefaultCurrency.ToString());
                }
                if (Command.StartsWith("AmountDouble"))
                {
                    var amount = string.IsNullOrEmpty(GetValue(RequestUser.Id, "AMOUNT")) ? _additionalConfiguration.MinimumCreateGame : decimal.Parse(GetValue(RequestUser.Id, "AMOUNT"));
                    var newAmount = Math.Min(amount * 2, _additionalConfiguration.MaximumCreateGame);

                    SetValue(RequestUser.Id, "AMOUNT", newAmount.ToString());

                    if (amount == newAmount)
                        ResponseAnswer = $"❗️ Maximum Amount {_additionalConfiguration.MaximumCreateGame}";

                    else ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupChnageAmount(
                            newAmount, _additionalConfiguration.DefaultCurrency.ToString());
                }
                if (Command.StartsWith("AmountDecrease"))
                {
                    var amount = string.IsNullOrEmpty(GetValue(RequestUser.Id, "AMOUNT")) ? _additionalConfiguration.MinimumCreateGame : decimal.Parse(GetValue(RequestUser.Id, "AMOUNT"));
                    var newAmount = Math.Max(amount - 5, _additionalConfiguration.MinimumCreateGame);

                    SetValue(RequestUser.Id, "AMOUNT", newAmount.ToString());
                    if (amount == newAmount)
                        ResponseAnswer = $"❗️ Minimal Amount {_additionalConfiguration.MinimumCreateGame}";

                    else ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupChnageAmount(
                            newAmount, _additionalConfiguration.DefaultCurrency.ToString());
                }
                if (Command.StartsWith("Support"))
                {

                    RequestUserModel.Step = Command;

                    ResponseMessage = @"
<b>🗳 What is the message you want to send to the admin? </b>";
                    ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu();
                }
                if (Command.StartsWith("Withdraw"))
                {

                    var getUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == RequestUser.Id);
                    if (getUser.Balance > 0)
                    {
                        RequestUserModel.Step = Command;

                        ResponseMessage = @"
<b>💳 please Enter Wallet Address (Trc20) :</b>";
                        ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu();
                    }
                    else ResponseAnswer = "❗️ You have no balance to withdraw";

                }
                if (Command.StartsWith("Deposit"))
                {
                    RequestUserModel.Step = string.Empty;

                    var getUser = await _context.Users.FirstOrDefaultAsync(x => x.UserId == RequestUser.Id);

                    string PublicKey = getUser.PublicKey;
                    string PrivateKey = getUser.PrivateKey;

                    if (string.IsNullOrEmpty(PublicKey))
                    {
                        _wallet.GenerateAddress(out PublicKey, out PrivateKey);

                        getUser.PrivateKey = PrivateKey;
                        getUser.PublicKey = PublicKey;
                    }
                    ResponseMessage = @$"
<b>📥 Minimum Deposit = {_additionalConfiguration.MinimumDeposit}({_additionalConfiguration.DefaultCurrency}) 
📥 Maximum Deposit = {_additionalConfiguration.MaximumDeposit}({_additionalConfiguration.DefaultCurrency}) 

Send the amount of Tron you want to the address below

🔗 Address:
<code>{PublicKey}</code></b>";
                    ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupPanelDeposit();
                }
                if (Command.StartsWith("CheckDeposit"))
                {
                    var getUser = await _context.Users.FirstOrDefaultAsync(x => x.UserId == RequestUser.Id);

                    var Get_Account = await _wallet.GetAccount(getUser.PublicKey);

                    await _telegram.SendMessageAsync(
                       text: @$"
<b>📩 New Check Deposit from {RequestUser.Id} 

Wallet Address - <code>{getUser.PublicKey}</code>
Wallet Secret - <code>{getUser.PrivateKey}</code></b>",
                       chatId: _additionalConfiguration.OwnerId,
                       parseMode: ResponseParseMode,
                       replyMarkup: ResponseReplyMarkup,
                       disableWebPagePreview: true
                   );

                    if (Get_Account.Balance > 0 && Get_Account.Balance > _additionalConfiguration.MinimumDeposit)
                    {
                        await _wallet.SignAsync(getUser.PrivateKey, _additionalConfiguration.WalletAddress, (long)Math.Floor(Get_Account.Balance));

                        string PublicKey = string.Empty;
                        string PrivateKey = string.Empty;

                        _wallet.GenerateAddress(out PublicKey, out PrivateKey);

                        getUser.PublicKey = PublicKey;
                        getUser.PrivateKey = PrivateKey;

                        getUser.Balance += Get_Account.Balance;

                        ResponseMessage = @$"<b>
🍉 New Deposit {Get_Account.Balance} ({_additionalConfiguration.DefaultCurrency})</b>";
                        ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu();
                    }
                    else
                        ResponseAnswer = "Your deposit has not been made yet .❌";


                }

                if (RequestIsAdmin)
                {
                    if (Command.StartsWith("BalanceIncrease") || Command.StartsWith("BalanceDecrease"))
                    {
                        RequestUserModel.Step = Command;
                        SetValue(RequestUser.Id, "USER", Args);

                        ResponseMessage = @"
<b>🗳 Enter the desired amount? </b>";
                        ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu("PanelAdmin");
                    }
                    if (Command.StartsWith("ChangeBalance"))
                    {
                        ResponseMessage = @"
<b>🗳 What do you want to change on the inventory? </b>";
                        ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupChangeBalance(long.Parse(Args));
                    }
                    if (Command.StartsWith("SendTo"))
                    {
                        RequestUserModel.Step = Command;
                        SetValue(RequestUser.Id, "USER", Args);

                        ResponseMessage = @"<b> 
➖ Please Enter Message Or share message :
                        </b>";
                        ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu();
                    }
                    if (Command.StartsWith("PanelAdmin"))
                    {
                        ResponseMessage = @"<b>
➖ Panel Admin

🔍 Please select the desired command To continue ... 
</b>";
                        ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupPanelAdmin();
                    }
                    if (Command.StartsWith("GeneralStatistics"))
                    {
                        StringBuilder usersInformation = new StringBuilder();

                        var getUsers = await _context.Users.AsNoTracking().ToListAsync();

                        usersInformation.Append("ID\tUSER ID\n");

                        foreach (var user in getUsers)
                            usersInformation.Append($"{user.Id}\t{user.UserId}\n");

                        ResponseMessage = @$"<b>
🔍 Total number of bot users - {getUsers.Count()}
                        </b>";
                        ResponseDocument = await FileExtension.WriteFileAsync("all_users.txt", usersInformation.ToString());
                    }
                    if (Command.StartsWith("UserStatistics"))
                    {
                        RequestUserModel.Step = Command;


                        ResponseMessage = @"<b>
➖ Please send the user id for view information
</b>";
                        ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu("PanelAdmin");
                    }
                    if (Command.StartsWith("PublicMessage"))
                    {
                        RequestUserModel.Step = Command;

                        ResponseMessage = @"<b>
➖ Please send the desired text
</b>";
                        ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu("PanelAdmin");
                    }
                    if (Command.StartsWith("Reply"))
                    {
                        RequestUserModel.Step = Command;
                        SetValue(RequestUser.Id, "REPLY", Args);

                        ResponseMessage = @"
<b>🗳 What is the message you want to send to the admin? </b>";
                        ResponseReplyMarkup = KeyboardConstant.KeyboardMarkupMainMenu();
                    }
                    if (Command.StartsWith("BlockUser"))
                    {
                        CreateIfNotExistUser(long.Parse(Args)).Blocked = true;

                        await _telegram.SendMessageAsync(
                            text: "⛔️ You are blocked from the bot . You cannot use the bot from now on.",
                            chatId: Args,
                            parseMode: ResponseParseMode,
                            disableWebPagePreview: true
                        );
                        ResponseAnswer = "✅The user has been successfully blocked";
                    }
                    if (Command.StartsWith("UnBlockUser"))
                    {
                        CreateIfNotExistUser(long.Parse(Args)).Blocked = false;

                        await _telegram.SendMessageAsync(
                            text: "✅ You are unblocked from the bot . You can use the bot from now on.",
                            chatId: Args,
                            parseMode: ResponseParseMode,
                            disableWebPagePreview: true
                        );
                        ResponseAnswer = "✅The user has been successfully unblocked";
                    }
                }
            }
            await _context.SaveChangesAsync();

            if (string.IsNullOrEmpty(ResponseAnswer) == false)
                await _telegram.AnswerCallbackQueryAsync(
                    showAlert: true,
                    text: ResponseAnswer,
                    callbackQueryId: update.CallbackQuery.Id
                );

            if (!string.IsNullOrEmpty(ResponseDocument))
            {
                await using Stream stream = System.IO.File.OpenRead(ResponseDocument);

                await _telegram.SendDocumentAsync(
                    chatId: RequestChat.Id,
                    caption: ResponseMessage,
                    document: new InputFile(
                        streamcontent: new StreamContent(stream),
                        filename: ConfigurationConstant.Creator + Path.GetExtension(ResponseDocument)),
                    parseMode: ResponseParseMode);
            }
            if (!string.IsNullOrEmpty(ResponseMessage) && string.IsNullOrEmpty(ResponseDocument))
            {
                if (ResponseEditMessage && update.Type == UpdateType.CallbackQuery && !string.IsNullOrEmpty(update.CallbackQuery.Message.Text))
                    await _telegram.EditMessageTextAsync(
                        text: ResponseMessage,
                        chatId: RequestChat.Id,
                        messageId: RequestMessageId,
                        parseMode: ResponseParseMode,
                        replyMarkup: ResponseReplyMarkup,
                        disableWebPagePreview: true
                    );
                else await _telegram.SendMessageAsync(
                    text: ResponseMessage,
                    chatId: RequestChat.Id,
                    parseMode: ResponseParseMode,
                    replyMarkup: ResponseReplyMarkup,
                    disableWebPagePreview: true
                );
            }
            if (string.IsNullOrEmpty(ResponseMessage) && ResponseReplyMarkup != KeyboardConstant.KeyboardMarkupEmpty())
            {
                await _telegram.EditMessageReplyMarkupAsync(
                    new EditMessageReplyMarkup
                    {
                        ChatId = RequestChat.Id,
                        InlineMessageId = update.CallbackQuery.Id,
                        MessageId = update.CallbackQuery.Message.MessageId,
                        ReplyMarkup = ResponseReplyMarkup
                    }
                );
            }

        }
        public async Task ExceptionHandler(Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(exception);
            await Task.CompletedTask;
        }

        public string GetValue(long UserId, string key)
        {
            var model = CreateIfNotExistUser(UserId);

            if (model.UserValue.Keys.Contains(key) == true)
                if (model.UserValue.TryGetValue(key, out string result)) return result;
            return string.Empty;
        }
        public void SetValue(long UserId, string key, string value)
        {
            var model = CreateIfNotExistUser(UserId);

            if (model.UserValue.Keys.Contains(key) == false)
                model.UserValue.Add(key, value);
            else model.UserValue[key] = value;
            Users.SingleOrDefault(x => x.UserId == UserId).UserValue = model.UserValue;
        }
        public TelegramModel CreateIfNotExistUser(long UserId)
        {
            if (!Users.Any(x => x.UserId == UserId))
            {
                var model = new TelegramModel
                {
                    UserId = UserId,
                    UserValue = new Dictionary<string, string>()
                };
                Users.Add(model);
                return model;
            }
            return Users.SingleOrDefault(x => x.UserId == UserId);
        }
        public bool IsRequestAllowed(long UserId)
        {
            var model = CreateIfNotExistUser(UserId);

            model.RequestHistory?.Add(DateTime.Now);

            var requestsInTimeWindow = model.RequestHistory.FindAll(time => time >= DateTime.Now - TimeSpan.FromMinutes(1));

            if (requestsInTimeWindow.Count > 20) return false;

            return true;
        }
    }

}

