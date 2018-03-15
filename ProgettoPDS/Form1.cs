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
        private BackgroundWorker sendWorker = new BackgroundWorker();
        private BackgroundWorker recvWorker = new BackgroundWorker();

        byte[] buffer;
        string sendlocalIP = "";
        string myUserName = "";
        int indexImage;

        ImageList imgs;
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

            //string mylocalIP = "";
            myUserName = Environment.UserName;
            
            //Get the local IP
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ipLocal in host.AddressList)
            {
                if (ipLocal.AddressFamily == AddressFamily.InterNetwork)
                    sendlocalIP = ipLocal.ToString();
            }

            while ((label1.Text).Equals("ONLINE") && !sendWorker.CancellationPending)
            {
                buffer = Encoding.Unicode.GetBytes(label1.Text + "," + sendlocalIP + "," + myUserName);
                sender.Send(buffer, buffer.Length, remoteIp);
            }
            buffer = Encoding.Unicode.GetBytes(label1.Text + "," + sendlocalIP + "," + myUserName);
            sender.Send(buffer, buffer.Length, remoteIp);
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


            while (!recvWorker.CancellationPending)
            {
                Byte[] data = reciver.Receive(ref localEp);
                string rcvData = Encoding.Unicode.GetString(data);

                char delimiter = ',';
                string[] message = rcvData.Split(delimiter);
                string status = message[0];                
                string localIP = message[1];
                string UserName = message[2];

                Invoke((MethodInvoker)delegate
                {
                    if (status.Equals("ONLINE"))
                    {
                        //Cerco nella lista di IP se si è già ricevuto un messaggio
                        if (!ipList.Contains(localIP) && !myUserName.Equals(UserName))
                        {
                            //Aggiungo l'ip alla lista
                            ipList.Add(localIP);
                            //listBox1.Items.Add(localIP);
                            
                            imgs = new ImageList();
                            imgs.ImageSize = new Size(50, 50);
                            //Uso la funzione Add(key,image)
                            imgs.Images.Add(localIP, myImage);

                            listView1.SmallImageList = imgs;
                            listView1.Items.Add(localIP, UserName, localIP);
                        }
                    }
                    else
                    {
                        if (ipList.Contains(localIP))
                        {
                            indexImage = imgs.Images.IndexOfKey(localIP);
                            imgs.Images.RemoveAt(indexImage);
                            listView1.Items.RemoveByKey(localIP);
                            //listBox1.Items.Remove(localIP);
                            ipList.Remove(localIP);
                        }
                    }
                });
            }
        }
        
        public FileSharing()
        {
            InitializeComponent();
            pictureBox1.Image = img2; //assign image1 to picturebox here

            InitializeBackgroundWorker();
            sendWorker.WorkerReportsProgress = true;
            sendWorker.WorkerSupportsCancellation = true;

            recvWorker.WorkerReportsProgress = true;
            recvWorker.WorkerSupportsCancellation = true;
        }

        // Set up the BackgroundWorker object by attaching event handlers. 
        private void InitializeBackgroundWorker()
        {
            sendWorker.DoWork += new DoWorkEventHandler(sendWorker_DoWork);
            //sendWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(sendWorker_RunWorkerCompleted);

            recvWorker.DoWork += new DoWorkEventHandler(recvWorker_DoWork);
            //recvWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(recvWorker_RunWorkerCompleted);
        }

        // This event handler is where the actual, potentially time-consuming work is done.
        private void sendWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            SendMulticast();
        }

        // This event handler deals with the results of the background operation.
        /*private void sendWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
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

        // This event handler is where the actual, potentially time-consuming work is done.
        private void recvWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            RecvMulticast();
        }

        // This event handler deals with the results of the background operation.
        /*private void recvWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
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
            listView1.View = View.Details;
            listView1.Columns.Add("Users", 400);

            if (!sendWorker.IsBusy)//Check if the worker is already in progress
                sendWorker.RunWorkerAsync();//Call the background worker   
            if (!recvWorker.IsBusy)//Check if the worker is already in progress
                recvWorker.RunWorkerAsync();//Call the background worker 
        }

        private void pictureBox1_Click_1(object sender, EventArgs e)
        {
            if (pictureBox1.Image == img1)
            {
                //MODALITA' PUBBLICA (INVIARE E RICEVERE)
                label1.Text = "ONLINE";
                pictureBox1.Image = img2;

                if (!sendWorker.IsBusy)
                    sendWorker.RunWorkerAsync();
            }

            else
            {
                //MODALITA' PRIVATA (INVIARE E NON RICEVERE)
                label1.Text = "OFFLINE";
                pictureBox1.Image = img1;

                if(!sendWorker.IsBusy)
                    sendWorker.RunWorkerAsync();
            }
        }


        private void ricercaDispositiviToolStripMenuItem_Click_1(object sender, EventArgs e)
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}