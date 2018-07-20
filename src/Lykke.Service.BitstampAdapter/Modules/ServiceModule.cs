﻿using Autofac;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.Common.Log;
using Lykke.Service.BitstampAdapter.AzureRepositories;
using Lykke.Service.BitstampAdapter.AzureRepositories.Entities;
using Lykke.Service.BitstampAdapter.Services;
using Lykke.Service.BitstampAdapter.Services.Settings;
using Lykke.Service.BitstampAdapter.Settings;
using Lykke.SettingsReader;
using Microsoft.Extensions.Hosting;

namespace Lykke.Service.BitstampAdapter.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;

        public ServiceModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            // Do not register entire settings in container, pass necessary settings to services which requires them

            var settings = _appSettings.CurrentValue.BitstampAdapterService;

            builder.RegisterInstance(settings).AsSelf();

            builder.RegisterType<OrderbookPublishingService>()
                .As<IHostedService>()
                .AsSelf()
                .WithParameter(new TypedParameter(typeof(OrderbookSettings), settings.Orderbooks))
                .WithParameter(new TypedParameter(typeof(RabbitMqSettings), settings.RabbitMq))
                .WithParameter(new TypedParameter(typeof(InstrumentSettings), settings.Instruments))
                .SingleInstance();

            builder.Register(ctx =>
                    AzureTableStorage<LimitOrder>.Create(
                        _appSettings.ConnectionString(x => x.BitstampAdapterService.Db.OrdersConnString),
                        "BitstampLimitOrders",
                        ctx.Resolve<ILogFactory>()))
                .As<INoSQLTableStorage<LimitOrder>>()
                .SingleInstance();

            builder.RegisterType<LimitOrderRepository>()
                .SingleInstance()
                .AsSelf();
        }
    }
}
