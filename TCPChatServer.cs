using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Windows.Forms;

namespace NDS_Networking_Project
{
    /* 
     * the ":" symbol means this class inherits from the class named after it
     * in this case TCPChatServer inherits from TCPChatBase, meaning that 
     * TCPChatServer will have access to everything TCPChateBase has in it.
     */

    public class TCPChatServer : TCPChatBase
    {
        public static Form1 window = new Form1();

        // Socket to represent the server itself
        public Socket serverSocket = new Socket(AddressFamily.InterNetwork, 
                                                SocketType.Stream, 
                                                ProtocolType.Tcp);
        // Connected clients
        public List<ClientSocket> clientSockets = new List<ClientSocket>();

        // name of private message receiver... duh
        public string privateMsgReceiver = "";
        public string privateMsgSender = "";
        public string privateMessage = "";
        public bool isPrivateMessage = false;

        //Helper creator function
        public static TCPChatServer CreateInstance(int port, TextBox chatTextBox, PictureBox logo, TextBox usernameTextBox)
        {
            TCPChatServer tcp = null; // set to null, if it returns null, user did something wrong

            //setup if port within range & textbox not null
            if(port > 0 && port < 65535 && // port within range
               chatTextBox != null) // text box exists
            {
                tcp = new TCPChatServer();
                tcp.port = port;
                tcp.chatTextBox = chatTextBox;
                tcp.logoPicBox = logo;
                tcp.clientUsernameTextBox = usernameTextBox;
            }

            //retunr as null OR built server
            return tcp;
        }

        public void SetupServer()
        {
            chatTextBox.Text += "...setting up server..." + nl; // notify text box

            //bind socket to listen on (listen on what port for incoming messages?)
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            serverSocket.Listen(0); // listening for incoming connections/messages

            //start thread to read connecting clients, when connection happens, use AcceptCallBack function to deal with it
            serverSocket.BeginAccept(AcceptCallBack, this); // BeginAccept makes a thread, an asyncronus activity (if there are multiple, they work at their OWN pace)
            chatTextBox.Text += "<< Server Setup Complete >>" + nl;
        }

        // to close/diconnect all sockets at end of program
        public void CloseAllSockets()
        {
            // reference all ClientSockets in clientSockets LIST
            foreach(ClientSocket clientSocket in clientSockets)
            {
                clientSocket.socket.Shutdown(SocketShutdown.Both);
                clientSocket.socket.Close();
            }
            clientSockets.Clear(); // empty list
            serverSocket.Close(); // shut down its own socket
        }

        // callback called when a client joins the server
        public void AcceptCallBack(IAsyncResult AR)
        {
            //create socket
            Socket joiningSocket;

            // when client join, try get socket data of client (Port & IP info) 
            try
            {
                //retrieve socket info from connection join event
                joiningSocket = serverSocket.EndAccept(AR);
            }
            catch(ObjectDisposedException)
            {
                chatTextBox.Text += "...Client Join Failed...";
                return; // bail on error...
            }

            // build wrapper arond client who just joined
            ClientSocket newClientSocket = new ClientSocket();
            newClientSocket.socket = joiningSocket;

            clientSockets.Add(newClientSocket); // add new client socket to list

            //start a thread so we can send and receive data between us and the new client
            joiningSocket.BeginReceive(newClientSocket.buffer,    // add buffer
                                       0,                         // set socket flags, if needed
                                       ClientSocket.BUFFER_SIZE,  // how big chunks of data can be
                                       SocketFlags.None,          // add flags, if any
                                       ReceiveCallBack,           // function to call when data comes in
                                       newClientSocket);          // object associated with this socket

            // notify text box
            AddToChat(nl + "<< Client Connected >>"); 

            // This wait for new client thread done, so start another thread to get a new client :)
            serverSocket.BeginAccept(AcceptCallBack, null);
        }

        // this function calls anytime data comes in from a client
        public void ReceiveCallBack(IAsyncResult AR)
        {
            // get ClientSocket object from 'IAsyncResult AR' so we can deal with individual client data
            ClientSocket currentClientSocket = (ClientSocket)AR.AsyncState; // cast 'AR.AsyncState' as '(ClientSocket)' to access

            // how many bytes of data received
            int received;

            try
            {
                received = currentClientSocket.socket.EndReceive(AR); // find byte size
            }
            catch(SocketException ex)
            {
                AddToChat("Error: " + ex.Message + nl + nl);
                AddToChat("! Error Occured !" + nl + "<< Client Disconnected >>");
                currentClientSocket.socket.Close(); // shut it down
                clientSockets.Remove(currentClientSocket); // remove from list
                return; // leave function
            }

            //build array ready for data
            byte[] recBuf = new byte[received];
            Array.Copy(currentClientSocket.buffer, recBuf, received); // copy info into array
            //convert received byte data into string
            string text = Encoding.ASCII.GetString(recBuf);

            AddToChat(text); //TODO this paints er'rything on the server window

            //Check for Username specific commands from clients and adjust data accordingly
            string setupUserName = "";
            string changeNameUserName = "";
            string magicQuestion = "";
            
            if (text.Contains("!username ")) // setting name
            {
                // create string to hold the username data
                setupUserName = text.Remove(0, 10);

                //append text so it triggers command
                text = text.Remove(9, text.Length - 9);
            }
            else if (text.Contains("!user ")) // changing name
            {
                changeNameUserName = text.Remove(0, 6);
                text = text.Remove(5, text.Length - 5);
            }
            else if (text.Contains("!whisper ")) // private messaging
            {
                string[] subStrings = text.Split(' ');
                privateMsgReceiver = subStrings[1];

                int messageIndex = (9 + privateMsgReceiver.Length);
                privateMessage = text.Substring(messageIndex, (text.Length - messageIndex));

                text = subStrings[0]; // revert text to command only

                //TODO WORKS!
                //privateMsgReceiver = text.Substring(9); // isolate username
                //privateMsgSender = currentClientSocket.clientUserName;
                //text = text.Remove(8, text.Length - 8); // re-create command string
            }
            else if(text.Contains("!magic "))
            {
                string[] subStrings = text.Split(' '); // split

                int messageIndex = (7 + privateMsgReceiver.Length);
                magicQuestion = text.Substring(messageIndex, (text.Length - messageIndex));

                text = subStrings[0]; // revert text to command only
            }

            if (text.ToLower() == "!commands")
            {
                byte[] data = Encoding.ASCII.GetBytes(nl + "<< COMMANDS >>" +
                                                      nl + "!commands   --> see a list of commands" +
                                                      nl + "!username [new_username]   --> set yourself a new username" +
                                                      nl + "!user   --> change your current username" +
                                                      nl + "!about   --> see details of the program" +
                                                      nl + "!who   --> see who is in the chat" +
                                                      nl + "!whisper [username]   --> select user for private message" +
                                                      nl + "!magic [question]   --> ask the Magic-8-Ball a question, reap its wisdom" +
                                                      nl + "!exit   --> disconnect from the server");
                currentClientSocket.socket.Send(data); // send straight back to person who sent in data
                AddToChat("\n...commands sent to client...");//TODO CHANGE THIS ?
            }
            else if (text.ToLower() == "!about")
            {
                string IP = serverSocket.LocalEndPoint.ToString(); // LOCAL is the server host

                //Append strings
                string appendedPort = IP.Replace("0.0.0.0:", "");
                string appendedIP = IP.Replace(":6666", "");

                byte[] data = Encoding.ASCII.GetBytes(nl + "Created by Matthew Carr & Charles Bird" +
                                                      nl + "to ensure people have a ways to communicate in style." +
                                                      nl + nl + "IP address: " + appendedIP +
                                                      nl + "Port number: " + appendedPort +
                                                      nl + nl + "Netwoes INC. Copyright (c) All Rights Reserved, TM (2022)");
                currentClientSocket.socket.Send(data);

            }
            else if (text.ToLower() == "!who")
            {
                //TODO When server receives this message, it sends back messages containing the names of the
                //TODO connected users to the client to be output to the chat window

                //create byte array to store names
                byte[] data = Encoding.ASCII.GetBytes(nl + "-- Connected Users --");
                currentClientSocket.socket.Send(data);

                string names = "";

                // run through connected users, add to string seperated by empty space.
                for (int i = 0; i < clientSockets.Count; ++i)
                {
                    names += " " + clientSockets[i].clientUserName;
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
                        data = Encoding.ASCII.GetBytes(nl + "User: " + temp);
                        currentClientSocket.socket.Send(data);
                    }
                }

                if (allNames[0] == "" && allNames[1] == "") // you're all alone here..
                {
                    data = Encoding.ASCII.GetBytes(nl + "...it's just you here..." +
                                                   nl + " *tumbleweed blows by*");
                    currentClientSocket.socket.Send(data);
                }
            }
            else if (text.ToLower() == "!whisper")
            {
                //int messageIndex = (9 + privateMsgReceiver.Length);
                //string sub = text.Substring(messageIndex, text.Length - messageIndex);


                byte[] data = Encoding.ASCII.GetBytes(nl + "<Private Message> " + currentClientSocket.clientUserName +
                                                      ":" + privateMessage);

                for (int i = 0; i < clientSockets.Count; ++i)
                {
                    if (clientSockets[i].clientUserName == privateMsgReceiver)
                    {
                        clientSockets[i].socket.Send(data); // send to the reciever!
                    }
                }

                //reset strings for other !whisper
                privateMsgReceiver = "";
                privateMessage = "";

                ////loop through clients, match their username
                //bool isFound = false;
                //for(int i =0; i < clientSockets.Count; ++i)
                //{
                //    if(clientSockets[i].clientUserName == privateMsgReceiver)
                //    {
                //        isFound = true;
                //    }
                //}
                //
                //if(isFound)
                //{
                //    byte[] data = Encoding.ASCII.GetBytes(nl + "< Next message will only be sent to " + privateMsgReceiver + " >" +
                //                                          nl + "Type your message: ");
                //    currentClientSocket.socket.Send(data);
                //    isPrivateMessage = true;
                //}
                //else // no client exists
                //{
                //    byte[] data = Encoding.ASCII.GetBytes(nl + "< User " + privateMsgReceiver + " does not exist >");
                //    currentClientSocket.socket.Send(data);
                //    privateMsgReceiver = ""; // clear string
                //}

            }
            else if (text.ToLower() == "!username") // concatonate or whatever the first half.
            {
                /*
                    string IP = currentClientSocket.socket.LocalEndPoint.ToString(); // LOCAL is the server host
                    string port = currentClientSocket.socket.RemoteEndPoint.ToString(); // REMOTE is the connected user

                    //Append string to remove IPaddress "127.0.0.1:" from front to leave only port
                    //port.Replace("127.0.0.1:", "");
                    string appendedPort = port.Remove(0, 10); // remove IPAddress
                    //port.Remove(0, 10); // remove IPAddress
                */

                byte[] data = Encoding.ASCII.GetBytes(" "); // create empty byte array
                bool getKicked = false;

                //set username------
                // run through connected users
                for (int i = 0; i < clientSockets.Count; ++i)
                {
                    // if the name is taken
                    if (clientSockets[i].clientUserName == setupUserName)
                    {
                        //TODO  - tell them it's been taken
                        data = Encoding.ASCII.GetBytes(nl + "!!! Username: " + setupUserName + " is already taken !!!" +
                                                       nl + "<< Disconnecting User >>");
                        currentClientSocket.socket.Send(data); // send it back

                        //TODO  - boot them
                        getKicked = true;
                        break;

                    }
                    else if (clientSockets[i].clientUserName == null)
                    {
                        currentClientSocket.clientUserName = setupUserName;

                        // Send data to update display box for client Usernames
                        data = Encoding.ASCII.GetBytes("!displayusername " + setupUserName);
                        currentClientSocket.socket.Send(data);

                        // change data to represent success
                        data = Encoding.ASCII.GetBytes(nl + "<< Success >>" +
                                                       nl + "Your New Username: " + setupUserName + nl +
                                                       nl + "Welcome to chat " + "'" + setupUserName + "'");
                    }
                }

                // Notify
                if (getKicked)
                {
                    data = Encoding.ASCII.GetBytes("!kick"); // command for client to get disconnect 
                    currentClientSocket.socket.Send(data); // send it back
                    clientSockets.Remove(currentClientSocket);
                    getKicked = false; // reset bool
                    return; // bail early
                }
                else
                {
                    SendToAll(nl + "<< " + currentClientSocket.clientUserName + " has joined the chat >>", currentClientSocket);
                    currentClientSocket.socket.Send(data); // send it back
                    // notify all you're here!
                }

            }
            else if (text.ToLower() == "!user")
            {
                byte[] data = Encoding.ASCII.GetBytes(" "); // create empty byte array

                // try change username!
                string temp = currentClientSocket.clientUserName;
                bool canChangeName = false;

                // roll through list
                for (int i = 0; i < clientSockets.Count; ++i)
                {
                    if (clientSockets[i].clientUserName != changeNameUserName)
                    {
                        //change name!
                        canChangeName = true;
                    }
                    else
                    {
                        data = Encoding.ASCII.GetBytes(nl + "< Cannot Change Username >" +
                                                        nl + "User: " + changeNameUserName + " already exists.");
                        canChangeName = false;
                        break;
                    }
                }

                if (canChangeName)
                {
                    currentClientSocket.clientUserName = changeNameUserName;
                    // Send data to update display box for client Usernames
                    data = Encoding.ASCII.GetBytes("!displayusername " + changeNameUserName);
                    currentClientSocket.socket.Send(data);

                    //Notify others of thy success
                    SendToAll(nl + "..." + temp + " has transformed..." + nl +
                              "They shall now be know as: '" + currentClientSocket.clientUserName + "'" + nl,
                              currentClientSocket);
                }
                else
                {
                    currentClientSocket.socket.Send(data); // send denial data
                }


            }
            else if (text.ToLower() == "!magic")
            {
                string[] phrases = { "It is certain.", "It is decidedly so.", "Without a doubt.", "Yes, definitely.", "You may rely on it.",
                                     "As I see it, yes.", "Most likely.", "Outlook looks good.", "Yes.", "Signs point to yes.",
                                     "Reply hazy, try again.", "Ask again later.", "Better not tell you now.", "Cannot predict now.", "Concentrate and ask again.",
                                     "Don't count on it.", "My reply is no.", "My sources say no.", "Outlook looks bleak.", "Very doubtful.",};
                
                // choose a phrase at random,
                Random rnd = new Random();
                int index = rnd.Next(0, 19);

                //send the question and answer back to client who asked question (Maybe to all??)
                byte[] data = Encoding.ASCII.GetBytes(nl + "Your Question --->" + magicQuestion + 
                                                      nl + "My Divine Answer --->" + phrases[index]);
                //concoct message based on answer
                string messageToAll = "";
                if(index >= 0 && index <= 9) // positive message
                {
                    messageToAll = nl + currentClientSocket.clientUserName + " has asked the Magic 8 Ball a question!" + nl + "...things are in their favour...";
                }
                else if(index >= 10 && index <= 14) // uncertain message
                {
                    messageToAll = nl + currentClientSocket.clientUserName + " has asked the Magic 8 Ball a question!" + nl + "...things are questionable...";
                }
                else if(index >= 15 && index <= 19) // Negative message
                {
                    messageToAll = nl + currentClientSocket.clientUserName + " has asked the Magic 8 Ball a question!" + nl + "...misfortune befalls them...";
                }

                
                SendToAll(messageToAll, currentClientSocket); //Let the chat know of the askers fortunes
                currentClientSocket.socket.Send(data); // reply to client
            }
            else if (text.ToLower() == "!exit") // client wants to exit gracefully...
            {
                byte[] data = Encoding.ASCII.GetBytes("!exit");
                currentClientSocket.socket.Send(data); // send back data to change IDENT

                // notify all they're leaving!
                SendToAll(nl + "<< " + currentClientSocket.clientUserName + " has left the chat >>", currentClientSocket);

                currentClientSocket.socket.Shutdown(SocketShutdown.Both); // shutdown server-side for client
                currentClientSocket.socket.Close();
                clientSockets.Remove(currentClientSocket);
                AddToChat("<< Client Disconnected >>");

                if (text.ToLower() == "!exit" && clientSockets.Count <= 0) // all clients disconnected
                {
                    IndentIcon(); // change server Icon Identation
                }

                return; // bail early, rest of function not useful for !exit
            }
            else // no command, REGULAR CHAT MESSAGE
            {
                ////check for private messages going out && if it was the original !whisper caller
                //if(isPrivateMessage &&
                //   privateMsgSender == currentClientSocket.clientUserName)
                //{
                //    byte[] data = Encoding.ASCII.GetBytes("<Private Message> " + currentClientSocket.clientUserName + ": " + text);
                //
                //    //loop through clients, match their username
                //    for(int i = 0; i < clientSockets.Count; ++i)
                //    {
                //        if (clientSockets[i].clientUserName == privateMsgReceiver)
                //        {
                //            clientSockets[i].socket.Send(data); // send data to specific client
                //        }
                //    }
                //
                //    // reset bool and clear msg receiver/sender string for another whisper moment
                //    isPrivateMessage = false;
                //    privateMsgReceiver = "";
                //    privateMsgSender = "";
                //}
                //else // regular ole message
                {
                    SendToAll(currentClientSocket.clientUserName + ": " + text, currentClientSocket); //TODO does not send to self though...?
                }

            }

            // now data has been received from this client, the thread is finished...
            // so start a new thread to receive new data!
            currentClientSocket.socket.BeginReceive(currentClientSocket.buffer,
                                                    0,                         
                                                    ClientSocket.BUFFER_SIZE,  
                                                    SocketFlags.None,          
                                                    ReceiveCallBack,           
                                                    currentClientSocket);     
        }

        // function for server to send messages out to all clients
        // i.e 'from' says "Hello", server broadcasts to the other clients
        public void SendToAll(string str, ClientSocket from)
        {
            //Send to all clients EXECPT the 'from' client
            foreach(ClientSocket clientSocket in clientSockets)
            {
                if(from == null || !from.socket.Equals(clientSocket)) // if there is NO from person, also NOT the original sender
                {
                    byte[] data = Encoding.ASCII.GetBytes(str); // convert string to a byte array
                    clientSocket.socket.Send(data); // then send it
                }
            }
        }
    }
}
