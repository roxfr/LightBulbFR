﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using LightBulb.Internal.Extensions;
using LightBulb.Models;
using LightBulb.WindowsApi;

namespace LightBulb.Services
{
    public class HotKeyService : IDisposable
    {
        private readonly List<GlobalHotKey> _hotKeyRegistrations = new();

        public void RegisterHotKey(HotKey hotKey, Action callback)
        {
            // Get codes that represent virtual key and modifiers
            var virtualKey = KeyInterop.VirtualKeyFromKey(hotKey.Key);
            var modifiers = (int) hotKey.Modifiers;

            var hotKeyRegistration = GlobalHotKey.TryRegister(virtualKey, modifiers, callback);

            if (hotKeyRegistration != null)
                _hotKeyRegistrations.Add(hotKeyRegistration);
            else
                Debug.WriteLine("Échec de l'enregistrement du raccourci clavier.");
        }

        public void UnregisterAllHotKeys()
        {
            var hotKeyRegistrationsCopy = _hotKeyRegistrations.ToArray();
            _hotKeyRegistrations.Clear();

            hotKeyRegistrationsCopy.DisposeAll();
        }

        public void Dispose() => UnregisterAllHotKeys();
    }
}