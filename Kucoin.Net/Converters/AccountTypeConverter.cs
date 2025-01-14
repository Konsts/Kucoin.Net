﻿using System.Collections.Generic;
using CryptoExchange.Net.Converters;
using Kucoin.Net.Enums;

namespace Kucoin.Net.Converters
{
    internal class AccountTypeConverter : BaseConverter<AccountType>
    {
	    private readonly bool _useCaps;
        public AccountTypeConverter() : this(true) { }

        public AccountTypeConverter(bool quotes, bool useCaps = false) : base(quotes)
        {
	        _useCaps = useCaps;
        }
        protected override List<KeyValuePair<AccountType, string>> Mapping => new List<KeyValuePair<AccountType, string>>
        {
            new KeyValuePair<AccountType, string>(AccountType.Main, _useCaps ? "MAIN" : "main"),
            new KeyValuePair<AccountType, string>(AccountType.Trade, _useCaps ? "TRADE" : "trade"),
            new KeyValuePair<AccountType, string>(AccountType.Margin, _useCaps ? "MARGIN" : "margin"),
            new KeyValuePair<AccountType, string>(AccountType.Pool, _useCaps ? "POOL" : "pool"),
        };
    }
}
