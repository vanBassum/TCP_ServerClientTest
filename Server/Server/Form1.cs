using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using MasterLibrary.Controls;

namespace Server
{
    public partial class Form1 : Form
    {
        TCP_Server server = new TCP_Server();
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            server.StartListener();
            server.ClientJoined += Server_ClientJoined;
            server.ClientLeft += Server_ClientLeft;
            console1.AddCommand("GetOnlineClients", PrintNoClients);
            console1.AddCommand("SetMaxClients", SetMaxClients);
        }

        private void Server_ClientLeft()
        {
            console1.InvokeIfRequired(t => t.WriteLine("Client left " + server.ConnectedClients + "/" + server.MaxClients + " remaining."));
        }

        private void Server_ClientJoined()
        {
            console1.InvokeIfRequired(t => t.WriteLine("Client joined " + server.ConnectedClients + "/" + server.MaxClients + " remaining."));
        }


        public void PrintNoClients(string args)
        {
            console1.InvokeIfRequired(t => t.WriteLine(server.ConnectedClients + "/" + server.MaxClients + " online."));
        }

        public void SetMaxClients(string arg)
        {
            server.MaxClients = int.Parse(arg.Trim(' '));

            console1.InvokeIfRequired(t => t.WriteLine("MaxClients = " + server.MaxClients));
        }


    }


    

}
