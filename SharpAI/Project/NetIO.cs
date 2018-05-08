using Core.Classes;
using Core.Enums;
using Core.Modifications;
using System;
using System.Drawing;
using System.Windows.Forms;


/*############################################################################*
 *                       Input/Output Visualisation                           *
 *             Visualisation of input/output of a NEAT network                *
 *                    writen in c# by Martin Kober 2017                       *
 *############################################################################*/


namespace NeuralNet.Project
{
    public class NetIO : UniPanel
    {
        double[] Inputs;
        double[] Outputs;
        int StartLeft = 20;
        int StartTop = 20;

        //WIDTH
        float WidthHalf;
        float WidthQuad;
        float WidthSpace;

        //HEIGHT
        float HeightHalf;
        float HeightSpace;
        float HeightNeedI;
        float HeightNeedO;

        float LineMax = 10;
        PointF[] PointText = new PointF[2];
        string[] DrawText = new string[] { "Inputs", "Outputs" };
        Font MainFont = Fonts.MainFont;
        SolidBrush MainBursh = new SolidBrush(Colors.MainDark);
        Pen Pen;

        public NetIO(Control xForm, int xLeft, int xTop, int xWidth, int xHeight, BorderStyle xBorderStyle, Color xColorBack, bool xVisible) : base(xForm, xLeft, xTop, xWidth, xHeight, xBorderStyle, xColorBack, xVisible, false, true)
        {
            //CREATE NET IO OBJECT
            WidthHalf = xWidth / 2;
            WidthQuad = WidthHalf / 2;
            WidthSpace = WidthHalf - StartLeft * 2;
            HeightHalf = StartTop + Height / 2;
            HeightSpace = Height - StartTop * 3;

            //CALCULATION
            float[] textWidth = new float[] { Mod_Convert.StringToWidth(DrawText[0], MainFont), Mod_Convert.StringToWidth(DrawText[1], MainFont) };
            PointText[0] = new PointF(WidthQuad - textWidth[0] / 2, StartTop / 2);
            PointText[1] = new PointF(WidthHalf + WidthQuad - textWidth[1] / 2, StartTop / 2);

            //GRAPHICS
            Pen = new Pen(Colors.getColor(COLOR.GREY));

            //EVENT LISTENER
            Paint += eventPaint;
        }

        public void SecureRefesh()
        {
            //SECURE REFRESH
            if (InvokeRequired)
            { Invoke(new Action(SecureRefesh)); return; } //ENABLE TO REFRESH IN A BACKGROUNDWORKER
            Refresh();
        }

        public Pen getPenInput()
        {
            //GET PEN INPUT
            float lines = HeightSpace / Inputs.Length;
            if (lines > LineMax) lines = LineMax;
            HeightNeedI = lines * Inputs.Length;
            return new Pen(new SolidBrush(Colors.MainRecessive), lines);
        }

        public Pen getPenOutput()
        {
            //GET PEN OUTPUT
            float lines = HeightSpace / Outputs.Length;
            if (lines > LineMax) lines = LineMax;
            HeightNeedO = lines * Outputs.Length;
            return new Pen(new SolidBrush(Colors.MainDominant), lines);
        }

        public void setIO(double[] xInputs, double[] xOutputs)
        {
            //SET IO
            Inputs = xInputs;
            Outputs = xOutputs;
        }

        public void eventPaint(object sender, PaintEventArgs e)
        {
            //PAINT EVENT
            Graphics g = e.Graphics;

            //DRAW TEXT
            g.DrawString(DrawText[0], MainFont, MainBursh, PointText[0]);
            g.DrawString(DrawText[1], MainFont, MainBursh, PointText[1]);

            //ABBRUCH
            if (Inputs == null || Outputs == null)
                return;

            //DRAW INPUTS           
            Pen penI = getPenInput();
            float startI = HeightHalf - HeightNeedI / 2;
            g.DrawRectangle(Pen, StartLeft - 0.5f, startI, WidthSpace + 1f, HeightNeedI);
            for (int i = 0; i < Inputs.Length; i++)
                g.DrawLine(penI, new PointF(StartLeft, startI + (i * penI.Width)+ penI.Width/2), new PointF(StartLeft + (int)(Inputs[i] * WidthSpace), startI + (i * penI.Width) + penI.Width / 2));

            //DRAW OUTPUTS
            Pen penO = getPenOutput();
            float startO = HeightHalf - HeightNeedO / 2;
            g.DrawRectangle(Pen, StartLeft + WidthHalf - 0.5f, startO, WidthSpace + 1f, HeightNeedO);
            g.DrawLine(Pen, new PointF(StartLeft + WidthHalf + WidthSpace / 2, startO), new PointF(StartLeft + WidthHalf + WidthSpace / 2, startO + HeightNeedO));
            g.DrawString("-1", MainFont, MainBursh, new PointF(StartLeft / 2 + WidthHalf, startO + HeightNeedO));                //-1
            g.DrawString("1", MainFont, MainBursh, new PointF(StartLeft / 2 + WidthHalf + WidthSpace, startO + HeightNeedO));    //1
            for (int i = 0; i < Outputs.Length; i++)
                g.DrawLine(penO, new PointF(StartLeft + WidthHalf + WidthSpace / 2, startO + (i * penO.Width) + penO.Width / 2), new PointF(StartLeft + WidthHalf + WidthSpace / 2 + (float)Outputs[i] * (WidthSpace / 2), startO + (i * penO.Width) + penO.Width / 2));
            Console.WriteLine(string.Join("\t", Outputs));
        }
    }
}
