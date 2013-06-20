using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;

namespace AsyncZeroClient
{
    class ClientProgram
    {
         static void Main(string[] args)
        {
            var context = ZmqContext.Create();
            var server = new Client(context, "one");
            var token = new CancellationTokenSource();
            server.Start(token.Token);
            Console.ReadLine();
            token.Cancel();
            context.Dispose();
        }
    }

    internal class Client
    {
        private readonly ZmqContext _context;
        private readonly string _id;

        public Client(ZmqContext context, string id)
        {
            _context = context;
            _id = id;
        }

        public void Start(CancellationToken token)
        {
            var flag = new TaskCompletionSource<bool>();
            Task.Factory.StartNew(() => start(token, flag), TaskCreationOptions.LongRunning);
            flag.Task.Wait();
        }

        private void start(CancellationToken token, TaskCompletionSource<bool> flag)
        {
            using (var socket = _context.CreateSocket(SocketType.REQ))
            {
                socket.Connect("tcp://127.0.0.1:8085");
                flag.TrySetResult(true);

                while (token.IsCancellationRequested == false)
                {
                    Console.WriteLine("Requesting from {0}", _id);
                    socket.Send(_id, Encoding.ASCII);
                    var result = socket.Receive(Encoding.ASCII);
                    Console.WriteLine(result);
                }
            }
        }
    }
}
