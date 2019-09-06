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

namespace Client
{
    public partial class Form1 : Form
    {

        Connector connector = new Connector();
        ThreadedBindingList<Message> messages;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Start();
            connector.Connect();
            messages = new ThreadedBindingList<Message>();
            connector.ObjectRecieved += Connector_ObjectRecieved;
            listBox1.DataSource = connector.OtherClients;
            listBox2.DataSource = messages;
        }

        private void Connector_ObjectRecieved(Client arg1, object arg2)
        {
            switch(arg2)
            {
                case Message msg:
                    messages.Add(msg);
                    break;
            }
            
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            connector.SendUpdates();
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            connector.MyClient.Username = textBox1.Text;
        }

        private void TextBox2_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                Message m = new Message();
                m.Msg = textBox2.Text;
                m.Name = connector.MyClient.Username;
                messages.Add(m);
                connector.SendObject(m);
                textBox2.Text = "";
            }
        }
    }

    public class Message
    {
        public string Msg { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Name + ": " + Msg;
        }
    }


}
