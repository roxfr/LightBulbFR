using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LightBulb.WindowsApi.Internal;

namespace LightBulb.WindowsApi
{
    public partial class DeviceContext : IDisposable
    {
        private readonly GammaRamp? _initialRamp;

        private int _gammaChannelOffset;

        public IntPtr Handle { get; }

        public DeviceContext(IntPtr handle)
        {
            Handle = handle;

            _initialRamp = GetGammaRamp();
        }

        ~DeviceContext()
        {
            Dispose();
        }

        private GammaRamp? GetGammaRamp() =>
            NativeMethods.GetDeviceGammaRamp(Handle, out var result)
                ? result
                : (GammaRamp?) null;

        private bool SetGammaRamp(GammaRamp ramp) => NativeMethods.SetDeviceGammaRamp(Handle, ref ramp);

        public void SetGamma(double redMultiplier, double greenMultiplier, double blueMultiplier, bool respectInitialGamma = false)
        {
            // Create native ramp object
            var ramp = new GammaRamp
            {
                Red = new ushort[256],
                Green = new ushort[256],
                Blue = new ushort[256]
            };

            // Create linear ramps for each color
            for (ushort i = 0; i < 256; i++)
            {
                var redBase = respectInitialGamma && _initialRamp != null
                    ? _initialRamp.Value.Red[i]
                    : i * 255;

                var greenBase = respectInitialGamma && _initialRamp != null
                    ? _initialRamp.Value.Green[i]
                    : i * 255;

                var blueBase = respectInitialGamma && _initialRamp != null
                    ? _initialRamp.Value.Blue[i]
                    : i * 255;

                ramp.Red[i] = (ushort) (redBase * redMultiplier);
                ramp.Green[i] = (ushort) (greenBase * greenMultiplier);
                ramp.Blue[i] = (ushort) (blueBase * blueMultiplier);
            }

            // Some drivers will ignore request to change gamma if the ramp is the same as last time
            // so we randomize it a bit. Even though our ramp may not have changed, other applications
            // could have affected the gamma and we need to "force-refresh" it to ensure it's valid.
            _gammaChannelOffset = ++_gammaChannelOffset % 5;
            ramp.Red[255] = (ushort) (ramp.Red[255] + _gammaChannelOffset);
            ramp.Green[255] = (ushort) (ramp.Green[255] + _gammaChannelOffset);
            ramp.Blue[255] = (ushort) (ramp.Blue[255] + _gammaChannelOffset);

            // Set gamma
            if (!SetGammaRamp(ramp))
                Debug.WriteLine("Could not set gamma ramp.");
        }

        public void Dispose()
        {
            // Reset gamma
            SetGamma(1, 1, 1);

            if (!NativeMethods.DeleteDC(Handle))
                Debug.WriteLine("Could not dispose device context.");

            GC.SuppressFinalize(this);
        }
    }

    public partial class DeviceContext
    {
        public static DeviceContext? FromDeviceName(string deviceName)
        {
            var handle = NativeMethods.CreateDC(deviceName, null, null, IntPtr.Zero);
            return handle != IntPtr.Zero ? new DeviceContext(handle) : null;
        }

        public static IReadOnlyList<DeviceContext> GetAllMonitorDeviceContexts()
        {
            var result = new List<DeviceContext>();

            foreach (var screen in Screen.AllScreens)
            {
                var deviceContext = FromDeviceName(screen.DeviceName);
                if (deviceContext != null)
                    result.Add(deviceContext);
            }

            return result;
        }
    }
}