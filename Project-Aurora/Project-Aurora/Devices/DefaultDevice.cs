﻿using Aurora.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Aurora.Devices
{
    public abstract class DefaultDevice : IDevice
    {
        private readonly Stopwatch watch = new();
        private long lastUpdateTime;
        private long updateTime;

        public abstract string DeviceName { get; }

        protected virtual string DeviceInfo => "";

        public virtual string DeviceDetails => IsInitialized
            ? $"Initialized{(string.IsNullOrWhiteSpace(DeviceInfo) ? "" : ": " + DeviceInfo)}"
            : "Not Initialized";

        public string DeviceUpdatePerformance => IsInitialized
            ? lastUpdateTime + "(" + updateTime + ")" + " ms"
            : "";

        public Task InitializeTask
        {
            get => _initializeTask;
            set
            {
                _initializeTask?.Wait();
                _initializeTask = value;
            }
        }

        public virtual bool IsInitialized { get; protected set; }

        public abstract bool Initialize();
        public abstract void Shutdown();

        public virtual void Reset()
        {
            Shutdown();
            Initialize();
        }

        protected abstract bool UpdateDevice(Dictionary<DeviceKeys, Color> keyColors, DoWorkEventArgs e, bool forced = false);

        private readonly Stopwatch _tempStopWatch = new();
        public bool UpdateDevice(DeviceColorComposition colorComposition, DoWorkEventArgs e, bool forced = false)
        {
            _tempStopWatch.Restart();
            var updateResult = UpdateDevice(colorComposition.KeyColors, e, forced);

            if (!updateResult) return false;
            lastUpdateTime = watch.ElapsedMilliseconds;
            updateTime = _tempStopWatch.ElapsedMilliseconds;
            watch.Restart();

            return true;
        }

        public virtual IEnumerable<string> GetDevices()
        {
            yield break;
        }

        #region Variables
        private VariableRegistry variableRegistry;
        private Task _initializeTask;

        public VariableRegistry RegisteredVariables
        {
            get
            {
                if (variableRegistry is null)
                {
                    variableRegistry = new VariableRegistry();
                    RegisterVariables(variableRegistry);
                }
                return variableRegistry;
            }
        }
        protected virtual void RegisterVariables(VariableRegistry variableRegistry) { }

        protected void LogInfo(string s) => Global.logger.Info($"[Device][{DeviceName}] {s}");

        protected void LogError(string s) => Global.logger.Error($"[Device][{DeviceName}] {s}");
        #endregion
    }
}
