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
            //HostPortTextBox.Text = "You Clicked Good!";
            //string pef = HostPortTextBox.Text;

            if(CanHostOrJoin())
            {
                try
                {
                    // pass in text(as string) from host port text box to try convert to INT
                    int port = int.Parse(HostPortTextBox.Text);
                    server = TCPChatServer.CreateInstance(port, ChatTextBox); //try build a TCPChatServer object
                    if(server == null)
                    {
                        // ERRORS!
                        // throw error to be caught by 'catch' block
                        throw new Exception("<< Incorrect Port Value >>"); // when thrown, it exits try block, starts ctach block
                    }

                    server.SetupServer();

                }
                catch(Exception ex) // if chars other than numbers passed in...
                {
                    ChatTextBox.Text += "\nError: " + ex + "\n";
                }
            }
        }

        private void JoinServerButton_Click(object sender, EventArgs e)
        {
            if(CanHostOrJoin())
            {
                try
                {
                    // check if ports are correct format..
                    int port = int.Parse(HostPortTextBox.Text);
                    int serverPort = int.Parse(ServerPortTextBox.Text);

                    // assigne details to the client connecting
                    client = TCPChatClient.CreateInstance(port, 
                                                          serverPort, 
                                                          ServerIPTextBox.Text, 
                                                          ChatTextBox);
                    if(client == null)
                    {
                        //assume port issue
                        throw new Exception("<< Incorrect Port Value >>");
                    }

                    client.ConnectToServer();
                }
                catch(Exception ex)
                {
                    ChatTextBox.Text += "\nError: " + ex + "\n";
                }
            }
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            if(client != null) // sender is a client
            {
                client.SendString(TypeTextBox.Text);
            }
            else if (server != null) // if sender is the server
            {
                server.SendToAll(TypeTextBox.Text, null);
            }
        }

        public bool CanHostOrJoin()
        {
            if(server == null && client == null) //no server/client existt yet, can host/join
            {
                return true;
            }
            else // you're a client or server already
            {
                return false;
            }
        }
    }
}
