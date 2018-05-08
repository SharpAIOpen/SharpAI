using Core.Classes;
using Core.Enums;
using Core.Modifications;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;


/*############################################################################*
 *                      Neural Network Optimisation                           *
 *           Try to find best parameter for Perzeptron Network                *
 *                    writen in c# by Martin Kober 2017                       *
 *############################################################################*/


namespace NeuralNet.Project
{
    public class NetSim
    {
        //PREPARE VARIABLES
        NetMain NetMain;
        NetNeural Net;

        //DIALOG
        public UniDialog Dialog;
        UniPanel PanelSettings;
        UniPanel PanelPlot;

        //VARIABLES
        List<Run> PerformanceList = new List<Run>();
        DateTime Start;
        double LRbefore;
        double LRmin;
        double LRmax;
        double LRdelta;
        double LRnow;
        int Emin;
        int Emax;
        int Edelta;
        int Enow;

        //CONTROLS
        UniSpin SpinLRnow;
        UniSpin SpinLRmin;
        UniSpin SpinLRmax;
        UniSpin SpinLRdelta;
        UniSpin SpinEnow;
        UniSpin SpinEmin;
        UniSpin SpinEmax;
        UniSpin SpinEdelta;

        //PREPARE ACTIONS
        Action<int, int, int, int, double, MODE, bool> NetInit;
        Action<int> NetTrain;
        Func<string[], double> NetCheck;

        public NetSim(NetMain xNetMain)
        {
            //CREATE NET SIM OBJECT
            NetMain = xNetMain;

            //LEARNING RATE
            LRbefore = NetMain.LearningRate.getValue() / 100;
            LRmin = 0.005;       //0.5%
            LRmax = 0.02;        //2.0%
            LRdelta = LRmin;
            LRnow = LRmin;

            //EPOCHS
            Emin = 3;
            Emax = 15;
            Edelta = 1;
            Enow = Emin;

            //SET ACTIONS
            NetInit = NetMain.neuralNetworkInit;
            NetTrain = NetMain.eventWorkerStart;
            NetCheck = NetMain.neuralNetworkTest;

            //SET DIALOG
            createDialog();
            createSettings();
            createPlot();
        }

        private void createDialog()
        {
            //CREATE DIALOG
            Dialog = UniDialog.Create(ICON.CHART_LINE, "Simulation");
            Dialog.setButton("OK", "leave the dialog", new Action(() => { Dialog.Close(); }));
            Dialog.AllowAutoSize();
        }

        private void createSettings()
        {
            //CREATE SETTINGS
            PanelSettings = UniPanel.Create(Dialog, BorderStyle.None, false);
            PanelSettings.Size = new Size(400, 160);

            //LEARNING RATE
            decimal minimum = (decimal)NetMain.LearningRate.getMinimum() / 100;
            decimal maximum = (decimal)NetMain.LearningRate.getMaximum() / 100;
            decimal increment = (decimal)NetMain.LearningRate.TickFrequency / 1000;
            SpinLRnow = UniSpin.Create(PanelSettings, "Start:", "set start value of learningrate", NetMain.startposition[0], NetMain.startposition[1], minimum, maximum, increment, (decimal)LRnow);
            SpinLRmin = UniSpin.Create(PanelSettings, "LR (min):", "set minimal learnrate to check", Mod_Forms.nextLeft(SpinLRnow, NetMain.gap[0]), Mod_Forms.sameTop(SpinLRnow), minimum, maximum, increment, (decimal)LRmin);
            SpinLRmax = UniSpin.Create(PanelSettings, "LR (max):", "set maximal learnrate to check", Mod_Forms.nextLeft(SpinLRmin, NetMain.gap[0]), Mod_Forms.sameTop(SpinLRnow), minimum, maximum, increment, (decimal)LRmax);
            SpinLRdelta = UniSpin.Create(PanelSettings, "LR (delta):", "set delta increment per run", Mod_Forms.nextLeft(SpinLRmax, NetMain.gap[0]), Mod_Forms.sameTop(SpinLRnow), (decimal)0.001, (decimal)0.01, (decimal)0.001, (decimal)LRdelta);

            //EPOCHS
            SpinEnow = UniSpin.Create(PanelSettings, "Start:", "set start value of epoch", Mod_Forms.sameLeft(SpinLRnow), Mod_Forms.nextTop(SpinLRnow, NetMain.gap[1]), NetMain.SpinEpochs.Minimum, NetMain.SpinEpochs.Maximum, NetMain.SpinEpochs.Increment, Enow);
            SpinEmin = UniSpin.Create(PanelSettings, "Epoch (min):", "set minimal epoch to check", Mod_Forms.nextLeft(SpinEnow, NetMain.gap[0]), Mod_Forms.sameTop(SpinEnow), NetMain.SpinEpochs.Minimum, NetMain.SpinEpochs.Maximum, NetMain.SpinEpochs.Increment, Emin);
            SpinEmax = UniSpin.Create(PanelSettings, "Epoch (max):", "set maximal epoch to check", Mod_Forms.nextLeft(SpinEmin, NetMain.gap[0]), Mod_Forms.sameTop(SpinEnow), NetMain.SpinEpochs.Minimum, NetMain.SpinEpochs.Maximum, NetMain.SpinEpochs.Increment, Emax);
            SpinEdelta = UniSpin.Create(PanelSettings, "Epoch (delta):", "set delta increment per run", Mod_Forms.nextLeft(SpinEmax, NetMain.gap[0]), Mod_Forms.sameTop(SpinEnow), 1, 10, 1, Edelta);
        }

        private void createPlot()
        {
            Size size = new Size(740, 400);

            //CREATE PLOT
            PanelPlot = UniPanel.Create(Dialog, BorderStyle.None, false);
            PanelPlot.Size = new Size(size.Width, size.Height + 30);

            //CHART
            UniChart chart = UniChart.Create(PanelPlot, 0, 0, size.Width, size.Height, BorderStyle.FixedSingle);
            chart.setTitles(null, null, "Performance in %");
            chart.setLabelFormat(null, "0%");
            chart.setStartFromZeros(false);

            //EVENT LISTENER
            PanelPlot.VisibleChanged += (s, e) =>
            {
                //GET PERFORMANCE
                double[] perArray = new double[PerformanceList.Count];
                for (int i = 0; i < perArray.Length; i++)
                    perArray[i] = PerformanceList[i].Performance;

                //PREPARE CHART
                chart.SeriesRemove();
                Series series = chart.SeriesLine("Performance of Network", SeriesChartType.Line, Colors.MainRecessive, 1, perArray, null);
                series.MarkerStyle = MarkerStyle.Circle;
                series.MarkerColor = Colors.MainDominant;
                series.MarkerSize = 4;
                series.MarkerBorderWidth = 1;
                series.MarkerBorderColor = Colors.MainRecessive;

                //DATA LABEL
                for (int i = 0; i < PerformanceList.Count; i++)
                    series.Points[i].Label = "LR: " + (PerformanceList[i].LearningRate * 100) + "%\nE: " + PerformanceList[i].Epochs + "\n(" + PerformanceList[i].Duration.ToString(@"mm\:ss") + ")";
            };
        }

        public void ShowSettings()
        {
            //SHOW SETTINGS
            PanelPlot.Visible = false;
            PanelSettings.Visible = true;
            Dialog.ShowDialog();
        }

        public void ShowPlot()
        {
            //SHOW PLOT
            PanelSettings.Visible = false;
            PanelPlot.Visible = true;
            Dialog.ShowDialog();
        }

        public void Run()
        {
            //CHECK TABLE TRAIN
            if (NetMain.TableTrain.RowCount == 0)
            { NetMain.setConsoleInvoke("No items in table"); NetMain.ToggleSim.Checked = false; }

            //ABBRUCH
            if (!NetMain.ToggleSim.Checked)
                return;

            //LEARNING RATE
            LRmin = (double)SpinLRmin.Value;
            LRmax = (double)SpinLRmax.Value;
            LRdelta = (double)SpinLRdelta.Value;
            LRnow = (double)SpinLRnow.Value;

            //EPOCHS
            Emin = SpinEmin.getInteger();
            Emax = SpinEmax.getInteger();
            Edelta = SpinEdelta.getInteger();
            Enow = SpinEnow.getInteger();

            Net = NetMain.Net;
            NetMain.Content = NetMain.TableTrain.ToStringArray(NetMain.MainSplit);
            SimNext();
        }

        private void SimNew()
        {
            //NEW SIMULATION
            NetMain.Worker = new UniProcess("simulation LR: " + LRnow + ", E: " + Enow, SimStart, SimCancel, null, SimCompleted);
            Start = DateTime.Now;
        }

        private void SimStart()
        {
            //START SIMULATION
            NetInit?.Invoke(Net.PixelWidth, Net.PixelHeight, Net.NodesHidden, Net.NodesOutput, LRnow, Net.Mode, false);
            NetTrain?.Invoke(Enow);
            double performance = NetCheck.Invoke(NetMain.Content);
            PerformanceList.Add(new Run(NetMain.Worker.Name, DateTime.Now - Start, LRnow, Enow, performance));
        }

        private void SimCompleted()
        {
            //COMPLETED SIMULATION
            LRnow = LRnow + LRdelta;
            if (LRnow > LRmax)
            {
                Enow = Enow + Edelta;
                LRnow = LRmin;
                if (Enow > Emax) { NetMain.ToggleSim.Checked = false; SimCancel(); } //RESET NET TO START
            }

            //NEXT SIMULATION
            SimNext();
        }

        private void SimCancel()
        {
            //CANCEL SIMULATION
            NetMain.ToggleSim.Checked = false;
            SimNext();
        }

        private void SimNext()
        {
            //NEXT SIMULATION
            if (NetMain.ToggleSim.Checked)
            {
                SimNew();
                NetMain.PanelProgress.AddProgress(NetMain.Worker);
            }
            else //FIND MINIMUM
            {
                NetInit?.Invoke(Net.PixelWidth, Net.PixelHeight, Net.NodesHidden, Net.NodesOutput, LRbefore, Net.Mode, false);
                Run best = new Run(null, TimeSpan.MinValue, 0, 0, 0);

                //LOOP PERFORMANCE LIST
                foreach (Run run in PerformanceList)
                    if (run.Performance > best.Performance)
                        best = run;

                double percent = Mod_Convert.DoubleFormat(best.Performance * 100, 2);
                NetMain.setConsoleInvoke("Simulation stoppt, best performance: " + percent + " % (LR: " + (best.LearningRate * 100) + " %, E: " + best.Epochs + ")");

                //PERFORMANCE PLOT
                ShowPlot();
            }
        }
    }

    class Run
    {
        public string Name;
        public TimeSpan Duration;
        public double LearningRate;
        public int Epochs;
        public double Performance;

        public Run(string xName, TimeSpan xDuration, double xLearningRate, int xEpochs, double xPerformance)
        {
            //CREATE RUN OBJECT
            Name = xName;
            Duration = xDuration;
            LearningRate = xLearningRate;
            Epochs = xEpochs;
            Performance = xPerformance;
        }

        //TO STRING
        public new string ToString() { return "Run [" + Performance + ", " + LearningRate + ", " + Epochs + "]"; }
    }
}
