using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace PeakCutTest
{

    enum PEAK_CUT_STATE { PC_STOP, PC_RUN, PC_STANDBY };

    class Ramp
    {
        public float fDelta;
        public float fOut;
    };

    struct PEAK_CUT
    {
	    public bool bEnable;
        public float fTargetPower;
        public float fSocMin;
        public float fPowerRamp;
        public bool bSaveConfig;

        public float fMeterPower;
        public PEAK_CUT_STATE state;
        public Ramp powerRamp;
    };

    struct SIMUL
    {
        public float meterP;
        public float essP;
        public float loadP;
        public Ramp ramp;
    }

    


    public partial class Form1 : Form
    {
        PEAK_CUT peakCut;
        SIMUL simul;
        Thread _thread;
        bool Enable;

        public Form1()
        {
            peakCut.powerRamp = new Ramp();
            peakCut.powerRamp.fDelta = 50;

            simul.ramp = new Ramp();
            simul.ramp.fDelta = 20;


            InitializeComponent();
        }

        float RAMP_Change(Ramp r, float fDSt)
        {
	        float diff;

	        diff = fDSt - r.fOut;

	        if ( diff >= r.fDelta ) r.fOut = r.fOut + r.fDelta;
	        else if ( diff <= -r.fDelta ) r.fOut = r.fOut - r.fDelta;
	        else r.fOut = fDSt;

	        return r.fOut;
        }

        void thread()
        {
            float loadPower = 0;
            float outputPower = 0;
            float targetPower = 0;
            float pcsPower = 0;
            float meterPower = 0;
            float fKp = 0;
            double dTemp = 0;
            string log;
            Random rand = new Random();

            simul.ramp.fOut = 300; // 300kW부터 시작 하도록..
            peakCut.powerRamp.fOut = 0;

            CheckForIllegalCrossThreadCalls = false;

            string Target_path = @"C:\Result.csv";

            System.IO.FileStream file_stream = new FileStream(Target_path, FileMode.Create, FileAccess.Write);
            StreamWriter psWriter = new StreamWriter(file_stream, System.Text.Encoding.Default);

            log = "목표파워,실제로드,METER파워,ESS파워,계산로드파워,ESS출력지령파워";
            psWriter.WriteLine(log);
            txtLog.Text = "";
            txtLog.AppendText(log + Environment.NewLine);

            //for(int i =0; i<27; i++)
            //{
            //    switch(i)
            //    {
            //        case 0: simul.loadP = 5; break;
            //        case 1: simul.loadP = 5; break;
            //        case 2: simul.loadP = 5; break;
            //        case 3: simul.loadP = 150; break;
            //        case 4: simul.loadP = 180; break;
            //        case 5: simul.loadP = 180; break;
            //        case 6: simul.loadP = 180; break;
            //        case 7: simul.loadP = 190; break;
            //        case 8: simul.loadP = 200; break;
            //        case 9: simul.loadP = 200; break;
            //        case 10: simul.loadP = 200; break;
            //        case 11: simul.loadP = 210; break;
            //        case 12: simul.loadP = 210; break;
            //        case 13: simul.loadP = 210; break;
            //        case 14: simul.loadP = 210; break;
            //        case 15: simul.loadP = 210; break;
            //        case 16: simul.loadP = 210; break;
            //        case 17: simul.loadP = 210; break;
            //        case 18: simul.loadP = 220; break;
            //        case 19: simul.loadP = 220; break;
            //        case 20: simul.loadP = 220; break;
            //        case 21: simul.loadP = 220; break;
            //        case 22: simul.loadP = 220; break;
            //        case 23: simul.loadP = 220; break;
            //        case 24: simul.loadP = 220; break;
            //        case 25: simul.loadP = 230; break;
            //        case 26: simul.loadP = 230; break;
            //        default:
            //            _thread = null;
            //            break;
            //    }

            while (Enable)
            {
                simul.loadP = RAMP_Change(simul.ramp, rand.Next(500, 900));
                txtLoadPowerActual.Text = simul.loadP.ToString();

                simul.meterP = simul.loadP - simul.essP;

                dTemp = peakCut.fTargetPower * 0.97;
                targetPower = peakCut.fTargetPower; // kW
                pcsPower = simul.essP;
                meterPower = simul.meterP;

                #region OLD
                // Set Point = target
                loadPower = meterPower + pcsPower;
                if (loadPower < 0)
                    loadPower = 0;

                /* Error (target - meter + pcs P)*/
                outputPower = loadPower - ((float)dTemp); // targetPower -> dTemp 0.97 배 적용..
                //fKp = 1.2F;
                //outputPower *= fKp;
                if (outputPower < 1)
                {
                    outputPower = 0;
                }
                #endregion

                // PCS P = outputPower;
                outputPower = RAMP_Change(peakCut.powerRamp, outputPower);
                ///
                log = targetPower.ToString() + "," + simul.loadP.ToString() + "," + meterPower.ToString() + "," + simul.essP.ToString() + "," + loadPower.ToString() + "," + outputPower.ToString();
                psWriter.WriteLine(log);

                txtLog.AppendText(log + Environment.NewLine);
                

                simul.essP = outputPower;
                Thread.Sleep(200);
            }

            psWriter.Close();
        }

        private void btnEnable_Click(object sender, EventArgs e)
        {
            if (_thread == null)
            {
                Enable = true;
                _thread = new Thread(new ThreadStart(thread));
                _thread.Start();

            }
        }

        private void btnDisable_Click(object sender, EventArgs e)
        {
            if (_thread != null)
            {
                Enable = false;
                _thread = null;
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            peakCut.fTargetPower = int.Parse(txtTargetPower.Text);
            simul.loadP = int.Parse(txtLoadPowerActual.Text);
        }
    }
}
