using Core.Classes;
using Core.Enums;
using Core.Modifications;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;


/*############################################################################*
 *                             Self-drawn Images                              *
 *        Panel for self-drawn images used to learn Perzeptron network        *
 *                    writen in c# by Martin Kober 2017                       *
 *############################################################################*/


namespace NeuralNet.Project
{
    class NetDraw
    {
        public static NetMain NetMain;
        private static Font BmpFont = Mod_Convert.FontSize(Fonts.MainFont, 20);

        public static double[] BitmapToDoubleArray(Image xBitmap)
        {
            //ABBRUCH
            if (xBitmap == null)
                return null;

            //CONVERT BITMAP TO DOUBLE[] ARRAY
            Bitmap bitmap = new Bitmap(xBitmap);
            if (bitmap.Width > NetMain.Net.SizeNet.Width || bitmap.Height > NetMain.Net.SizeNet.Height) bitmap = (Bitmap)ScaleDown(xBitmap);

            List<double> pixelList = new List<double>();
            for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; ++x)
                {
                    Color color = bitmap.GetPixel(x, y);
                    pixelList.Add(Colors.getGrayScale(color));
                }
            return pixelList.ToArray();
        }

        public static Image ScaleUp(Image xImage)
        {
            //SCALE IMAGE UP
            return Mod_PNG.getScaleImage(xImage, NetMain.Net.SizeScale, false);
        }

        public static Image ScaleDown(Image xImage)
        {
            //SCALE IMAGE DOWN
            return Mod_PNG.getScaleImage(xImage, NetMain.Net.SizeNet, false);
        }

        public static void Draw(Image xDraw, params UniPanel[] xHistoryPanel)
        {
            //DRAW ALL
            drawCheck(xDraw, xHistoryPanel);
            drawRedraw(xDraw, NetMain.PanelRedraw);
            drawBackQuery(NetMain.PanelNeural);
        }

        public static void drawCheck(Image xDraw, params UniPanel[] xPanel)
        {
            //CHECK DRAWN BITMAP
            double[] dblArray = NetMain.Cam.getDoubleArray();

            //GET ANSWER
            int answer = NetMain.neuralNetworkQuery(dblArray, 0.0,true);

            //ABBRUCH
            if (xPanel.Length == 0 || answer == int.MinValue)
                return;

            //TRANSFER IMAGE AND TOOLTIP TO NEXT PANEL
            for (int i = xPanel.Length - 2; i >= 0; i--)
            {
                if (xPanel[i].BackgroundImage != null) { xPanel[i + 1].BackgroundImage = xPanel[i].BackgroundImage; };
                xPanel[i + 1].setToolTip(xPanel[i].getToolTip());
            }

            //SET FIRST PANEL
            Size size = xPanel[0].Size;
            xDraw = (Bitmap)Mod_PNG.getScaleImage(xDraw, size, false);
            Graphics g = Graphics.FromImage(xDraw);
            g.DrawString(Mod_Convert.IntegerToString(answer), Fonts.getFontCooper(9), new SolidBrush(Color.Red), new Point(size.Width - 14, size.Height - 18));
            xPanel[0].BackgroundImage = xDraw;
            xPanel[0].setToolTip(NetMain.ConsoleBox.Tag.ToString());
        }

        public static void drawPlot(Panel xRedrawPanel, object xAnswer, double[] xPixel)
        {
            //DRAW PLOT FROM TEST DATA
            Bitmap bitmap = new Bitmap(NetMain.Net.SizeNet.Width, NetMain.Net.SizeNet.Height);

            int index = 0;
            for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; ++x)
                {
                    switch (NetMain.Net.Mode)
                    {
                        case MODE.PIXEL: bitmap.SetPixel(x, y, Color.FromArgb((int)xPixel[index], (int)xPixel[index], (int)xPixel[index])); break;
                        case MODE.HSENSOR: if (Mod_Check.isEven(y) && xPixel[y] * bitmap.Width > x) bitmap.SetPixel(x, y, Color.Black); break;
                    }
                    index++;
                }

            //DISPLAY BITMAP
            bitmap = (Bitmap)ScaleUp(bitmap);
            bitmap = drawAnswer(bitmap, xAnswer);
            xRedrawPanel.BackgroundImage = bitmap;
        }

        public static void drawRedraw(Image xDraw, Panel xRedrawPanel)
        {
            //CHECK DRAWN BITMAP            
            double[] dblBitmap = BitmapToDoubleArray(xDraw);

            //REDRAW BITMAP
            Bitmap rebitmap = new Bitmap(NetMain.Net.SizeNet.Width, NetMain.Net.SizeNet.Height);
            int index = 0;
            for (int y = 0; y < rebitmap.Height; y++)
                for (int x = 0; x < rebitmap.Width; ++x)
                {
                    rebitmap.SetPixel(x, y, Color.FromArgb((int)dblBitmap[index], (int)dblBitmap[index], (int)dblBitmap[index]));
                    index++;
                }
            rebitmap = (Bitmap)ScaleUp(rebitmap);

            //SET BITMAP TO PANEL
            rebitmap = drawAnswer(rebitmap, NetMain.Net.Answer);
            xRedrawPanel.BackgroundImage = rebitmap;
        }

        public static void drawBackQuery(Panel xBackQueryPanel)
        {
            //DRAW NEURAL NETWORK BACK QUERY
            double[] backQuery = NetMain.Net.QueryBack(NetMain.convertDoubleToMatrix(NetMain.Net.AnswerArray, 1, NetMain.Net.AnswerArray.Length));

            //CONVERT TO COLOR
            for (int i = 0; i < backQuery.Length; i++)
                backQuery[i] = backQuery[i] * 255;

            int pixel = (int)Math.Sqrt(backQuery.Length);
            int[] pixelArray = Mod_Convert.DoubleArrayToIntegerArray(backQuery);

            //ABBRUCH
            if (pixelArray[0] == int.MinValue)
                return;

            Bitmap bitmap = new Bitmap(pixel, pixel);

            int index = 0;
            for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; ++x)
                {
                    bitmap.SetPixel(x, y, Color.FromArgb(pixelArray[index], pixelArray[index], pixelArray[index]));
                    index++;
                }

            //SET BACKGROUND IMAGE
            xBackQueryPanel.BackgroundImage = ScaleUp(bitmap);
        }

        public static Bitmap drawAnswer(Bitmap xBitmap, object xAnswer)
        {
            //DRAW ANSWER
            Graphics g = Graphics.FromImage(xBitmap);
            string answer = xAnswer.ToString();
            g.DrawString(answer, BmpFont, new SolidBrush(Color.Red), new Point(NetMain.PanelDraw.Width - Mod_Convert.StringToWidth(answer, BmpFont), 6));
            return xBitmap;
        }

        public static object[] Invert(object[] xObjectArray)
        {
            //INVERT
            List<object> objList = new List<object>();
            foreach (object obj in xObjectArray)
            {
                int[] split = Mod_Convert.ObjectArrayToIntegerArray(Mod_Convert.StringSplitToObjectArray(obj, ","));
                for (int i = 0; i < split.Length; i++)
                    split[i] = 255 - split[i];

                object str = String.Join(",", split);
                objList.Add(str);
            }
            return objList.ToArray();
        }
    }
}
