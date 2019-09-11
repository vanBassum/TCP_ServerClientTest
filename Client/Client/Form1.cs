using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using MasterLibrary.Misc;
using MasterLibrary.Ethernet;
using System.Reflection;

namespace Client
{
    public partial class Form1 : Form
    {
        TCP_Client<Client> con;

        public Form1()
        {
            InitializeComponent();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            con = new TCP_Client<Client>();
            listBox1.DataSource = con.otherClients;
            con.Connect();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            con.myClient.Username = textBox1.Text;
        }
    }


    

    public class Client : TCP_Object
    {
        public string Username { get => GetPar<string>(); set => SetPar(value); }

        public override string ToString()
        {
            return Username;
        }
    }

}
