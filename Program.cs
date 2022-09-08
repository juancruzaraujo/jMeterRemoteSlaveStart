using System;
using System.IO;
using Sockets;

namespace jMeterRemoteSlaveStart
{

    class Program
    {

        #if DEBUG
            private static bool modo_Debug = true;
            
        #else
            private static bool modo_Debug = false;
            private const int C_MAX_LINEAS = 34;
        #endif

        //c 1492 100.100.100.1,100.100.100.2 pepe.bat variable "valor de la variable"
        const int C_PARAM_NUM_MODE              = 0;
        const int C_PARAM_NUM_PORT              = 1;
        const int C_PARAM_NUM_HOSTS             = 2;
        const int C_PARAM_NUM_FILE              = 3;
        const int C_PARAM_NUM_VARIABLE          = 4;
        const int C_PARAM_NUM_VARIABLE_VALUE    = 5;

        const string C_SERVER_MODE = "s";
        const string C_CLIENT_MODE = "c";
        const string C_CLOSE = "quit";

        static Socket _obSocket;
        static bool _keepRunnig;
        static bool _serverMode;
        static bool _clientMode;

        static string _msgToHosts;

        //va a sumar la cantidad de respuestas oks de los slaves
        //si llega al numero de conexiones, significa que todos se ejecutaron, así que cierro las conexiones
        //y cierro el programa
        //medio bestia, pero tengo sueño y me quiero ir a dormir
        //sé que hay formas mejores
        static int _numCommandsOk;
        static int _numCommandsTotal;

        static void Main(string[] args)
        {
            try
            {
                //parametros
                //modo hosts puerto batch variable linea
                //s puerto
                //c 1492 100.100.100.1,100.100.100.2 pepe.bat variable "valor de la variable"
                _serverMode = false;
                _clientMode = false;

                if ((args[C_PARAM_NUM_MODE] == C_SERVER_MODE) || (args[C_PARAM_NUM_MODE] == C_CLIENT_MODE))
                {
                    _obSocket = new Socket();
                    _obSocket.Event_Socket += ObSocket_Event_Socket;

                    if (args[C_PARAM_NUM_MODE] == C_SERVER_MODE)
                    {
                        ServerMode(args);
                    }

                    if (args[C_PARAM_NUM_MODE] == C_CLIENT_MODE)
                    {
                        ClientMode(args);
                    }
                }
                else
                {
                    ShowInfo();
                }

                _keepRunnig = true;

                while (_keepRunnig)
                {
                    if (_clientMode)
                    {
                        if (_numCommandsOk == _numCommandsTotal)
                        {
                            Console.WriteLine("todos los comandos enviados");

                            ExitApp(0);
                        }
                    }
                }
            }
            catch(Exception err)
            {
                Console.WriteLine(err.Message);
                Console.WriteLine();
                ShowInfo();
            }
        }


        static void ShowInfo()
        {
            Console.WriteLine("modo cliente, se conecta a los servidores en inicia el slave de jemeter");
            Console.WriteLine("jMeterRemoteSlaveStart [modo] [puerto] [hosts] [path batch] [variable linea]");
            Console.WriteLine("jMeterRemoreSlaveStart c 1492 100.100.100.1,100.100.100.2 \"c:\\pepe.bat\" variable \"valor de la variable\"");
            Console.WriteLine("modo servidor, se ejecuta en las maquinas a correr el slave de jmeter");
            Console.WriteLine("jMeterRemoteSlaveStart [s] [puerto]");
            Console.WriteLine("jMeterRemoreSlaveSTart s 1492");
            ExitApp(1);
        }

        private static void ServerMode(string[] args)
        {
            _obSocket.ServerMode = true;
            _obSocket.SetServer(Convert.ToInt32(args[C_PARAM_NUM_PORT]), Protocol.ConnectionProtocol.TCP,10);
            _obSocket.StartServer();
            Console.WriteLine("Server mode start");
            _serverMode = true;
        }

        private static void ClientMode(string[] args)
        {
            _clientMode = true;
            string[] listHost = args[C_PARAM_NUM_HOSTS].Split(',');
            string hostPort = args[C_PARAM_NUM_PORT];

            if (args.Length == 4)
            {
                _msgToHosts = args[C_PARAM_NUM_FILE];
            }
            else
            {
                _msgToHosts = args[C_PARAM_NUM_FILE] + "|" + args[C_PARAM_NUM_VARIABLE] + "|" + args[C_PARAM_NUM_VARIABLE_VALUE];
            }
            _numCommandsTotal = listHost.Length;

            for (int i = 0; i< _numCommandsTotal; i++)
            {
                ConnectionParameters conParams = new ConnectionParameters();
                conParams.SetHost(listHost[i])
                    .SetPort(Convert.ToInt32(hostPort))
                    .SetConnectionTag(listHost[i])
                    .SetProtocol(Protocol.ConnectionProtocol.TCP);

                _obSocket.ConnectClient(conParams);
                

            }

        }

        private static void ObSocket_Event_Socket(EventParameters eventParameters)
        {
            switch (eventParameters.GetEventType)
            {
                case EventParameters.EventType.ERROR:
                    Console.WriteLine(eventParameters.GetData);
                    if (_serverMode)
                    {
                        _obSocket.Send(eventParameters.GetConnectionNumber, "ERROR! " + eventParameters.GetData);

                    }
                    //ExitApp(1);
                    break;

                case EventParameters.EventType.SERVER_NEW_CONNECTION:
                    Console.WriteLine("conection ok from " + eventParameters.GetClientIp);
                    break;

                case EventParameters.EventType.CLIENT_CONNECTION_OK:
                    Console.WriteLine("conectado ok a->" + eventParameters.GetClientTag);
                    eventParameters.GetSocketInstance.Send(eventParameters.GetClientTag, _msgToHosts);
                    break;

                case EventParameters.EventType.DATA_IN:
                    
                    if (_serverMode)
                    {
                        if (eventParameters.GetData == C_CLOSE)
                        {
                            _obSocket.Send(eventParameters.GetConnectionNumber, "saliendo");
                            _obSocket.DisconnectAllConnectedClientsToMe();
                            ExitApp(0);
                        }
                        else
                        {
                            ServerDataIn(eventParameters.GetData);
                            _obSocket.Send(eventParameters.GetConnectionNumber, "comando ejecutado");
                        }
                        _obSocket.DisconnectAllConnectedClientsToMe();

                    }
                    if (_clientMode)
                    {
                        Console.WriteLine(eventParameters.GetData + " en " + eventParameters.GetClientTag);
                        _numCommandsOk++;
                    }
                    break;
            }
        }

        //busco, reemplazo la linea y ejecuto el .bat
        static private void ServerDataIn(string data)
        {
            //que va a llegar!?
            //pepe.bat variable "valor de la variable"
            const int C_FILE = 0;
            const int C_VARIABLE = 1;
            const int C_VALUE = 2;

            try
            {
                bool variableFound = false;

                string[] vecData = data.Split('|');
                string filePath = vecData[C_FILE];
                string[] batchFileData = File.ReadAllLines(filePath);


                for (int i = 0; i < batchFileData.Length; i++)
                {
                    if (batchFileData[i].Contains(vecData[C_VARIABLE]))
                    {
                        variableFound = true;
                        //me quedo con"set prop="
                        batchFileData[i] = batchFileData[i].Remove(batchFileData[i].IndexOf("="));
                        batchFileData[i] = batchFileData[i].TrimEnd(' '); //saco el espacio del final
                        batchFileData[i] = batchFileData[i] + "=" + vecData[C_VALUE];
                        break; //salgo de for
                    }
                }

                //no encontré nada, así que salgo, pero muestro que estoy buscando
                if (!variableFound)
                {
                    Console.WriteLine("variable \" " + vecData[C_VARIABLE] + "=\" no fue encontrada");
                    ExitApp(1);
                }

                StreamWriter sw = new StreamWriter(filePath);
                for (int i = 0; i < batchFileData.Length; i++)
                {
                    sw.WriteLine(batchFileData[i]);
                }

                sw.Flush();
                sw.Close();

                System.Diagnostics.Process.Start(filePath);
                //Process.Start("cmd.exe", "/k " + filePath);
                Console.WriteLine(filePath + " ejecutado");
            }
            catch(Exception err)
            {
                _obSocket.SendAll(err.Message);
            }
        }

        static private void ExitApp(int exitCode)
        {
            if (modo_Debug)
            {
                Console.WriteLine("debug mode: saliendo");
                Console.ReadLine();
            }

            System.Environment.Exit(exitCode);
        }
    }
}
