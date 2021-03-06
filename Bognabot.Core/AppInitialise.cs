﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using Bognabot.Data;
using Bognabot.Data.Config;
using Bognabot.Data.Config.Contracts;
using Bognabot.Data.Exchange;
using Bognabot.Data.Mapping;
using Bognabot.Domain.Entities.Instruments;
using Bognabot.Services;
using Bognabot.Services.Exchange;
using Bognabot.Services.Jobs;
using Bognabot.Services.Jobs.Jobs;
using Bognabot.Services.Repository;
using Bognabot.Services.Trader;
using Bognabot.Storage.Core;
using Bognabot.Storage.Stores;
using Bognabot.Trader;
using Bognabot.Trader.Signals;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NLog;

namespace Bognabot.Core
{
    public static class AppInitialise
    {
        private static readonly ILogger _logger;

        static AppInitialise()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        public static void AddServices(IServiceCollection services, IEnumerable<Type> exchangeServiceTypes)
        {
            _logger.Log(LogLevel.Info, "Checking exchange services...");

            var contractType = typeof(IExchangeService);

            foreach (var exchType in exchangeServiceTypes)
            {
                var exchName = exchType.Name.Replace("Service", "");

                if (!contractType.IsAssignableFrom(exchType))
                    _logger.Log(LogLevel.Warn, $"{exchType} exchange service does not derive from {contractType} and will not be loaded");

                services.AddSingleton(contractType, service => ExchangeServiceFactory(service, exchType, exchName));

                _logger.Log(LogLevel.Info, $"Found {exchName} exchange");
            }
            
            services.AddSingleton<GeneralConfig>((x) => ConfigFactory<GeneralConfig>().GetAwaiter().GetResult());
            services.AddSingleton<IConfig, GeneralConfig>(x => x.GetService<GeneralConfig>());

            services.AddSingleton<ServiceManager>();
            services.AddSingleton<RepositoryService>();
            services.AddSingleton<CandleService>();
            services.AddSingleton<JobService>();
            services.AddSingleton<SignalsService>();

            services.AddTransient<IRepository<Candle>, Repository<Candle>>();

            services.AddTransient<SignalsJob>();

            services.AddTransient<ISignal, OverBoughtOverSoldSignal>();
        }

        public static void LoadUserData(IServiceProvider serviceProvider, string appRootPath)
        {
            var exchangeServices = serviceProvider.GetServices<IExchangeService>();

            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile<DataProfile>();

                foreach (var exchangeService in exchangeServices)
                    exchangeService.ConfigureMap(cfg);
            });

            Cfg.InitialiseConfig(serviceProvider.GetServices<IConfig>());
        }

        public static Task Start(ServiceManager serviceManager)
        {
            return serviceManager.StartAsync();
        }

        private static object ExchangeServiceFactory(IServiceProvider service, Type exchType, string exchName)
        {
            var cfg = ConfigFactory<ExchangeConfig>(exchName).GetAwaiter().GetResult();

            if (cfg == null || !String.Equals(cfg.ExchangeName, exchName, StringComparison.CurrentCultureIgnoreCase))
                Cfg.Logger.Log(LogLevel.Warn, $"Cannot locate the config for {exchType.Name}");

            return Activator.CreateInstance(exchType, new object[] { service.GetService<ILogger>(), cfg });
        }

        private static async Task<T> ConfigFactory<T>(string name = null) where T : IConfig
        {
            var configName = name ?? typeof(T).Name.Replace("Config", "");

            _logger.Log(LogLevel.Info, $"Loading {configName} application data...");

            try
            {
                var config = JsonConvert.DeserializeObject<T>(await Cfg.GetConfigJson(configName));

                _logger.Log(LogLevel.Info, $"{configName} application data load complete");

                await config.LoadUserConfigAsync(Cfg.AppDataPath);

                _logger.Log(LogLevel.Info, $"{configName} user data load complete");

                return config;
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Fatal, $"Unable to load {configName} application data");
                _logger.Log(LogLevel.Fatal, e.Message);
                _logger.Log(LogLevel.Fatal, e.StackTrace);

                throw;
            }
        }
    }
}
