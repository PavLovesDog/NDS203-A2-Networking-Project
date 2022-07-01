using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace NDS_Networking_Project
{
    public class TCPChatClient : TCPChatBase
    {
        // socket for client
        public Socket socket = new Socket(AddressFamily.InterNetwork,
                                          SocketType.Stream,
                                          ProtocolType.Tcp);

        public ClientSocket clientSocket = new ClientSocket();

        //info to connect to server
        public int serverPort; // Port number
        public string serverIP; // IP address

        // helper creator static function
        public static TCPChatClient CreateInstance(int port, int serverPort, string serverIP, TextBox chatTextBox)
        {
            TCPChatClient tcp = null;

            //check ports are open and in range
            if (port > 0 && port < 65535 &&
                serverPort > 0 && serverPort < 65535 &&
                serverIP.Length < 0 && chatTextBox != null) //TODO serverIP.Length > 0 ?? should be NOT empty??
            {
                tcp = new TCPChatClient();
                tcp.port = port;
                tcp.serverPort = serverPort;
                tcp.serverIP = serverIP;
                tcp.chatTextBox = chatTextBox;
                tcp.clientSocket.socket = tcp.socket;
            }
            return tcp;
        }

        public void ConnectToServer()
        {
            //connection attempts per user
            int attempts = 0;

            //while socket is not connected to server
            while(!socket.Connected)
            {
                try
                {
                    attempts++;
                    SetChat("Connection Attempt: " + attempts);
                    socket.Connect(serverIP, serverPort);
                } 
                catch(Exception ex)
                {
                    chatTextBox.Text += "\nError: " + ex.Message + "\n";
                }
            }
            AddToChat("<< Connected >>");

            //start thread for receeiving data from the server
            clientSocket.socket.BeginReceive(clientSocket.buffer, 
                                             0, 
                                             ClientSocket.BUFFER_SIZE, 
                                             SocketFlags.None, 
                                             ReceiveCallBack, 
                                             clientSocket);
        }
        
        //Everytime a bit of data comes in from server, this function reads it
        public void ReceiveCallBack(IAsyncResult AR)
        {
            //get our client socket from AR
            ClientSocket currentClientSocket = (ClientSocket)AR.AsyncState;

            //How many bytes of data received
            int received;
            try
            {
                received = currentClientSocket.socket.EndReceive(AR);
            }
            catch(SocketException ex)
            {
                AddToChat("\nError: " + ex.Message);
                AddToChat("\n << Disconnecting >>");
                currentClientSocket.socket.Close();
                return;
            }

            //build array ready for data
            byte[] recBuf = new byte[received];
            Array.Copy(currentClientSocket.buffer, recBuf, received); // copy info into array
            //convert received byte data into string
            string text = Encoding.ASCII.GetString(recBuf);

            //any data at this point from server is likely chat message, so put in text box.
            //TODO NOTE Assignment 2 will also send OTHER types of data, so don't auto dump to chat right away
            AddToChat(text); // change this for A2...

            //start thread for receeiving data from the server
            currentClientSocket.socket.BeginReceive(currentClientSocket.buffer,
                                             0,
                                             ClientSocket.BUFFER_SIZE,
                                             SocketFlags.None,
                                             ReceiveCallBack,
                                             currentClientSocket);
        }

        // Sends string to server
        public void SendString(string text)
        {
            //Send data to the server
            byte[] buffer = Encoding.ASCII.GetBytes(text); // encode data
            socket.Send(buffer, 0, buffer.Length, SocketFlags.None); // send encoded data
        }

        // shut down, no longer listening to server
        public void Close()
        {
            socket.Close();
        }
    }
}
