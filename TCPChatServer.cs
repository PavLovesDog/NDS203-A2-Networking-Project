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
        // Socket to represent the server itself
        public Socket serverSocket = new Socket(AddressFamily.InterNetwork, 
                                                SocketType.Stream, 
                                                ProtocolType.Tcp);
        // Connected clients
        public List<ClientSocket> clientSockets = new List<ClientSocket>();

        //Helper creator function
        public static TCPChatServer CreateInstance(int port, TextBox chatTextBox)
        {
            TCPChatServer tcp = null; // set to null, if it returns null, user did something wrong

            //setup if port within range & textbox not null
            if(port > 0 && port < 65535 && // port within range
               chatTextBox != null) // text box exists
            {
                tcp = new TCPChatServer();
                tcp.port = port;
                tcp.chatTextBox = chatTextBox;
            }

            //retunr as null OR built server
            return tcp;
        }

        public void SetupServer()
        {
            chatTextBox.Text += "Setting up server... \n"; // notify text box

            //bind socket to listen on (listen on what port for incoming messages?)
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            serverSocket.Listen(0); // listening for incoming connections/messages

            //start thread to read connecting clients, when connection happens, use AcceptCallBack function to deal with it
            serverSocket.BeginAccept(AcceptCallBack, this); // BeginAccept makes a thread, an asyncronus activity (if there are multiple, they work at their OWN pace)
            chatTextBox.Text += "Server setup complete.\n";
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
                chatTextBox.Text += "Client join failed...";
                return; // bail on error...
            }

            // build wrapper arond client wjo just joined
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
            AddToChat("Client Connected, ready to receive data...");

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
                AddToChat("Error: " + ex.Message);
                AddToChat("! Error Occured !\n...disconnecting client...");
                currentClientSocket.socket.Close(); // shut it down
                clientSockets.Remove(currentClientSocket); // remove from list
                return; // leave function
            }

            //build array ready for data
            byte[] recBuf = new byte[received];
            Array.Copy(currentClientSocket.buffer, recBuf, received); // copy info into array
            //convert received byte data into string
            string text = Encoding.ASCII.GetString(recBuf);

            AddToChat(text);

            //TODO Check for commands from Users first
            if(text.ToLower() == "!commands")
            {
                byte[] data = Encoding.ASCII.GetBytes("Commands are; \n\n!commands\n!about\n!who\n!whisper\n!exit");
                currentClientSocket.socket.Send(data); // send straight back to person who sent in data
                AddToChat("<< Commands sent to client >> " + currentClientSocket.socket.SocketType);//TODO CHANGE THIS
            }
            else if (text.ToLower() == "!about")
            {
                //TODO FILL me in...
            }
            else if (text.ToLower() == "!who")
            {
                //TODO fill ME in...
            }
            else if (text.ToLower() == "!whisper")
            {
                //TODO fill me IN...
            }
            else if (text.ToLower() == "!exit") // client wants to exit gracefully...
            {
                //TODO FILL ME IN....
                currentClientSocket.socket.Shutdown(SocketShutdown.Both); // shutdown server-side for client
                currentClientSocket.socket.Close();
                clientSockets.Remove(currentClientSocket);
                AddToChat("<< Client Disconnected >>");
                return; // bail early, rest of function not useful for !exit
            }
            else // no cammond, REGULAR CHAT MESSAGE
            {
                // must be normal chat message form client, so send to all other clients
                SendToAll(text, currentClientSocket); // does not send to self though...
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
