using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets; // to access the .NET core socket

namespace NDS_Networking_Project
{
    // Represents a client
    public class ClientSocket
    {
        /*
         Users currently do not have usernames. When a user joins the server, they should also send their
         proposed username, they should send this data in the form of !username [new_username] e.g
         !username Bob. When the server receives this message, it needs to see if this name is in use. If no
         other user is using this name, then let the user know it was a success. If it is already in use, send a
         message to the user to tell them it failed. If the user gets a failed message, they should be
         disconnected on both client and server side.
        */
        //TODO add username & other client specific data
        public string clientUserName;

        public Socket socket; //port and IP address
        public const int BUFFER_SIZE = 2048;
        public byte[] buffer = new byte[BUFFER_SIZE]; //byte aray like a string, data we want to send and receive

    }
}
