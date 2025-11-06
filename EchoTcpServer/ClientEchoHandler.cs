using System;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServer
{
    // Клас, який містить ізольовану логіку, готову до тестування
    public class ClientEchoHandler
    {
        public async Task HandleClientStreamAsync(INetworkStream stream, CancellationToken token)
        {
            try
            {
                byte[] buffer = new byte[8192];
                int bytesRead;

                while (!token.IsCancellationRequested && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    await stream.WriteAsync(buffer, 0, bytesRead, token);
                    Console.WriteLine($"Echoed {bytesRead} bytes to the client.");
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                Console.WriteLine($"Error during client handling: {ex.Message}");
            }
        }
    }
}