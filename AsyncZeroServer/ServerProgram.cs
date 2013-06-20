using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;

namespace AsyncZeroServer
{
    class ServerProgram
    {
        static void Main(string[] args)
        {
            var context = ZmqContext.Create();
            var token = new CancellationTokenSource();

            var proxy = new Proxy(context);
            proxy.Start(token.Token);
            
            var server = new Server(context);
            server.Start(token.Token);
            
            var input = Console.ReadLine();
            while (input != "quit")
            {
                if (input == "add")
                {
                    server.Start(token.Token);
                }

                input = Console.ReadLine();
            }

            token.Cancel();
            context.Dispose();
        }
    }

    internal class Proxy
    {
        private readonly ZmqContext _context;

        public Proxy(ZmqContext context)
        {
            _context = context;

        }

        public void Start(CancellationToken token)
        {
            var flag = new TaskCompletionSource<bool>();
            Task.Factory.StartNew(() => start(token, flag));
            flag.Task.Wait();
            Console.WriteLine("Proxy ready");
        }

        private void start(CancellationToken token, TaskCompletionSource<bool> flag)
        {
            Console.WriteLine("starting proxy...");
            using (var router = _context.CreateSocket(SocketType.ROUTER))
            using (var dealer = _context.CreateSocket(SocketType.DEALER))
            {
                router.Bind("tcp://127.0.0.1:8085");
                dealer.Bind("tcp://127.0.0.1:8087");


                Console.WriteLine("Binding done...");
                router.ReceiveReady += (sender, args) => proxy(router, dealer);
                dealer.ReceiveReady += (sender, args) => proxy(dealer, router);
                var poller = new Poller(new[] {router, dealer});
                Console.WriteLine("poll");
                
                flag.SetResult(true);

                //If you were following along with the live coding, this is the bit that was wrong.
                //I forgot the "== false" part. Kinda important, huh? Who'd have thunk ;P
                while (token.IsCancellationRequested == false) 
                {
                    poller.Poll(TimeSpan.FromMilliseconds(500));
                }

                Console.WriteLine("Exiting...");
                poller.Dispose();
                Console.WriteLine("Poller disposed...");
            }

            Console.WriteLine("Poller sockets disposed...");
        }

        private void proxy(ZmqSocket from, ZmqSocket to)
        {
            var buffer = new byte[2048];

            while (true)
            {
                var count = from.Receive(buffer);

                Console.WriteLine("proxying");

                if (from.ReceiveMore)
                    to.Send(buffer, count, SocketFlags.SendMore);
                else
                {
                    to.Send(buffer, count, SocketFlags.None);
                    break;
                }
            }
        }
    }

    internal class Server
    {
        private readonly ZmqContext _context;

        public Server(ZmqContext context)
        {
            _context = context;
        }

        public void Start(CancellationToken token)
        {
            var flag = new TaskCompletionSource<bool>();
            Task.Factory.StartNew(()=> start(token, flag), TaskCreationOptions.LongRunning);
            flag.Task.Wait();
            Console.WriteLine("Server ready");
        }

        private void start(CancellationToken token, TaskCompletionSource<bool> flag)
        {
            using (var socket = _context.CreateSocket(SocketType.REP))
            {
                socket.Connect("tcp://127.0.0.1:8087");
                flag.TrySetResult(true);

                while (token.IsCancellationRequested == false)
                {
                    var requesterId = socket.Receive(Encoding.ASCII);
                    if (string.IsNullOrWhiteSpace(requesterId) == false)
                    {
                        Console.WriteLine("Received message from {0}", requesterId);

                        Task.Delay(3000).Wait();
                        socket.Send(string.Format("{0} to {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), requesterId), Encoding.ASCII);
                    }
                }
            }
        }
    }
}
