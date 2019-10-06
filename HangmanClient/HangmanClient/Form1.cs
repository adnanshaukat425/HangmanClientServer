using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace HangmanClient
{
    public partial class Form1 : Form
    {
        private static byte[] data = new byte[1024];
        private static byte[] rdata = new byte[1024];
        private static Socket client;
        private static IPEndPoint ipep;
        private static string msg;
        private static bool isWelcome = false;
        private static bool isReceivable = true;
        byte[] rdataWord = new byte[1024];
        int time = 10;
        bool is_Send = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ipep = new IPEndPoint(IPAddress.Loopback, 9050);
            client.SendTimeout = 100;
        }

        private void btn_con_Click(object sender, EventArgs e)
        {
            Thread th = new Thread(new ThreadStart(RecieveThread));
            th.Start();
            try
            {
                if (!client.Connected)
                {
                    client.BeginConnect(ipep, new AsyncCallback(Connect), client);
                    Thread.Sleep(500);
                    PopulateGUI();
                }
                else
                {
                    MessageBox.Show("Already Connected!!", "Connected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (SocketException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void btn_stop_Click(object sender, EventArgs e)
        {

        }

        private void Connect(IAsyncResult ar)
        {
            try
            {
                client = (Socket)ar.AsyncState;
                client.EndConnect(ar);
                rdata = new byte[1024];
                client.BeginReceive(rdata, 0, rdata.Length, SocketFlags.None, new AsyncCallback(Recieve), client);
                isWelcome = true;
            }
            catch (SocketException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Recieve(IAsyncResult ar)
        {
            Socket remote = (Socket)ar.AsyncState;
            int recv = remote.EndReceive(ar);
            msg = Encoding.ASCII.GetString(rdata, 0, recv);
            rdata = new byte[1024];
        }
        private void RecieveWords(IAsyncResult ar)
        {
            is_Send = false;
            Socket remote = (Socket)ar.AsyncState;
            int recv = 0;
            try
            {
                recv = remote.EndReceive(ar);

            }
            catch
            {

                timer1.Stop();
                //MessageBox.Show("Server Shutdown, Disconnecting...", "Disconnecting", MessageBoxButtons.OK, MessageBoxIcon.Error);
                remote.Shutdown(SocketShutdown.Both);
                remote.Close();
                Environment.Exit(0);
            }
            msg = Encoding.ASCII.GetString(rdataWord, 0, recv);
            //MessageBox.Show(msg.Substring(0, 5));
            string scoreMsg = msg;
            if (scoreMsg.Contains("Score"))
            {
                scoreMsg = scoreMsg.Substring(6, scoreMsg.Length-6);
                string[] score = scoreMsg.Split(new char[] { '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string[,] scoreArray = new string[score.Length % 2, 2];
                this.Invoke(new MethodInvoker(delegate()
                {
                    listBox2.Items.Clear();
                }));
                try
                {
                    for (int i = 0; i < score.Length - 1; i += 2)
                    {
                        if (score[i] == remote.LocalEndPoint.ToString())
                        {
                            this.Invoke(new MethodInvoker(delegate()
                            {
                                //for (int i = 0; i < listBox2.Items.Count; i++)
                                //{ 
                                //}
                                listBox2.Items.Add("My Score: " + score[i + 1]);
                            }));
                        }
                        else
                        {
                            this.Invoke(new MethodInvoker(delegate()
                            {
                                listBox2.Items.Add(score[i] + " Socore: " + score[i + 1]);
                            }));
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
            else if (msg != "Welcome" && msg != "Correct :D" && msg != "Wrong :( :p" && msg != "Word Complete :) , Next Word :O" && msg != "Time Exceed Limit!!! Better Luck Again :(")
            {
                this.Invoke(new MethodInvoker(delegate()
                {
                    timer1.Start();
                    timer1.Interval = 1000;
                    textBox4.Enabled = true;
                    listBox1.Items.Add(msg);
                }));
            }
            else
            {
                this.Invoke(new MethodInvoker(delegate()
                {
                    listBox1.Items.Add(msg);
                }));
            }
            rdata = new byte[1024];
            isReceivable = true;
        }
        private void PopulateGUI()
        {
            try
            {
                 this.Text = ((IPEndPoint)client.LocalEndPoint).ToString();
                listBox1.Items.Add(msg);
            }
            catch (Exception se)
            {
                MessageBox.Show(se.Message);
            }
        }
        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            is_Send = true;
            label8.Text = "10";
            time = 10; 
            timer1.Stop();
            string a = textBox4.Text;
            textBox4.Text = "";
            textBox4.Enabled = false;
            data = Encoding.ASCII.GetBytes(a);
            client.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(Send), client);
            isReceivable = true;
        }
        private void Send(IAsyncResult ar)
        {
            Socket remote = (Socket)ar.AsyncState;
            int sent = remote.EndSend(ar);
        }

        public void RecieveThread()
        {
            while (true)
            {
                if (isWelcome && isReceivable)
                {
                    isReceivable = false;
                    try
                    {
                        client.BeginReceive(rdataWord, 0, rdataWord.Length, SocketFlags.None, new AsyncCallback(RecieveWords), client);
                    }
                    catch {
                        timer1.Stop();
                        
                        data = Encoding.ASCII.GetBytes("Disconnect");
                        client.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(Send), client);
                        Thread.Sleep(100);

                        MessageBox.Show("Server Shutdown, Disconnecting...", "Disconnecting", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        client.Shutdown(SocketShutdown.Both);
                        client.Close();
                        Environment.Exit(0);
                    }
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label8.Text = time.ToString();
            time--;
            if (time == 0)
            {
                timer1.Stop();
                time = 10;
                if (!is_Send)
                {
                    data = Encoding.ASCII.GetBytes("-1");
                    client.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(Send), client);
                    Thread.Sleep(100);
                    isReceivable = true;
                    textBox4.Enabled = false;
                    
                }
            }
        }
    }
}
