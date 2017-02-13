using System;
using System.Management;
using System.Collections.Generic;
using System.IO.Ports;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        private String Rx;
        
        public Form1()
        {
            InitializeComponent();
        }

        

        private void Form1_Load(object sender, EventArgs e)
        {
            string arduinoLine="";
            string arduPort="";
            int arduinoItem = -1;
            using (var searcher = new ManagementObjectSearcher
                ("SELECT * FROM WIN32_SerialPort"))
            {
                string[] portnames = SerialPort.GetPortNames();
                var ports = searcher.Get().Cast<ManagementBaseObject>().ToList();
                var tList = (from n in portnames
                             join p in ports on n equals p["DeviceID"].ToString()
                             select n + " - " + p["Caption"]).ToList();
                
                foreach (string s in tList)
                {
                    comboBox1.Items.Add(s);
                    if (s.Contains("Arduino")) 
                    {
                        arduinoLine = s;
                        arduinoItem = comboBox1.Items.IndexOf(s);
                        arduPort = s.Substring(0,5).Replace(" ",string.Empty);
                    }
                }
                if (arduinoItem > -1)
                {
                    comboBox1.SelectedIndex = arduinoItem;
                    serialPort1.PortName = arduPort;
                    try
                    {
                        serialPort1.Open();
                        toolStripStatusLabel1.Text = "Connecté à " + arduinoLine;
                        comboBox1.Enabled = false;
                        timer1.Enabled = true;
                        //serialPort1.Write("STATUS\n");
                    }
                    catch
                    {
                        DialogResult result;
                        result = MessageBox.Show("Impossible d'ouvrir le port " + serialPort1.PortName + " !\n" +
                                                 "Ce port est peut être déja ouvert.\n" +
                                                 "Voulez-vous recommencé ?" 
                                                 , "Erreur !", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                        if (result == DialogResult.Yes) Application.Restart();
                        else Application.Exit();
                    }
                }
                else
                {
                    DialogResult result;
                    result=MessageBox.Show("Pas de carte Arduino détectée !\nConnectez une Arduino maintenant.", "Erreur !", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                    if (result == DialogResult.OK) Application.Restart();
                    else Application.Exit();
                }
                
            } 
        }

        private void btPort_Click(object sender, EventArgs e)
        {
            if (btPort.Text == "Connecter")
            {
                timer1.Enabled = false;
                serialPort1.Close();
                serialPort1.PortName = comboBox1.SelectedItem.ToString().Substring(0, 5).Replace(" ", string.Empty);
                try
                { 
                    serialPort1.Open();
                    comboBox1.Enabled = false;
                    btPort.Text = "Modifier";
                    toolStripStatusLabel1.Text = "Connecté à " + comboBox1.SelectedItem.ToString();
                    timer1.Enabled = true;
                    //serialPort1.Write("STATUS\n");
                }
                catch
                {
                    DialogResult result;
                    result = MessageBox.Show("Impossible d'ouvrir le port " + serialPort1.PortName + " !\n" +
                                                 "Ce port est peut être déja ouvert.\n" +
                                                 "Voulez-vous recommencé ?"
                                                 , "Erreur !", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                    if (result == DialogResult.Yes) Application.Restart();
                    else Application.Exit();
                }
            }
            else if (comboBox1.Enabled)
            {
                comboBox1.Enabled = false;
                btPort.Text = "Modifier";
            }
            else
            {
                comboBox1.Enabled = true;
                btPort.Text = "Connecter";
            }
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                Rx = serialPort1.ReadTo("\r\n");
            }
            catch { }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            /*  Afficher les différents éléments de la trames GGA  :
                https://fr.wikipedia.org/wiki/NMEA_0183
            $GPGGA       : Type de trame
            064036.289   : Trame envoyée à 06h 40m 36,289s (heure UTC)
            4836.5375,N  : Latitude 48,608958° Nord = 48°36'32.25" Nord
            00740.9373,E : Longitude 7,682288° Est = 7°40'56.238" Est
            1            : Type de positionnement (le 1 est un positionnement GPS)
            04           : Nombre de satellites utilisés pour calculer les coordonnées
            3.2          : Précision horizontale ou HDOP (Horizontal dilution of precision)
            200.2,M      : Altitude 200,2, en mètres
            ,,,,,0000    : D'autres informations peuvent être inscrites dans ces champs
            *0E          : Somme de contrôle de parité, un simple XOR sur les caractères entre $ et *
            */

            //lblGps.Text = Rx;
            
            try
            {
                string[] nmea = Rx.Split(',');
                if (nmea[0] == "$GPGGA")
                {
                    float lat = Convert.ToSingle(nmea[2].Replace('.',','))/100;
                    float lon = Convert.ToSingle(nmea[4].Replace('.', ',')) / 100;
                    float alt = Convert.ToSingle(nmea[9].Replace('.', ','));

                    int lat_d = (int)(lat);
                    int lat_m = (int)((lat - lat_d)*60);
                    float lat_s = (lat - (float)lat_d - (float)lat_m / 60) * 3600;

                    int lon_d = (int)(lon);
                    int lon_m = (int)((lon - lon_d) * 60);
                    float lon_s = (lon - (float)lon_d - (float)lon_m / 60) * 3600;

                    string message;

                    message = "Heure : " + nmea[1].Substring(0, 2) + ":" + nmea[1].Substring(2, 2) + ":" + nmea[1].Substring(4,2);
                    message += "\nLat = " + lat_d.ToString("00") + "° " + lat_m.ToString("00") + "' " + lat_s.ToString("##.##") + "\", " + nmea[3];
                    message += "\nLon = " + lon_d.ToString("00") + "° " + lon_m.ToString("00") + "' " + lon_s.ToString("##.##") + "\", " + nmea[5];
                    message += "\nAlt = " + alt.ToString("###,##") + " m";

                    message += "\n\nNombre de satellites : " + Convert.ToInt16(nmea[7]).ToString("00");

                    lblGps.Text = message;
                }
            }
            catch 
            {
                lblGps.Text = "En attente d'une trame ...";
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            serialPort1.Close();
        }


    }
}
