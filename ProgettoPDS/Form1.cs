using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace ProgettoPDS
{
    public partial class FileSharing : Form
    {
        //private BackgroundWorker myWorker = new BackgroundWorker();
        Thread sendThread;
        Thread recvThread;
        public static string mode = "ONLINE";

        // Creare e istanziare una nuova ArrayList per memorizzare gli indirizzi ip disponibili
        ArrayList ipList = new ArrayList();
        Image myImage = Properties.Resources.genericUser;

        Bitmap img1 = Properties.Resources.redButton;
        Bitmap img2 = Properties.Resources.greenButton;

        public void SendMulticast()
        {
            UdpClient sender = new UdpClient();
            IPAddress multicastIp = IPAddress.Parse("224.5.6.7");
            sender.JoinMulticastGroup(multicastIp);
            IPEndPoint remoteIp = new IPEndPoint(multicastIp, 1500);

            string localIP = "";
            string UserName = Environment.UserName;
            
            //Get the local IP
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ipLocal in host.AddressList)
            {
                if (ipLocal.AddressFamily == AddressFamily.InterNetwork)
                    localIP = ipLocal.ToString();
            }

            while (true)
            {
                byte[] buffer = Encoding.Unicode.GetBytes(localIP + "," + UserName);

                //if (mode.Equals("ONLINE"))
                //{
                    sender.Send(buffer, buffer.Length, remoteIp);   //PROBLEMA!!!!! --> Questa sent deve essere periodica
                //    RecvMulticast();
                //}
                //    else
                //        RecvMulticast();
            }
        }

        public void RecvMulticast()
        {
            UdpClient reciver = new UdpClient();

            reciver.ExclusiveAddressUse = false;
            IPEndPoint localEp = new IPEndPoint(IPAddress.Any, 1500);

            reciver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            reciver.ExclusiveAddressUse = false;

            reciver.Client.Bind(localEp);

            IPAddress multicastaddress = IPAddress.Parse("224.5.6.7");
            reciver.JoinMulticastGroup(multicastaddress);

            while (true)
            {
                Byte[] data = reciver.Receive(ref localEp);
                string rcvData = Encoding.Unicode.GetString(data);

                char delimiter = ',';
                string[] message = rcvData.Split(delimiter);
                string localIP = message[0];
                string UserName = message[1];

                //Cerco nella lista di IP se si è già ricevuto un messaggio
                if (!ipList.Contains(localIP))
                {
                    //Aggiungo l'ip alla lista
                    ipList.Add(localIP.ToString());
                    //listBox1.Items.Add(localIP);

                    ImageList imgs = new ImageList();
                    imgs.ImageSize = new Size(50, 50);
                    imgs.Images.Add(localIP, myImage);

                    listView1.SmallImageList = imgs;
                    listView1.Items.Add(UserName, localIP);
                }
            }
        }
        
        public FileSharing()
        {
            InitializeComponent();
            pictureBox1.Image = img2; //assign image1 to picturebox here

            sendThread = new Thread(() => { while (true) { SendMulticast(); Thread.Sleep(1000); } });
            sendThread.Start();
            recvThread = new Thread(() => { while (true) { RecvMulticast(); Thread.Sleep(1000); } });
            recvThread.Start();

            //InitializeBackgroundWorker();
            //myWorker.WorkerReportsProgress = true;
            //myWorker.WorkerSupportsCancellation = true;
        }

        // Set up the BackgroundWorker object by 
        // attaching event handlers. 
        /*private void InitializeBackgroundWorker()
        {
            myWorker.DoWork += new DoWorkEventHandler(myWorker_DoWork);
            myWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(myWorker_RunWorkerCompleted);
            //myWorker.ProgressChanged += new ProgressChangedEventHandler(myWorker_ProgressChanged);
        }

        // This event handler is where the actual, potentially time-consuming work is done.
        private void myWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //MessageBox.Show("Worker Start!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Get the BackgroundWorker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;

            SendMulticast();
            
            //e.Result = "Success";
        }


        // This event handler deals with the results of the
        // background operation.
        private void myWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // First, handle the case where an exception was thrown.
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                // Next, handle the case where the user canceled the operation.
                // Note that due to a race condition in the DoWork event handler, the Cancelled flag may not have been set, even though
                // CancelAsync was called.
                MessageBox.Show("Cancelled");
            }
            else
            {
                // Finally, handle the case where the operation succeeded.
                MessageBox.Show("Operation Succeeded: " + e.Result.ToString());
            }
        }*/


        private void Form1_Load(object sender, EventArgs e)
        {
            /*if (!myWorker.IsBusy)//Check if the worker is already in progress
                myWorker.RunWorkerAsync();//Call the background worker*/
            listView1.View = View.Details;
            listView1.Columns.Add("Users", 400);
            
        }

        private void pictureBox1_Click_1(object sender, EventArgs e)
        {
            if (pictureBox1.Image == img1)
            {
                //MODALITA' PUBBLICA (INVIARE E RICEVERE)
                mode = "ONLINE";
                label1.Text = "ONLINE";
                pictureBox1.Image = img2;
            }

            else
            {
                //MODALITA' PRIVATA (INVIARE E NON RICEVERE)
                mode = "OFFLINE";
                label1.Text = "OFFLINE";
                pictureBox1.Image = img1;
            }

            Thread t = new Thread(new ThreadStart(SendMulticast));
            t.Start();
        }


        private void ricercaDispositiviToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            /*
            // Create a new instance of the Form2 class
            Form2 settingsForm = new Form2();

            // Show the settings form
            settingsForm.Show();
            settingsForm.updateListBox();
            */
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
