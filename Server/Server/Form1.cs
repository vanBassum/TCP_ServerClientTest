using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MasterLibrary.Ethernet;
using MasterLibrary.Ethernet.DataPackages;
using MasterLibrary.Misc;
using MasterLibrary.Datasave.Serializers;
using System.IO;
using System.Threading;

namespace Server
{
    public partial class Form1 : Form
    {
        int maxClients = 10;
        TcpSocketListener serverSocket = new TcpSocketListener();
        ThreadedBindingList<Client> clients;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            clients = new ThreadedBindingList<Client>();
            listBox1.DataSource = clients;
            serverSocket.BeginListening(1000);
            serverSocket.OnClientAccept += ServerSocket_OnClientAccept;
            
        }

        private void ServerSocket_OnClientAccept(TcpSocketClient socket)
        {

            Client c = new Client(socket, clients.Count < maxClients);
            c.OnDisposed += C_Disposed;
            clients.Add(c);
        }

        private void C_Disposed(object sender, EventArgs e)
        {
            clients.Remove(sender as Client);
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            listBox1.Refresh();
        }
    }


    public class Client : PropertySensitive
    {
        private TcpSocketClient socket;
        private JSONIgnore serializer = new JSONIgnore();
        private static int nextID = 0;
        public int ID { get => GetPar<int>(); private set =>SetPar(value); }
        public string Username { get => GetPar<string>(); private set => SetPar(value); }

        public event EventHandler OnDisposed;
       

        public Client(TcpSocketClient sock, bool accept)
        {
            ID = Interlocked.Increment(ref nextID);
            
            socket = sock;
            socket.OnConnected += Socket_OnConnected;
            socket.OnConnectionFailed += Socket_OnConnectionFailed;
            socket.OnConnectionTimeout += Socket_OnConnectionTimeout;
            socket.OnDataRecieved += Socket_OnDataRecieved;
            socket.OnDisconnected += Socket_OnDisconnected;

            if(accept)
            {
                //Send his welcome and ID
                SendFrame(new SendID(ID));
            }
            else
            {
                //Send a connection decline and remove.

            }
        }

        public void SendFrame(Frame dataFrame)
        {
            socket.SendDataSync(serializer.Serialize(dataFrame));

        }

        void Dispose()
        {
            OnDisposed?.Invoke(this, null);
        }


        private void Socket_OnDisconnected(object sender, EventArgs e)
        {
            Dispose();
        }

        private void Socket_OnDataRecieved(object sender, byte[] data)
        {
            Frame f = serializer.Deserialize<Frame>(data);

            switch (f.CMD)
            {
                case Command.SendUsername:
                    Username = (f as SendUsername).Username;
                    break;
            }
        }

        private void Socket_OnConnectionTimeout(object sender, EventArgs e)
        {
            Dispose();
        }

        private void Socket_OnConnectionFailed(object sender, EventArgs e)
        {
            Dispose();
        }

        private void Socket_OnConnected(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "Client " + ID.ToString("###") + " " + Username;
        }
    }

}
