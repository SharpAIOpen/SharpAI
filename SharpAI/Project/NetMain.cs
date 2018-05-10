using Core.Classes;
using Core.Enums;
using Core.Modifications;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static NeuralNet.Project.NetMario;


/*############################################################################*
 *                       Main Project User Interface                          *
 *          To organize user interface and properties of network              *
 *                    writen in c# by Martin Kober 2017                       *
 *############################################################################*/


namespace NeuralNet.Project
{
    public class NetMain : UniForm
    {
        public const string MainSplit = ",";
        private static Size MainSize = new Size(1424, 750);
        public Timer MainClock;
        public static TextBox ConsoleBox;
        public static Delegate ConsoleCall = Mod_Convert.MethodToDelegate(typeof(NetMain), "setConsole", typeof(object), typeof(bool), typeof(bool));

        //MASTER
        public NetNeural Net;
        public NetMario Mario;
        public NetCam Cam;
        public NetSim Sim;
        public NetPlot Plot;
        public NetIO IO;

        //CONTENT
        public string[] Content;
        public UniProcess Worker;
        private List<string> ColumnList = new List<string>();
        private List<string> AnswerList = new List<string>();

        //CONTROLS
        private UniPanel FormProperties;                //1. PROPERTIES
        private UniPanel FormTrain;                     //2. TRAIN FOR PERZEPTRON
        private UniPanel FormLearn;                     //2. LEARN FOR NEAT
        private UniPanel FormDraw;                      //3. DRAW FOR PERZEPTRON
        private UniPanel FormDisplay;                   //3. DISPLAY FOR NEAT
        private UniPanel FormLive;                      //4. LIVE
        private UniLabel[] LabelOption;
        private Action<bool> EnableAction;              //PROPERTIES ENABLE ACTION

        //PROPERTIES CONTROL
        string[] TypeItems = new string[] { "Perzeptron", "NEAT" };
        public UniCombo ComboType;                      //NETWORK TYPE
        public UniSpin PixelWidth;                      //NETWORK & CAM WIDTH
        public UniSpin PixelHeight;                     //NETWORK & CAM HEIGHT
        public UniSpin NodesHidden;                     //HIDDEN NODES PERZEPTRON
        public UniSpin NodesOutput;                     //OUTPUT NODES PERZEPTRON
        public UniSpin NeatPopulation;                  //POPULATION NEAT
        public UniSpin NeatStalness;                    //STALNESS NEAT
        public UniTrack LearningRate;                   //LEARNING RATE PERZEPTRON
        public UniGroup GroupMode;                      //INPUT MODE       

        //TRAINING CONTROL
        public UniCheck ToggleSim;
        public UniSpin SpinEpochs;
        public UniProgressPanel PanelProgress;
        public CheckComboBox TableFilter;
        private InfoLabel LabelStat;
        public UniTable TableTrain;

        //LEARN CONTROL
        public UniChart ScoreChart;
        public Series ScoreSeries;
        public Font ScoreFont = Mod_Convert.FontSize(Fonts.MainFont, 9);
        public Action<int> RoundFinished;

        //CAMERA CONTROL
        public UniLabel LabelScore;
        public UniLabel LabelFps;
        public UniText PosiX;
        public UniText PosiY;
        public NetTable TableLive;
        public UniSpin SpinFps;
        public UniSpin SpinDelay;
        public UniTrack TrackOpacity;
        public UniTrack TrackZoom;
        public UniToggle ToggleLearn;
        public UniToggle ToggleCam;
        public UniToggle ToggleQuery;
        public UniToggle ToggleTeach;
        public UniToggle ToggleSend;

        //PANEL CONTROL
        public DrawPanel PanelDraw;
        public UniPanel PanelRedraw;
        public UniPanel PanelNeural;
        public UniPanel PanelShow;
        public UniPanel PanelBinary;

        //DISTANCES
        public int[] startposition = new int[] { 20, 30 };
        public int[] ctlWidth = new int[] { 60, 80, 140 };
        public int[] ctlHeight = new int[] { 20, 260, 260 };
        public int[] gap = new int[] { 20, 30, 60, 80, 100 };

        //VARIABLES
        public bool FormFocus;

        //LAST
        private int LastCount = 0;

        public NetMain() : base(ICON.GRAPH_CLAW, "SharpAI beta v1.0", MainSize, new Size(0, 0), FormBorderStyle.FixedSingle, null, true, false, FORMTYPE.NORMAL)
        {
            //CREATE NET MAIN OBJECT
            NetDraw.NetMain = this;
            FormProperties = createPanelProperties();
            FormTrain = createPanelTrain();
            FormLearn = createPanelLearn();
            FormDraw = createPanelDraw();
            FormDisplay = createPanelDisplay();
            FormLive = createPanelLive();

            //CREATE MASTER
            Cam = new NetCam(this);
            Mario = new NetMario(this);
            Sim = new NetSim(this);
            Plot = new NetPlot(this);

            //CREATE LABEL
            Font font = Mod_Convert.Font(Fonts.MainFont, 14, FontStyle.Bold);
            UniLabel.CreateControl(this, FormProperties, "1.) Properties", 0, font);
            UniLabel.CreateControl(this, FormTrain, "2.) Training", 0, font);
            UniLabel.CreateControl(this, FormLearn, "2.) Self-learning", 0, font);
            UniLabel.CreateControl(this, FormDraw, "3.) Practice", 0, font);
            UniLabel.CreateControl(this, FormDisplay, "3.) Input / Output", 0, font);
            UniLabel.CreateControl(this, FormLive, "4.) Live", 0, font);

            //ENABLE PANEL TRAIN
            setPanelEnable(false);

            //CREATE CONSOLE
            ConsoleBox = createConsole();

            //MAIN CLOCK
            MainClock = new Timer();
            MainClock.Interval = 1000;

            //CHECK FOR UPDATES
            UpdateCheck();

            //GLOBAL LISTENR
            new UniGlobal();

            //SHORT KEY LISTENER
            KeyDown += (s, e) => { if (e.Control && e.KeyCode == Keys.Space) { if (AnswerList.Count > 0) eventDrawConvert(PanelDraw.getBitmap(), AnswerList.Last()); } };

            //GLOBAL KEY LISTENER
            UniGlobal.GlobalEvents.KeyDown += (s, e) =>
            {
                switch (e.KeyData)
                {
                    case Keys.Space: //KILL SWITCH
                        if (ToggleQuery.Checked || ToggleTeach.Checked || ToggleSend.Checked || ToggleLearn.Checked)
                        {
                            setConsoleInvoke("[SPACE] Kill switch, all actions terminated", true);
                            ToggleQuery.Checked = false;
                            ToggleTeach.Checked = false;
                            ToggleSend.Checked = false;
                            ToggleLearn.Checked = false;
                        }
                        break;

                    case Keys.F9: ToggleLearn.Checked = !ToggleLearn.Checked; break;
                    case Keys.F10: ToggleQuery.Checked = !ToggleQuery.Checked; break;
                    case Keys.F11: ToggleTeach.Checked = !ToggleTeach.Checked; break;
                    case Keys.F12: ToggleSend.Checked = !ToggleSend.Checked; break;
                }
            };
        }

        private async void UpdateCheck()
        {
            try
            {
                //CHECK GIT FOR UPDATES
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://github.com/SharpAIOpen/SharpAI/blob/master/Update.txt");
                HttpResponseMessage response = await client.SendAsync(request);
                String jsonStr = await response.Content.ReadAsStringAsync();
                bool update = !jsonStr.Contains(Text);

                //IF UPDATE AVAILABLE
                if (update)
                    if (UniMsg.Show("Update available..", "A new SharpAI version is under https://github.com/SharpAIOpen/SharpAI available!\n\nDownload now?", MessageBoxButtons.OKCancel, MSGICON.LAMP))
                        System.Diagnostics.Process.Start("https://github.com/SharpAIOpen/SharpAI");

                client.Dispose();
            }
            catch
            { }
        }

        private UniPanel createPanelProperties()
        {
            //CREATE PROPERTIES PANEL
            UniPanel panelProperties = UniPanel.Create(this, startposition[0], startposition[1], (Width / 2) - (startposition[0] * 2), ctlHeight[1], BorderStyle.FixedSingle);

            //CREATE COMBOBOX
            ComboType = UniCombo.Create(panelProperties, "Network type:", TypeItems, "choose neural network type", startposition[0], startposition[0], ctlWidth[1], ctlHeight[0], TypeItems[0]);

            //CREATE BUTTONS
            UniButton btnNew = UniButton.Create(panelProperties, "New", "initzialize a new neural network", Mod_Forms.sameLeft(ComboType), Mod_Forms.nextTop(ComboType, gap[0]), ctlWidth[1], new Action(() => { neuralNetworkNew(); }));
            UniButton btnLoad = UniButton.Create(panelProperties, "Load", "load train progress as .xml", Mod_Forms.sameLeft(btnNew), Mod_Forms.nextTop(btnNew, gap[0]), ctlWidth[1], new Action(() => { neuralNetworkLoad(); }));
            UniButton btnSave = UniButton.Create(panelProperties, "Save", "save train progress as .xml", Mod_Forms.sameLeft(btnNew), Mod_Forms.nextTop(btnLoad, gap[0]), ctlWidth[1], new Action(() => { neuralNetworkSave(); }), ctlHeight[0], null, ICON.EMPTY, false);
            UniButton btnPlot = UniButton.Create(panelProperties, "Plot", "plot neural network", Mod_Forms.sameLeft(btnNew), Mod_Forms.nextTop(btnSave, gap[0]), ctlWidth[1], new Action(() => { Plot.Open(); }), ctlHeight[0], null, ICON.EMPTY, false);
            UniButton btnOffline = UniButton.Create(panelProperties, "Offline", "set neural network offline", Mod_Forms.sameLeft(btnNew), Mod_Forms.nextTop(btnPlot, gap[0]), ctlWidth[1], new Action(() => { neuralNetworkOffline(); }), ctlHeight[0], null, ICON.EMPTY, false);

            //CREATE OPTION ELEMENTS
            PixelWidth = UniSpin.Create(panelProperties, "width:", "set pixel width", Mod_Forms.nextLeft(ComboType, gap[2]), startposition[1], 10, 200, 1, 28);
            PixelHeight = UniSpin.Create(panelProperties, "height:", "set pixel height", Mod_Forms.nextLeft(PixelWidth, gap[0]), Mod_Forms.sameTop(PixelWidth), 10, 200, 1, 28);
            NodesHidden = UniSpin.Create(panelProperties, "hidden nodes:", "set amount of hidden nodes", Mod_Forms.sameLeft(PixelWidth), Mod_Forms.nextTop(PixelWidth, gap[1]), 10, 1000, 10, 200);
            NodesOutput = UniSpin.Create(panelProperties, "output nodes:", "set amount of output nodes", Mod_Forms.nextLeft(NodesHidden, gap[0]), Mod_Forms.sameTop(NodesHidden), 1, 100, 1, 10);
            NeatPopulation = UniSpin.Create(panelProperties, "population:", "set amount of population in NEAT network", Mod_Forms.sameLeft(PixelWidth), Mod_Forms.nextTop(PixelWidth, gap[1]), 1, 500, 1, 300);
            NeatStalness = UniSpin.Create(panelProperties, "stalness:", "set stalness in NEAT network", Mod_Forms.nextLeft(NodesHidden, gap[0]), Mod_Forms.sameTop(NodesHidden), 1, 100, 1, 15);
            LearningRate = UniTrack.Create(panelProperties, "learning rate:", "set learning rate of neural network in percent", Mod_Forms.sameLeft(NodesHidden), Mod_Forms.nextTop(NodesHidden, gap[1]), NodesHidden.Width * 2 + gap[0], gap[0], 1, 100, 1, 1, TickStyle.BottomRight, Orientation.Horizontal, 1, "%");
            LearningRate.setScale(0.1);
            GroupMode = UniGroup.Create(panelProperties, new string[] { "Pixel", "HSensor" }, new string[] { "XY Pixel Network with range from 0 to 255", "1Y Sensor Network with range from 0 to X" }, LearningRate.Left, Mod_Forms.nextTop(LearningRate, gap[1]), LearningRate.Width, gap[2]);
            UniLabel.CreateControl(panelProperties, GroupMode, "Mode:");

            //CREATE OPTION LABELS
            int smallgap = 3; Font font = Fonts.MainFont;
            UniLabel labelStatus = UniLabel.Create(panelProperties, "Neural Network.......offline", Mod_Forms.nextLeft(PixelHeight, gap[2]), startposition[1], 0, 0, font);
            UniLabel labelPixel = UniLabel.Create(panelProperties, string.Empty, Mod_Forms.sameLeft(labelStatus), Mod_Forms.nextTop(labelStatus, smallgap), 0, 0, font);
            UniLabel labelInput = UniLabel.Create(panelProperties, string.Empty, Mod_Forms.sameLeft(labelStatus), Mod_Forms.nextTop(labelPixel, smallgap), 0, 0, font);
            UniLabel labelHidden = UniLabel.Create(panelProperties, string.Empty, Mod_Forms.sameLeft(labelStatus), Mod_Forms.nextTop(labelInput, smallgap), 0, 0, font);
            UniLabel labelOutput = UniLabel.Create(panelProperties, string.Empty, Mod_Forms.sameLeft(labelStatus), Mod_Forms.nextTop(labelHidden, smallgap), 0, 0, font);
            UniLabel labelRate = UniLabel.Create(panelProperties, string.Empty, Mod_Forms.sameLeft(labelStatus), Mod_Forms.nextTop(labelOutput, smallgap), 0, 0, font);
            UniLabel labelMode = UniLabel.Create(panelProperties, string.Empty, Mod_Forms.sameLeft(labelStatus), Mod_Forms.nextTop(labelRate, smallgap), 0, 0, font);
            LabelOption = new UniLabel[] { labelStatus, labelPixel, labelInput, labelHidden, labelOutput, labelRate, labelMode };

            //EVENT LISTENER
            ComboType.SelectedIndexChanged += eventComboTypeChange;
            LearningRate.ValueChanged += (s, e) => { if (Net != null) { Net.LearningRate = LearningRate.getValue() / 100; setLabelNetwork(); } };

            //ENABLE ACTION
            EnableAction = new Action<bool>((bool xEnable) =>
            {
                Control[] ctlA = new Control[] { btnNew, btnLoad, ComboType, PixelWidth, PixelHeight, NodesHidden, NodesOutput, NeatPopulation, NeatStalness };
                foreach (Control ctl in ctlA)
                    ctl.Enabled = xEnable;

                Control[] ctlB = new Control[] { btnSave, btnPlot, btnOffline };
                foreach (Control ctl in ctlB)
                    ctl.Enabled = !xEnable;
            });

            return panelProperties;
        }

        private UniPanel createPanelTrain()
        {
            //CREATE TRAIN PANEL
            UniPanel panelTrain = UniPanel.Create(this, Mod_Forms.nextLeft(FormProperties), Mod_Forms.sameTop(FormProperties), Mod_Forms.fillWidth(this, Mod_Forms.nextLeft(FormProperties), gap[0]), ctlHeight[1], BorderStyle.FixedSingle);

            //CREATE ELEMENTS
            int shift = 5;
            UniButton btnTest = UniButton.Create(panelTrain, string.Empty, "choose knowledge base .csv to test neural network", startposition[0], startposition[1], ctlWidth[0] / 2, gap[1], null, ICON.GRAPH_CLAW);
            UniButton btnOpen = UniButton.Create(panelTrain, string.Empty, "open knowledge base (.csv)", Mod_Forms.nextLeft(btnTest, shift), btnTest.Top, ctlWidth[0] / 2, null, gap[1], null, ICON.FOLDER_OPEN);
            UniButton btnSave = UniButton.Create(panelTrain, string.Empty, "save table (.csv)", Mod_Forms.nextLeft(btnOpen, shift), btnTest.Top, ctlWidth[0] / 2, null, gap[1], null, ICON.SAVE);
            UniButton btnShuffle = UniButton.Create(panelTrain, string.Empty, "shuffle value table", Mod_Forms.nextLeft(btnSave, shift), btnTest.Top, ctlWidth[0] / 2, null, gap[1], null, ICON.NAV_REFRESH);
            UniButton btnInv = UniButton.Create(panelTrain, string.Empty, "invert value table", Mod_Forms.nextLeft(btnShuffle, shift), btnTest.Top, ctlWidth[0] / 2, null, gap[1], null, ICON.ROTATE_LEFT);
            UniButton btnClear = UniButton.Create(panelTrain, string.Empty, "clear table", Mod_Forms.nextLeft(btnInv, shift), btnTest.Top, ctlWidth[0] / 2, gap[1], null, ICON.TRASH);

            SpinEpochs = UniSpin.Create(panelTrain, "epochs:", "set amount of epochs", Mod_Forms.nextLeft(btnClear, gap[1]), btnTest.Top - shift * 2, 1, 100, 1, 10, LeftRightAlignment.Left);
            UniButton btnTrain = UniButton.Create(panelTrain, string.Empty, "start training", Mod_Forms.nextLeft(SpinEpochs), SpinEpochs.Top, ctlWidth[0] / 2, gap[0], null, ICON.PLAY);
            ToggleSim = UniToggle.Create(panelTrain, "Sim", "find best performance with modelling learningrate and epochs", Mod_Forms.nextLeft(btnTrain, gap[0]), SpinEpochs.Top, ctlWidth[0], SpinEpochs.Height);
            UniButton btnOpt = UniButton.Create(panelTrain, string.Empty, "simulation setup", Mod_Forms.nextLeft(ToggleSim), ToggleSim.Top, ToggleSim.Height, null, ToggleSim.Height, null, ICON.EDIT);
            PanelProgress = UniProgressPanel.Create(panelTrain, Mod_Forms.sameLeft(SpinEpochs), Mod_Forms.nextTop(SpinEpochs, shift), (btnOpt.Right - SpinEpochs.Left) + shift * 3, gap[1], BorderStyle.None, Colors.MainBackground);

            //CREATE TABLE TRAIN
            TableTrain = UniTable.Create(panelTrain, Mod_Forms.sameLeft(btnTest), Mod_Forms.nextTop(btnTest, gap[0]), Mod_Forms.fillWidth(panelTrain, Mod_Forms.sameLeft(btnTest), gap[0]), Mod_Forms.fitHeight(panelTrain, Mod_Forms.nextTop(btnTest, gap[1]), gap[0]), DataGridViewColumnSortMode.NotSortable, DataGridViewSelectionMode.FullRowSelect, false, false, false, false, false);
            TableTrain.setRowHeader(".");
            TableTrain.AllowRemove();
            TableTrain.setActionRemove(eventTableStat);
            LabelStat = InfoLabel.Create(panelTrain, "0", Mod_Forms.prevLeft(panelTrain, ctlWidth[0], gap[1] / 2), shift, Mod_Convert.FontSize(Fonts.MainFont, 16));

            //CREATE FILTER COMBOCHECKBOX
            TableFilter = CheckComboBox.Create(panelTrain, "choose filter", "filter answers", null, TableTrain.Right - ctlWidth[2], Mod_Forms.prevTop(TableTrain, gap[0]), ctlWidth[2], gap[0], true);
            TableFilter.setAction(null, eventFilterChange);

            //BACKGROUND WORKER EVENTS
            Action actionStart = new Action(() => eventWorkerStart(SpinEpochs.getInteger()));
            Action actionCancle = new Action(() => btnTrain.Enabled = true);
            Action actionCompleted = new Action(() => eventWorkerCompleted(btnTrain));

            //EVENT LISTENER     
            panelTrain.VisibleChanged += (s, e) => { if (panelTrain.Visible) { PanelProgress.Left = Mod_Forms.sameLeft(SpinEpochs); PanelProgress.Top = Mod_Forms.nextTop(SpinEpochs, shift); } };
            btnTest.Click += (s, e) => neuralNetworkTest();
            btnOpen.Click += (s, e) => eventTrainingOpen();
            btnSave.Click += (s, e) => eventTrainingSave();
            btnShuffle.Click += (s, e) => eventTrainingShuffle();
            btnInv.Click += (s, e) => TableTrain.setColumnValues(1, NetDraw.Invert(TableTrain.getColumnValues(1)));
            btnClear.Click += (s, e) => { TableTrain.Columns.Clear(); LabelStat.Text = "0"; AnswerList = new List<string>(); };
            btnTrain.Click += (s, e) => { btnTrain.Enabled = false; eventTrainingStart(TableTrain); Worker = new UniProcess("training", actionStart, null, null, actionCompleted); PanelProgress.AddProgress(Worker); };
            ToggleSim.Click += (s, e) => Sim.Run();
            btnOpt.Click += (s, e) => Sim.ShowSettings();

            return panelTrain;
        }

        private UniPanel createPanelLearn()
        {
            //CREATE TRAIN PANEL
            UniPanel panelLearn = UniPanel.Create(this, Mod_Forms.nextLeft(FormProperties), Mod_Forms.sameTop(FormProperties), Mod_Forms.fillWidth(this, Mod_Forms.nextLeft(FormProperties), gap[0]), ctlHeight[1], BorderStyle.FixedSingle, null, false);

            //SPECIES INFORMATION
            UniText textGen = UniText.CreateLabeled(panelLearn, "Generation:", string.Empty, "current generation", startposition[0], startposition[1], ctlWidth[0], 0, null, 0, false, false, true);
            UniText textSpecies = UniText.CreateLabeled(panelLearn, "Species:", string.Empty, "current species", Mod_Forms.sameLeft(textGen), Mod_Forms.nextTop(textGen, gap[0]), ctlWidth[0], 0, null, 0, false, false, true);
            UniText textGenome = UniText.CreateLabeled(panelLearn, "Genome:", string.Empty, "current genome", Mod_Forms.sameLeft(textGen), Mod_Forms.nextTop(textSpecies, gap[0]), ctlWidth[0], 0, null, 0, false, false, true);
            UniText textStale = UniText.CreateLabeled(panelLearn, "Stalness:", string.Empty, "current stalness", Mod_Forms.sameLeft(textGen), Mod_Forms.nextTop(textGenome, gap[0]), ctlWidth[0], 0, null, 0, false, false, true);
            UniText textFitness = UniText.CreateLabeled(panelLearn, "Fitness:", string.Empty, "current fitness and average fitness", Mod_Forms.sameLeft(textGen), Mod_Forms.nextTop(textStale, gap[0]), ctlWidth[0], 0, null, 0, false, false, true);
            UniText textMeasured = UniText.CreateLabeled(panelLearn, "Measured:", string.Empty, "current progress", Mod_Forms.sameLeft(textGen), Mod_Forms.nextTop(textFitness, gap[0]), ctlWidth[0], 0, null, 0, false, false, true);

            //CREATE ELEMENTS
            int shift = 5;
            UniText textStatus = UniText.CreateLabeled(panelLearn, "Status:", string.Empty, "current status", Mod_Forms.nextLeft(textGen, gap[1]), Mod_Forms.sameTop(textGen), ctlWidth[2], 0, null, 0, false, false, true);
            ToggleLearn = UniToggle.Create(panelLearn, "Learn", "network learn by them self", Mod_Forms.nextLeft(textStatus, gap[0]), Mod_Forms.sameTop(textGen), ctlWidth[0], gap[0]);
            UniButton btnOpen = UniButton.Create(panelLearn, string.Empty, "open learning", Mod_Forms.nextLeft(ToggleLearn, gap[0]), Mod_Forms.sameTop(textGen), ctlWidth[0] / 2, null, gap[1], null, ICON.FOLDER_OPEN);
            UniButton btnSave = UniButton.Create(panelLearn, string.Empty, "save learning", Mod_Forms.nextLeft(btnOpen, shift), Mod_Forms.sameTop(textGen), ctlWidth[0] / 2, null, gap[1], null, ICON.SAVE);
            UniButton btnReset = UniButton.Create(panelLearn, string.Empty, "reset learning", Mod_Forms.nextLeft(btnSave, shift), Mod_Forms.sameTop(textGen), ctlWidth[0] / 2, null, gap[1], null, ICON.NAV_REFRESH);

            //SCORE CHART
            ScoreChart = UniChart.Create(panelLearn, Mod_Forms.nextLeft(textGen, gap[1]), Mod_Forms.nextTop(btnOpen, gap[0]), Mod_Forms.fillWidth(panelLearn, Mod_Forms.nextLeft(textGen, gap[1]), gap[4]), Mod_Forms.fitHeight(panelLearn, Mod_Forms.nextTop(btnOpen, gap[0]), gap[1]), BorderStyle.FixedSingle);

            //POOL INFORMATION
            UniText textDuration = UniText.CreateLabeled(panelLearn, "Training duration:", string.Empty, "total duration of training", Mod_Forms.prevLeft(panelLearn, ctlWidth[2], gap[0]), Mod_Forms.sameTop(textGen), ctlWidth[2], 0, null, 0, false, false, true);
            UniText textHighscore = UniText.CreateLabeled(panelLearn, "Highscore:", string.Empty, "highscore from pool", Mod_Forms.prevLeft(panelLearn, ctlWidth[0], gap[0]), Mod_Forms.nextTop(textDuration, gap[1]), ctlWidth[0], 0, null, 0, false, false, true);

            //EVENT LISTENER
            ToggleLearn.CheckedChanged += (s, e) =>
            {
                if (ToggleLearn.Checked)
                {
                    //RESET SERIES
                    ScoreChart.Clear();
                    ScoreSeries = ScoreChart.SeriesLine("Score", SeriesChartType.Line, Color.Red, 1, null);
                    ScoreChart.Focus();
                }
                Mario.Run(TableLive.getKeys());
            };
            btnOpen.Click += (s, e) => Mario.Load();                                                //LOAD TXT
            btnSave.Click += (s, e) => Mario.Save();                                                //SAVE TXT
            btnReset.Click += (s, e) => Mario.Initialize(TableLive.getKeys());                      //RESET

            RoundFinished = new Action<int>((int xScore) =>
            {
                //ROUND FINISHED ACTION
                if (Mario.Pool == null) return;
                TimeSpan duration = Mario.Duration;
                if (Mario.Learning) duration = duration + (DateTime.Now - Mario.Start);

                int gen = Mario.Pool.generation;
                int species = Mario.Pool.currentSpecies + 1;
                int genome = Mario.Pool.currentGenome + 1;

                newSpecies currSpecies = Mario.Pool.species[Mario.Pool.currentSpecies];             //GET CURRENT SPECIES
                newGenome currGenome = currSpecies.genomes[Mario.Pool.currentGenome];               //GET CURRENT GENOME

                textDuration.Text = duration.ToString(@"hh\:mm\:ss") + " - " + (int)duration.TotalDays + " Days";                       //DURATION
                textHighscore.Text = Mod_Convert.DoubleToString(Mario.Pool.maxFitness);                                                 //HIGHSCORE
                textStatus.Text = Mario.Status;                                                                                         //STATUS
                textGen.Text = Mod_Convert.IntegerToString(gen);                                                                        //GENERATION
                textSpecies.Text = species + "/" + Mario.Pool.species.Count;                                                            //SPECIES
                textGenome.Text = genome + "/" + currSpecies.genomes.Count;                                                             //GENOME
                textStale.Text = currSpecies.staleness + "/" + (StaleSpecies - 1);                                                      //STALNESS
                textFitness.Text = Cam.Score + " (" + currSpecies.averageFitness + ")";                                                 //FITNESS
                textMeasured.Text = Mod_Convert.DoubleToString(Mario.Pool.measured) + "%";                                              //MEASURED

                //ABBRUCH
                if (xScore == 0)
                    return;

                //ADD CHART SERIES POINT                
                ScoreSeries.Points.Add(xScore);
                ScoreChart.AnnotationText(ScoreFont, "(" + gen + ", " + species + ", " + genome + ")", new DataPoint(ScoreSeries.Points.Count - 0.15, xScore), 0, 0, ContentAlignment.MiddleCenter);

                //DYNAMIC Y-AXIS
                int max = 7;
                int points = ScoreSeries.Points.Count;
                if (points > max)
                    ScoreChart.getLastArea().AxisX.Minimum = points - max;
            });

            return panelLearn;
        }

        private UniPanel createPanelDraw()
        {
            //CREATE DRAW PANEL
            UniPanel panelDraw = UniPanel.Create(this, startposition[0], Mod_Forms.nextTop(FormProperties, gap[1]), FormProperties.Width, ctlHeight[2], BorderStyle.FixedSingle);

            //CREATE DRAW PANELS
            PanelDraw = DrawPanel.Create(panelDraw, startposition[0], startposition[1], ctlWidth[2], ctlWidth[2]);

            UniPanel redrawPanel = UniPanel.Create(panelDraw, Mod_Forms.calcCenterParent(PanelDraw, panelDraw, -gap[0]).X, Mod_Forms.sameTop(PanelDraw), PanelDraw.Width, PanelDraw.Height, BorderStyle.FixedSingle, Colors.MainDark);
            PanelRedraw = UniPanel.Create(redrawPanel, 0, 0, redrawPanel.Width, redrawPanel.Height, BorderStyle.None, Colors.getColor(COLOR.WHITE));

            UniPanel neuralPanel = UniPanel.Create(panelDraw, Mod_Forms.prevLeft(panelDraw, PanelDraw.Width, gap[0]), Mod_Forms.sameTop(PanelDraw), PanelDraw.Width, PanelDraw.Height, BorderStyle.FixedSingle, Colors.MainDark);
            PanelNeural = UniPanel.Create(neuralPanel, 0, 0, neuralPanel.Width, neuralPanel.Height, BorderStyle.None, Colors.getColor(COLOR.WHITE));

            List<UniPanel> PanelList = new List<UniPanel>();
            Size historyPanelSize = new Size(56, 56);

            //HISTORY PANELS
            for (int i = 0; i < (panelDraw.Width - startposition[0] * 2) / historyPanelSize.Width; i++)
            {
                UniPanel historyPanel = UniPanel.Create(panelDraw, Mod_Forms.sameLeft(PanelDraw, historyPanelSize.Width * i), Mod_Forms.nextTop(PanelDraw, gap[0]), historyPanelSize.Width, historyPanelSize.Height, BorderStyle.FixedSingle, Colors.getColor(COLOR.WHITE));
                historyPanel.addToolTip("History Panel " + (i + 1));
                PanelList.Add(historyPanel);
            }

            //CREATE BUTTON
            UniButton btnCheck = UniButton.Create(panelDraw, "check", "check drawn picture", Mod_Forms.nextLeft(PanelDraw, gap[0]), Mod_Forms.sameTop(PanelDraw), ctlWidth[0], new Action(() => { NetDraw.Draw(PanelDraw.getBitmap(), PanelList.ToArray()); PanelDraw.Clear(); }));
            UniButton btnAdd = UniButton.Create(panelDraw, "add", "add draw to table", Mod_Forms.nextLeft(PanelDraw, gap[0]), Mod_Forms.nextTop(btnCheck), ctlWidth[0], new Action(() => eventTableAdd()));

            //EVENTLISTENER
            TableTrain.SelectionChanged += eventTableSelectionChange;
            TableTrain.MouseClick += eventTableSelectionChange;

            return panelDraw;
        }

        private UniPanel createPanelDisplay()
        {
            //CREATE DISPLAY PANEL
            UniPanel panelDisplay = UniPanel.Create(this, startposition[0], Mod_Forms.nextTop(FormProperties, gap[1]), FormProperties.Width, ctlHeight[2], BorderStyle.FixedSingle);

            //INPUT|OUTPUT PANEL
            IO = new NetIO(panelDisplay, startposition[0], startposition[1], Mod_Forms.fitWidth(panelDisplay, gap[0]), Mod_Forms.fitHeight(panelDisplay, startposition[1], gap[0]), BorderStyle.FixedSingle, Colors.MainLight, true);

            return panelDisplay;
        }

        private UniPanel createPanelLive()
        {
            //CREATE LIVE PANEL
            UniPanel panelLive = UniPanel.Create(this, Mod_Forms.sameLeft(FormTrain), Mod_Forms.sameTop(FormDraw), FormTrain.Width, ctlHeight[2], BorderStyle.FixedSingle);

            //CREATE SHOW PANEL
            UniPanel showPanel = UniPanel.Create(panelLive, startposition[0], startposition[1], ctlWidth[2], ctlWidth[2], BorderStyle.FixedSingle, Colors.MainDark);
            PanelShow = UniPanel.Create(showPanel, 0, 0, showPanel.Width, showPanel.Height, BorderStyle.None, Colors.getColor(COLOR.WHITE));

            //CREATE BINARY PANEL
            UniPanel binaryPanel = UniPanel.Create(panelLive, Mod_Forms.nextLeft(showPanel, gap[1]), showPanel.Top, showPanel.Width, showPanel.Height, BorderStyle.FixedSingle, Colors.MainDark);
            PanelBinary = UniPanel.Create(binaryPanel, 0, 0, binaryPanel.Width, binaryPanel.Height, BorderStyle.None, Colors.getColor(COLOR.WHITE));

            //CREATE SCORE LABEL
            LabelScore = UniLabel.CreateFeedback(panelLive, "0", showPanel.Left, gap[0] / 2);

            //CREATE POSITION BOXES
            string startX = Mod_Convert.IntegerToString(Screen.PrimaryScreen.WorkingArea.Width / 2 - PixelWidth.getInteger() / 2);
            string startY = Mod_Convert.IntegerToString(Screen.PrimaryScreen.WorkingArea.Height / 2 - PixelHeight.getInteger() / 2);
            PosiX = UniText.Create(panelLive, startX, "x-position of cam", showPanel.Right - gap[2], gap[0] / 2, gap[1], 0, Fonts.getFont(7), 0, true);
            PosiY = UniText.Create(panelLive, startY, "y-position of cam", showPanel.Right - gap[1], Mod_Forms.sameTop(PosiX), gap[1], 0, Fonts.getFont(7), 0, true);

            //CREATE CAM FRAME RATE LABEL
            UniLabel labelFps = UniLabel.Create(panelLive, "FPS:", Mod_Forms.sameLeft(binaryPanel), gap[0] / 2, 0, 0, Mod_Convert.FontSize(Fonts.MainFont, 10));
            LabelFps = UniLabel.Create(panelLive, string.Empty, Mod_Forms.nextLeft(labelFps), Mod_Forms.sameTop(labelFps), 0, 0, labelFps.Font);

            //CREATE CAM AND LEARNING TOGGLE
            ToggleCam = UniToggle.Create(panelLive, string.Empty, "open camera window", Mod_Forms.sameLeft(showPanel), Mod_Forms.nextTop(PanelDraw, gap[0]), ctlWidth[0], gap[0], false);
            ToggleCam.Image = Mod_PNG.getImage(ICON.CAMERA_WALL);

            //CREATE FPS SPIN
            SpinFps = UniSpin.Create(panelLive, "Fps:", "set frames per second of camera", Mod_Forms.nextLeft(ToggleCam, gap[0]), Mod_Forms.sameTop(ToggleCam), 1, 40, 1, 25);
            SpinDelay = UniSpin.Create(panelLive, "Delay:", "set frame delay between camera and the net-view", SpinFps.Left, Mod_Forms.nextTop(SpinFps, gap[0]), 0, 40, 1, 0);

            //CREATE OPACITY TRACKBAR
            TrackOpacity = UniTrack.Create(panelLive, "Opacity:", "opacity of camera", Mod_Forms.nextLeft(SpinFps, gap[0]), Mod_Forms.nextTop(ToggleCam), gap[4], gap[0], 1, 100, 1, 50, TickStyle.BottomRight, Orientation.Horizontal, 1, " x");
            TrackOpacity.setScale(0.01);

            //CREATE ZOOM TRACKBAR
            TrackZoom = UniTrack.Create(panelLive, "Zoom:", "zoom of camera", Mod_Forms.nextLeft(TrackOpacity, gap[0]), Mod_Forms.sameTop(TrackOpacity), gap[4], gap[0], 1, 50, 1, 10, TickStyle.BottomRight, Orientation.Horizontal, 1, " x");
            TrackZoom.setScale(0.1);

            //CREATE TOGGLE BUTTONS
            ToggleQuery = UniToggle.Create(panelLive, "Query", "send query to network for current picture", Mod_Forms.nextLeft(TrackZoom, gap[0]), Mod_Forms.sameTop(TrackOpacity), ctlWidth[0], gap[0]);
            ToggleTeach = UniToggle.Create(panelLive, "Teach", "teach network by current picture", Mod_Forms.nextLeft(ToggleQuery, gap[0]), Mod_Forms.sameTop(TrackOpacity), ctlWidth[0], gap[0]);
            ToggleSend = UniToggle.Create(panelLive, "Send", "allow network to send keys", Mod_Forms.nextLeft(ToggleTeach, gap[0]), Mod_Forms.sameTop(TrackOpacity), ctlWidth[0], gap[0]);

            //CREATE TABLE
            TableLive = new NetTable(panelLive, Mod_Forms.nextLeft(binaryPanel, gap[1]), binaryPanel.Top, ToggleSend.Right - Mod_Forms.nextLeft(binaryPanel, gap[1]), binaryPanel.Height);

            //CLICK ACTION
            ToggleCam.CheckedChanged += (s, e) => { Cam.Visible = ToggleCam.Checked; };

            //EVENT LISTENER
            ToggleSend.CheckedChanged += (s, e) => { if (!ToggleSend.Checked && NetCam.LastCell != null) NetCam.LastCell.Style.BackColor = Color.White; else FormLive.Focus(); };

            return panelLive;
        }

        private TextBox createConsole()
        {
            //CREATE CONSOLE
            TextBox console = new TextBox();
            console.Text = "***SharpAI by MK***";
            console.BackColor = Color.White;
            console.ReadOnly = true;
            console.WordWrap = true;
            console.Multiline = true;
            console.ScrollBars = ScrollBars.Vertical;
            console.Height = 120;
            console.Dock = DockStyle.Bottom;
            Mod_Forms.setToolTip(ConsoleBox, "display important informations, which you can copy by pushing 'ctrl + c'");
            this.Controls.Add(console);
            return console;
        }

        private void PanelUpdate()
        {
            //UPDATE PANEL
            UniPanel[] panel = new UniPanel[] { PanelRedraw, PanelNeural, PanelShow, PanelBinary };

            foreach (UniPanel item in panel)
            {
                //RESIZE
                item.Size = Net.SizeScale;

                //CENTER PANEL
                item.Top = item.Parent.Height / 2 - item.Height / 2;
                item.Left = item.Parent.Width / 2 - item.Width / 2;
            }
        }

        private double[] DataGridViewCellToIntegerArray(DataGridViewCell xCell, string xSplit = MainSplit)
        {
            //CONVERT A DATAGRIDVIEW CELL TO INTEGER ARRAY
            string value = Mod_Convert.ObjectToString(xCell.Value);
            return Mod_Convert.StringArrayToDoubleArray(value.Split(new string[] { xSplit }, StringSplitOptions.None));
        }

        private void setPanelEnable(bool xEnable)
        {
            //ENABLE TRAIN PANEL
            foreach (Control ctl in FormTrain.Controls)
            { if (ctl.Tag == null) ctl.Enabled = xEnable; }

            //ENABLE LEARN PANEL
            foreach (Control ctl in FormLearn.Controls)
                ctl.Enabled = xEnable;

            //ENABLE DRAW PANEL
            foreach (Control ctl in FormDraw.Controls)
                ctl.Enabled = xEnable;

            //ENABLE LIVE PANEL
            foreach (Control ctl in FormLive.Controls)
                ctl.Enabled = xEnable;
        }

        private void setLabelNetwork(bool xOnline = true)
        {
            //SET OPTION LABELS
            if (Net.Exist() && xOnline)
            {
                switch (ComboType.SelectedIndex)
                {
                    case 0: //PERZEPTRON
                        LabelOption[0].Text = "Neural Network.........online";
                        LabelOption[1].Text = "Network Size............." + Net.PixelWidth + "x" + Net.PixelHeight;
                        LabelOption[2].Text = "Input Nodes..............." + Net.NodesInput;
                        LabelOption[3].Text = "Hidden Nodes............" + Net.NodesHidden;
                        LabelOption[4].Text = "Output Nodes............." + Net.NodesOutput;
                        LabelOption[5].Text = "Learning Rate............" + Net.LearningRate;
                        LabelOption[6].Text = "Mode.........................." + Mod_Convert.EnumToString(Net.Mode);
                        break;
                    case 1: //NEAT
                        LabelOption[0].Text = "Neural Network.........online";
                        LabelOption[1].Text = "Cam Size..................." + Net.PixelWidth + "x" + Net.PixelHeight;
                        LabelOption[2].Text = "Population................." + NeatPopulation.getInteger();
                        LabelOption[3].Text = "Staleness.................." + NeatStalness.getInteger();
                        LabelOption[4].Text = "Max Nodes................." + MaxNodes;
                        LabelOption[5].Text = "Mode.........................." + Mod_Convert.EnumToString(Net.Mode);
                        break;
                }
            }
            else
            {
                LabelOption[0].Text = "Neural Network.........offline";
                for (int i = 1; i < LabelOption.Length; i++)
                    LabelOption[i].Text = string.Empty;
            }
        }

        public static string[] getFileContent()
        {
            //GET FILE CONTENT
            string[] path = Mod_File.FileOpenDialog(FILTER.CSV);

            //ABBRUCH 
            if (Mod_Check.isEmpty(path)) { return null; }

            //READ CSV
            return Mod_TXT.readTXT(path[0]);
        }

        public MODE getMode()
        {
            //GET MODE
            object[] ModeList = Mod_Convert.EnumToList(typeof(MODE));
            return (MODE)ModeList[GroupMode.getIndex()];
        }

        public string[] getTableContent(UniTable xTable)
        {
            //GET TABLE CONTENT
            string[] content = xTable.ToStringArray(MainSplit);
            return content;
        }

        private List<string> getAnswers()
        {
            //GET COLUMN ANSWERS
            ColumnList = new List<string>(Mod_Convert.ObjectArrayToStringArray(TableTrain.getColumnValues(0)));

            //GET ALL DIFFERENT ITEMS
            List<string> itemList = new List<string>();
            foreach (string item in ColumnList) { if (!itemList.Contains(item)) itemList.Add(item); }

            //REFRESH FILTER COMBOBOX
            if (itemList.Count != LastCount)
                eventFilterDropDown(itemList.ToArray());

            LastCount = itemList.Count;

            return itemList;
        }

        private void eventComboTypeChange(object sender, EventArgs e)
        {
            //COMBO TYPE CHANGE EVENT
            switch (ComboType.SelectedIndex)
            {
                case 0: //PERZEPTRON
                    //FORMS
                    FormTrain.Visible = true;
                    FormDraw.Visible = true;
                    FormLearn.Visible = false;
                    FormDisplay.Visible = false;

                    //CONTROLS
                    NodesHidden.Visible = true;
                    NodesOutput.Visible = true;
                    NeatPopulation.Visible = false;
                    NeatStalness.Visible = false;
                    LearningRate.Visible = true;
                    break;

                case 1: //NEAT
                    //FORMS
                    FormTrain.Visible = false;
                    FormDraw.Visible = false;
                    FormLearn.Visible = true;
                    FormDisplay.Visible = true;

                    //CONTROLS
                    NodesHidden.Visible = false;
                    NodesOutput.Visible = false;
                    NeatPopulation.Visible = true;
                    NeatStalness.Visible = true;
                    LearningRate.Visible = false;
                    break;
            }
        }

        private void eventTableSelectionChange(object sender, EventArgs e)
        {
            //TABLE TRAIN SELECTION CHANGE EVENT
            if (TableTrain.Focused && TableTrain.SelectedRows.Count > 0)
                NetDraw.drawPlot(PanelRedraw, TableTrain.Rows[TableTrain.SelectedRows[0].Index].Cells[0].Value, DataGridViewCellToIntegerArray(TableTrain.Rows[TableTrain.SelectedRows[0].Index].Cells[1]));
        }

        private void eventTableStat()
        {
            //TABLE STATISTICS EVENT
            LabelStat.Text = Mod_Convert.IntegerToString(TableTrain.RowCount);

            //GET ALL DIFFERENT ITEMS
            List<string> itemList = getAnswers();

            //GENERATE TOOLTIP
            itemList.Sort();
            object[] count = new object[itemList.Count];

            for (int i = 0; i < itemList.Count; i++)
                count[i] = ColumnList.Where(x => x == itemList[i]).Count();

            //SET STATISTICS
            LabelStat.setDataPie(itemList.ToArray(), count);
        }

        private void eventFilterChange()
        {
            //FILTER CHANGE EVENT
            ComboBox.ObjectCollection source = TableFilter.Items;

            List<object> filter = new List<object>();
            foreach (var item in source)
                if (((CheckComboBoxItem)item).CheckState)
                    filter.Add(((CheckComboBoxItem)item).Text);

            //SET FILTER
            TableTrain.setFilter(0, filter.ToArray());
        }

        private void eventFilterDropDown(string[] xAnswers)
        {
            //FILTER DROP DOWN EVENT
            TableFilter.setItems(xAnswers, true);
        }

        private void eventTableAdd()
        {
            //GET START ITEM
            string startItem = string.Empty;
            if (AnswerList.Any()) startItem = AnswerList.Last();

            //TALBE ADD EVENT
            Panel panel = new Panel();
            UniText answerBox = UniText.Create(panel, startItem, "insert answer", startposition[0], startposition[1], 60);
            AnswerList.Reverse();
            UniCombo comboBox = UniCombo.Create(panel, "given answers", AnswerList.ToArray(), "select given answer", Mod_Forms.nextLeft(answerBox, gap[1]), Mod_Forms.sameTop(answerBox), 100, gap[0], 0);
            AnswerList.Reverse();
            answerBox.Focus();

            //EVENTLISTENER
            answerBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { foreach (Control container in panel.Parent.Controls) if (container is Panel) { foreach (Control ctl in container.Controls) { if (ctl.Text == "OK") { ((Button)ctl).PerformClick(); } } } } };
            comboBox.SelectedIndexChanged += (s, e) => { if (comboBox.Focused) { answerBox.Text = comboBox.Text; answerBox.Focus(); } };

            //SHOW MSGBOX
            UniMsg.Show("insert answer...", panel, new Action(() => { eventDrawConvert(PanelDraw.getBitmap(), answerBox.Text); }), MessageBoxButtons.OKCancel, MSGICON.QUESTION, LeftRightAlignment.Left);
        }

        public void eventDrawConvert(Bitmap xBitmap, string xAnswer)
        {
            //ABBRUCH
            if (xBitmap == null)
                return;

            //CONVERT DRAW
            double[] dblArray = null;
            string[] strArray = null;
            switch (Net.Mode)
            {
                case MODE.PIXEL:
                    dblArray = NetDraw.BitmapToDoubleArray(xBitmap);
                    strArray = Mod_Convert.DoubleArrayToStringArray(dblArray, 0);
                    break;
                case MODE.HSENSOR:
                    dblArray = Cam.LastSensor;
                    strArray = Mod_Convert.DoubleArrayToStringArray(dblArray, 2);
                    break;
            }

            //ABBRUCH
            if (strArray == null)
                return;

            //ADD TO TABLE                   
            string[] insert = new string[] { xAnswer, string.Join(MainSplit, strArray) };
            setTable(TableTrain, new string[][] { insert });
            AnswerList.Add(xAnswer);
            PanelDraw.Clear();
            eventTableStat();
        }

        private void eventTrainingOpen()
        {
            //OPEN TRAINING EVENT                   
            string[] content = getFileContent();

            //ABBRUCH
            if (content == null) { return; }

            //START LOADING
            UniLoad.loadingStart();

            //FILL TABLE
            List<string[]> dataList = new List<string[]>();
            foreach (string line in content)
            {
                string[] temp = line.Split(',');
                string[] row = new string[] { temp[0], string.Join(MainSplit, temp.Skip(1)) };
                dataList.Add(row);
            }

            //SET TABLE
            setTable(TableTrain, dataList.ToArray(), true);
            eventTableStat();

            //END LOADING
            UniLoad.loadingEnd();
        }

        private void eventTrainingSave()
        {
            //SAVE TRAINING EVENT
            string path = Mod_File.FileSaveDialog("knowledge_base_" + PixelWidth.getInteger() + "x" + PixelHeight.getInteger(), FILTER.CSV);

            //ABBRUCH
            if (Mod_Check.isEmpty(path)) { return; };

            //CONVERT TO CSV
            string[] content = getTableContent(TableTrain);
            Mod_File.CreateFile(content, path);
            setConsoleInvoke("Training save to: " + path);
        }

        private void eventTrainingShuffle()
        {
            //CONVERT TO CSV
            string[] content = getTableContent(TableTrain);
            content = Mod_Convert.ArrayShuffle(content);

            List<string> listAnswer = new List<string>();
            List<string> listValues = new List<string>();

            //SPLIT CONTENT IN ANSWER AND VALUES
            foreach (string item in content)
            {
                string[] split = item.Split(new string[] { MainSplit }, StringSplitOptions.None);
                listAnswer.Add(split[0]);
                listValues.Add(string.Join(MainSplit, split, 1, split.Length - 1));
            }

            //SET COLUMN VALUES
            TableTrain.setColumnValues(0, listAnswer.ToArray());
            TableTrain.setColumnValues(1, listValues.ToArray());
        }

        private void eventTrainingStart(UniTable xTable)
        {
            //START TRAINING EVENT
            Content = getTableContent(xTable);

            //ABBRUCH
            if (Content.Length == 0) { setConsoleInvoke("Start training: no items in table"); return; }

            //CONSOLE
            setConsoleInvoke("Start training with " + Content.Length + " items", true);
        }

        public void eventWorkerStart(int xEpochs)
        {
            //WORKER DO WORK EVENT            
            neuralNetworkTrain(xEpochs);
        }

        private void eventWorkerCompleted(UniButton xBtnTrain)
        {
            //WORKER PROGRESS EVENT
            xBtnTrain.Enabled = true;
            TimeSpan duration = Worker.Close();
            setConsoleInvoke("...finished! (" + duration.ToString(@"mm\:ss") + ")");
        }

        public void setConsoleInvoke(object xMsg, bool xNextLine = true, bool xTab = false)
        {
            //INVOKE CONSOLE
            ConsoleBox.Invoke(ConsoleCall, xMsg, xNextLine, xTab);
        }

        public static void setConsole(object xMsg, bool xNextLine = true, bool xTab = false)
        {
            char N = '\n';

            //ABBRUCH
            if (ConsoleBox == null)
                return;

            //TEMPORARY LAST MESSAGE
            ConsoleBox.Tag = xMsg;

            //LOOKING FOR LINE BREAK
            if (((string)xMsg).Contains(N.ToString()))
            {
                string[] split = ((string)xMsg).Split(N);
                foreach (string item in split)
                    if (xTab) ConsoleBox.AppendText(Environment.NewLine + "\t" + item);
                    else ConsoleBox.AppendText(Environment.NewLine + item);
                return;
            }

            //TIME
            string time = Mod_Convert.DateTimeStamp("HH:mm - ");

            //SET CONSOLE
            if (xNextLine)
                ConsoleBox.AppendText(Environment.NewLine + time + xMsg);
            else
                ConsoleBox.AppendText(Mod_Convert.ObjectToString(xMsg));
        }

        private void setTable(UniTable xTable, object[][] xDataSource, bool xLoad = false)
        {
            //ABBRUCH
            if (Mod_Check.isEmpty(xDataSource))
            { return; }

            //RESET TABEL
            if (xLoad)
                xTable.Reset();

            //SET TABLE HEADER
            string[] header = new string[] { "answer", "values" };

            //CREATE DATA SET
            if (xTable.Rows.Count == 0)
            {
                xTable.setDataTable(xDataSource, header);
                xTable.Format(new int[] { 60 }, DataGridViewContentAlignment.MiddleCenter, DataGridViewContentAlignment.MiddleCenter, true);
            }
            else { xTable.addDataTableRow(xDataSource[0]); };
        }

        private void neuralNetworkNew()
        {
            //NEW NEURAL NETWORK
            EnableAction(false);
            neuralNetworkInit(PixelWidth.getInteger(), PixelHeight.getInteger(), NodesHidden.getInteger(), NodesOutput.getInteger(), LearningRate.getValue() / 100, getMode());
        }

        public void neuralNetworkInit(int xPixelWidth, int xPixelHeight, int xHiddenNodes, int xOutputNodes, double xLearningRate, MODE xMode, bool xFeedback = true)
        {
            //INITIALIZE NEURAL NETWORK
            Net = new NetNeural(xPixelWidth, xPixelHeight, xHiddenNodes, xOutputNodes, xLearningRate, xMode);

            //FEEDBACK
            switch (ComboType.SelectedIndex)
            {
                case 0: //PERZEPTRON
                    setConsoleInvoke("New network type 'Perzeptron' initialised (" + Net.NodesInput + ", " + Net.NodesHidden + ", " + Net.NodesOutput + ", " + Net.LearningRate + ")");
                    break;
                case 1: //NEAT
                    setConsoleInvoke("New network type 'NEAT' initialised (" + Net.PixelWidth + "x" + Net.PixelHeight + ", " + NeatPopulation.getInteger() + ", " + NeatStalness.getInteger() + ")");
                    break;
            }

            //ABBRUCH
            if (!xFeedback)
                return;

            //SET OPTION LABELTEXTS
            setLabelNetwork();

            //ENABLE PANEL TRAIN
            setPanelEnable(true);

            //PANEL UPDATE
            PanelUpdate();
        }

        private void neuralNetworkLoad()
        {
            //LOAD NETWORK
            object[] network = (object[])Mod_XML.Deserialize(typeof(object[]));

            //ABBRUCH
            if (network == null) { return; }

            //EXECUTE ENABLE ACTION
            EnableAction(false);

            object[] properties = (object[])network[0];
            object[] settings = (object[])network[1];
            object[][] tableTrain = Mod_Convert.StringSplitArrayToObjectArray((object[])network[2]);
            object[] tableLive = (object[])network[3];
            object[] inputs = (object[])network[4];
            object[] outputs = (object[])network[5];

            //LOAD PROPERTIES
            PixelWidth.Value = (int)(properties[0]);
            PixelHeight.Value = (int)(properties[1]);
            NodesHidden.Value = (int)properties[2];
            NodesOutput.Value = (int)properties[3];
            NeatPopulation.Value = (int)properties[4];
            NeatStalness.Value = (int)properties[5];
            LearningRate.Value = (int)(Mod_Convert.ObjectToDouble(properties[6]) * 1000);
            GroupMode.setIndex((int)properties[7]);
            ComboType.setSelection(Mod_Convert.ObjectToString(properties[8]));

            //INITIALIZE NETWORK
            neuralNetworkInit((int)properties[0], (int)properties[1], (int)properties[2], (int)properties[3], (double)properties[6], NetNeural.getMode((int)properties[7]));

            //LOAD CAM
            if (Cam != null) Cam.UpdateSize();

            //LOAD SETTINGS
            PosiX.Text = Mod_Convert.ObjectToString(settings[0]);
            PosiY.Text = Mod_Convert.ObjectToString(settings[1]);
            SpinEpochs.Value = (int)settings[2];
            SpinFps.Value = (int)settings[3];
            TrackOpacity.Value = (int)settings[4];
            TrackZoom.Value = (int)settings[5];

            //LOAD TABLES
            setTable(TableTrain, tableTrain, true);
            TableLive.Load(tableLive);
            eventTableStat();

            //LOAD INPUTWEIGHTS
            int w = Net.weightInput.GetLength(0), h = Net.weightInput.GetLength(1), index = 0;
            for (int j = 0; j < w; ++j)
                for (int i = 0; i < h; i++)
                    Net.weightInput[j, i] = Mod_Convert.ObjectToDouble(inputs[index++]);

            //LOAD OUTPUTWEIGHTS
            w = Net.weightOutput.GetLength(0); h = Net.weightOutput.GetLength(1); index = 0;
            for (int j = 0; j < w; ++j)
                for (int i = 0; i < h; i++)
                    Net.weightOutput[j, i] = Mod_Convert.ObjectToDouble(outputs[index++]);

            //SET OPTION LABELTEXTS
            setLabelNetwork();

            //ENABLE PANEL TRAIN
            setPanelEnable(true);
        }

        private void neuralNetworkSave()
        {
            //ABBRUCH
            if (Net == null)
                return;

            //PROPERTIES
            object[] properties = new object[] { Net.PixelWidth, Net.PixelHeight, Net.NodesHidden, Net.NodesOutput, NeatPopulation.getInteger(), NeatStalness.getInteger(), Net.LearningRate, (int)Net.Mode, ComboType.Text };

            //LIVE SETTINGS
            object[] settings = new object[] { PosiX.ToInteger(), PosiY.ToInteger(), SpinEpochs.getInteger(), SpinFps.getInteger(), TrackOpacity.Value, TrackZoom.Value };

            //TABLES
            object[] tableTrain = Mod_Convert.StringArrayToObjectArray(TableTrain.ToStringArray());
            object[] tableLive = TableLive.Save();

            //SAVE INPUTWEIGHTS
            int w = Net.weightInput.GetLength(0), h = Net.weightInput.GetLength(1);
            List<object> inputs = new List<object>();
            for (int j = 0; j < w; ++j)
                for (int i = 0; i < h; i++)
                    inputs.Add(Net.weightInput[j, i]);

            //SAVE OUTPUTWEIGHTS
            List<object> outputs = new List<object>();
            w = Net.weightOutput.GetLength(0); h = Net.weightOutput.GetLength(1);
            for (int j = 0; j < w; ++j)
                for (int i = 0; i < h; i++)
                    outputs.Add(Net.weightOutput[j, i]);

            //SELECT NAME
            string name = ComboType.Text + " Network Properties [" + Net.NodesInput + "-" + Net.NodesHidden + "-" + Net.NodesOutput + "]";
            if (ComboType.SelectedIndex == 1) name = ComboType.Text + " Network Properties [" + Net.PixelWidth + "x" + Net.PixelHeight + "-" + NeatPopulation.getInteger() + "-" + NeatStalness.getInteger() + "]";

            //SAVE TRAINING
            object[] network = new object[] { properties, settings, tableTrain, tableLive, inputs.ToArray(), outputs.ToArray() };
            Mod_XML.Serialize(name, network, typeof(object[]));

            //FEEDBACK
            setConsoleInvoke("Neural network saved in " + Mod_XML.LastPath);
        }

        private void neuralNetworkTrain(int xEpochs = 10)
        {
            //ABBRUCH
            if (Content == null || Net == null)
                return;

            //TRAIN NEURAL NETWORK           
            double gesamt = Content.Length * xEpochs;
            double next = gesamt / 100;
            double line = 0;

            //GET TRIGGER KEYS
            List<object> triggerList = new List<object>(TableLive.getColumnValues((int)TYP.TRIGGER));

            //LOOP EPOCHS
            for (int epochs = 0; epochs < xEpochs; epochs++)
            {
                foreach (string item in Content)
                {
                    string[] split = item.Split(new string[] { MainSplit }, StringSplitOptions.None);
                    double[] dbl = Mod_Convert.StringArrayToDoubleArray(split);

                    //CHECK TRIGGER KEY
                    if (triggerList.Contains(split[0]))
                        dbl[0] = triggerList.IndexOf(split[0]); //OVERWRITE ANSWER

                    double[] inputArray = dbl.Skip(1).ToArray();
                    int targetIndex = (int)dbl[0];

                    //CONVERT TO MATRIX
                    double[,] input = convertDoubleToMatrix(inputArray, 1, inputArray.Length);
                    double[,] target = convertDoubleToMatrix(Mod_Convert.DoubleMultiplier(0.01, Net.NodesOutput), 1, Net.NodesOutput);
                    target[0, targetIndex] = 0.99;
                    Net.Train(input, target);
                    line++;

                    if (line >= next)
                    { Worker.setProgress((int)(next / gesamt * 100)); next += gesamt / 100; }
                }
                ConsoleBox.Invoke(ConsoleCall, "Epoch complete (" + (epochs + 1) + "/" + xEpochs + ")", true, false);
            }
        }

        public double neuralNetworkTest(string[] xContent = null)
        {
            //TEST NEURAL NETWORK
            if (xContent == null)
            {
                string[] path = Mod_File.FileOpenDialog(FILTER.CSV);

                //ABBRUCH 
                if (Mod_Check.isEmpty(path))
                    return 0.0;

                //READ CSV
                xContent = Mod_TXT.readTXT(path[0]);
            }

            //START LOADING
            setConsoleInvoke("Start testing performance of network. (" + xContent.Length + ")");
            DateTime start = DateTime.Now;
            UniLoad.loadingStart();

            //GET TRIGGER KEYS
            List<object> triggerList = new List<object>(TableLive.getColumnValues((int)TYP.TRIGGER));

            double correct = 0, incorrect = 0;

            foreach (string item in xContent)
            {
                string[] split = item.Split(new string[] { MainSplit }, StringSplitOptions.None);
                double[] dbl = Mod_Convert.StringArrayToDoubleArray(split);

                //CHECK TRIGGER KEY
                if (triggerList.Contains(split[0]))
                    dbl[0] = triggerList.IndexOf(split[0]); //OVERWRITE ANSWER

                int answer = (int)dbl[0];
                int net_answer = neuralNetworkQuery(dbl.Skip(1).ToArray());

                if (answer == net_answer) correct++;                //ANSWER CORRECT
                else if (net_answer == int.MinValue) return 0.0;    //ERROR
                else incorrect++;                                   //ANSWER INCORRECT
            }

            //CONSOLE FEEDBACK
            TimeSpan span = start - DateTime.Now;
            setConsoleInvoke("\tTest complete! (" + span.ToString(@"mm\:ss") + ")", false);
            double performance = correct / (correct + incorrect);
            string feedback =
                "Tested images: " + (correct + incorrect) +
                "\t\tcorrect answers: " + correct +
                "\t\tincorrect answers: " + incorrect +
                "\t\tperformance: " + Mod_Convert.DoubleFormat((100 * performance), 2) + " %";

            setConsoleInvoke(feedback, true, true);

            //END LOADING
            UniLoad.loadingEnd();

            return performance;
        }

        public int neuralNetworkQuery(double[] xUnknown, double xSecurity = 0.0, bool xFeedback = false)
        {
            //QUERY NEURAL NETWORK
            double[,] query = convertDoubleToMatrix(xUnknown, 1, xUnknown.Length);
            double[] answer = Net.Query(query);
            int index = Net.Answer;

            //CONSOLE FEEDBACK
            if (xFeedback && answer != null)
            {
                List<double> dblList = new List<double>(answer);
                dblList.Sort();
                dblList.Reverse();
                double[] sorted = dblList.ToArray();
                string first = "first:\t" + index + "\t(" + Mod_Convert.DoubleToPercent(sorted[0]) + ")";
                string secound = "\tsecond:\t" + Array.IndexOf(answer, sorted[1]) + "\t(" + Mod_Convert.DoubleToPercent(sorted[1]) + ")";
                string third = "\tthird:\t" + Array.IndexOf(answer, sorted[2]) + "\t(" + Mod_Convert.DoubleToPercent(sorted[2]) + ")";
                setConsoleInvoke("Neural network query:\t" + first + secound + third);
            }
            else if (answer == null || answer.Max() < xSecurity) //ABBRUCH
                return int.MinValue;

            return index;
        }

        private void neuralNetworkOffline()
        {
            //OFFLINE NEURAL NETWORK
            if (UniMsg.Show("set network offline...", "After confimation you have to initialise or load a new network.\nData could get lost!", MessageBoxButtons.OKCancel, MSGICON.EXCLAMATION))
            {
                EnableAction(true);
                setPanelEnable(false);
                setLabelNetwork(false);
                if (ToggleLearn.Checked) ToggleLearn.Checked = false;
                if (ToggleCam.Checked) ToggleCam.Checked = false;
                if (ToggleQuery.Checked) ToggleQuery.Checked = false;
                if (ToggleTeach.Checked) ToggleTeach.Checked = false;
                if (ToggleSend.Checked) ToggleSend.Checked = false;
            }
        }

        public static double[,] convertDoubleToMatrix(double[] xDoubleArray, int xRows, int xColumns)
        {
            //CONVERT A DOUBLE[] ARRAY TO MATRIX
            if (xDoubleArray == null || xDoubleArray.Length != xRows * xColumns)
                return null;

            double[,] matrix = new double[xRows, xColumns];
            //BLOCKCOPY USES BYTE LENGTHS: A DOUBLE IS 8 BYTES
            Buffer.BlockCopy(xDoubleArray, 0, matrix, 0, xDoubleArray.Length * sizeof(double));
            return matrix;
        }
    }
}
