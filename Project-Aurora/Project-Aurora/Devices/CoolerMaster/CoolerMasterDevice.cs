﻿using Aurora.Settings;
using Aurora.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DK = Aurora.Devices.DeviceKeys;

namespace Aurora.Devices.CoolerMaster
{
    public class CoolerMasterDevice : DefaultDevice
    {
        public override string DeviceName => "CoolerMaster";

        private readonly List<(Native.DEVICE_INDEX Device, Native.COLOR_MATRIX Matrix)> _initializedDevices = new();

        protected override string DeviceInfo => string.Join(", ", _initializedDevices.Select(d => Enum.GetName(typeof(Native.DEVICE_INDEX), d.Device)));

        private bool _loggedLayout;

        public override bool Initialize()
        {
            if (IsInitialized)
                return IsInitialized;

            LogInfo($"Trying to initialize CoolerMaster SDK version {Native.GetCM_SDK_DllVer()}");

            lock (_initializedDevices)
            {
                foreach (var device in Native.Devices.Where(d => d != Native.DEVICE_INDEX.DEFAULT))
                {
                    if (Native.IsDevicePlug(device) && Native.EnableLedControl(true, device))
                    {
                        _initializedDevices.Add((device, Native.COLOR_MATRIX.Create()));
                        Native.RefreshLed(true);
                    }
                }

                return IsInitialized = _initializedDevices.Any();
            }
        }

        public override void Shutdown()
        {
            if (!IsInitialized)
                return;

            lock (_initializedDevices)
            { 
                _initializedDevices.ForEach(d => Native.EnableLedControl(false, d.Device));
                _initializedDevices.Clear();
            };

            IsInitialized = false;
        }

        protected override bool UpdateDevice(Dictionary<DK, Color> keyColors, DoWorkEventArgs e, bool forced = false)
        {
            lock (_initializedDevices)
            {
                foreach (var (dev, colors) in _initializedDevices)
                {
                    if (Native.Mice.Contains(dev) && Global.Configuration.DevicesDisableMouse)
                        continue;
                    if (Native.Keyboards.Contains(dev) && Global.Configuration.DevicesDisableKeyboard)
                        continue;

                    if (!KeyMaps.LayoutMapping.TryGetValue(dev, out var dict))
                    {
                        dict = KeyMaps.GenericFullSize;
                        if (!_loggedLayout)
                        {
                            LogError($"Could not find layout for device {Enum.GetName(typeof(Native.DEVICE_INDEX), dev)}, using generic.");
                            _loggedLayout = true;
                        }
                    }

                    foreach (var (dk, clr) in keyColors)
                    {
                        DK key = dk;
                        //HACK: the layouts for some reason switch backslash and enter
                        //around between ANSI and ISO needlessly. We swap them around here
                        if (key == DK.ENTER && !Global.kbLayout.Loaded_Localization.IsANSI())
                            key = DK.BACKSLASH;

                        if (dict.TryGetValue(key, out var position))
                            colors.KeyColor[position.row, position.column] = new Native.KEY_COLOR(ColorUtils.CorrectWithAlpha(clr));
                    }

                    Native.SetAllLedColor(colors, dev);
                }
                return true;
            }
        }
    }
}
