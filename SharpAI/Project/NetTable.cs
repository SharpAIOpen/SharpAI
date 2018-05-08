using Core.Classes;
using Core.Modifications;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;


/*############################################################################*
 *                Neural Network Cam Table in Live-Section                    *
 *                   To organize cam and keyboard inputs                      *
 *                    writen in c# by Martin Kober 2017                       *
 *############################################################################*/


namespace NeuralNet.Project
{
    public enum TYP
    {
        //TABLE ENUM
        ENEMY, FRIEND, TRIGGER
    }

    public class NetTable : UniTable
    {
        DataGridViewCell Cell;
        object LastValue;
        public List<Color> ColorEnemy;
        public List<Color> ColorFriend;

        public NetTable(Control xForm, int xLeft, int xTop, int xWidth, int xHeight) : base(xForm, xLeft, xTop, xWidth, xHeight, DataGridViewColumnSortMode.NotSortable, DataGridViewSelectionMode.CellSelect, false, false, false, false, true)
        {
            AutoSizeFill();
            setRowHeader(".");
            addColumns(new string[] { "Enemy", "Friend", "Trigger" });
            addRows(20);
            ClearSelection();

            //EVENT LISTENER
            CellClick += eventCellClick;
            KeyDown += eventRemove;

            //COLUMN ACTION
            Action actionPickColor = new Action(() => { Cell.Value = "pick..."; UniGlobal.PickColor(CellColor); });
            Action actionPickKey = new Action(() => { LastValue = Cell.Value; Cell.Value = "pick..."; UniGlobal.PickKey(CellKey); });

            setActionColumn(0, actionPickColor);
            setActionColumn(1, actionPickColor);
            setActionColumn(2, actionPickKey);

            //ALLOW REMOVE            
            AllowRemove((int)TYP.TRIGGER);

            //INITIALIZE
            Initialize();
        }

        public void Initialize()
        {
            Color enemy = Color.Black;
            Color friend = Color.White;

            foreach (DataGridViewRow row in Rows)
            {
                //ENEMY
                row.Cells[(int)TYP.ENEMY].Style.BackColor = enemy;
                row.Cells[(int)TYP.ENEMY].Tag = enemy.ToArgb();

                //FRIEND
                row.Cells[(int)TYP.FRIEND].Style.BackColor = friend;
                row.Cells[(int)TYP.FRIEND].Tag = friend.ToArgb();

                //KEY
                row.Cells[(int)TYP.TRIGGER].Value = string.Empty;
                row.Cells[(int)TYP.TRIGGER].Tag = null;
            }
            setColor();
        }

        public void CellColor(Color xColor, bool xGlobal)
        {
            //CELL COLOR
            Cell.Style.BackColor = xColor;
            Cell.Tag = xColor.ToArgb();
            if (xGlobal)
            {
                Cell.Value = LastValue;
                ClearSelection();
                setColor();
            }
        }

        public void CellKey(KeyEventArgs xKey, bool xGlobal)
        {
            //ABBRUCH
            if (xKey.KeyData == Keys.Escape)
            { Cell.Value = LastValue; return; }
            else if (xKey.KeyData == Keys.Delete)
            { Cell.Value = null; return; }

            //CELL KEY            
            Cell.Value = xKey.KeyData.ToString();
            Cell.Tag = Cell.Value;
            if (xGlobal) ClearSelection();
        }

        public object[] Save()
        {
            //SAVE NET TABLE
            List<object> objList = new List<object>();
            foreach (DataGridViewRow row in Rows)
                objList.Add((row.Cells[0].Tag + ";" + row.Cells[1].Tag + ";" + row.Cells[2].Value));

            return objList.ToArray();
        }

        public void Load(object[] xObject)
        {
            //LOAD NET TABLE
            for (int i = 0; i < xObject.Length; i++)
            {
                DataGridViewRow row = Rows[i];
                object[] split = Mod_Convert.StringSplitToObjectArray(xObject[i]);

                //COLOR ENEMY
                row.Cells[(int)TYP.ENEMY].Tag = split[(int)TYP.ENEMY];
                row.Cells[(int)TYP.ENEMY].Style.BackColor = Color.FromArgb(Mod_Convert.ObjectToInteger(split[(int)TYP.ENEMY]));

                //COLOR FRIEND
                row.Cells[(int)TYP.FRIEND].Tag = split[(int)TYP.FRIEND];
                row.Cells[(int)TYP.FRIEND].Style.BackColor = Color.FromArgb(Mod_Convert.ObjectToInteger(split[(int)TYP.FRIEND]));

                //TRIGGER
                row.Cells[(int)TYP.TRIGGER].Tag = split[(int)TYP.TRIGGER];
                row.Cells[(int)TYP.TRIGGER].Value = split[(int)TYP.TRIGGER];
            }
        }

        public List<Color> getColor(TYP xTyp)
        {
            //GET COLOR FROM TABLE
            List<Color> colorList = new List<Color>();
            foreach (DataGridViewRow row in Rows)
            {
                Color color = row.Cells[(int)xTyp].Style.BackColor;
                if (!colorList.Contains(color))
                    colorList.Add(color);
            }
            return colorList;
        }

        public void setColor()
        {
            //SET COLOR 
            ColorEnemy = getColor(TYP.ENEMY);
            ColorFriend = getColor(TYP.FRIEND);
        }

        public string[] getKeys()
        {
            //GET TRIGGER KEYS
            return Mod_Convert.ObjectArrayToStringArray(getColumnTags(2));
        }

        private void eventCellClick(object sender, DataGridViewCellEventArgs e)
        {
            //ABBRUCH
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            //CELL CLICK EVENT
            Cell = Rows[e.RowIndex].Cells[e.ColumnIndex];
            LastValue = Cell.Value;
        }

        private void eventRemove(object sender, KeyEventArgs e)
        {
            //ABBRUCH
            if (SelectedCells.Count == 0)
                return;

            //REMOVE EVENT
            DataGridViewCell cell = SelectedCells[0];
            if (e.KeyData == Keys.Delete)
            {
                switch (cell.ColumnIndex)
                {
                    case (int)TYP.ENEMY:
                        cell.Style.BackColor = Color.Black;
                        cell.Tag = Color.Black.ToArgb();
                        break;
                    case (int)TYP.FRIEND:
                        cell.Style.BackColor = Color.White;
                        cell.Tag = Color.White.ToArgb();
                        break;
                }
                setColor();
            }
        }
    }
}
