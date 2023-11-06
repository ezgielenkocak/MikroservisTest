using Book.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Data;
using Newtonsoft.Json;
using System.Text;
using System;
using System.Net;
using Microsoft.AspNetCore.Routing.Template;

namespace BookService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookController : ControllerBase
    {
        public BookController()
        {
            Listen();
        }
        private readonly string _connectionString = "Server=DESKTOP-VTNRLAJ;Database=BookDb;Trusted_Connection=True; MultipleActiveResultSets=true; TrustServerCertificate=True";

        private void Listen()
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost"
            };
            var connection = factory.CreateConnection();
            using
            var channel = connection.CreateModel();
            channel.QueueDeclare("book_queue", exclusive: false);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, eventArgs) => {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                UpdateBookStatus(Convert.ToInt32(message), false);
                Console.WriteLine(message);
            };
            channel.BasicConsume(queue: "book_queue", autoAck: true, consumer: consumer);

        }

        private void UpdateBookStatus(int bookId, bool isInside)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE Books SET IsBookInside = @IsBookInside WHERE Id = @Id";
                    command.Parameters.AddWithValue("@IsBookInside", isInside);
                    command.Parameters.AddWithValue("@Id", bookId);

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        Console.WriteLine($"Book (ID: {bookId}) updated to IsBookInside={isInside}");
                    }
                    else
                    {
                        Console.WriteLine($"Book (ID: {bookId}) not found or no updates were made.");
                    }

                }
            }
        }
        [HttpGet]
        public List<Books> GetBookList()
        {
            List<Books> bookList = new List<Books>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT * FROM Books";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Books book = new Books
                            {
                                Id = (int)reader["Id"],
                                BookName = reader["BookName"].ToString(),
                                IsBookInside = reader.GetBoolean(reader.GetOrdinal("IsBookInside"))
                            };
                            bookList.Add(book);
                        }
                    }
                }
            }

            return bookList;
        }

        [HttpPost]
        public IActionResult CreateBook([FromBody] Books book)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO Books (BookName, IsBookInside) VALUES (@BookName, @IsBookInside)";
                        command.Parameters.Add("@BookName", SqlDbType.NVarChar).Value = book.BookName;
                        command.Parameters.Add("@IsBookInside", SqlDbType.NVarChar).Value = "True"; 

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return Ok("Book added.");
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

    }
}
