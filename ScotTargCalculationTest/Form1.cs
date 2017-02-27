﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScotTargCalculationTest
{
    public partial class Form1 : Form
    {

        private static readonly Color NCOLOR = Color.DarkSlateGray;
        private static readonly Color LCOLOR = Color.Red;
        private const char DELIMETER = ',';
        private const int NSIZE = 10;
        private const int LSIZE = 20;
        private const int RES_INCREASE_FACTOR = 1;
        private const int WIDTH = 600;
        private const int GRIDSCALE = 5;
        private const int SCALESIZE = 3;

        private CalculatePoint cp = new CalculatePoint();
        private Comms comms = new Comms();
        private Bitmap gridImg;
        private Graphics gr;
        int TimeA = 0;
        int TimeB = 0;
        int TimeC = 0;
        int TimeD = 0;

        int AB = 0;
        int CD = 0;
        int AD = 0;
        int BC = 0;

        public Form1()
        {
            InitializeComponent();
            cp.CalcConst = pbGrid.Width;
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                cmboPorts.Items.Add(port);
            }
            comms.OnHitRecorded += on_HitRecorded;

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DrawGrid();
        }

        /// <summary>
        /// Eventhandler for the Comms.OnHitRecorded event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void on_HitRecorded(object sender, Comms.HitRecordedEventArgs e)
        {
            cp.CalcConst = int.Parse(txtTimeWidth.Text);

            TimeA = e.TimeA;
            TimeB = e.TimeB;
            TimeC = e.TimeC;
            TimeD = e.TimeD;
            AB = TimeA - TimeB;
            BC = TimeB - TimeC;
            CD = TimeC - TimeD;
            AD = TimeA - TimeD;

            txtTimeA.Text = TimeA.ToString();
            txtTimeB.Text = TimeB.ToString();
            txtTimeC.Text = TimeC.ToString();
            txtTimeD.Text = TimeD.ToString();

            
            txtDiffAB.Text = AB.ToString();
            txtDiffBC.Text = BC.ToString();
            txtDiffCD.Text = CD.ToString();
            txtDiffAD.Text = AD.ToString();

            double x = 0, y = 0;
            cp.FindCoords((double)AB, (double)BC, (double)CD, (double)AD, ref x, ref y);

            txtCalculatedX.Text = x.ToString();
            txtCalculatedY.Text = y.ToString();

            DsData.DtShotsRow row = dsData.DtShots.NewDtShotsRow();
            row.TimeA = TimeA;
            row.TimeB = TimeB;
            row.TimeC = TimeC;
            row.TimeD = TimeD;
            row.CalcX = (int)x;
            row.CalcY = (int)y;
            dsData.DtShots.AddDtShotsRow(row);

            ReDrawShots();
        }

        /// <summary>
        /// Draws the fixed fraphics on the grid
        /// </summary>
        private void DrawGrid()
        {
            int gridWidth = (int)nudWidth.Value;

            gridImg = new Bitmap(gridWidth, gridWidth);
            gr = Graphics.FromImage(gridImg);

            //Graphics gr = Graphics.FromImage(gridImg);

            int xMiddle = gridWidth / 2;
            int yMiddle = gridWidth / 2;

            gr.FillRectangle(Brushes.LightGray, 0, 0, gridWidth, gridWidth);
            Pen pen = new Pen(Color.Black, 1);

            gr.DrawLine(pen, 0, yMiddle, gridWidth, yMiddle);   // X line
            gr.DrawLine(pen, xMiddle, 0, xMiddle, gridWidth);  // Y line
            int z = Width / 2 / GRIDSCALE;
            for (int x = 1; x < z; x++)
            {
                int xPlus = xMiddle + (GRIDSCALE * x);
                int xMinus = xMiddle - (GRIDSCALE * x);
                int yPlus = yMiddle - (GRIDSCALE * x);
                int yMinus = yMiddle + (GRIDSCALE * x);
                int yTop = yMiddle - SCALESIZE;
                int yBottom = yMiddle + SCALESIZE;
                int xLeft = xMiddle - SCALESIZE;
                int xRight = xMiddle + SCALESIZE;

                gr.DrawLine(pen, xPlus, yTop, xPlus, yBottom);
                gr.DrawLine(pen, xMinus, yTop, xMinus, yBottom);

                gr.DrawLine(pen, xLeft, yPlus, xRight, yPlus);
                gr.DrawLine(pen, xLeft, yMinus, xRight, yMinus);

            }
            pbGrid.Image = gridImg;
            DrawMics();
        }

        /// <summary>
        /// Draws Mic Images in the corners of the grid
        /// </summary>
        private void DrawMics()
        {
            int gridWidth = (int)nudWidth.Value;

            Image img = gridImg;
            //Graphics gr = Graphics.FromImage(img);
            int r = 40;
            gr.FillPie(Brushes.Black, -(r / 2), -(r / 2), r, r,0,90);
            gr.FillPie(Brushes.Black, -(r / 2), gridWidth - (r / 2), r, r, 270, 90);
            gr.FillPie(Brushes.Black, gridWidth - (r / 2), -(r / 2), r, r, 90, 90);
            gr.FillPie(Brushes.Black, gridWidth - (r / 2), gridWidth - (r / 2), r, r,180,90);
            gr.DrawString("A", this.Font, Brushes.White, 1, gridWidth - 16, StringFormat.GenericDefault);
            gr.DrawString("B", this.Font, Brushes.White, 1, 1, StringFormat.GenericDefault);
            gr.DrawString("C", this.Font, Brushes.White, gridWidth - 16, 1, StringFormat.GenericDefault);
            gr.DrawString("D", this.Font, Brushes.White, gridWidth - 16, gridWidth - 16, StringFormat.GenericDefault);
        }

        /// <summary>
        /// Draws the hit point on the grid with the coordinates of the point next to it.
        /// </summary>
        /// <param name="e"></param>
        private void DrawHitPoint(Point e, int id, Color color, int size)
        {
            try
            {
                int cc = cp.CalcConst;
                int mmWidth = 300;
                float shotsize = (float)pbGrid.Image.Width / (float)mmWidth * 4.5f;
                size = (int)shotsize;
                //Graphics gr = Graphics.FromImage(gridImg);
                //Pen pen = new Pen(Color.Red, 1);
                Brush brush = new SolidBrush(color);
                Font font = new Font(this.Font.FontFamily, 10);

                int x = (e.X - (pbGrid.Image.Width / 2)) * RES_INCREASE_FACTOR;
                int y = (e.Y - (pbGrid.Image.Height / 2)) * -RES_INCREASE_FACTOR;

                gr.FillEllipse(brush, e.X - (size/2), e.Y - (size / 2), size, size);
                gr.DrawString("(" + id + ")", font, brush, e.X + size, e.Y);
            }
            catch { }
        }

        /// <summary>
        /// This function takes the actual coordinates of the mouse click and then calculates the direct distance from each microphone
        /// For simplicity (and accuracy), there is no conversion from distance to time. IE time = distance.  
        /// </summary>
        /// <param name="e">Point of the mouse click</param>
        /// <param name="t1">Time/distance to corner 1 (mic B)</param>
        /// <param name="t2">Time/distance to corner 2 (mic C)</param>
        /// <param name="t3">Time/distance to corner 3 (mic D)</param>
        /// <param name="t4">Time/distance to corner 4 (mic A)</param>
        private void CalculateTimimg(Point e, ref double t1, ref double t2, ref double t3, ref double t4)
        {
            int gridWidth = (int)nudWidth.Value;

            int cornerX = gridWidth / 2 * RES_INCREASE_FACTOR;
            int cornerY = gridWidth / 2 * RES_INCREASE_FACTOR;
            int x = (e.X - (gridWidth / 2)) * RES_INCREASE_FACTOR;
            int y = (e.Y - (gridWidth / 2)) * -RES_INCREASE_FACTOR;

            int addToX = 0;
            int addToY = 0;

            // Time1 Calculation (Bottom Left corner)
            addToX = (x <= 0) ? 0 : Math.Abs(x) * 2;
            addToY = (y <= 0) ? 0 : Math.Abs(y) * 2;
            t1 = Math.Sqrt(Math.Pow(cornerX + addToX - Math.Abs(x), 2) + Math.Pow(cornerY + addToY - Math.Abs(y), 2));

            // Time2 Calculation (Top Left corner)
            addToX = (x <= 0) ? 0 : Math.Abs(x) * 2;
            addToY = (y >= 0) ? 0 : Math.Abs(y) * 2;
            t2 = Math.Sqrt(Math.Pow(cornerX + addToX - Math.Abs(x), 2) + Math.Pow(cornerY + addToY - Math.Abs(y), 2));

            // Time3 Calculation (Top Right corner)
            addToX = (x >= 0) ? 0 : Math.Abs(x) * 2;
            addToY = (y >= 0) ? 0 : Math.Abs(y) * 2;
            t3 = Math.Sqrt(Math.Pow(cornerX + addToX - Math.Abs(x), 2) + Math.Pow(cornerY + addToY - Math.Abs(y), 2));

            // Time4 Calculation (Bottom Right corner)
            addToX = (x >= 0) ? 0 : Math.Abs(x) * 2;
            addToY = (y <= 0) ? 0 : Math.Abs(y) * 2;
            t4 = Math.Sqrt(Math.Pow(cornerX + addToX - Math.Abs(x), 2) + Math.Pow(cornerY + addToY - Math.Abs(y), 2));


            double deduct = GetLowestValue(t1, t2, t3, t4);

            t1 = Math.Round(t1 - deduct, 0);
            t2 = Math.Round(t2 - deduct, 0);
            t3 = Math.Round(t3 - deduct, 0);
            t4 = Math.Round(t4 - deduct, 0);
        }

        /// <summary>
        /// Get the lowest value of the four timings
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="t3"></param>
        /// <param name="t4"></param>
        /// <returns></returns>
        private double GetLowestValue(double t1,double t2,double t3,double t4)
        {
            double val = 0;
            if (t1 <= t2 && t1 <= t3 && t1 <= t4)
            {
                val = t1;
            }
            else if (t2 <= t1 && t2 <= t3 && t2 <= t4)
            {
                val = t2;
            }
            else if (t3 <= t1 && t3 <= t2 && t3 <= t4)
            {
                val = t3;
            }
            else
            {
                val = t4;
            }
            return val;
        }

        /// <summary>
        /// Event occures when the user clicks anywhere in the grid. This represents the shot hitting the target
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pbGrid_MouseClick(object sender, MouseEventArgs e)
        {
            int gridWidth = (int)nudWidth.Value;
            cp.CalcConst = gridWidth;

            DrawGrid();                 //! Clear the previous shot from the grid
            int tx = e.X - (pbGrid.Width / 2 - pbGrid.Image.Width / 2);
            int ty = e.Y - (pbGrid.Height / 2 - pbGrid.Image.Height / 2);
            Point location = new Point(tx, ty);
            DrawHitPoint(location, 0, NCOLOR, NSIZE);   //! Draw the actual hit point on the grid
            double t1 = 0, t2 = 0, t3 = 0, t4 = 0;
            CalculateTimimg(location, ref t1, ref t2, ref t3, ref t4);    //! Calculate the mic timings

            TimeA = (int)t1;
            TimeB = (int)t2;
            TimeC = (int)t3;
            TimeD = (int)t4;

            AB = TimeA - TimeB;
            BC = TimeB - TimeC;
            CD = TimeC - TimeD;
            AD = TimeA - TimeD;

            txtSelX.Text = e.X.ToString();
            txtSelY.Text = e.Y.ToString();

            txtTA.Text = TimeA.ToString();// +" (" + Math.Round(t1, 0) + ")";
            txtTB.Text = TimeB.ToString();// + " (" + Math.Round(t2, 0) + ")";
            txtTC.Text = TimeC.ToString();// + " (" + Math.Round(t3, 0) + ")";
            txtTD.Text = TimeD.ToString();// + " (" + Math.Round(t4, 0) + ")";

            txtTdoaAB.Text = AB.ToString();
            txtTdoaBC.Text = BC.ToString();
            txtTdoaCD.Text = CD.ToString();
            txtTdoaAD.Text = AD.ToString();

            double x = 0, y = 0;
            cp.FindCoords((double)AB / RES_INCREASE_FACTOR, (double)BC / RES_INCREASE_FACTOR, (double)CD / RES_INCREASE_FACTOR, (double)AD / RES_INCREASE_FACTOR, ref x, ref y);

            x = Math.Round(x);
            y = Math.Round(y);

            txtCalcX.Text = x.ToString();
            txtCalcY.Text = y.ToString();
            txtDiffX.Text = (e.X - x).ToString();
            txtDiffY.Text = (e.Y - y).ToString();

            DrawHitPoint(new Point((int)x, (int)y), 0, LCOLOR, NSIZE);

        }

        /// <summary>
        /// Valitates that the value enterred in txtTargetSize is a valid int value
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtTargetSize_Validating(object sender, CancelEventArgs e)
        {
            int gridWidth = (int)nudWidth.Value;
        }

        /// <summary>
        /// Redraws the target area after the new value in txtTargetSize has been changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtTargetSize_Validated(object sender, EventArgs e)
        {
            DrawGrid();
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            int testX, testY;
            try
            {
                testX = int.Parse(txtTextX.Text);
                testY = int.Parse(txtTextY.Text);
            }
            catch
            {
                testX = 400;
                testY = 400;
            }
            MouseEventArgs mea = new MouseEventArgs(System.Windows.Forms.MouseButtons.Left, 1, testX, testY, 0);
            pbGrid_MouseClick(pbGrid, mea);
        }

        bool started = false;
        private void button1_Click(object sender, EventArgs e)
        {
            if (started)
            {
                comms.StopListening();
                started = false;
                button1.Text = "Connect";
            }
            else
            {
                string portName = cmboPorts.Text;
                int baudRate = 9600;
                comms.StartListening(portName, baudRate);
                started = true;
                button1.Text = "Disconnect";
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            comms.StopListening();
        }

        FormCalculations formCalc = new FormCalculations();
        private void btnCalcForm_Click(object sender, EventArgs e)
        {
            if (!formCalc.Visible)
            {
                formCalc = new FormCalculations();
                formCalc.Show(this);
            }
        }

        /// <summary>
        /// Recalculates all XY positions in the datagrid after txtTimeWidth has been changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtTimeWidth_Validated(object sender, EventArgs e)
        {
            cp.CalcConst = int.Parse(txtTimeWidth.Text);
            foreach( DsData.DtShotsRow row in dsData.DtShots.Rows)
            {
                CalculatePoint.Timings t = new CalculatePoint.Timings();
                t.TimeA = row.TimeA;
                t.TimeB = row.TimeB;
                t.TimeC = row.TimeC;
                t.TimeD = row.TimeD;

                TimeA = row.TimeA;
                TimeB = row.TimeB;
                TimeC = row.TimeC;
                TimeD = row.TimeD;

                AB = TimeA - TimeB;
                BC = TimeB - TimeC;
                CD = TimeC - TimeD;
                AD = TimeA - TimeD;

                double x = 0, y = 0;
                cp.FindCoords((double)AB, (double)BC, (double)CD, (double)AD, ref x, ref y);
                //Point p = cp.GetBestTimings(t);

                row.CalcX = (int)x;
                row.CalcY = (int)y;
            }
            ReDrawShots();
        }

        /// <summary>
        /// Re-Draw the grin and all the current shots
        /// </summary>
        private void ReDrawShots()
        {
            int gridWidth = (int)nudWidth.Value;

            DrawGrid();
            int rowCount = dsData.DtShots.Rows.Count;
            for (int x = 0; x < rowCount; x++)
            {
                DsData.DtShotsRow row = (DsData.DtShotsRow)dsData.DtShots.Rows[x];
                double px = row.CalcX; 
                double py = row.CalcY; 

                px = px / cp.CalcConst * gridWidth;
                py = py / cp.CalcConst * gridWidth;


                bool lastone = (x >= (rowCount - 1));
                int size =  (lastone) ? LSIZE : NSIZE;
                Color col = (lastone) ? LCOLOR : NCOLOR;

                DrawHitPoint(new Point((int)Math.Round(px, 0), (int)Math.Round(py, 0)), row.Id,col, size);
            }
        }

        /// <summary>
        /// Import previously exported shots
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnImport_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                dsData.DtShots.Clear();
                TextReader tr = new StreamReader(openFileDialog1.OpenFile());
                while (true)
                {
                    try
                    {
                        string line = tr.ReadLine();
                        int p0 = 0;
                        int p1 = line.IndexOf(DELIMETER);
                        string strId = line.Substring(p0, p1 - p0);
                        p0 = p1 + 1;
                        p1 = line.IndexOf(DELIMETER, p0);
                        string strA = line.Substring(p0, p1-p0);
                        p0 = p1 + 1;
                        p1 = line.IndexOf(DELIMETER, p0);
                        string strB = line.Substring(p0, p1 - p0);
                        p0 = p1 + 1;
                        p1 = line.IndexOf(DELIMETER, p0);
                        string strC = line.Substring(p0, p1 - p0);
                        p0 = p1 + 1;
                        p1 = line.IndexOf(DELIMETER, p0);
                        string strD = line.Substring(p0, p1 - p0);

                        DsData.DtShotsRow row = dsData.DtShots.NewDtShotsRow();
                        row.TimeA = int.Parse(strA);
                        row.TimeB = int.Parse(strB);
                        row.TimeC = int.Parse(strC);
                        row.TimeD = int.Parse(strD);
                        dsData.DtShots.Rows.Add(row);
                    }
                    catch
                    {
                        tr.Close();
                        break;
                    }
                }
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                TextWriter tw = new StreamWriter(saveFileDialog1.OpenFile());
                foreach (DsData.DtShotsRow row in dsData.DtShots)
                {
                    tw.Write(row.Id);
                    tw.Write(DELIMETER);
                    tw.Write(row.TimeA);
                    tw.Write(DELIMETER);
                    tw.Write(row.TimeB);
                    tw.Write(DELIMETER);
                    tw.Write(row.TimeC);
                    tw.Write(DELIMETER);
                    tw.Write(row.TimeD);
                    tw.Write(DELIMETER);
                    tw.Write("\r\n");
                }
                tw.Close();
            }
        }
    }
}
