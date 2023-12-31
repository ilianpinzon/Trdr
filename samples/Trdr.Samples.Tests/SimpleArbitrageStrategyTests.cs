﻿using System.Reactive.Subjects;
using Nito.AsyncEx;
using Trdr.Connectivity.Binance;
using Trdr.Connectivity.CoinJar.Phoenix;
using Trdr.Samples.SimpleArbitrage;

namespace Trdr.Samples.Tests;

[TestFixture]
public class SimpleArbitrageStrategyTests
{
    [Test]
    public async Task Can_buy_and_sell()
    {
        // Mock a source of ticker events.
        var binanceSubject = new Subject<Ticker>();
        var coinJarSubject = new Subject<TickerPayload>();

        // Synchronize so we can test the scenario reliably.
        var countdown = new AsyncCountdownEvent(2);

        int buyAtBinance = 0;
        int sellAtCoinJar = 0;

        var strategy = new SimpleArbitrageStrategy(
            coinJarSubject.ToAsyncEnumerable(),
            binanceSubject.ToAsyncEnumerable(),
            _ =>
            {
                ++buyAtBinance;
                countdown.Signal();
            },
            _ =>
            {
                ++sellAtCoinJar;
                countdown.Signal();
            });

        await strategy.Start();

        // Generate events that will trigger a buy and sell.
        binanceSubject.OnNext(new Ticker { AskRaw = "9.200", BidRaw = "9.005" });
        coinJarSubject.OnNext(new TickerPayload { AskRaw = "9.505", BidRaw = "9.205" });

        // Wait for the strategy to process the events
        await countdown.WaitAsync();
        countdown.AddCount(2);

        // Generate events that will NOT trigger a buy and sell.
        binanceSubject.OnNext(new Ticker { AskRaw = "9.200", BidRaw = "9.005" });
        coinJarSubject.OnNext(new TickerPayload { AskRaw = "9.505", BidRaw = "9.201" });

        // Generate events that will trigger a buy and sell.
        binanceSubject.OnNext(new Ticker { AskRaw = "9.200", BidRaw = "9.005" });
        coinJarSubject.OnNext(new TickerPayload { AskRaw = "9.505", BidRaw = "9.205" });

        await countdown.WaitAsync();

        // Strategy should have bought and sold twice.
        Assert.That(buyAtBinance, Is.EqualTo(2));
        Assert.That(sellAtCoinJar, Is.EqualTo(2));
    }
}