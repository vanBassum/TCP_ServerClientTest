using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using MasterLibrary.Ethernet.Frames;
using MasterLibrary.Misc;
using System.Collections.Generic;
using System.ComponentModel;
using MasterLibrary.Datasave.Serializers;
using MasterLibrary.Ethernet;
using System.Reflection;

namespace Client
{
    public partial class Form1 : Form
    {


        public Form1()
        {
            InitializeComponent();

        }

        private void Form1_Load(object sender, EventArgs e)
        {


        }

        private void Button1_Click(object sender, EventArgs e)
        {

        }
    }

    public class Client : PropertySensitive
    {
        public string Username { get => GetPar<string>(); set => SetPar(value); }

        public override string ToString()
        {
            return Username;
        }
    }

    public class Connector<T>
    {
        public T myClient = Activator.CreateInstance<T>();
        public Dictionary<int, T> otherClients = new Dictionary<int, T>();

        private int myId;
        private JSONIgnore serializer = new JSONIgnore();
        TcpSocketClientEscaped socket = new TcpSocketClientEscaped();
        private Timer sendChangesTimer = new Timer();
        private Dictionary<string, object> changedPars = new Dictionary<string, object>();

        public Connector()
        {
            sendChangesTimer.Interval = 500;
            sendChangesTimer.Tick += SendChangesTimer_Tick;
            ((PropertySensitive)myClient).PropertyChanged
        }

        public void Connect()
        {
            socket.BeginConnect("127.0.0.1", 1000, 1000);
            socket.SetTcpKeepAlive(true, 1000, 100);
            socket.OnPackageRecieved += Socket_OnPackageRecieved;
        }

        private void Socket_OnPackageRecieved(object sender, byte[] data)
        {
            IFrame f = serializer.Deserialize<IFrame>(data);

            switch (f)
            {
                case SendId frame:
                    //Yaay we got an ID from the server.
                    myId = frame.ClientID;
                    sendChangesTimer.Start();
                    break;

                case SendClientJoined frame:
                    //Someone joined the party.
                    otherClients[frame.ClientID] = Activator.CreateInstance<T>();
                    break;

                case SendClientList frame:
                    //Create clients, if already existed overwrite
                    foreach(int id in frame.Clients)
                        otherClients[id] = Activator.CreateInstance<T>();
                    break;

                case SendClientLeft frame:
                    otherClients.Remove(frame.ClientID);
                    break;

                case SendParameterUpdate frame:
                    foreach (KeyValuePair<string, object> kvp in frame.Parameters)
                        typeof(T).GetProperty(kvp.Key).SetValue(otherClients[frame.ClientID], kvp.Value);
                    break;

                default:
                    throw new NotImplementedException();
                    break;

            }
        }

        private void SendChangesTimer_Tick(object sender, EventArgs e)
        {
            //SendFrame(new SendParameterUpdate(ID, GetChangedPars()));
        }

        private void SendFrame(IFrame dataFrame)
        {
            socket.SendPackage(serializer.Serialize(dataFrame));
        }

    }

    

    /*
    
    public class Sync : PropertySensitive
    {
        //Keep a list of changed parameters, so we can send them periodically
        private Dictionary<string, object> changedPars = new Dictionary<string, object>();
        private object SendParLock = new object();

        public Sync()
        {
            if (this.GetType() == typeof(MyClient))
                this.PropertyChanged += Client_PropertyChanged;
        }

        private void Client_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            changedPars[e.PropertyName] = this.GetType().GetProperty(e.PropertyName).GetValue(this);
        }

        public Dictionary<string, object> GetChangedPars()
        {
            Dictionary<string, object> cpy = new Dictionary<string, object>();
            lock (SendParLock)
            {
                foreach (KeyValuePair<string, object> kvp in changedPars)
                    cpy[kvp.Key] = kvp.Value;
                changedPars.Clear();
            }
            return cpy;
        }
    }

    */
}
