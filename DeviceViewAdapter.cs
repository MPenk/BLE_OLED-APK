using Android.Graphics;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Button;
using Plugin.BluetoothLE;
using System;
using System.Collections.Generic;

namespace BLE_OLED
{
    public class DeviceViewAdapter : RecyclerView.Adapter
    {
        BluetoothConnection btConn;

        List<IDevice> devices = new List<IDevice>();

        public event EventHandler<IDevice> DeviceSelected;

        public DeviceViewAdapter()
        {
            this.btConn = new BluetoothConnection(this);
            this.devices = btConn.devices;
        }
        public override int ItemCount => btConn.items;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            DeviceViewHolder myHolder = holder as DeviceViewHolder;
            myHolder.deviceUuid.Text = devices[position].Uuid.ToString();
            myHolder.deviceName.Text = devices[position].Name;
            if (myHolder.deviceName.Text == "" || myHolder.deviceName.Text == null) myHolder.deviceName.Text = "NoName";
            if (myHolder.deviceName.Text == "BT05") myHolder.deviceName.SetTextColor(Color.Green); 
            myHolder.id = position;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.device, parent, false);

            TextView deviceUuid = itemView.FindViewById<TextView>(Resource.Id.textViewDeviceUuid);
            TextView deviceName = itemView.FindViewById<TextView>(Resource.Id.textViewDeviceName);

            DeviceViewHolder view = new DeviceViewHolder(itemView, OnChoose) { deviceName = deviceName, deviceUuid = deviceUuid };
            return view;

        }

        void OnChoose(int position)
        {
            if (DeviceSelected != null)
                DeviceSelected(this, devices[position]);
        }

        protected override void Dispose(bool disposing)
        {
            btConn.stopScan();
            //devices.Clear();
            base.Dispose(disposing);

        }

        internal class DeviceViewHolder : RecyclerView.ViewHolder
        {
            public View itemView { get; set; }
            public TextView deviceName { get; set; }
            public TextView deviceUuid { get; set; }
            public int id;
            public DeviceViewHolder(View itemView, Action<int> listener) : base(itemView)
            {
                this.itemView = itemView;
                itemView.FindViewById<MaterialButton>(Resource.Id.materialButton1).Click += (sender, e) => listener(base.LayoutPosition);
                //deviceName = itemView.FindViewById<TextView>(Resource.Id.textViewDeviceName);
                //deviceMac = itemView.FindViewById<TextView>(Resource.Id.textViewDeviceMAC);
            }
        }

    }
}