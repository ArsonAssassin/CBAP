using Archipelago.Core.AvaloniaGUI.ViewModels;
using Archipelago.Core;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Archipelago.Core.AvaloniaGUI.Models;
using Archipelago.Core.GameClients;
using Serilog;
using Archipelago.Core.Util;
using Archipelago.Core.Models;
using Newtonsoft.Json;
using System.Linq;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using ReactiveUI;
using System.Collections.Generic;
using System;
using System.Reactive.Concurrency;
using Avalonia.Media;
using Archipelago.Core.AvaloniaGUI.Views;

namespace CBAP;

public partial class App : Application
{
    static MainWindowViewModel Context;
    public static ArchipelagoClient Client { get; set; }
    private static readonly object _lockObject = new object();
    public static List<Location> GameLocations { get; set; }
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Context = new MainWindowViewModel();
        Context.ConnectClicked += Context_ConnectClicked;
        Context.CommandReceived += (e, a) =>
        {
            Client?.SendMessage(a.Command);
        };
        Context.ConnectButtonEnabled = true;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Context
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainWindow
            {
                DataContext = Context
            };
        }
        base.OnFrameworkInitializationCompleted();


    }
    private async void Context_ConnectClicked(object? sender, ConnectClickedEventArgs e)
    {
        if (Client != null)
        {
            Client.CancelMonitors();
            Client.Connected -= OnConnected;
            Client.Disconnected -= OnDisconnected;
            Client.ItemReceived -= ItemReceived;
            Client.MessageReceived -= Client_MessageReceived;
        }
        DuckstationClient client = new DuckstationClient();
        var DuckstationConnected = client.Connect();
        if (!DuckstationConnected)
        {
            Log.Logger.Warning("duckstation not running, open duckstation and launch the game before connecting!");
            return;
        }
        Client = new ArchipelagoClient(client);

        Memory.GlobalOffset = Memory.GetDuckstationOffset();

        Client.Connected += OnConnected;
        Client.Disconnected += OnDisconnected;

        await Client.Connect(e.Host, "Crash Bash");
        GameLocations = Helpers.GetLocations();
        Client.MessageReceived += Client_MessageReceived;
        Client.ItemReceived += ItemReceived;
        await Client.Login(e.Slot, !string.IsNullOrWhiteSpace(e.Password) ? e.Password : null);
        Client.MonitorLocations(GameLocations);
    }
    private async void ItemReceived(object? o, ItemReceivedEventArgs args)
    {
        Log.Logger.Information($"Item Received: {JsonConvert.SerializeObject(args.Item)}");

    }
    private static void LogItem(Item item)
    {
        var messageToLog = new LogListItem(new List<TextSpan>()
            {
                new TextSpan(){Text = $"[{item.Id.ToString()}] -", TextColor = Color.FromRgb(255, 255, 255)},
                new TextSpan(){Text = $"{item.Name}", TextColor = Color.FromRgb(200, 255, 200)},
                new TextSpan(){Text = $"x{item.Quantity.ToString()}", TextColor = Color.FromRgb(200, 255, 200)}
            });
        lock (_lockObject)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                Context.ItemList.Add(messageToLog);
            });
        }
    }
    private void Client_MessageReceived(object? sender, Archipelago.Core.Models.MessageReceivedEventArgs e)
    {
        if (e.Message.Parts.Any(x => x.Text == "[Hint]: "))
        {
            LogHint(e.Message);
        }
        Log.Logger.Information(JsonConvert.SerializeObject(e.Message));
    }
    private static void LogHint(LogMessage message)
    {
        var newMessage = message.Parts.Select(x => x.Text);

        if (Context.HintList.Any(x => x.TextSpans.Select(y => y.Text) == newMessage))
        {
            return; //Hint already in list
        }
        List<TextSpan> spans = new List<TextSpan>();
        foreach (var part in message.Parts)
        {
            spans.Add(new TextSpan() { Text = part.Text, TextColor = Color.FromRgb(part.Color.R, part.Color.G, part.Color.B) });
        }
        lock (_lockObject)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                Context.HintList.Add(new LogListItem(spans));
            });
        }
    }
    private static void OnConnected(object sender, EventArgs args)
    {
        Log.Logger.Information("Connected to Archipelago");
        Log.Logger.Information($"Playing {Client.CurrentSession.ConnectionInfo.Game} as {Client.CurrentSession.Players.GetPlayerName(Client.CurrentSession.ConnectionInfo.Slot)}");
    }

    private static void OnDisconnected(object sender, EventArgs args)
    {
        Log.Logger.Information("Disconnected from Archipelago");
    }
}
