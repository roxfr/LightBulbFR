using System;
using System.Threading.Tasks;
using System.Windows;
using LightBulb.Internal;
using LightBulb.Services;
using LightBulb.ViewModels.Components;
using LightBulb.ViewModels.Components.Settings;
using LightBulb.ViewModels.Dialogs;
using LightBulb.ViewModels.Framework;
using LightBulb.WindowsApi;
using Stylet;

namespace LightBulb.ViewModels
{
    public class RootViewModel : Screen, IDisposable
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly DialogManager _dialogManager;
        private readonly SettingsService _settingsService;

        private readonly Timer _checkForUpdatesTimer;

        public CoreViewModel Core { get; }

        public RootViewModel(
            IViewModelFactory viewModelFactory,
            DialogManager dialogManager,
            SettingsService settingsService,
            UpdateService updateService)
        {
            _viewModelFactory = viewModelFactory;
            _dialogManager = dialogManager;
            _settingsService = settingsService;

            Core = viewModelFactory.CreateCoreViewModel();

            DisplayName = $"{App.Name} v{App.VersionString}";

            _checkForUpdatesTimer = new Timer(
                TimeSpan.FromHours(3),
                async () => await updateService.CheckPrepareUpdateAsync()
            );
        }

        private async Task ShowGammaRangePromptAsync()
        {
            if (_settingsService.IsExtendedGammaRangeUnlocked)
                return;

            var message = $@"
{App.Name} a détecté que la plage gamma étendue n'est pas déverrouillée sur cet ordinateur.
Cela peut entraîner un dysfonctionnement de l'application pour certaines configurations de couleurs.

Appuyer sur OK pour déverrouiller la plage gamma.".Trim();

            var dialog = _viewModelFactory.CreateMessageBoxViewModel(
                "Limited gamma range",
                message,
                "OK", "CANCEL"
            );

            if (await _dialogManager.ShowDialogAsync(dialog) == true)
            {
                _settingsService.IsExtendedGammaRangeUnlocked = true;
                _settingsService.Save();
            }
        }

        private async Task ShowFirstTimeExperienceMessageAsync()
        {
            if (!_settingsService.IsFirstTimeExperienceEnabled)
                return;

            var message = $@"
Merci d'avoir installé {App.Name}!
Pour bénéficier de l'expérience la plus personnalisée, configurez votre emplacement dans les paramètres.

Appuyer sur OK pour ouvrir les paramètres.".Trim();

            var dialog = _viewModelFactory.CreateMessageBoxViewModel(
                "Bienvenu !",
                message,
                "OK", "ANNULER"
            );

            // Disable first time experience in the future
            _settingsService.IsFirstTimeExperienceEnabled = false;
            _settingsService.IsAutoStartEnabled = true;
            _settingsService.Save();

            if (await _dialogManager.ShowDialogAsync(dialog) == true)
            {
                var settingsDialog = _viewModelFactory.CreateSettingsViewModel();
                settingsDialog.ActivateTabByType<LocationSettingsTabViewModel>();

                await _dialogManager.ShowDialogAsync(settingsDialog);
            }
        }

        protected override void OnViewLoaded()
        {
            base.OnViewLoaded();

            _checkForUpdatesTimer.Start();
        }

        // This is a custom event that fires when the dialog host is loaded
        public async void OnViewFullyLoaded()
        {
            await ShowGammaRangePromptAsync();
            await ShowFirstTimeExperienceMessageAsync();
        }

        public async void ShowSettings() =>
            await _dialogManager.ShowDialogAsync(_viewModelFactory.CreateSettingsViewModel());

        public void ShowAbout() => ProcessEx.StartShellExecute(App.GitHubProjectUrl);

        public void Exit() => Application.Current.Shutdown();

        public void Dispose()
        {
            _checkForUpdatesTimer.Dispose();
        }
    }
}