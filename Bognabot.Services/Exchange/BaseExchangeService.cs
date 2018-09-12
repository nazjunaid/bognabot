﻿using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Bognabot.Data.Config;
using Bognabot.Data.Exchange.Contracts;
using Bognabot.Data.Exchange.Enums;
using Bognabot.Data.Exchange.Models;
using Bognabot.Data.Models.Exchange;

namespace Bognabot.Services.Exchange
{
    public abstract class BaseExchangeService : IExchangeService
    {
        public ExchangeConfig ExchangeConfig { get; }
        public abstract DateTimeOffset Now { get; }
        
        public abstract void ConfigureMap(IMapperConfigurationExpression cfg);
        public abstract Task StartStreamingChannels();

        public abstract Task SubscribeToTradeChannel(Func func);
        public abstract Task SubscribeToBookChannel(Action<BookModel[]> onRecieve);
        public abstract Task SubscribeToCandleChannel(TimePeriod period, Action<CandleModel[]> onRecieve);

        public abstract Task GetCandlesAsync(Instrument instrument, TimePeriod timePeriod, DateTimeOffset startTime, DateTimeOffset endTime, Func<CandleModel[], Task> onRecieve);

        protected BaseExchangeService(ExchangeConfig config)
        {
            ExchangeConfig = config;
        }

        protected Instrument? ToInstrumentType(string symbol)
        {
            var instrumentKvp = ExchangeConfig.SupportedInstruments.FirstOrDefault(x => x.Value == symbol);

            if (instrumentKvp.Value == null)
                throw new ArgumentOutOfRangeException();

            return instrumentKvp.Key;
        }

        protected string ToSymbol(Instrument instrument)
        {
            var supportedInstruments = ExchangeConfig.SupportedInstruments;

            return supportedInstruments.ContainsKey(instrument)
                ? supportedInstruments[instrument]
                : throw new ArgumentOutOfRangeException();
        }

        protected string ToTimePeriod(TimePeriod period)
        {
            var supportedPeriods = ExchangeConfig.SupportedTimePeriods;

            return supportedPeriods.ContainsKey(period)
                ? supportedPeriods[period]
                : throw new ArgumentOutOfRangeException();
        }
    }
}