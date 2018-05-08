using Core.Classes;
using Core.Enums;
using Core.Modifications;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using static NeuralNet.Project.NetMario;


/*############################################################################*
 *                      Neural Network Visualisation                          *
 *                            State: in progress                              *
 *                    writen in c# by Martin Kober 2017                       *
 *############################################################################*/


namespace NeuralNet.Project
{
    public class NetPlot : UniDialog
    {
        public NetMain NetMain;
        private PlotNetwork Network;
        private PlotNeuroevolution Neuroevolution;

        //SIZE
        static Size SizeDialog = new Size(550, 680);
        static Size SizePanel = new Size(550, 600);
        UniPanel Panel;

        //VARIABLES
        bool isMoving;

        public NetPlot(NetMain xNetMain) : base(ICON.GRAPH_CLAW, "Network", SizeDialog)
        {
            //CREATE DIALOG
            NetMain = xNetMain;

            //SET BUTTON
            setButton("OK", "leave the dialog", new Action(() => { Close(); }));

            //CREATE PANEL
            Panel = UniPanel.Create(this);
            Panel.Size = SizePanel;
            Panel.Refresh();

            //PLOT CLASSES
            Network = new PlotNetwork(NetMain, Panel);
            Neuroevolution = new PlotNeuroevolution(NetMain, Panel);

            //TIMER
            double fps = 0.1;
            Timer timer = new Timer();
            timer.Interval = (int)(1000 / fps);
            timer.Tick += eventTick;
            timer.Start();

            //EVENT LISTENER
            Panel.Paint += eventPaint;
            ResizeBegin += (s, e) => { isMoving = true; };              //RESIZE & MOVING HANDLE
            ResizeEnd += (s, e) => { isMoving = false; Refresh(); };
            Visible = false;
        }

        public static double getAbsMax(double[,] xDoubleMatrix)
        {
            //ABBRUCH
            if (Mod_Check.checkParams(MethodBase.GetCurrentMethod(), xDoubleMatrix))
                return 0;

            //GET MAXIMUM FROM DOUBLE MATRIX
            double max = double.MinValue;
            foreach (double item in xDoubleMatrix)
                if (Math.Abs(item) > max) max = Math.Abs(item);
            return max;
        }

        public void Open()
        {
            //OPEN DIALOG
            if (!Visible) Show();
        }

        public void eventTick(object sender, EventArgs e)
        {
            //TICK EVENT
            Panel.Refresh();
        }

        public void eventPaint(object sender, PaintEventArgs e)
        {
            //ABBRUCH
            if (isMoving)
                return;

            //PAINT EVENT           
            Graphics g = e.Graphics;

            //CHOOSE PLOT
            if (NetMain.ComboType.SelectedIndex == 1) Neuroevolution.Plot(g);
            else Network.Plot(g);
        }

        public class PlotNetwork
        {
            NetMain Main;
            UniPanel Panel;
            NetNeural Net;

            //PANEL          
            int[] PanelTop;
            float PanelHeight;
            float PanelCenter;
            float PanelMax;
            float[] PanelRatio;

            //GRAPHICS
            Color ColorLine = Colors.MainDominant;
            Font DrawFont = Fonts.MainFont;
            Pen PenNode = new Pen(Color.Black);
            Pen PenLine;
            SolidBrush Brush = new SolidBrush(Colors.MainRecessive);
            int Alpha = 155;

            //VARIABLES
            const float Gap = 2;
            const float Radius = 2;
            const float Start = 20;
            float Current = Start;
            float[] NodesLeft = new float[] { 75, 275, 475 };
            const float NodesHeight = Radius * 2 + Gap;
            float[] NodesNext = new float[] { NodesHeight, NodesHeight, NodesHeight };

            //NODES
            int[] Count;
            float[] Area;

            //TEXT
            string[] Caption;
            string[] Factor;

            //LINES            
            List<PointF[]> PointList;

            public PlotNetwork(NetMain xNetMain, UniPanel xPanel)
            {
                //NETWORK PLOT
                Main = xNetMain;
                Panel = xPanel;
            }

            private Pen getPen(double xDouble)
            {
                //GET PEN
                return new Pen(Color.FromArgb((int)(255 * Math.Abs(xDouble)), ColorLine.R, ColorLine.G, ColorLine.B));
            }

            public void Plot(Graphics g)
            {
                //PLOT
                Net = Main.Net;

                //ABBRUCH
                if (Net == null)
                    return;

                //RECALCULATE PARAMETERS
                RecalculateNetwork(Net);

                //LOOP NODES
                for (int i = 0; i < Count.Length; i++)
                {
                    //DRAW CAPTION
                    int strWidth = Mod_Convert.StringToWidth(Caption[i], DrawFont) / 2;
                    g.DrawString(Caption[i], DrawFont, Brush, NodesLeft[i] - strWidth, PanelTop[0]);

                    Current = Area[i];
                    List<PointF> tempList = new List<PointF>();

                    //DRAW NODES
                    for (int node = 0; node < Count[i]; node++)
                    {
                        tempList.Add(new PointF(NodesLeft[i], Current));
                        g.DrawEllipse(PenNode, NodesLeft[i] - Radius, Current - Radius, Radius + Radius, Radius + Radius);
                        Current = Current + NodesNext[i];
                    }

                    PointList.Add(tempList.ToArray());

                    //DRAW SCALE
                    strWidth = Mod_Convert.StringToWidth(Factor[i], DrawFont) / 2;
                    g.DrawString(Factor[i], DrawFont, Brush, NodesLeft[i] - strWidth, PanelTop[2]);
                }

                double maxInput = getAbsMax(Net.weightInput);
                double maxOutput = getAbsMax(Net.weightOutput);

                //DRAW LINES
                foreach (PointF pt1 in PointList[0])
                    foreach (PointF pt2 in PointList[1])
                    {
                        int x = (int)(Array.IndexOf(PointList[0], pt1) * PanelRatio[0]);
                        int y = (int)(Array.IndexOf(PointList[1], pt2) * PanelRatio[1]);
                        double weight = Math.Abs(Net.weightInput[y, x]);
                        PenLine = getPen(weight / maxInput);
                        if (PenLine.Color.A > Alpha)
                            g.DrawLine(PenLine, pt1, pt2); //INPUT TO HIDDEN

                        foreach (PointF pt3 in PointList[2])
                        {
                            int z = (int)(Array.IndexOf(PointList[2], pt3) * PanelRatio[2]);
                            weight = Math.Abs(Net.weightOutput[z, y]);
                            PenLine = getPen(weight / maxOutput);
                            if (PenLine.Color.A > Alpha)
                                g.DrawLine(PenLine, pt2, pt3); //HIDDEN TO OUTPUT
                        }
                    }
            }

            private void RecalculateNetwork(NetNeural xNet)
            {
                //RECALCULATE NETWORK
                int nodesInput = xNet.NodesInput;
                int nodesHidden = xNet.NodesHidden;
                int nodesOutput = xNet.NodesOutput;

                //PANEL
                PanelTop = new int[] { 0, (int)Start * 2, Panel.Height - (int)Start * 2 };
                PanelHeight = PanelTop[2] - PanelTop[1];
                PanelCenter = PanelHeight / 2 + PanelTop[1];
                PanelMax = PanelHeight / NodesHeight;
                PanelRatio = new float[] { nodesInput / PanelMax, nodesHidden / PanelMax, nodesOutput / PanelMax };
                for (int i = 0; i < PanelRatio.Length; i++) if (PanelRatio[i] < 1) PanelRatio[i] = 1;

                //NODES
                Count = new int[] { (int)(nodesInput / PanelRatio[0]), (int)(nodesHidden / PanelRatio[1]), (int)(nodesOutput / PanelRatio[2]) };
                float[] space = new float[] { Count[0] * NodesHeight, Count[1] * NodesHeight, Count[2] * NodesHeight };
                Area = new float[] { PanelCenter - space[0] / 2, PanelCenter - space[1] / 2, PanelCenter - space[2] / 2 };

                //TEXTS
                Caption = new string[] { "Input Nodes (" + nodesInput + ")", "Hidden Nodes (" + nodesHidden + ")", "Output Nodes (" + nodesOutput + ")" };
                Factor = new string[] { Mod_Convert.DoubleFormat(PanelRatio[0], 2) + "x", Mod_Convert.DoubleFormat(PanelRatio[1], 2) + "x", Mod_Convert.DoubleFormat(PanelRatio[2], 2) + "x" };

                //LIST
                PointList = new List<PointF[]>();
            }
        }
    }

    public class PlotNeuroevolution
    {
        NetMain Main;
        UniPanel Panel;
        newPool Pool;

        //PANEL          
        float[] PanelTop;
        float[] PanelLeft;
        float PanelMax;

        //GRAPHICS
        Pen PenPositive;
        Pen PenNegative;
        Pen PenText;

        //VARIABLES
        float Start = 20;
        float HighScore;

        public PlotNeuroevolution(NetMain xNetMain, UniPanel xPanel)
        {
            //NEUROEVOLUTION PLOT
            Main = xNetMain;
            Panel = xPanel;
        }

        public void Plot(Graphics g)
        {
            Pool = Main.Mario.Pool;

            //ABBRUCH
            if (Pool == null)
                return;

            //RECALCULATE NEUROEVOLUTION
            RecalculateNeuroevolution(Pool);

            //GET SPCIES LIST
            List<newSpecies> spciesList = Pool.species;

            for (int i = 0; i < spciesList.Count; i++)
            {
                float width = PanelLeft[2] + getScoreWidth(spciesList[i].topFitness);
                float average = PanelLeft[2] + getScoreWidth(spciesList[i].averageFitness);
                float height = PanelTop[0] + i * PenPositive.Width;

                //DRAW NUMBER
                g.DrawString((i + 1) + ".", Mod_Convert.FontSize(Fonts.MainFont, PenText.Width), PenText.Brush, new PointF(PanelLeft[1], height - PenPositive.Width / 2));

                //DRAW SCORE
                if (Mod_Check.isEven(i)) g.DrawLine(PenNegative, new PointF(PanelLeft[2], height), new PointF(width, height));
                else g.DrawLine(PenPositive, new PointF(PanelLeft[2], height), new PointF(width, height));

                //DRAW STALE
                for (int l = 0; l < Pool.species[i].staleness; l++)
                    g.FillEllipse(new SolidBrush(Colors.MainLight), PanelLeft[2] + 1f + l * PenPositive.Width, height, PenPositive.Width / 2, PenPositive.Width / 2);

                //DRAW AVERAGE
                g.DrawLine(new Pen(Colors.MainDominant, 1), new PointF(average, height), new PointF(average, height + PenPositive.Width / 2));
            }

            //INFORMATIONS
            newSpecies currSpecies = Pool.species[Pool.currentSpecies];             //GET CURRENT SPECIES
            newGenome currGenome = currSpecies.genomes[Pool.currentGenome];               //GET CURRENT GENOME

            float left = PanelLeft[0];
            Font infoFont = Mod_Convert.FontSize(Fonts.MainFont, 9);
            g.DrawString("Generation:", infoFont, PenNegative.Brush, new PointF(left, PanelTop[1]));
            g.DrawString(Pool.generation.ToString(), infoFont, PenText.Brush, new PointF(left, PanelTop[2]));

            left += PanelLeft[0] * 2.5f;
            g.DrawString("Species:", infoFont, PenNegative.Brush, new PointF(left, PanelTop[1]));
            g.DrawString((Pool.currentSpecies + 1) + "/" + Pool.species.Count, infoFont, PenText.Brush, new PointF(left, PanelTop[2]));

            left += PanelLeft[0] * 2.5f;
            g.DrawString("Genome:", infoFont, PenNegative.Brush, new PointF(left, PanelTop[1]));
            g.DrawString((Pool.currentGenome + 1) + "/" + currSpecies.genomes.Count, infoFont, PenText.Brush, new PointF(left, PanelTop[2]));

            left += PanelLeft[0] * 2.5f;
            g.DrawString("Stale:", infoFont, PenNegative.Brush, new PointF(left, PanelTop[1]));
            g.DrawString(currSpecies.staleness + "/" + (NetMario.StaleSpecies - 1), infoFont, PenText.Brush, new PointF(left, PanelTop[2]));

            left += PanelLeft[0] * 2.5f;
            g.DrawString("Fitness:", infoFont, PenNegative.Brush, new PointF(left, PanelTop[1]));
            g.DrawString(currGenome.fitness + " (" + Pool.maxFitness + ", " + currSpecies.averageFitness + ")", infoFont, PenText.Brush, new PointF(left, PanelTop[2]));

            left += PanelLeft[0] * 2.5f;
            g.DrawString("Measure:", infoFont, PenNegative.Brush, new PointF(left, PanelTop[1]));
            g.DrawString(Pool.measured + " %", infoFont, PenText.Brush, new PointF(left, PanelTop[2]));
        }

        public void RecalculateNeuroevolution(newPool xPool)
        {
            //RECALCULATE NEUROEVOLUTION
            int spcies = xPool.species.Count;

            //PANEL
            PanelTop = new float[] { Start, Panel.Height - Start * 1.8f, Panel.Height - Start * 1.2f };
            PanelLeft = new float[] { Start, Start * 1.5f, Start * 2f };
            PanelMax = Panel.Width - (PanelLeft.Last() * 2 + Start);

            //CALCULATE LINE WIDTH
            float height = PanelTop[1] - PanelTop[0];
            PenPositive = new Pen(Colors.MainRecessive, height / spcies);
            PenNegative = new Pen(Colors.getLighter(Colors.MainRecessive, 0.5f), PenPositive.Width);
            PenText = new Pen(Colors.getColor(COLOR.BLACK), PenPositive.Width);

            //GET HIGHSCORE
            HighScore = (float)xPool.maxFitness;
        }

        private float getScoreWidth(double xScore)
        {
            //GET SCORE WIDTH
            return (PanelMax * (float)xScore) / HighScore;
        }
    }
}

