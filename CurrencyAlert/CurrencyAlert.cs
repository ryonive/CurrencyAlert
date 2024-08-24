﻿using System;
using System.Collections.Generic;
using System.Linq;
using CurrencyAlert.Classes;
using CurrencyAlert.Windows;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiLib.CommandManager;
using KamiLib.Window;
using KamiToolKit;

namespace CurrencyAlert;

public sealed class CurrencyAlertPlugin : IDalamudPlugin {
    public unsafe CurrencyAlertPlugin(IDalamudPluginInterface pluginInterface) {
        pluginInterface.Create<Service>();

        // Ensure required game settings are set
        if (RaptureAtkModule.Instance()->AtkTextureResourceManager.DefaultTextureVersion is not 2) {
            const string warningString = "Plugin requires\"System Configuration\" \u2192 \"Graphics Setting\" \u2192 \"UI Resolution\" System Configuration setting to be set to \"High (WQHD/4K)\"\n\n" +
                                         "This setting has nothing to do with your actual screen resolution.\n\n" +
                                         "This setting does not effect your UI Scaling.";
            
            Service.ChatGui.PrintError(warningString);
            Service.Log.Warning(warningString);
            Service.NotificationManager.AddNotification(new Notification {
                Type = NotificationType.Error,
                Content = warningString,
                RespectUiHidden = false,
                Minimized = false,
                InitialDuration = TimeSpan.FromSeconds(30),
            });

            throw new Exception("Plugin unable to load, incompatible game settings.");
        }
        
        System.Config = Configuration.Load();
        System.CommandManager = new CommandManager(Service.PluginInterface, "currencyalert", "calert");

        if (System.Config is { Currencies.Count: 0 } or { Currencies: null } or { Version: not 7 }) {
            Service.Log.Verbose("Generating Initial Currency List.");

            System.Config.Currencies = GenerateInitialList();
            System.Config.Version = 7;
            System.Config.Save();
        }

        System.NativeController = new NativeController(Service.PluginInterface);
        System.WindowManager = new WindowManager(Service.PluginInterface);

        System.ConfigurationWindow = new ConfigurationWindow();
        System.WindowManager.AddWindow(System.ConfigurationWindow, WindowFlags.IsConfigWindow | WindowFlags.RequireLoggedIn);

        System.OverlayController = new OverlayController();
        System.OverlayController.Enable();

        Service.ClientState.TerritoryChanged += OnZoneChange;
        Service.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose() {
        Service.ClientState.TerritoryChanged -= OnZoneChange;
        Service.Framework.Update -= OnFrameworkUpdate;

        System.OverlayController.Dispose();
        System.NativeController.Dispose();
    }

    private void OnFrameworkUpdate(IFramework framework) {
        if (!Service.ClientState.IsLoggedIn) return;

        System.OverlayController.Update();
    }

    private void OnZoneChange(ushort e) {
        if (System.Config is { ChatWarning: false }) return;

        foreach (var currency in System.Config.Currencies.Where(currency => currency is { HasWarning: true, ChatWarning: true, Enabled: true })) {
            Service.ChatGui.Print($"{currency.Name} is {(currency.Invert ? "below" : "above")} threshold.", "CurrencyAlert", 43);
        }
    }

    private static List<TrackedCurrency> GenerateInitialList() => [
        new TrackedCurrency { Type = CurrencyType.Item, ItemId = 20, Threshold = 75000, Enabled = true, }, // StormSeal
        new TrackedCurrency { Type = CurrencyType.Item, ItemId = 21, Threshold = 75000, Enabled = true, }, // SerpentSeal
        new TrackedCurrency { Type = CurrencyType.Item, ItemId = 22, Threshold = 75000, Enabled = true, }, // FlameSeal

        new TrackedCurrency { Type = CurrencyType.Item, ItemId = 25, Threshold = 18000, Enabled = true, }, // WolfMarks
        new TrackedCurrency { Type = CurrencyType.Item, ItemId = 36656, Threshold = 18000, Enabled = true, }, // TrophyCrystals

        new TrackedCurrency { Type = CurrencyType.Item, ItemId = 27, Threshold = 3500, Enabled = true, }, // AlliedSeals
        new TrackedCurrency { Type = CurrencyType.Item, ItemId = 10307, Threshold = 3500, Enabled = true, }, // CenturioSeals
        new TrackedCurrency { Type = CurrencyType.Item, ItemId = 26533, Threshold = 3500, Enabled = true, }, // SackOfNuts

        new TrackedCurrency { Type = CurrencyType.Item, ItemId = 26807, Threshold = 800, Enabled = true, }, // BicolorGemstones

        new TrackedCurrency { Type = CurrencyType.Item, ItemId = 28, Threshold = 1400, Enabled = true, }, // Poetics
        new TrackedCurrency { Type = CurrencyType.NonLimitedTomestone, Threshold = 1400, Enabled = true, }, // NonLimitedTomestone
        new TrackedCurrency { Type = CurrencyType.LimitedTomestone, Threshold = 1400, Enabled = true, }, // LimitedTomestone

        new TrackedCurrency { Type = CurrencyType.Item, ItemId = 28063, Threshold = 7500, Enabled = true, }, // Skybuilders scripts
    ];
}