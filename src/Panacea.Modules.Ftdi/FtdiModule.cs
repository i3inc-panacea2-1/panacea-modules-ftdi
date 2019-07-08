using FTD2XX_NET;
using Panacea.Modularity.Relays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Modules.Ftdi
{
    public class FtdiModule : IRelayModule
    {
        FTDI _ftdi;
        private uint _recievedBytes;

        public FtdiModule()
        {
            if (IsDriverInstalled())
            {
                _ftdi = new FTDI();
                if (_ftdi.OpenByIndex(0) != FTDI.FT_STATUS.FT_OK
                    || _ftdi.SetBaudRate(921600) != FTDI.FT_STATUS.FT_OK
                    || _ftdi.SetBitMode(255, 4) != FTDI.FT_STATUS.FT_OK)
                {
                    _ftdi = null;
                }
                else _ftdi.Write(new byte[1] { 0 }, 1, ref _recievedBytes);
            }
        }

        ~FtdiModule()
        {
            if (_ftdi == null) return;
            try
            {
                _ftdi.Close();
            }
            catch
            {
                //ignore
            }
        }

        public Task<bool> SetStatusAsync(bool on, int port)
        {
            if (_ftdi == null) return Task.FromResult(false);
            try
            {
                FtdiSetRelayStatus(port, on);
            }
            catch (Exception)
            {
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }


        private bool IsDriverInstalled()
        {
            var handler = LoadLibrary(@"FTD2XX.DLL");
            var loaded = handler != IntPtr.Zero;
            FreeLibrary(handler);
            return loaded;
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hModule);

        private static object _lock = new object();
        private byte _ftdiStatus = 0;

        public void FtdiSetRelayStatus(int index, bool on)
        {
            lock (_lock)
            {
                if (on)
                {
                    switch (index)
                    {
                        case 4:
                            _ftdiStatus |= 128;
                            break;
                        case 3:
                            _ftdiStatus |= 32;
                            break;
                        case 2:
                            _ftdiStatus |= 8;
                            break;
                        case 1:
                            _ftdiStatus |= 2;
                            break;
                    }
                }
                else
                {
                    switch (index)
                    {
                        case 4:
                            _ftdiStatus &= 127;
                            break;
                        case 3:
                            _ftdiStatus &= 223;
                            break;
                        case 2:
                            _ftdiStatus &= 247;
                            break;
                        case 1:
                            _ftdiStatus &= 253;
                            break;
                    }
                }
                _ftdi.Write(new byte[1] { _ftdiStatus }, 1, ref _recievedBytes);
            }
        }
    }
}
