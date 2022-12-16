using System;
using System.Text;
using SimpleTCP;
using SharpConfig;
using System.Net;

class Program
{
    public static SimpleTcpClient client = null;
    public static Configuration config = null;
    public static int serverport = 2233;
    public static string serverip = "127.0.0.1";
    public static string myip = "127.0.0.1";
    

    public static void Readconfig()
    {
        config = Configuration.LoadFromFile("client.cfg");

        //Web 部分读取
        var section = config["Web"];
        serverport = section["server_port"].IntValue;
        serverip = section["server_ip"].StringValue;
    }
    /*
     * cfg文件示例：
     * 首先名字必须是 client.cfg。
     * [Web]
     * server_port=<服务器端口>
     * server_ip=<服务器IP>
     */

    public static void onDataReceived(object sender, Message msg)
    {
        //字节数组
        //Console.WriteLine("Data:" + BitConverter.ToString(msg.Data));
        //字符串消息
        Console.WriteLine(msg.MessageString);
    }

    //获取本地的IP地址
    public static string GetLocalIp()
    {
        
        string AddressIP = string.Empty;
        foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
            {
                AddressIP = _IPAddress.ToString();
            }
        }
        return AddressIP;
    }

    public static void Main(String[] args)
    {
        Console.WriteLine("Warning!此客户端是调试使用的CLI客户端，我们强烈建议您使用SharpChat GUI!");
        //获取ip
        myip = GetLocalIp();
        //读cfg
        Readconfig();

        client = new SimpleTcpClient();

        //收到数据的事件
        client.DataReceived += onDataReceived;

        //心跳包 断线重连
        bool exit = false;
        bool connected = false;
        Task.Factory.StartNew(() =>
        {
            while (!exit)
            {
                try
                {
                    if (connected)
                    {
                        //发送心跳
                        //client.Write("Heartbag_by_" + myip);
                    }
                    else
                    {
                        //断线重连
                        client.Connect(serverip, serverport);
                        connected = true;
                    }
                    Task.Delay(1000).Wait();
                }
                catch (Exception)
                {
                    connected = false;
                    client.Disconnect();
                }
            }

        }, TaskCreationOptions.LongRunning);

        //发消息
        while (true)
        {
            string strLine = Console.ReadLine();
            //strLine = strLine.Substring(0, strLine.Length - 1);
            if (strLine == "exit")
            {
                exit = true;
                client.Disconnect();
                return;
            }
            if (connected)
            {
                //使用Write、WriteLine方法发送数据，WriteLine会自动在后面加上设置的分隔符
                client.Write(strLine);
            }
        }
    }
}