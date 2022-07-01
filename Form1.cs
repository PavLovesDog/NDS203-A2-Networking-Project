using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NDS_Networking_Project
{
    public partial class Form1 : Form
    {
        TCPChatServer server = null;
        TCPChatClient client = null;


        public Form1()
        {
            InitializeComponent();
        }

        private void HostServerButton_Click(object sender, EventArgs e)
        {
            HostPortTextBox.Text = "You Clicked Good!";
            string pef = HostPortTextBox.Text;
        }

        private void JoinServerButton_Click(object sender, EventArgs e)
        {

        }

        private void SendButton_Click(object sender, EventArgs e)
        {

        }
    }
}
