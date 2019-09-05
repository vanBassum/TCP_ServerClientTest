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

namespace Client
{
    public partial class Form1 : Form
    {

        Connector client = new Connector();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            client.Username = "just 'a' name";
            client.Connect();
        }

        



    }

    public class Connector
    {
        private int myID;
        public string Username { get; set; } = "unnamed";
        private JSONIgnore serializer = new JSONIgnore();
        TcpSocketClient socket = new TcpSocketClient();

        public void Connect()
        {
            socket.BeginConnect("127.0.0.1", 1000, 1000);
            socket.SetTcpKeepAlive(true, 1000, 100);
            socket.OnDataRecieved += Socket_OnDataRecieved;
        }

        private void Socket_OnDataRecieved(object sender, byte[] data)
        {
            Frame f = serializer.Deserialize<Frame>(data);

            switch (f.CMD)
            {
                case Command.SendID:
                    myID = (f as SendID).ID;
                    SendFrame(new SendUsername(Username));
                    break;
                case Command.Decline:
                    //connection was declined by server

                    break;
            }



        }

        public void SendFrame(Frame dataFrame)
        {
            socket.SendDataSync(serializer.Serialize(dataFrame));

        }
    }

}
