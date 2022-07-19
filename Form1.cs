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
        public string clientUserName = null;
        public int clientID = 1;

        //public struct connectectedClient
        //{
        //    public string port;
        //    public int clientID;
        //    public string username;
        //}
        //public List<connectectedClient> connectectedClients = new List<connectectedClient>();



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
                    server = TCPChatServer.CreateInstance(port, ChatTextBox, LogoPicBox, ClientUsernameTextBox); //try build a TCPChatServer object
                    if(server == null)
                    {
                        // ERRORS!
                        // throw error to be caught by 'catch' block
                        throw new Exception("<< Incorrect Port Value >>"); // when thrown, it exits try block, starts ctach block
                    }

                    server.SetupServer();

                    // Indent Icon for connectivity
                    LogoPicBox.BorderStyle = BorderStyle.Fixed3D;
                    ClientUsernameTextBox.Text = "HOST";

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
                                                          ClientUsernameTextBox);

                    if(client == null)
                    {
                        //assume port issue
                        throw new Exception("<< Incorrect Port Value >>");
                    }

                    client.ConnectToServer();

                    // Indent Icon for connectivity
                    LogoPicBox.BorderStyle = BorderStyle.Fixed3D;


                   // clientID += 1; // increment for next user to  join
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
                TypeTextBox.Clear(); // clears previous message
            }
            else if (server != null) // if sender is the server
            {
                // ---------------------------------------------------------------------------- !mod & !mods commands
                string message = TypeTextBox.Text;
                if (TypeTextBox.Text.Contains("!mod "))
                {
                    // concatonate string,
                    string[] sub = message.Split(' ');
                    string clientToMod = sub[1];
                    bool clientFound = false;
                    bool clientDemoted = false;

                    // check name against connected clients
                    for(int i = 0; i < server.clientSockets.Count; ++i)
                    {
                        //TODO check if user is ALREADY a mod! if they are, demote them
                        if(clientToMod == server.clientSockets[i].clientUserName &&    // if user exists
                                          server.clientSockets[i].isModerator == true) // if they're already a mod
                        {
                            server.clientSockets[i].isModerator = false; // Demote client!
                            clientDemoted = true;
                            break; // leave loop, demoting is done
                        }
                        else if(clientToMod == server.clientSockets[i].clientUserName) // if user exists
                        {
                            // make that server a moderator
                            server.clientSockets[i].isModerator = true;
                            clientFound = true;
                            //break; // leave loop so client isn't immediaely demoted?
                        }
                    }

                    if(clientDemoted)
                    {
                        server.SendToAll("\n< " + clientToMod + " has been demoted as Moderator >", null); // notify others
                        server.AddToChat("\n< " + "Demoted " + clientToMod + " as Moderator >"); // notify self
                        clientDemoted = false; // reset for next run
                    }
                    else if (clientFound)
                    {
                        server.SendToAll("\n< " + clientToMod + " has been designated a Moderator >", null); // notify others
                        server.AddToChat("\n< " + "Designated " + clientToMod + " as Moderator >"); // notify self
                        clientFound = false; // reset for next run
                    }
                    else // no client by that username
                    {
                        server.AddToChat("\n" + "< No client by that name found >"); // notify self
                    }

                    clientToMod = ""; //reset for next run
                }
                else if(TypeTextBox.Text.Contains("!mods")) // ----------------------------- end !mod, start !mods command
                {
                    //create title for readability
                    server.AddToChat("\n\n" + "-- Moderators --");

                    string names = "";

                    // run through connected users, add to string seperated by empty space.
                    for (int i = 0; i < server.clientSockets.Count; ++i)
                    {
                        // if the client is listed as a moderator, add them to string
                        if (server.clientSockets[i].isModerator) 
                        names += " " + server.clientSockets[i].clientUserName;
                    }

                    //append string, store seperate names in an array
                    string[] allNames = names.Split(' ');

                    //loop through array and send their data to client window!
                    for (int i = 0; i < allNames.Length; ++i)
                    {
                        string temp = allNames[i];
                        if (temp == "")
                        {
                            //SKIP
                        }
                        else
                        {
                            server.AddToChat("\n" + "User: " + temp);
                        }
                    }

                    if (allNames.Length <= 1) // there's only an empty string within array
                    {
                        server.AddToChat("\n" + "...no current moderators...");
                    }
                }
                else // regular message ------------------------------------------------------------end !mods comands
                {
                    server.SendToAll("HOST: " + TypeTextBox.Text, null);
                }
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
