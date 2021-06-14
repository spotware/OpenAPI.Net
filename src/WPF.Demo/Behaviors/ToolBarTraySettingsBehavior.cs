using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Trading.UI.Demo.Behaviors
{
    public static class ToolBarTraySettingsBehavior
    {
        #region Dependency Properties

        public static readonly DependencyProperty SaveBandIndexProperty =
                DependencyProperty.RegisterAttached("SaveBandIndex", typeof(bool), typeof(ToolBarTraySettingsBehavior),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSaveBandIndexChanged));

        public static readonly DependencyProperty SaveBandProperty =
            DependencyProperty.RegisterAttached("SaveBand", typeof(bool), typeof(ToolBarTraySettingsBehavior),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSaveBandChanged));

        public static readonly DependencyProperty SaveWidthProperty =
            DependencyProperty.RegisterAttached("SaveWidth", typeof(bool), typeof(ToolBarTraySettingsBehavior),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSaveWidthChanged));

        #endregion Dependency Properties

        #region Dependency Properties Get/Set methods

        public static object GetSaveBandIndex(DependencyObject source)
        {
            return source.GetValue(SaveBandIndexProperty);
        }

        public static void SetSaveBandIndex(DependencyObject source, object value)
        {
            source.SetValue(SaveBandIndexProperty, value);
        }

        public static object GetSaveBand(DependencyObject source)
        {
            return source.GetValue(SaveBandProperty);
        }

        public static void SetSaveBand(DependencyObject source, object value)
        {
            source.SetValue(SaveBandProperty, value);
        }

        public static object GetSaveWidth(DependencyObject source)
        {
            return source.GetValue(SaveWidthProperty);
        }

        public static void SetSaveWidth(DependencyObject source, object value)
        {
            source.SetValue(SaveWidthProperty, value);
        }

        #endregion Dependency Properties Get/Set methods

        #region Dependency Properties on changed methods

        private static void OnSaveBandIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                return;
            }

            var toolBarTray = d as ToolBarTray;

            ThrowExceptionIfNamesNotSet(toolBarTray);

            toolBarTray.Loaded += (sender, args) => SaveToolBarsBandIndexOnLoaded(sender as ToolBarTray);
        }

        private static void OnSaveBandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                return;
            }

            var toolBarTray = d as ToolBarTray;

            ThrowExceptionIfNamesNotSet(toolBarTray);

            toolBarTray.Loaded += (sender, args) => SaveToolBarsBandOnLoaded(sender as ToolBarTray);
        }

        private static void OnSaveWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                return;
            }

            var toolBarTray = d as ToolBarTray;

            ThrowExceptionIfNamesNotSet(toolBarTray);

            toolBarTray.Loaded += (sender, args) => SaveToolBarsWidthOnLoaded(sender as ToolBarTray);
        }

        #endregion Dependency Properties on changed methods

        #region Other Methods

        private static void OverridePropertyOnChanged(ToolBarTray toolBarTray, DependencyProperty property, EventHandler handler)
        {
            PropertyDescriptor propertyDescriptor = DependencyPropertyDescriptor.FromProperty(property, typeof(ToolBar));

            foreach (var toolBar in toolBarTray.ToolBars)
            {
                propertyDescriptor.AddValueChanged(toolBar, handler);
            }
        }

        private static void ApplySettingsToToolBars(ToolBarTray toolBarTray, Action<ToolBar, ToolBarSettings> function)
        {
            var settings = GetSettings(toolBarTray.Name);

            if (settings.ToolBarSettings == null || !settings.ToolBarSettings.Any())
            {
                return;
            }

            foreach (var toolBar in toolBarTray.ToolBars)
            {
                var settingsModel = settings.ToolBarSettings.FirstOrDefault(iSettings => iSettings.Name.Equals(
                    toolBar.Name, StringComparison.InvariantCultureIgnoreCase));

                if (settingsModel == null)
                {
                    continue;
                }

                function(toolBar, settingsModel);
            }
        }

        private static void SetToolBarBandIndex(ToolBar toolBar, ToolBarSettings settings)
        {
            toolBar.BandIndex = settings.BandIndex;
        }

        private static void SetToolBarBand(ToolBar toolBar, ToolBarSettings settings)
        {
            toolBar.Band = settings.Band;
        }

        private static void SetToolBarWidth(ToolBar toolBar, ToolBarSettings settings)
        {
            toolBar.Width = settings.Width;
        }

        private static void SaveSettings(ToolBarTray toolBarTray, ToolBarSettingsType settingsType)
        {
            var settings = GetSettings(toolBarTray.Name);

            settings.ToolBarSettings ??= new List<ToolBarSettings>();

            foreach (var toolBar in toolBarTray.ToolBars)
            {
                var toolBarSettings = settings.ToolBarSettings.FirstOrDefault(iSettings => iSettings.Name.Equals(
                    toolBar.Name, StringComparison.InvariantCultureIgnoreCase));

                if (toolBarSettings != null)
                {
                    switch (settingsType)
                    {
                        case ToolBarSettingsType.BandIndex:
                            toolBarSettings.BandIndex = toolBar.BandIndex;
                            break;

                        case ToolBarSettingsType.Band:
                            toolBarSettings.Band = toolBar.Band;
                            break;

                        case ToolBarSettingsType.Width:
                            toolBarSettings.Width = toolBar.Width;
                            break;
                    }
                }
                else
                {
                    toolBarSettings = new ToolBarSettings
                    {
                        Name = toolBar.Name,
                        BandIndex = toolBar.BandIndex,
                        Band = toolBar.Band,
                        Width = toolBar.Width,
                    };

                    settings.ToolBarSettings.Add(toolBarSettings);
                }
            }

            settings.Save();
        }

        private static void ThrowExceptionIfNamesNotSet(ToolBarTray toolBarTray)
        {
            var names = new List<string>() { toolBarTray.Name };

            toolBarTray.ToolBars.ToList().ForEach(toolBar => names.Add(toolBar.Name));

            if (names.Any(name => string.IsNullOrEmpty(name)))
            {
                throw new NullReferenceException("Please set a unique name for your 'ToolBarTray' and all it's tool bars before enabling this" +
                    " behavior");
            }
        }

        private static void SaveToolBarsBandIndexOnLoaded(ToolBarTray toolBarTray)
        {
            ApplySettingsToToolBars(toolBarTray, SetToolBarBandIndex);

            OverridePropertyOnChanged(toolBarTray, ToolBar.BandIndexProperty, (sender, args) => SaveSettings(toolBarTray,
                ToolBarSettingsType.BandIndex));
        }

        private static void SaveToolBarsBandOnLoaded(ToolBarTray toolBarTray)
        {
            ApplySettingsToToolBars(toolBarTray, SetToolBarBand);

            OverridePropertyOnChanged(toolBarTray, ToolBar.BandProperty, (sender, args) => SaveSettings(toolBarTray,
                ToolBarSettingsType.Band));
        }

        private static void SaveToolBarsWidthOnLoaded(ToolBarTray toolBarTray)
        {
            ApplySettingsToToolBars(toolBarTray, SetToolBarWidth);

            OverridePropertyOnChanged(toolBarTray, ToolBar.WidthProperty, (sender, args) => SaveSettings(toolBarTray,
                ToolBarSettingsType.Width));
        }

        private static ToolBarTraySettings GetSettings(string name)
        {
            var assemblyName = Assembly.GetEntryAssembly().GetName().Name;

            var settingsKey = $"{assemblyName}.{name}";

            return new ToolBarTraySettings(settingsKey);
        }

        #endregion Other Methods
    }
}