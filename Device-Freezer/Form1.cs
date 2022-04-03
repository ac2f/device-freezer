using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.IO;
using QRCoder;
using System.Runtime.InteropServices;
namespace Device_Freezer
{
    public partial class Form1 : Form
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern void BlockInput([In, MarshalAs(UnmanagedType.Bool)] bool fBlockIt);
        bool locked = true;
        IPHostEntry host;
        IPAddress ipAddress;
        IPEndPoint localEndPoint;
        Socket listener;
        public Form1()
        {
            InitializeComponent();
            OnLockedChange(true);
            host = Dns.GetHostEntry("localhost");
            ipAddress = host.AddressList[0];
            localEndPoint = new IPEndPoint(ipAddress, 11000);
            pictureBox1.Image = new QRCode(new QRCodeGenerator().CreateQrCode(GetLocalIPAddress() + ":" + localEndPoint.Port, QRCodeGenerator.ECCLevel.Q)).GetGraphic(20);
            try
            {
                listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // Create a Socket that will use Tcp protocol
                // A Socket must be associated with an endpoint using the Bind method
                listener.Bind(localEndPoint);
                // Specify how many requests a Socket can listen before it gives Server busy response.
                // We will listen 10 requests at a time
                listener.Listen(10000);
                Handler();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }
        public async void Handler()
        {
            while (true)
            {
                Socket handler = await listener.AcceptAsync();

                byte[] bytes = null;
                bool continueLoop = true;
                while (continueLoop)
                {
                    try
                    {
                        string data = null;
                        await Task.Delay(1);
                        bytes = new byte[1024];
                        var args = new SocketAsyncEventArgs();
                        args.SetBuffer(bytes, 0, bytes.Length);
                        int bytesRec = await handler.ReceiveAsync(new ArraySegment<byte>(bytes), SocketFlags.None);
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        if (data.IndexOf("<EXIT_0x0000>") > -1)
                        {

                        }
                        bool sec = true;
                        string validateString = "msf0b";
                        Dictionary<char, int> counter = new Dictionary<char, int>() { };
                        foreach (char item in validateString) { counter[item] = 0; }
                        for (int i = 0; i < data.Length; i++)
                        {
                            char item = data[i];
                            if (validateString.Contains(item))
                            {
                                counter[item]++;
                            }
                        }
                        foreach (char item in validateString) { if (counter[item] < 3) sec = false; }
                        if (data.StartsWith("%DF%") && sec && data.Split('|').Length > 1)
                        {
                            string paramsText = data.Split('|')[1];
                            Dictionary<string, string> parameters = new Dictionary<string, string>() { };
                            foreach (string param in paramsText.Split(';'))
                            {
                                try
                                {
                                    parameters[param.Split('=')[0]] = param.Split('=')[1].Trim();
                                }
                                catch (Exception err) { MessageBox.Show(err.Message); }
                            }
                            if (parameters.ContainsKey("u") && parameters.ContainsKey("p"))
                            {
                                if (parameters["u"] == "_I0gin" && (parameters["p"] == "_53CUR3P455W0R6" || parameters["p"] == "_"))
                                {
                                    this.Text = "LOGGED IN";
                                    OnLockedChange(false);
                                    Timeout(42);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                    continueLoop = false;
                    handler.Close();
                    handler = null;
                    await Task.Delay(1);
                }
            }
        }
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapter!");
        }
        public async void Timeout(int minutes)
        {
            int seconds = minutes * 60;
            while (seconds > 0)
            {
                seconds--;
                await Task.Delay(1000);
            }
            OnLockedChange(true);
        }
        public void OnLockedChange(bool lockDevice)
        {
            if (lockDevice)
            {
                this.Show();
                this.Size = new Size(100, 100);
            }
            else
            {
                this.Hide();
            }
            locked = lockDevice;
            BlockInput(locked);
            this.Text = locked ? "Kilitli" : "Kilitli Değil";
            this.Size = new System.Drawing.Size(1920, 1080);
            this.Location = new System.Drawing.Point(0, 0);
            this.TopMost = locked;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            OnLockedChange(!locked);
        }
        private void button2_Click(object sender, EventArgs e)
        {
            string toEncode = textBox2.Text;
            toEncode = Aes.EncryptionHelper.EncryptAndEncode(toEncode);
            textBox3.Text = toEncode;
        }
    }
}
