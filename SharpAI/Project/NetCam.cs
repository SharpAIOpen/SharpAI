using Core.Classes;
using Core.Enums;
using Core.Modifications;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;


/*############################################################################*
 *                            Desktop Camera                                  *
 *        Camera to capture desktop screen and convert to network input       *
 *                    writen in c# by Martin Kober 2017                       *
 *############################################################################*/


namespace NeuralNet.Project
{
    public class NetCam : UniForm
    {
        //CONTORLS
        NetMain NetMain;

        //GRAPHICS
        int CamLeft, CamTop, CamWidth, CamHeight;

        //VARIABLES
        Pen Pen = new Pen(Color.Red, 0.5f);
        UniStack<Bitmap> ListFrame = new UniStack<Bitmap>(0);
        Point Position;
        bool Busy;
        public int Fps;
        int FpsCounter = 0;
        int FrameCounter = 0;
        int FrameFreeze = 5;
        bool Black;
        public bool Alive, Reset;
        public int Score = 0, ScoreHigh = 0, ScoreFactor = 4;
        public static Bitmap LastFrameCam;
        public static Bitmap LastFrameNet;
        public static DataGridViewCell LastCell;
        public double[] LastSensor;

        //FRAME RATE
        int LastTick = 0, LastFrameRate = 0, FrameRate = 0;

        public NetCam(NetMain xNetMain) : base(ICON.CAMERA, "Live", new Size(20, 20), new Size(0, 0), FormBorderStyle.None, Colors.MainDominant, false, true, FORMTYPE.NORMAL)
        {
            //CREATE NETCAM OBJECT                    
            NetMain = xNetMain;

            //SET OPACITY
            Opacity = NetMain.TrackOpacity.getValue();

            //SET VISIBLE
            Visible = false;

            //TIMER
            Timer timer = new Timer();
            timer.Interval = 1000 / (int)NetMain.SpinFps.Value;
            timer.Tick += eventTick;
            timer.Start();

            //FPS
            Fps = NetMain.SpinFps.getInteger();
            ScoreFactor = Fps;

            //EVENT LISTENER
            VisibleChanged += (s, e) => { if (Visible) { setLocation(); UpdatePosition(); UpdateSize(); } };
            NetMain.PosiX.TextChanged += eventPosiChange;
            NetMain.PosiY.TextChanged += eventPosiChange;
            MouseDown += eventMouseDown;
            MouseMove += eventMouseMove;
            KeyDown += eventKeyDown;
            Paint += eventPaint;

            //GLOBAL EVENT LISTENER
            UniGlobal.GlobalEvents.KeyDown += eventGlobalKeyDown;
            UniGlobal.GlobalEvents.KeyUp += eventGlobalKeyUp;

            //EVENT SETTINGS
            NetMain.TrackOpacity.ValueChanged += (s, e) => setOpacity();
            NetMain.TrackZoom.ValueChanged += (s, e) => getPosition();
            NetMain.SpinFps.ValueChanged += (s, e) => { Fps = NetMain.SpinFps.getInteger(); timer.Interval = 1000 / Fps; };
            NetMain.SpinDelay.ValueChanged += (s, e) => { ListFrame.setLimit(NetMain.SpinDelay.getInteger()); };

            //INITIALIZE CAM
            getPosition();
        }

        public void UpdateSize()
        {
            //UPDATE SIZE
            Size = NetMain.Net.SizeNet;
        }

        private void UpdatePosition()
        {
            //UPDATE POSITION
            NetMain.PosiX.Text = Mod_Convert.IntegerToString(Location.X + Width / 2);
            NetMain.PosiY.Text = Mod_Convert.IntegerToString(Location.Y + Height / 2);
            getPosition();
        }

        private bool isAlive(Color xColor, int x, int y)
        {
            //CHECK IS ALIVE
            if (x == 0 && y == 0) { Black = false; FrameCounter++; }
            else if (!Black && !Mod_Check.isGray(xColor, 150))
                Black = true;

            if (Black)
            {
                Color after = xColor; if (xColor.R != 255 && xColor.G != 255 && xColor.B != 255) after = Color.FromArgb(xColor.A, xColor.R + 1, xColor.G + 1, xColor.B + 1);
                Color before = xColor; if (xColor.R != 0 && xColor.G != 0 && xColor.B != 0) before = Color.FromArgb(xColor.A, xColor.R - 1, xColor.G - 1, xColor.B - 1);
                Color last = LastFrameCam.GetPixel(x, y);
                if (last != xColor && last != after && last != before) { FrameCounter = 0; return true; }
                else return false;
            }

            //FRAME FREEZE
            if (FrameCounter > FrameFreeze) return false;
            else return true;
        }

        private void getPosition()
        {
            //ABBRUCH
            if (NetMain.Net == null)
                return;

            //GET CAM
            double CamZoom = NetMain.TrackZoom.getValue();
            CamWidth = (int)(NetMain.Net.SizeNet.Width / CamZoom);
            CamHeight = (int)(NetMain.Net.SizeNet.Height / CamZoom);
            CamLeft = Left + (NetMain.Net.SizeNet.Width - CamWidth) / 2;
            CamTop = Top + (NetMain.Net.SizeNet.Height - CamHeight) / 2;
        }

        public int getFrameRate()
        {
            //GET FRAME RATE
            if (Environment.TickCount - LastTick >= 1000)
            {
                LastFrameRate = FrameRate;
                FrameRate = 0;
                LastTick = Environment.TickCount;
            }
            FrameRate++;
            return LastFrameRate;
        }

        private Color getCustomConversion(Color xColorGet, Color xColorSet)
        {
            //GET CUSTOM USER CONVERSION
            if (NetMain.TableLive.ColorEnemy.Contains(xColorGet)) return Color.Black;
            else if (NetMain.TableLive.ColorFriend.Contains(xColorGet)) return Color.White;
            else return xColorSet;
        }

        public double[] getDoubleArray()
        {
            //GET DOUBLE ARRAY
            double[] dblArray = null;
            switch (NetMain.Net.Mode)
            {
                case MODE.PIXEL: dblArray = NetDraw.BitmapToDoubleArray(LastFrameCam); break;
                case MODE.HSENSOR: dblArray = LastSensor; break;
            }
            return dblArray;
        }

        private void setOpacity()
        {
            //SET OPACITY
            Opacity = NetMain.TrackOpacity.getValue();
        }

        private void setLocation()
        {
            //SET LOCATION
            Location = new Point(NetMain.PosiX.ToInteger() - Width / 2, NetMain.PosiY.ToInteger() - Height / 2);
        }

        private void setHighscore()
        {
            //SET HIGHSCORE
            if (ScoreHigh < Score) ScoreHigh = Score;
            Score = 0;
        }

        private void eventTick(object sender, EventArgs e)
        {
            //TICK EVENT
            FpsCounter++;
            if (Reset && Alive) { Reset = false; setHighscore(); }                                                                                          //RESET AFTER DEAD
            if (FpsCounter >= Fps / ScoreFactor && Alive) { FpsCounter = 1; NetMain.LabelScore.Text = Mod_Convert.IntegerToString(Score); Score++; }        //COUNT SCORE WHILE ALIVE
            if (Alive) NetMain.LabelScore.setColorFeedback(Color.Green, null);                                                                              //ALIVE FEEDBACK
            else { NetMain.LabelScore.setColorFeedback(Color.Red, null); Reset = true; }                                                                    //DEAD FEEDBACK
            Refresh();
        }

        private void eventPosiChange(object sender, EventArgs e)
        {
            //ABBRUCH
            if (!(sender as Control).Focused)
                return;

            //POSI CHANGE EVENT
            Location = new Point((NetMain.PosiX.ToInteger()) - Width / 2, (NetMain.PosiY.ToInteger()) - Height / 2);
        }

        private void eventMouseDown(object sender, MouseEventArgs e)
        {
            //MOUSE DOWN EVENT
            Position = e.Location;
        }

        private void eventMouseMove(object sender, MouseEventArgs e)
        {
            //MOUSE MOVE EVENT
            if (e.Button == MouseButtons.Left)
            {
                Location = new Point((Location.X - Position.X) + e.X, (Location.Y - Position.Y) + e.Y);
                UpdatePosition();
            }
        }

        private void eventGlobalKeyDown(object sender, KeyEventArgs e)
        {
            //ABBRUCH
            if (!NetMain.ToggleTeach.Checked || Busy)
                return;

            //GLOBAL KEY DOWN EVENT
            string[] keys = NetMain.TableLive.getKeys();

            //CHECK TRIGGER KEY DOWN
            foreach (string key in keys)
                if (e.KeyData.ToString() == key)
                {
                    NetMain.eventDrawConvert(LastFrameCam, key);
                    break;
                }
            Busy = true;
        }

        private void eventGlobalKeyUp(object sender, KeyEventArgs e)
        {
            //GLOBAL KEY UP EVENT
            Busy = false;
        }

        private void eventKeyDown(object sender, KeyEventArgs e)
        {
            //KEY DOWN EVENT
            int increment = 1;
            if (e.Modifiers == Keys.Shift) increment = 10; //BOOST

            switch (e.KeyData)
            {
                case Keys.Up: //UP
                case Keys.Up | Keys.Shift:
                    Top = Top - increment;
                    break;
                case Keys.Down: //DOWN
                case Keys.Down | Keys.Shift:
                    Top = Top + increment;
                    break;
                case Keys.Left: //LEFT
                case Keys.Left | Keys.Shift:
                    Left = Left - increment;
                    break;
                case Keys.Right: //RIGHT
                case Keys.Right | Keys.Shift:
                    Left = Left + increment;
                    break;
            }

            //POSITION UPDATE
            UpdatePosition();
        }

        public void eventGlobalSendKeys(int xAnswer, bool xWait = false)
        {          
            //ABBRUCH
            if (NetMain.isActivated())
                return;

            //GLOBAL SEND KEYS EVENT
            string[] keys = NetMain.TableLive.getKeys();
            if (xWait) SendKeys.SendWait("({" + keys[xAnswer] + "})");
            else if (xAnswer == int.MinValue) { LastCell.Style.BackColor = Color.White; return; }
            else SendKeys.Send("({" + keys[xAnswer] + "})");

            //FEEDBACK
            if (LastCell != null) LastCell.Style.BackColor = Color.White;
            LastCell = NetMain.TableLive.Rows[xAnswer].Cells[(int)TYP.TRIGGER];
            LastCell.Style.BackColor = Colors.MainDominant;
        }

        public void eventResetGame()
        {
            //RESET GAME EVENT
            for (int i = 0; i < NetMain.TableLive.getKeys().Length; i++)
                eventGlobalSendKeys(i, false);
        }

        private void eventPaint(object sender, PaintEventArgs e)
        {
            //TAKE LIVE IMAGE
            Bitmap screen = new Bitmap(CamWidth, CamHeight, PixelFormat.Format32bppArgb);
            Opacity = 0.0;
            Graphics.FromImage(screen).CopyFromScreen(CamLeft, CamTop, 0, 0, screen.Size, CopyPixelOperation.SourceCopy);
            Opacity = NetMain.TrackOpacity.getValue();

            //GET FRAME RATE
            NetMain.LabelFps.Text = getFrameRate().ToString();

            //INITIALIZE LAST FRAME
            if (LastFrameCam == null) LastFrameCam = screen;

            //SET TO SHOW PANEL
            NetMain.PanelShow.BackgroundImage = new Bitmap((Bitmap)NetDraw.ScaleUp(screen));
            ListFrame.Push(new Bitmap(screen));

            //DELAY
            if (ListFrame.Limit > 0)
                screen = ListFrame.getLast();

            //INITIALIZE VARIABLES
            bool next = false;
            Color getColor, setColor = Color.White;

            //SCALE DOWN
            screen = (Bitmap)NetDraw.ScaleDown(screen);
            Bitmap bmp = new Bitmap(screen);

            //CONVERT GRAY SCALE
            for (int y = 0; y < bmp.Height; y++)
                for (int x = 0; x < bmp.Width; x++)
                {
                    getColor = bmp.GetPixel(x, y);

                    //CHECK IS ALIVE
                    if (isAlive(getColor, x, y))
                        next = true;

                    switch (NetMain.Net.Mode)
                    {
                        case MODE.PIXEL: setColor = Colors.getGray(getColor); break;            //GREY SCALE CONVERSION 
                        case MODE.HSENSOR: setColor = Colors.getBlackWhite(getColor); break;    //BLACK OR WHITE CONVERSION
                    }

                    //CUSTOM CONVERSION
                    setColor = getCustomConversion(getColor, setColor);

                    //SET PIXEL COLOR
                    bmp.SetPixel(x, y, setColor);
                }

            //AFTER CONVERSION
            switch (NetMain.Net.Mode)
            {
                case MODE.PIXEL: ModePixel(bmp); break;
                case MODE.HSENSOR: ModeHSensor(new Bitmap(bmp), e); break;
            }

            //SET LAST FRAME AND ALIVE
            LastFrameCam = screen;
            LastFrameNet = bmp;
            Alive = next;

            //NET QUERY
            if (NetMain.ToggleQuery.Checked) NetDraw.Draw(NetDraw.ScaleDown(bmp));

            //NET SEND KEYS
            if (NetMain.ToggleSend.Checked) eventGlobalSendKeys(NetMain.neuralNetworkQuery(getDoubleArray(), 0.5));

            //NET LEARNING
            else NetMain.Mario.LearningRun();
        }

        private void ModePixel(Bitmap xBmp)
        {
            //PIXEL MODE           
            NetMain.PanelBinary.BackgroundImage = NetDraw.ScaleUp(new Bitmap(xBmp));
        }

        private void ModeHSensor(Bitmap xBmp, PaintEventArgs e)
        {
            //HSENSOR MODE
            Graphics g = e.Graphics;

            //GENERATE SENSOR DATA
            double max = xBmp.Width;
            double[] sensor = new double[xBmp.Height];

            for (int y = 0; y < xBmp.Height; y++)
            {
                int count = 0;
                for (int x = 0; x < xBmp.Width; x++)
                {
                    Color getColor = xBmp.GetPixel(x, y);
                    if (Mod_Check.isGray(getColor, 200)) count++;
                    else
                    {
                        if (Mod_Check.isEven(y)) //DISPLAY EVERY SECOND SENSOR
                        {
                            g.DrawLine(Pen, new Point(0, y), new Point(count, y));                              //DRAW SENSOR TO CAMERA
                            Graphics.FromImage(xBmp).DrawLine(Pen, new Point(0, y), new Point(count, y));       //DRAW SENSOR TO SCREEN
                        }
                        sensor[y] = count / max;
                        count = 0;
                        break;
                    }

                }
            }

            //SET BINARY IMAGE            
            NetMain.PanelBinary.BackgroundImage = NetDraw.ScaleUp(new Bitmap(xBmp));
            LastSensor = sensor;

            g.Dispose();
        }
    }
}
