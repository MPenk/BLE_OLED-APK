using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BLE_OLED
{
    class DrawView : View
    {
        Bitmap bitmapa;
        Canvas canva;
        Paint paintLine = new Paint();
        Paint paintScreen = new Paint();
        Typeface typeface;
        int multiply;
        int[] location = new int[2];

        public DrawView(Context context, int x, int y, int multiply, Typeface typeface) : base(context)
        {

            this.multiply = multiply;

            this.bitmapa = Bitmap.CreateBitmap(x, y, Bitmap.Config.Rgb565);
            this.canva = new Canvas(bitmapa);

            this.paintLine.SetStyle(Paint.Style.Fill);
            this.paintLine.SetARGB(255, 1, 1, 255);
            this.paintLine.StrokeWidth = multiply;

            //this.canva.DrawPaint(this.paintLine);

            this.SetBackgroundColor(Color.Black);
            this.typeface = typeface;
            paintLine.SetTypeface(typeface);

        }

        public void drawText(string text, float[] coordinates)
        {
            int test = 100;
            this.paintLine.TextSize = test;
            this.canva.DrawText(text, coordinates[0] - location[0], coordinates[1] - location[1] + (test / 2)+5, this.paintLine);
            this.PostInvalidate();
        }
        //public void drawText(string text, float[] coordinates)
        //{
        //    int test = 120;
        //    this.paintLine.TextSize = test;
        //    this.canva.DrawText(text, coordinates[0] - location[0], coordinates[1] - location[1] + (test / 2) - 10, this.paintLine);
        //    this.PostInvalidate();
        //}

        public void SetColor(int red, int green, int blue) {
            this.paintLine.SetARGB(255, red*(255 / 31), green * (255 / 63), blue * (255 / 31));
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            this.GetLocationOnScreen(location);
            base.OnSizeChanged(w, h, oldw, oldh);
        }



        public void DrawPoint(float[] coordinates)
        {
            this.canva.DrawCircle(coordinates[0] - location[0], coordinates[1] - location[1], 3, paintLine);
            this.PostInvalidate();
        }

        public void DrawLine(float[] startCoordinates, float[] stopCoordinates)
        {
            canva.DrawLine(startCoordinates[0] - location[0], startCoordinates[1] - location[1], stopCoordinates[0] - location[0], stopCoordinates[1] - location[1], paintLine);
            this.PostInvalidate();
        }
        public int[] GetCoordinates(float[] coordinates)
        {
            int X = (int)Math.Ceiling((coordinates[0] - location[0]) / this.multiply);
            int Y = (int)Math.Ceiling((coordinates[1] - location[1]) / this.multiply);
            if (X < 0 || Y < 0 || X > 96 || Y > 64) throw new InvalidCoordinatesException();
            return new int[2] { X, Y };
        }
        public int[] GetCoordinates(int[] coordinates)
        {
            int X = (int)Math.Ceiling((double)(coordinates[0] - location[0]) / this.multiply);
            int Y = (int)Math.Ceiling((double)(coordinates[1] - location[1]) / this.multiply);
            if (X < 0 || Y < 0 || X > 96 || Y > 64) throw new InvalidCoordinatesException();
            return new int[2] { X, Y };
        }
        public string giveXY(float x, float y)
        {
            int X = (int)Math.Ceiling((x - location[0]) / this.multiply);
            int Y = (int)Math.Ceiling((y - location[1]) / this.multiply);
            if (X < 0 || Y < 0 || X > 96 || Y > 64) return "}";
            return X + "," + Y;
        }

        public void Clear()
        {
            canva.DrawColor(Color.Black);
            this.PostInvalidate();

        }

        protected override void OnDraw(Canvas canvas)
        {
            canvas.DrawBitmap(bitmapa, 0, 0, paintScreen);
        }
    }
    public class InvalidCoordinatesException : Exception
    {
        public InvalidCoordinatesException() : base() { }
        public InvalidCoordinatesException(string message) : base(message) { }

    }
}