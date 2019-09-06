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
using MasterLibrary.Ethernet.Frames;
using MasterLibrary.Misc;
using MasterLibrary.Datasave.Serializers;
using System.IO;
using System.Threading;

namespace Server
{
    public partial class Form1 : Form
    {
        int maxClients = 10;
        TcpSocketListener<TcpSocketClientEscaped> serverSocket = new TcpSocketListener<TcpSocketClientEscaped>();
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

        private void ServerSocket_OnClientAccept(TcpSocketClientEscaped socket)
        {
            Client client = new Client(socket, clients.Count < maxClients);
            client.Disposing += C_Disposed;
            client.RelayData += Client_RelayData;
            client.SendFrame(new SendClientList(client.ID, (from c in clients select c.ID).ToList()));
            //Let the others know this client has joined
            foreach (Client c in clients)
                c.SendFrame(new SendClientJoined(client.ID));

            clients.Add(client);
        }

        private void Client_RelayData(Client sender, IFrame frame)
        {
            //Let the others know this client has left
            foreach (Client cient in clients.Where(c => c != sender))
                cient.SendFrame(frame);
        }


        private void C_Disposed(object sender, EventArgs e)
        {
            Client client = sender as Client;
            clients.Remove(client);

            //Let the others know this client has left
            foreach (Client c in clients)
                c.SendFrame(new SendClientLeft(client.ID));
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            listBox1.Refresh();
        }
    }


    public class Client : AutoIncID
    {
        private TcpSocketClientEscaped socket;
        private JSONIgnore serializer = new JSONIgnore();

        public event EventHandler Disposing;                    //Is called before object is disposed!
        public event Action<Client, IFrame> RelayData;          //Is called whebn client wants this frame resend to all other clients.


        public Client(TcpSocketClientEscaped sock, bool accept)
        {           
            socket = sock;
            socket.OnConnectionFailed += Socket_OnConnectionFailed;
            socket.OnConnectionTimeout += Socket_OnConnectionTimeout;
            socket.OnPackageRecieved += Socket_OnPackageRecieved;
            socket.OnDisconnected += Socket_OnDisconnected;

            if(accept)
            {
                //Send his welcome and ID
                SendFrame(new SendId(ID));
            }
            else
            {
                //Send a connection decline and remove.
                SendFrame(new SendDecline(ID));
                Dispose();
            }
        }

        private void Socket_OnPackageRecieved(object sender, byte[] data)
        {
            IFrame f = serializer.Deserialize<IFrame>(data);

            if (f.Relay)
            {
                RelayData?.Invoke(this, f);
            }
        }

        public void SendFrame(IFrame dataFrame)
        {
            socket.SendPackage(serializer.Serialize(dataFrame));

        }

        void Dispose()
        {
            Disposing?.Invoke(this, null);
            //Dispose here VVV


        }


        private void Socket_OnDisconnected(object sender, EventArgs e)
        {
            Dispose();
        }

        private void Socket_OnConnectionTimeout(object sender, EventArgs e)
        {
            Dispose();
        }

        private void Socket_OnConnectionFailed(object sender, EventArgs e)
        {
            Dispose();
        }

        public override string ToString()
        {
            return "Client " + ID.ToString();
        }
    }

}
