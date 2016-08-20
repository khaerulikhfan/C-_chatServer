using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.Sockets;
using System.Collections;

namespace chat_server
{
    class ChatClient
    {
        //—berisi daftar semua client—
        public static Hashtable AllClients = new Hashtable();
        //—informasi tentang client—
        private TcpClient _client;
        private string _clientIP;
        private string _clientNick;
        //—digunakan untuk mengirim/menerima data—
        private byte[] data;
        //—apakah nickname sudah dikirim—
        private bool ReceiveNick = true;
        public ChatClient(TcpClient client){
            _client = client;
            //—mendapatkan client IP address—
            _clientIP = client.Client.RemoteEndPoint.ToString();
            //—menambahkan client saat ini ke hash table—
            AllClients.Add(_clientIP, this);
            //—memulai membaca data dari client pada
            // thread terpisah—
            data = new byte[_client.ReceiveBufferSize];
            client.GetStream().BeginRead(data, 0,
            System.Convert.ToInt32(_client.ReceiveBufferSize), ReceiveMessage, null);
        }
        public void ReceiveMessage(IAsyncResult ar){
            //—membaca dari client—
            int bytesRead;
            try{
                lock (_client.GetStream()){
                    bytesRead = _client.GetStream().EndRead(ar);
                }
                //—client tidak tersambung—
                if (bytesRead < 1){
                    AllClients.Remove(_clientIP);
                    Broadcast(_clientNick + " tidak terhubung dengan server");
                    return;
                }else{
                    //—mendapat pesan yang telah dikirim—
                    string messageReceived = System.Text.Encoding.ASCII.GetString(data, 0, bytesRead);
                    //—client mengirimkan nicknamenya—
                    if (ReceiveNick){
                        _clientNick = messageReceived;
                        //—memberitahu semua client memasuki
                        // chat—
                        Broadcast(_clientNick + " terhubung dengan server");
                        ReceiveNick = false;
                    }else{
                        //—memberikan pesan pada semuanya—
                        Broadcast(_clientNick + ">" + messageReceived);
                        }
                 }
                 //—melanjutkan membaca dari client—
                 lock (_client.GetStream()){
                        _client.GetStream().BeginRead(data, 0,
                        System.Convert.ToInt32(_client.ReceiveBufferSize),ReceiveMessage,null);
                 }
                }
                catch (Exception ex){
                    AllClients.Remove(_clientIP);
                    Broadcast(_clientNick + " tidak terhubung dengan server");
                }
        }
        public void SendMessage(string message){
            try{
                //—mengirim text—
                System.Net.Sockets.NetworkStream ns;
                lock (_client.GetStream()){
                    ns = _client.GetStream();
                }
             byte[] bytesToSend =
             System.Text.Encoding.ASCII.GetBytes(message);
             ns.Write(bytesToSend, 0, bytesToSend.Length);
             ns.Flush();
            }
            catch (Exception ex){
                Console.WriteLine(ex.ToString());
            }
        }
        public void Broadcast(string message){
            //—log secara lokal—
            Console.WriteLine(message);
            foreach (DictionaryEntry c in AllClients){
                //—memberi pesan pada semua user—
                ((ChatClient)(c.Value)).SendMessage(
                message + Environment.NewLine);
            }
        }
    }
}
