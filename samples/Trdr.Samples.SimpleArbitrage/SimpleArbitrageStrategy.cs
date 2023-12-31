﻿using Trdr.Connectivity.Binance;
using Trdr.Connectivity.CoinJar.Phoenix;
using Trdr.Reactive;

namespace Trdr.Samples.SimpleArbitrage;

public sealed class SimpleArbitrageStrategy : Strategy
{
    private readonly IAsyncEnumerable<TickerPayload> _coinJarTicker;
    private readonly IAsyncEnumerable<Ticker> _binanceTicker;
    private readonly Action<decimal> _buyAtBinance;
    private readonly Action<decimal> _sellAtCoinJar;

    public SimpleArbitrageStrategy(
        IAsyncEnumerable<TickerPayload> coinJarTicker,
        IAsyncEnumerable<Ticker> binanceTicker,
        Action<decimal> buyAtBinance,
        Action<decimal> sellAtCoinJar)
    {
        _coinJarTicker = coinJarTicker ?? throw new ArgumentNullException(nameof(coinJarTicker));
        _binanceTicker = binanceTicker ?? throw new ArgumentNullException(nameof(binanceTicker));
        _buyAtBinance = buyAtBinance ?? throw new ArgumentNullException(nameof(buyAtBinance));
        _sellAtCoinJar = sellAtCoinJar ?? throw new ArgumentNullException(nameof(sellAtCoinJar));
    }

    protected override async Task Run(CancellationToken cancellationToken)
    {
        decimal buy = 0;
        decimal sell = 0;

        // Subscribe to Binance's ticker and store its ask everytime it's updated.
        // Subscribe to CoinJar's ticker and store its bid everytime it's updated.
        using var subscription = Subscribe(
            _binanceTicker.ToObservable()
                .ZipWithLatest(_coinJarTicker.ToObservable()),
            item =>
            {
                buy = item.Value.Item1.Ask;
                sell = item.Value.Item2.Bid;
            });

        // This simple strategy waits for an arbitrage opportunity by buying low at Binance
        // and selling high at CoinJar.
        //
        // Note: this is a toy trading strategy that will not work in the real world.
        // It also ignores transaction fees and bid/ask quantities.
        // This only serves to illustrate how the framework is meant to be used.
        while (await subscription.Watch(() => sell - buy > 0.002m, cancellationToken))
        {
            // Buy at Binance then sell at CoinJar
            _buyAtBinance(buy);
            _sellAtCoinJar(sell);
        }
    }
}