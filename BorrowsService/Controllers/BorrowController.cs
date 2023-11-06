using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static System.Reflection.Metadata.BlobBuilder;
using System.Data;
using Microsoft.Data.SqlClient;
using BorrowEntity;
using Microsoft.AspNetCore.Connections;
using System.Text;
using RabbitMQ.Client;
using Newtonsoft.Json;
using System;

namespace BorrowsService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BorrowController : ControllerBase
    {
        private readonly string _connectionString = "Server=DESKTOP-VTNRLAJ;Database=BorrowDb;Trusted_Connection=True; MultipleActiveResultSets=true; TrustServerCertificate=True";

        [HttpPost]
        public IActionResult CreateBorrow([FromBody] Borrows borrow)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO Borrows (BookId) VALUES (@BookId)";
                        command.Parameters.Add("@BookId", SqlDbType.NVarChar).Value = borrow.BookId;
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            SendMessage(borrow.BookId);

                            return Ok("Borrow success.");
                        }
                        else
                        {
                            return BadRequest("Book didn't add.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }
        private void SendMessageTest(int bookId)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (IConnection connection = factory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "book_queue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                string message = JsonConvert.SerializeObject(bookId);
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                                     routingKey: "book_queue",
                                     basicProperties: null,
                body: body);
                Console.WriteLine($"BookId: {bookId}");
            }
        }

        private void SendMessage<T>(T message)
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost"
            };
            var connection = factory.CreateConnection();
            using
            var channel = connection.CreateModel();
            channel.QueueDeclare("book_queue", exclusive: false);
            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);
            channel.BasicPublish(exchange: "", routingKey: "book_queue", body: body);
            Console.WriteLine(message);

        }
    }
}
