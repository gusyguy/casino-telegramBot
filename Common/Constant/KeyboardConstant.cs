using Presentation.Common.Models;
using Telegram.BotAPI.AvailableTypes;

namespace Presentation.Common.Constant;

public class KeyboardConstant
{
    public static InlineKeyboardMarkup KeyboardMarkupPanelMain(bool showAdminPanel)
    {
        var keyboard = new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "🎲 Games", callbackData: "ChangeAmount"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "💰 Wallet", callbackData: "Wallet"),
                InlineKeyboardButton.SetCallbackData(text: "💬 Support", callbackData: "Support"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "👬 Referral", callbackData: "Referral"),
                InlineKeyboardButton.SetCallbackData(text: "📚 Description", callbackData: "Description"),
            },
        };
        if (showAdminPanel)
            keyboard.Add(new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "👨‍✈️ Admin Panel", callbackData: "PanelAdmin"),
            });
        return new InlineKeyboardMarkup(keyboard);
    }

    public static InlineKeyboardMarkup KeyboardMarkupReferral()
    {
        var keyboard = new[]
        {
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "🔙", callbackData: "MainMenu"),
            },

        };
        return new InlineKeyboardMarkup(keyboard);
    }
    public static InlineKeyboardMarkup KeyboardMarkupPanelDeposit()
    {
        var keyboard = new[]
        {
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "🔍 DePosIt VeriFy", callbackData: "CheckDeposit"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "🔙", callbackData: "Wallet"),
            },
        };
        return new InlineKeyboardMarkup(keyboard);
    }
    public static InlineKeyboardMarkup KeyboardMarkupMainMenu(string returnURL = "MainMenu", string text = "🔙")
    {
        var keyboard = new[]
        {
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: text, callbackData: returnURL),
            },

        };
        return new InlineKeyboardMarkup(keyboard);
    }
    public static InlineKeyboardMarkup KeyboardMarkupSupport(string ChatId, string Fullname)
    {
        var keyboard = new[]
        {
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "🧤 Answer", callbackData: $"Reply|{ChatId}"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: Fullname, callbackData: $"Null"),
                InlineKeyboardButton.SetCallbackData(text: ChatId, callbackData: $"Null"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "🔙", callbackData: "MainMenu"),
            },

        };
        return new InlineKeyboardMarkup(keyboard);
    }

    public static InlineKeyboardMarkup KeyboardMarkupWallet()
    {
        var keyboard = new[]
        {
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "📥 Deposit", callbackData: $"Deposit"),
                InlineKeyboardButton.SetCallbackData(text: "📤 Withdraw", callbackData: $"Withdraw"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "🔙", callbackData: $"MainMenu"),
            },

        };
        return new InlineKeyboardMarkup(keyboard);
    }
    public static InlineKeyboardMarkup KeyboardMarkupPanelAdmin()
    {
        var keyboard = new[]
        {
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "💊 Public Message", callbackData: $"PublicMessage"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "🪦 General Info", callbackData: $"GeneralStatistics"),
                InlineKeyboardButton.SetCallbackData(text: "🚨  User Info", callbackData: $"UserStatistics"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "🔙", callbackData: $"MainMenu"),
            },

        };
        return new InlineKeyboardMarkup(keyboard);
    }
    public static InlineKeyboardMarkup KeyboardMarkupChangeBalance(long userId)
    {
        var keyboard = new[]
        {
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "📈 Increase", callbackData: $"BalanceIncrease|{userId}"),
                InlineKeyboardButton.SetCallbackData(text: "📉 Decrease", callbackData: $"BalanceDecrease|{userId}"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "🔙", callbackData: $"UserStatistics|{userId}"),
            },

        };
        return new InlineKeyboardMarkup(keyboard);
    }
    public static InlineKeyboardMarkup KeyboardMarkupUserStatistics(long userId, decimal balance)
    {
        var keyboard = new[]
        {
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "🔒 Block", callbackData: $"BlockUser|{userId}"),
                InlineKeyboardButton.SetCallbackData(text: "🔓 UnBlock", callbackData: $"UnBlockUser|{userId}"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: $"🆔 {userId}", callbackData: $"SendTo|{userId}"),
                InlineKeyboardButton.SetCallbackData(text: $"💰 {balance}", callbackData: $"ChangeBalance|{userId}"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "🔙", callbackData: $"PanelAdmin"),
            },
        };
        return new InlineKeyboardMarkup(keyboard);
    }
    public static InlineKeyboardMarkup KeyboardMarkupGames()
    {
        var keyboard = new[]
        {
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "🎲", callbackData: $"DicePrediction"),
                InlineKeyboardButton.SetCallbackData(text: "🎰", callbackData: $"StartGame|Slots"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "🏀", callbackData: $"StartGame|Basket"),
                InlineKeyboardButton.SetCallbackData(text: "🎯", callbackData: $"StartGame|Darts"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "🎳", callbackData: $"StartGame|Bowling"),
                InlineKeyboardButton.SetCallbackData(text: "⚽️", callbackData: $"StartGame|Football"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "⚙️ Bet Sizing", callbackData: $"ChangeAmount"),
            },

        };
        return new InlineKeyboardMarkup(keyboard);
    }
    public static InlineKeyboardMarkup KeyboardMarkupDiceSelect(string select)
    {
        var keyboard = new[]
        {
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "1️", callbackData: $"DiceSelect|1"),
                InlineKeyboardButton.SetCallbackData(text: "2️", callbackData: $"DiceSelect|2"),
                InlineKeyboardButton.SetCallbackData(text: "3️", callbackData: $"DiceSelect|3"),
                InlineKeyboardButton.SetCallbackData(text: "4️", callbackData: $"DiceSelect|4"),
                InlineKeyboardButton.SetCallbackData(text: "5️", callbackData: $"DiceSelect|5"),
                InlineKeyboardButton.SetCallbackData(text: "6️", callbackData: $"DiceSelect|6"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "1-2", callbackData: $"DiceSelect|1,2"),
                InlineKeyboardButton.SetCallbackData(text: "3-4", callbackData: $"DiceSelect|3,4"),
                InlineKeyboardButton.SetCallbackData(text: "5-6", callbackData: $"DiceSelect|5,6"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "▫️ Even", callbackData: $"DiceSelect|1,3,5"),
                InlineKeyboardButton.SetCallbackData(text: "▫️ Odd", callbackData: $"DiceSelect|2,4,6"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "🔙", callbackData: $"ChangeAmount"),
                InlineKeyboardButton.SetCallbackData(text: "Start 🎲", callbackData: $"StartGame|Dice"),
            },

        };
        foreach (var child in keyboard)
            foreach (var item in child)
                if (item.CallbackData.EndsWith($"|{select}")) item.Text = $"‹{item.Text}›";

        return new InlineKeyboardMarkup(keyboard);
    }
    public static InlineKeyboardMarkup KeyboardMarkupChnageAmount(decimal minimal, string currency)
    {
        var keyboard = new[]
        {
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "-", callbackData: $"AmountDecrease"),
                InlineKeyboardButton.SetCallbackData(text: $"{minimal}({currency})", callbackData: $"None"),
                InlineKeyboardButton.SetCallbackData(text: "+", callbackData: $"AmountIncrease"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "Min", callbackData: $"AmountMin"),
                InlineKeyboardButton.SetCallbackData(text: "2x", callbackData: $"AmountDouble"),
                InlineKeyboardButton.SetCallbackData(text: "Max", callbackData: $"AmountMax"),
            },
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.SetCallbackData(text: "🔙", callbackData: $"MainMenu"),
                InlineKeyboardButton.SetCallbackData(text: "Start ✔️", callbackData: $"CreateGame"),
            },

        };
        return new InlineKeyboardMarkup(keyboard);
    }

    public static InlineKeyboardMarkup KeyboardMarkupEmpty()
    {
        return new InlineKeyboardMarkup();
    }
}
