using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Google.Android.Material.Button;
using AndroidX.Core.Content;
using Android;
using Android.Content.PM;
using AndroidX.Core.App;
using Plugin.BluetoothLE;
using System.Collections.Generic;
using System.Text;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AndroidX.RecyclerView.Widget;
using Net.ArcanaStudio.ColorPicker;
using Android.Graphics;
using Android.Content.Res;
using Google.Android.Material.TextField;

namespace BLE_OLED
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity , IColorPickerDialogListener
    {
        enum DrawStep { CreatePoint, GetAngle, CheckedAndDraw, StartProgram }

        bool drawExist = false;
        DrawView drawView;
        List<IDevice> devices = new List<IDevice>();
        IDevice device;
        IGattCharacteristic gattCharacteristic;
        float[] lastDrawnCoordinates = new float[2];
        float[] firstPointOfLineCoordinates = new float[2];
        int[] lastDraw = new int[2];
        double lastDistance = 0;
        int red = 0;
        int green = 0;
        int blue = 0;
        int multiply = 8;
        DrawStep LastDrawStep = DrawStep.StartProgram;
        int drawningStep = 0;
        TextInputEditText textToSend;
        Typeface typeface;
        private RecyclerView recyclerView;
        private RecyclerView.LayoutManager layoutManager;
        DeviceViewAdapter adapter;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);


            //AssetManager assets = this.Assets;
            typeface = Typeface.CreateFromAsset(this.Assets, "fonts/t314.ttf");
            textToSend = FindViewById<TextInputEditText>(Resource.Id.textInputEditText1);
            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            //init();
            FindViewById<MaterialButton>(Resource.Id.button1).Click += TurnOnSerchingConnection;
        }
        protected void init()
        {
            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);
            drawView = new DrawView(this, 96 * multiply, 64 * multiply, multiply, typeface);
            Android.Widget.LinearLayout.LayoutParams lp = new Android.Widget.LinearLayout.LayoutParams(96 * multiply, 64 * multiply);
            lp.SetMargins(10, 10, 10, 10);
            lp.Gravity = GravityFlags.Center;
            drawView.LayoutParameters = lp;
            drawView.SetBackgroundColor(Android.Graphics.Color.ParseColor("#000000"));


        }

        private void SeekbarRelease(object sender, Android.Widget.SeekBar.StopTrackingTouchEventArgs e)
        {
            sendColorToArduino();            
        }

        private void sendColorToArduino() {

            string sRed = Convert.ToString(red, 2);
            int tmp = sRed.Length;
            for (int i = 4; i >= tmp; i--)
                sRed = "0" + sRed;

            string sGreen = Convert.ToString(green, 2);
            tmp = sGreen.Length;
            for (int i = 5; i >= tmp; i--)
                sGreen = "0" + sGreen;

            string sBlue = Convert.ToString(blue, 2);
            tmp = sBlue.Length;
            for (int i = 4; i >= tmp; i--)
                sBlue = "0" + sBlue;

            string first = sRed;
            for (int i = 0; i < 3; i++)
            {
                first = first + sGreen[i];
            }

            string second = sBlue;
            for (int i = 5; i > 2; i--)
            {
                second = sGreen[i] + second;
            }



            Console.WriteLine(first+second+" <- " + sRed+" "+sGreen +" "+ sBlue);
            SendToArduino("{k;" + Convert.ToInt32(first, 2) + "," + Convert.ToInt32(second, 2) + "}");
        }

        public MaterialButton addDeviceToLayout(int id)
        {
            MaterialButton button = new MaterialButton(this);
            FindViewById<Android.Widget.LinearLayout>(Resource.Id.linearLayout1).AddView(button);
            button.Id = 100 + id;
            button.Click += btnBLEClicked;
            return button;
        }

        private void buttonClearClicked(object sender, EventArgs e)
        {
            SendToArduino("{c;");
            drawView.Clear();
        }

        void OnSelectDevice(object sender, IDevice device)
        {
            device.WhenConnected().Subscribe(result => Connected(result));

            device.Connect();
            this.device = device;
            Snackbar.Make(FindViewById<Android.Widget.LinearLayout>(Resource.Id.linearLayout), "Łączenie...", Snackbar.LengthIndefinite)
                    .SetAction("Action", (View.IOnClickListener)null).Show();

            var grid = (FindViewById<Android.Widget.LinearLayout>(Resource.Id.linearLayout));
            grid.RemoveViewInLayout(recyclerView);
            recyclerView.Dispose();
        }

        private bool CheckPermission()
        {
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == (int)Permission.Granted)
            {
                Console.WriteLine("Mam uprawnienia");
                init();
                return true;
            }
            else
            {
                Console.WriteLine("Nie mam uprawnień");

                var requiredPermissions = new String[] { Manifest.Permission.AccessFineLocation };
                Snackbar.Make(FindViewById<Android.Widget.LinearLayout>(Resource.Id.linearLayout),
                               Resource.String.permission_location_rationale,
                               Snackbar.LengthIndefinite)
                        .SetAction("OK",
                                   new Action<View>(delegate (View obj)
                                   {
                                       ActivityCompat.RequestPermissions(this, requiredPermissions, 2);
                                   }
                        )
                ).Show();
                return false;
            }
        }


        private object DeviceDiscovered(IDevice device)
        {
            string a = device.Name + " " + device.Uuid + " " + device.NativeDevice;
            MaterialButton btn = new MaterialButton(this);
            btn.Text = a;
            btn.Id = 100 + devices.Count;
            FindViewById<Android.Widget.LinearLayout>(Resource.Id.linearLayout1).AddView(btn);
            devices.Add(device);
            btn.Click += btnBLEClicked;
            return 0;
        }

        private void btnBLEClicked(object sender, EventArgs e)
        {
            var btn = (MaterialButton)sender;
            int id = btn.Id - 100;
            var device = devices[id];
            Snackbar.Make(FindViewById<Android.Widget.LinearLayout>(Resource.Id.linearLayout), "id " + id, Snackbar.LengthShort)
                .SetAction("Action", (View.IOnClickListener)null).Show();
            device.WhenConnected().Subscribe(result => Connected(result));

            device.Connect();

        }

        private object Connected(IDevice result)
        {

            //Intent intent = new Intent(this, typeof(Activity1));
            //StartActivity(intent);
            result.WhenAnyCharacteristicDiscovered().Subscribe(result => _ = CharacteristicsDiscovered(result));

            adapter.Dispose();
            //_adapter.StopScan();
            drawExist = true;
            return 0;
        }

        private async Task CharacteristicsDiscovered(IGattCharacteristic result)
        {
            await result.Read();

            await result.EnableNotifications();
            result.WhenNotificationReceived().Subscribe();

            //Zmiany w interfejsie
            this.RunOnUiThread(() =>
            {
                
                FindViewById<AppCompatImageButton>(Resource.Id.buttonClear).Click += buttonClearClicked;
                FindViewById<AppCompatImageButton>(Resource.Id.buttonClear).Visibility = ViewStates.Visible;

                FindViewById<AppCompatImageButton>(Resource.Id.buttonColor).Click += buttonColorClicked;
                FindViewById<AppCompatImageButton>(Resource.Id.buttonColor).Visibility = ViewStates.Visible;

                FindViewById<AppCompatImageButton>(Resource.Id.buttonText).Click += buttonAddText;
                FindViewById<AppCompatImageButton>(Resource.Id.buttonText).Visibility = ViewStates.Visible;

                FindViewById<Android.Widget.LinearLayout>(Resource.Id.linearLayout1).AddView(drawView);
                FindViewById<FloatingActionButton>(Resource.Id.fab).Click += FabOnClick;

            });

            Snackbar.Make(FindViewById<Android.Widget.LinearLayout>(Resource.Id.linearLayout), "Połączono", Snackbar.LengthShort)
            .SetAction("Action", (View.IOnClickListener)null).Show();
            gattCharacteristic = result;
            drawExist = true;


        }

        private void buttonAddText(object sender, EventArgs e)
        {
            string text = textToSend.Text;
            drawView.drawText(text,lastDrawnCoordinates);
            SendToArduino("{t;"+text+"}");
            Console.WriteLine(text);
        }

        private void buttonColorClicked(object sender, EventArgs e)
        {
            ColorPickerDialog.NewBuilder().SetAllowPresets(false).SetShowAlphaSlider(false).SetColor(Color.Blue).Show(this);
        }

        public void SendToArduino(string a)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(a);
            int length;

            if (a.Length % 20 > 0)
                length = (int)(Math.Floor((double)(a.Length / 20)) * 20 + 20);
            else
                length = (int)(Math.Floor((double)(a.Length / 20)) * 20);

            for (int i = 0; i < length; i += 20)
            {
                byte[] bytesToSend;
                if (i + 20 > bytes.Length)
                    bytesToSend = new byte[20 - ((i + 20) - bytes.Length)];
                else
                    bytesToSend = new byte[20];

                for (int j = 0; j < bytesToSend.Length; j++)
                {
                    bytesToSend[j] = bytes[j + i];
                }
                gattCharacteristic.Write(bytesToSend).Subscribe();
            }
        }

        public void SendToArduino(string a, string x, string y)
        {
            string toSend = "{" + a + ";" + x + "," + y + "}";
            byte[] bytes = Encoding.ASCII.GetBytes(toSend);
            int length;

            if (toSend.Length % 20 > 0)
                length = (int)(Math.Floor((double)(a.Length / 20)) * 20 + 20);
            else
                length = (int)(Math.Floor((double)(a.Length / 20)) * 20);

            for (int i = 0; i < length; i += 20)
            {
                byte[] bytesToSend;
                if (i + 20 > bytes.Length)
                    bytesToSend = new byte[20 - ((i + 20) - bytes.Length)];
                else
                    bytesToSend = new byte[20];

                for (int j = 0; j < bytesToSend.Length; j++)
                {
                    bytesToSend[j] = bytes[j + i];
                }
                gattCharacteristic.Write(bytesToSend).Subscribe();
            }
        }

        public double Angle(float x, float y)
        {
            return Math.Atan2(y, x) * (180 / Math.PI);
        }

        public double Angle(double x, double y)
        {
            return Math.Tan(y / x);
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (drawExist == true)
            {
               DrawLines(new float[] { e.GetX(), e.GetY() }, e.Action, this.drawView);
            }
            return base.OnTouchEvent(e);
        }



        private void DrawLines(float[] currentCoordinates, MotionEventActions action, DrawView drawView)
        {
            int[] draw;
            try
            {
                draw = drawView.GetCoordinates(currentCoordinates);
            }
            catch (InvalidCoordinatesException)
            {
                try
                {
                    LastDrawStep = DrawStep.StartProgram;
                    int[] drawOld = drawView.GetCoordinates(lastDrawnCoordinates);
                    SendToArduino("{p;" + drawOld[0] + "," + drawOld[1] + "}");
                    drawView.DrawLine(firstPointOfLineCoordinates, lastDrawnCoordinates);
                    return;
                }
                catch (InvalidCoordinatesException)
                {
                    LastDrawStep = DrawStep.StartProgram;
                    return;
                }

            }

            if (!equalsNumbers(lastDraw, draw))
            {
                drawningStep++;
                double lastDistancea = Math.Sqrt(Math.Pow((lastDrawnCoordinates[0] - currentCoordinates[0]), 2) + Math.Pow((lastDrawnCoordinates[1] - currentCoordinates[1]), 2));
                int stepLimit = 2;
                if (lastDistancea < 8) stepLimit = 3;
                if (lastDistancea < 6) stepLimit = 4;
                if (lastDistancea < 3) stepLimit = 5;
                if (lastDistancea <= 1) stepLimit = 7;
                if (lastDistancea > 30) stepLimit = 1;
                if (lastDistancea > 40) stepLimit = 0;

                if ((LastDrawStep == DrawStep.GetAngle && drawningStep > stepLimit) || action == MotionEventActions.Up)
                {
                    float x2x1 = 0 - firstPointOfLineCoordinates[0] - lastDrawnCoordinates[0];
                    float x3x1 = 0 - firstPointOfLineCoordinates[0] - currentCoordinates[0];
                    float point21 = (0 - firstPointOfLineCoordinates[1] - lastDrawnCoordinates[1]) / x2x1;
                    float point31 = (0 - firstPointOfLineCoordinates[1] - currentCoordinates[1]) / x3x1;
                    double distance = Math.Sqrt(Math.Pow((firstPointOfLineCoordinates[0] - currentCoordinates[0]), 2) + Math.Pow((firstPointOfLineCoordinates[1] - currentCoordinates[1]), 2));
                    double varDecided = 0.002;

                    // DO DDEBUGOWANIA
                    // Console.WriteLine("{0:F5} ; {1:F5} ; {2:F8} ; {3:F8} ; {10:F8} ; X - {4:F2} ; Y - {5:F2} ; x - {6:F2} ; y - {7:F2} ; x - {8:F2} ; y - {9:F2}", point21, point31, Math.Abs(point21 - point31), lastDistancea, firstPointOfLineCoordinates[0], firstPointOfLineCoordinates[1], lastDrawnCoordinatesOnCanva[0] , lastDrawnCoordinatesOnCanva[1] ,currentCoordinates[0], currentCoordinates[1], varDecided);
                    // -----------------

                    if (x2x1 != 0 && x3x1 != 0)
                    {
                        if (Math.Abs(point21 - point31) > (float)varDecided || action == MotionEventActions.Up || lastDistance > distance)
                        {
                            lastDraw = drawView.GetCoordinates(lastDrawnCoordinates);
                            SendToArduino("{p;" + lastDraw[0] + "," + lastDraw[1] + "}");
                            drawView.DrawLine(firstPointOfLineCoordinates, lastDrawnCoordinates);

                            firstPointOfLineCoordinates = lastDrawnCoordinates;
                            LastDrawStep = DrawStep.CreatePoint;
                        }
                    }
                    if (action == MotionEventActions.Up)
                        LastDrawStep = DrawStep.CheckedAndDraw;
                    lastDistance = distance;
                    drawningStep = 0;
                }

                if (LastDrawStep == DrawStep.CreatePoint)
                {
                    lastDistance = Math.Sqrt((draw[0] * draw[0]) + (draw[1] * draw[1]));
                    LastDrawStep = DrawStep.GetAngle;
                    lastDrawnCoordinates = currentCoordinates;
                }
            }

            if (action == MotionEventActions.Down && (LastDrawStep == DrawStep.CheckedAndDraw || LastDrawStep == DrawStep.StartProgram))
            {
                firstPointOfLineCoordinates = currentCoordinates;
                SendToArduino("{d;" + draw[0] + "," + draw[1] + "}");
                LastDrawStep = DrawStep.CreatePoint;
                lastDrawnCoordinates = currentCoordinates;
                lastDistance = 0;
                drawView.DrawPoint(currentCoordinates);
                return;
            }
            lastDrawnCoordinates = currentCoordinates;
        }

        bool equalsNumbers(int[] a, int[] b)
        {
            if (a[0] == b[0] && a[1] == b[1]) return true;
            return false;
        }
        private void TurnOnSerchingConnection(object sender, EventArgs e)
        {
            if (!CheckPermission()) return;

            adapter = new DeviceViewAdapter();
            layoutManager = new LinearLayoutManager(this);

            recyclerView.SetLayoutManager(layoutManager);
            recyclerView.SetAdapter(adapter);
            adapter.DeviceSelected += OnSelectDevice;

            ((MaterialButton)sender).Visibility = ViewStates.Gone;
            ((MaterialButton)sender).Click -= TurnOnSerchingConnection;
            ((MaterialButton)sender).Dispose();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            device.CancelConnection();
            View view = (View)sender;
            Snackbar.Make(view, "Zatrzymano", Snackbar.LengthLong)
                .SetAction("Action", (View.IOnClickListener)null).Show();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        protected override void OnDestroy()
        {
            if (device != null) device.CancelConnection();
            base.OnDestroy();
        }

        public void OnColorSelected(int dialogId, Color color)
        {
            Console.WriteLine(color.R + " " + color.G + " " + color.B);
            red = (int)color.R / (255/31);
            green = (int)color.G / (255 / 63);
            blue = (int)color.B / (255 / 31);
            drawView.SetColor(red, green, blue);
            sendColorToArduino();

        }

        public void OnDialogDismissed(int dialogId)
        {
            //throw new NotImplementedException();
        }
    }
}

