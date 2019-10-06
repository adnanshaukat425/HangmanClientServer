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

namespace HangmanServer
{
    public partial class Form1 : Form
    {
        private static List<Socket> clients = new List<Socket>();
        private static Queue<Socket> clientsQueue1 = new Queue<Socket>();
        private static Queue<Socket> clientsQueue2 = new Queue<Socket>();
        private static string[,] scoresArray;
        private static Words words = new Words();
        private static string[] word;
        private static byte[] data = new byte[1024];
        private static Socket server;
        private static Socket client;
        private static List<string> hosts = new List<string>();
        private static int i = 0;
        private static int no_of_player_allowed;
        private static int no_of_player_connected;
        private static string currentWord;
        private static bool isQueue1 = true;
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            btn_start.Enabled = false;
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint iep = new IPEndPoint(IPAddress.Loopback, 9050);
            server.Bind(iep);
        }

        private void btn_start_Click(object sender, EventArgs e)
        {
            no_of_player_allowed = Convert.ToInt32(numericUpDown1.Value.ToString());
            server.Listen(no_of_player_allowed);

            label6.Text = "Serves is running now...";
            Thread th1 = new Thread(new ThreadStart(GetClients));
            th1.Start();

            while (no_of_player_connected < no_of_player_allowed)
            {
                if (i < clients.Count)
                {
                    if (!lb_clients.Items.Contains(((IPEndPoint)clients[i].RemoteEndPoint).ToString()))
                    {
                        string a = ((IPEndPoint)clients[i].RemoteEndPoint).ToString();
                        lb_clients.Items.Add(a);
                        i++;
                    }
                }
            }
            scoresArray = new string[no_of_player_allowed, 2];
            for (int i = 0; i < scoresArray.GetLength(0); i++)
            {
                scoresArray[i, 0] = ((IPEndPoint)clients[i].RemoteEndPoint).ToString();
            }
        }

        private void GetClients()
        {
            while (no_of_player_connected < no_of_player_allowed)
            {
                server.BeginAccept(new AsyncCallback(Accept), server);
            }
        }

        private void btn_stop_Click(object sender, EventArgs e)
        {
        }

        private void Accept(IAsyncResult ar)
        {
            Socket remote = (Socket)ar.AsyncState;
            client = remote.EndAccept(ar);
            data = Encoding.ASCII.GetBytes("Welcome");
            client.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(Send), client);
            clients.Add(client);
            clientsQueue1.Enqueue(client);
            no_of_player_connected++;
        }

        private void btn_gen_Click(object sender, EventArgs e)
        {
            Socket remote = clientsQueue1.Dequeue();
            clientsQueue2.Enqueue(remote);
            SendWordToClient(remote);
        }

        private void Send(IAsyncResult ar)
        {
            Socket remote = (Socket)ar.AsyncState;
            remote.EndSend(ar);
        }
        byte[] wordData = new byte[1024];
        private void SendWord(IAsyncResult ar)
        {
            Socket remote = (Socket)ar.AsyncState;
            remote.EndSend(ar);
            remote.BeginReceive(wordData, 0, wordData.Length, SocketFlags.None, new AsyncCallback(Recieve), remote);
        }

        private void Recieve(IAsyncResult ar)
        {
            string resultWord = "";
            Socket remote = (Socket)ar.AsyncState;
            int recv = remote.EndReceive(ar);

            string msg = Encoding.ASCII.GetString(wordData, 0, recv);
            wordData = new byte[1024];
            string recievedWord = "";
            for (int i = 0; i < currentWord.Length; i++)
            {
                if (currentWord[i] != ' ')
                {
                    recievedWord += currentWord[i];
                }
            }

            string temp = word[0][recievedWord.IndexOf('_')].ToString();

            if (temp == msg)
            {
                char[] charArray = recievedWord.ToCharArray();
                char[] currentWordArray = words.RemoveSpace(currentWord.ToCharArray()).ToCharArray();
                for (int i = 0; i < charArray.Length; i++)
                {
                    if (charArray[i] == '_')
                    {
                        charArray[i] = Convert.ToChar(msg);
                        currentWordArray[i] = Convert.ToChar(msg);
                        break;
                    }
                }
                data = Encoding.ASCII.GetBytes("Correct :D");
                remote.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(Send), remote);
                string clientIP = remote.RemoteEndPoint.ToString();
                Thread.Sleep(100);
                string scoreString = "Score\n";
                for (int i = 0; i < scoresArray.GetLength(0); i++)
                {
                    if (scoresArray[i, 0] == clientIP)
                    {
                        int score = Convert.ToInt32(scoresArray[i, 1]);
                        score += 5;
                        scoresArray[i, 1] = score.ToString();
                    }
                    else
                    {

                    }
                    scoreString += scoresArray[i, 0] + " " + scoresArray[i, 1] + "\n";
                }
                data = Encoding.ASCII.GetBytes(scoreString);
                foreach (Socket sockets in clients)
                {
                    sockets.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(Send), sockets);
                }

                Thread.Sleep(100);
                //}
                Thread.Sleep(1000);
                resultWord = words.RemoveSpace(charArray);
                currentWord = words.GetSpace(currentWordArray);
            }
            else if (msg == "-1")
            {
                data = Encoding.ASCII.GetBytes("Time Exceed Limit!!! Better Luck Again :(");
                remote.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(Send), remote);
                Thread.Sleep(100);
                resultWord = currentWord;
            }
            else if (msg == "Disconnect")
            {
                //Remove the clients from the list and queues
            }
            else
            {
                data = Encoding.ASCII.GetBytes("Wrong :( :p");
                remote.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(Send), remote);
                Thread.Sleep(100);
                resultWord = currentWord;
            }
            if (resultWord == word[0])
            {
                data = Encoding.ASCII.GetBytes("Word Complete :) , Next Word :O");
                remote.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(Send), remote);
                Thread.Sleep(100);
            }
            if (isQueue1)
            {
                try
                {
                    remote = clientsQueue1.Dequeue();
                }
                catch { }
                clientsQueue2.Enqueue(remote);
                if (clientsQueue1.Count == 0)
                {
                    isQueue1 = false;
                }
            }
            else
            {
                remote = clientsQueue2.Dequeue();
                clientsQueue1.Enqueue(remote);
                if (clientsQueue2.Count == 0)
                {
                    isQueue1 = true;
                }
            }
            if (resultWord == word[0])
            {
                SendWordToClient(remote);
            }
            else
            {
                if (!resultWord.Contains(" ")) { resultWord = words.GetSpace(resultWord.ToCharArray()); }
                data = Encoding.ASCII.GetBytes(resultWord);
                remote.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendWord), remote);
            }
            //MessageBox.Show(msg);
        }

        public void SendWordToClient(Socket client)
        {
            word = words.GetWords();
            currentWord = word[1];
            this.Invoke(new MethodInvoker(delegate()
            {
                lb_gen_words.Items.Add(word[0]);
            }));
            data = Encoding.ASCII.GetBytes(word[1]);
            client.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendWord), client);
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown1.Value > 0)
            {
                btn_start.Enabled = true;
            }
            else
            {
                btn_start.Enabled = false;
            }
        }
    }
}