using System;
using System.Collections.Generic;
using Plugin.BluetoothLE;

namespace BLE_OLED
{

    class BluetoothConnection
    {
        private IAdapter _adapter;
        public List<IDevice> devices = new List<IDevice>();
        public int items = 0;
        private DeviceViewAdapter adapter;

        public BluetoothConnection(DeviceViewAdapter adapter)
        {
            this.adapter = adapter;
            _adapter = CrossBleAdapter.Current;
            if (_adapter.IsScanning == true)
                _adapter.StopScan();
            _adapter.ScanForUniqueDevices().Subscribe(device => DeviceDiscovered(device));
        }
        private object DeviceDiscovered(IDevice device)
        {
            items++;
            devices.Add(device);
            adapter.NotifyDataSetChanged();

            return 0;
        }
        public void stopScan()
        {
            _adapter.StopScan();
        }
    }
}