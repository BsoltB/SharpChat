using System;
using SimpleTCP;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpConfig;
using System.Reflection;
using System.Collections;

class Program
{
    public static Configuration config = null;
    public static int serverport = 2233;
    public static SimpleTcpServer server = null;
    public static string server_ver = "alpha 20221211";
    public static Dictionary<string, string> map = new Dictionary<string, string>();
    public static List<Assembly> asm = new List<Assembly>();
    public static List<Type> types = new List<Type>();
    public static void Readconfig()
    {
        config = Configuration.LoadFromFile("server.cfg");

        //Web 部分读取
        var section = config["Web"];
        serverport = section["port"].IntValue;

    }
    /*
     * cfg文件示例：
     * 首先名字必须是 server.cfg。
     * [Web]
     * port=<端口>
     */

    //接收数据事件
    public static void onDataReceived(object sender, Message msg)
    {
        string msgip = msg.TcpClient.Client.RemoteEndPoint.ToString();
        string msgstr = msg.MessageString;
        if(msgstr.StartsWith("connect_"))
        {
            Console.WriteLine(msgip + ":" + msgstr.Substring(8));
            map[msgip] = msgstr.Substring(8);
            server.Broadcast(map[msgip] + "加入了聊天室");
        }
        else
        {
            Console.WriteLine(map[msgip] + ":" + msg.MessageString);
            server.Broadcast(map[msgip] + ":" + msg.MessageString);
        }

        //调用插件
        foreach (Type type in types)
        {
            MethodInfo method = type.GetMethod("OnDataReceived");
            object o = Activator.CreateInstance(type);
            object[] args = { sender, msg, server };
            if (method != null) method.Invoke(o, args);
        }
    }

    //客户端连接事件
    public static void onClientConnected(object sender, TcpClient msg)
    {
        Console.WriteLine("ClientConnected：" + msg.Client.RemoteEndPoint.ToString());

        //调用插件
        foreach (Type type in types)
        {
            MethodInfo method = type.GetMethod("OnClientConnected");
            object o = Activator.CreateInstance(type);
            object[] args = { sender, msg, server };
            if (method != null) method.Invoke(o, args);
        }
    }

    //客户端断开事件
    public static void onClientDisconnected(object sender, TcpClient msg)
    {
        string msgip = msg.Client.RemoteEndPoint.ToString();
        Console.WriteLine("ClientDisconnected：" + msg.Client.RemoteEndPoint.ToString());
        server.Broadcast(map[msgip] + "离开了聊天室");

        //调用插件
        foreach (Type type in types)
        {
            MethodInfo method = type.GetMethod("OnClientConnected");
            object o = Activator.CreateInstance(type);
            object[] args = { sender, msg, server };
            if (method != null) method.Invoke(o, args);
        }

        //这里最后移除map里的key-vaule对，方便在之前调用map
        map.Remove(msgip);
    }

    //连接的客户端数
    public static void doTask()
    {
        while (true)
        {
            //连接数监控
            int clientsConnected = server.ConnectedClientsCount;
            Console.WriteLine("当前连接的客户端数：" + clientsConnected);
        }
    }

    //帮助
    public static void doHelp()
    {
        Console.WriteLine("+======SharpChat Help=======+");
        Console.WriteLine("ccount 显示当前连接的客户端数量；");
        Console.WriteLine("help 显示帮助。");
        Console.WriteLine("+===========================+");
    }

    //未知指令
    public static void doUKE()
    {
        Console.WriteLine("未知指令，请再次输入...");
    }

    //加载插件的dll
    public static void Loadasm()
    {
        DirectoryInfo info = new DirectoryInfo(Environment.CurrentDirectory + @"\plugins");
        foreach (FileSystemInfo file in info.GetFileSystemInfos())
        {
            if (!(file is DirectoryInfo))
            {
                Assembly assembly = Assembly.LoadFrom(file.FullName);
                asm.Add(assembly);
            }
        }
    }

    //将插件的mainclass加载
    public static void Loadclass()
    {
        foreach(Assembly assembly in asm)
        {
            Type type = assembly.GetType("Plugin.MainClass");
            if (type != null)
            {
                types.Add(type);
            }
        }
    }

    public static bool Runcmd(string cmdd)
    {
        if (cmdd == "cconut")
        {
            doTask();
        }
        else if (cmdd == "help")
        {
            doHelp();
        }
        return false;
    }


    //Main函数
    public static void Main(String[] args)
    {
        Console.Title = "SharpChat Server " + server_ver;

        //读cfg
        Readconfig();
        Console.WriteLine("加载cfg成功！");

        //加载插件
        Loadasm();
        Loadclass();
        Console.WriteLine("加载plugin成功！");

        server = new SimpleTcpServer();
        //接收数据事件
        server.DataReceived += onDataReceived;

        //客户端连接事件
        server.ClientConnected += onClientConnected;

        //客户端断开事件
        server.ClientDisconnected += onClientDisconnected;


        //开始监听
        server.Start(serverport);

        //监听的IP
        //var listeningIps = server.GetListeningIPs();
        //监听的V4Ip
        //var listeningV4Ips = server.GetListeningIPs().Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

        //开始另一个Task，按10000ms间隔输出客户端的数量
        //Task.Factory.StartNew(doTask, TaskCreationOptions.LongRunning);

        foreach(Type type in types)
        {
            MethodInfo method = type.GetMethod("OnLoad");
            object o = Activator.CreateInstance(type);
            if (method != null) method.Invoke(o, null);
        }

        Console.WriteLine("服务器开启成功！");
        string cmd = "asolta";
        while(cmd != "exit")
        {
            cmd = Console.ReadLine();
            bool bb =  Runcmd(cmd);
            if (!bb)
            {
                bool cc =false;
                foreach (Type type in types)
                {
                    MethodInfo method = type.GetMethod("OnCommand");
                    object o = Activator.CreateInstance(type);
                    object[] arggs = { server };
                    if(method != null)  cc = cc || (bool)method.Invoke(o, arggs);
                }
                if (!cc) doUKE();
            }
        }
        //停止监听
        server.Stop();
        Console.WriteLine("Server Stoped!");
        Console.ReadKey();
    }
}