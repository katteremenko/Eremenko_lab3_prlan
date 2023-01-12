using System.Threading.Channels;

namespace Eremenko_lab3_prlan
{
    class Token
    {
        public string data;
        public int recipient;
        public int ttl;
    }

    class Node
    {
        Channel<Token>? input;
        Channel<Token>? output;
        int node;

        public Node(int id)
        {
            node = id;
        }

        public void Writer(Channel<Token> writer)
        {
            output = writer;
        }

        public void Reader(Channel<Token> reader)
        {
            input = reader;
        }
        public async Task process(Token token)
        {
            Console.WriteLine($"Token now at {node}.");
            token.ttl--;

            if (token.recipient == node)
                Console.WriteLine($"The message reiceved: {token.data}.");

            else if (token.ttl > 0)
            {
                if (output != null)
                {
                    await output.Writer.WriteAsync(token);
                    Console.WriteLine($"Token sent from {node}.");
                }
            }

            else Console.WriteLine("Time is up.");
        }

        public async void waiting()
        {
            while (true)
            {
                if (input != null)
                {
                    await input.Reader.WaitToReadAsync();
                    Token token = await input.Reader.ReadAsync();
                    await process(token);
                }
            }
        }

        public async Task sending(Token token)
        {
            if (output != null)
            {
                await output.Writer.WriteAsync(token);
                Console.WriteLine($"Token sent from {node}.");
            }
            while (true)
            {
                if (input != null)
                {
                    await input.Reader.WaitToReadAsync();
                    token = await input.Reader.ReadAsync();
                    await process(token);
                }
            }
        }

        internal class Program
        {
            static async Task Main(string[] args)
            {
                Console.WriteLine("Number of tokens: ");
                int number = Convert.ToInt32(Console.ReadLine());
                Console.WriteLine("Recipient: ");
                int last = Convert.ToInt32(Console.ReadLine());
                if (last == 0 || last >= number)
                {
                    Console.WriteLine("Error: enter recipient < n or > 0.");
                    last = Convert.ToInt32(Console.ReadLine());
                }
                Console.WriteLine("Time: ");
                int time = Convert.ToInt32(Console.ReadLine());
                Console.WriteLine("Message: ");
                string message = Console.ReadLine();

                Token token = new Token
                {
                    data = message,
                    recipient = last,
                    ttl = time,
                };

                Node[] nodes = new Node[number];
                nodes[0] = new Node(0);

                for (int i = 0; i < number - 1; i++)
                {
                    Channel<Token> newChannel = Channel.CreateBounded<Token>(1);
                    Channel<Token> channel = newChannel;

                    nodes[i + 1] = new Node(i + 1);
                    nodes[i].Writer(channel);
                    nodes[i + 1].Reader(channel);
                }

                Channel<Token> lastChannel = Channel.CreateBounded<Token>(1);
                Channel<Token> ch = lastChannel;
                nodes[0].Reader(ch);
                nodes[number - 1].Writer(ch);

                for (int i = 1; i < number; i++)
                {
                    Thread thr = new Thread(nodes[i].waiting);
                    thr.Start();
                }

                await nodes[0].sending(token);
            }
        }
    }
}