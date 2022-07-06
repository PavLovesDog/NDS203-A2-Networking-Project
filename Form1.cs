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
        string userName = null;


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
                    // Below Builds server with REFERENCES to chat box and logo pic for access
                    server = TCPChatServer.CreateInstance(port, ChatTextBox, LogoPicBox); //try build a TCPChatServer object
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
            if (CanHostOrJoin())
            {
                //TODO Input usernamer?
                ////ChatTextBox.Text = "\n--- Please Input Username  & click Send ---\n";
                ////SendButton_Click(sender, e);
                //////TODO HOW TO WAIT FOR USER INPUT?
                ////while(userName == null)
                ////{
                ////    if (userName != null) break;
                ////}

                try
                {
                    // check if ports are correct format..
                    int port = int.Parse(HostPortTextBox.Text);
                    int serverPort = int.Parse(ServerPortTextBox.Text);


                    // assigne details to the client connecting
                    client = TCPChatClient.CreateInstance(port, 
                                                          serverPort, 
                                                          ServerIPTextBox.Text, 
                                                          ChatTextBox,
                                                          LogoPicBox,
                                                          userName); // reference for logo pic ONLY EVER REFERENCES FIRST WINDOW??
                    if(client == null)
                    {
                        //assume port issue
                        throw new Exception("<< Incorrect Port Value >>");
                    }

                    client.ConnectToServer();

                    // Indent Icon for connectivity
                    LogoPicBox.BorderStyle = BorderStyle.Fixed3D;
                }
                catch(Exception ex)
                {
                    ChatTextBox.Text += "\nError: " + ex + "\n";
                }
            }
        }

        private void SendButton_Click(object sender, EventArgs e)
        {   
            ////if(client == null) // client has not joined yet, set username
            ////{
            ////    userName = TypeTextBox.Text;
            ////}

            if(client != null) // sender is a client
            {
                client.SendString(TypeTextBox.Text);
                TypeTextBox.Clear(); // clears previous message
            }
            else if (server != null) // if sender is the server
            {
                server.SendToAll(TypeTextBox.Text, null);
                TypeTextBox.Clear();
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
