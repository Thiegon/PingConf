using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace PingConsoleApp
{
    class Program
    {
        static ManualResetEventSlim pingEvent = new ManualResetEventSlim(false);

        static async Task Main(string[] args)
        {
            string ipsFile = "ips.txt";
            string tempoFile = "tempo.txt";

            List<string> ips = ReadIPsFromConfig(ipsFile);
            int tempoTotalSegundos = ReadPingTimeFromConfig(tempoFile);

            if (ips.Count == 0)
            {
                Console.WriteLine("Nenhum IP encontrado no arquivo de configuração. Encerrando o programa.");
                return;
            }

            Console.WriteLine("Iniciando o ping para os seguintes IPs:");
            foreach (string ip in ips)
            {
                Console.WriteLine(ip);
            }
            Console.WriteLine();

            List<Task> tasks = new List<Task>();

            foreach (string ip in ips)
            {
                string logFile = $"ping_{ip.Replace(".", "_")}.txt";
                Task task = Task.Run(() => PingIP(ip, tempoTotalSegundos, logFile));
                tasks.Add(task);
            }

            // Aguardar até que o tempo total expire
            await Task.Delay(tempoTotalSegundos * 1000);

            // Sinalizar que o tempo total expirou
            pingEvent.Set();

            // Aguardar todas as tarefas terminarem
            await Task.WhenAll(tasks);

            Console.WriteLine("Todos os pings foram concluídos durante o tempo especificado.");
            Console.WriteLine("Pressione qualquer tecla para encerrar...");
            Console.ReadKey();
        }

        static List<string> ReadIPsFromConfig(string ipsFile)
        {
            List<string> ips = new List<string>();

            if (File.Exists(ipsFile))
            {
                string[] lines = File.ReadAllLines(ipsFile);
                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        ips.Add(line.Trim());
                    }
                }
            }
            else
            {
                Console.WriteLine("Arquivo de configuração de IPs não encontrado. Nenhum IP será pingado.");
            }

            return ips;
        }

        static int ReadPingTimeFromConfig(string tempoFile)
        {
            int tempoTotalSegundos;

            if (File.Exists(tempoFile))
            {
                string[] lines = File.ReadAllLines(tempoFile);
                if (lines.Length > 0 && int.TryParse(lines[0], out tempoTotalSegundos))
                {
                    return tempoTotalSegundos;
                }
            }

            Console.WriteLine("Tempo de ping não especificado no arquivo de configuração. Usando tempo padrão de 60 segundos.");
            tempoTotalSegundos = 60;
            return tempoTotalSegundos;
        }

        static void PingIP(string ip, int tempoTotalSegundos, string logFile)
        {
            Ping ping = new Ping();
            DateTime startTime = DateTime.Now;
            DateTime endTime = startTime.AddSeconds(tempoTotalSegundos);

            while (DateTime.Now < endTime && !pingEvent.IsSet)
            {
                PingReply reply = ping.Send(ip);

                if (reply != null)
                {
                    string result = $"Data: {DateTime.Now}\tIP: {reply.Address}\tStatus: {reply.Status}\tTempo: {reply.RoundtripTime}ms";
                    Console.WriteLine(result);

                    // Salvar o resultado no respectivo arquivo de log
                    using (StreamWriter writer = new StreamWriter(logFile, true))
                    {
                        writer.WriteLine(result);
                    }
                }

                // Aguardar 1 segundo antes de enviar o próximo ping
                Thread.Sleep(1000);
            }
        }
    }
}

