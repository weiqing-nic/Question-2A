using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using producer10.Models;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;


namespace producer10.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TaskController : ControllerBase
    {

        [HttpPost]
        public void Post([FromBody] Models.Task task)
        {
            var keyval = new List<KeyValuePair<string, string>>();

            keyval.Add(new KeyValuePair<string, string>("email", task.email));
            keyval.Add(new KeyValuePair<string, string>("password", task.password));
            keyval.Add(new KeyValuePair<string, string>("task", task.task));
            object data = new
            {
                email = task.email,
                password = task.password,
                task = task.task
            };



            var myContent = JsonConvert.SerializeObject(data);
            var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var reponsestring = "https://reqres.in/api/login";
            var client = new HttpClient();
            var content = new FormUrlEncodedContent(keyval);
            var readAsync = client.PostAsync(reponsestring, byteContent);
            readAsync.Wait();
            if (readAsync.IsCompletedSuccessfully)
            {
                var json = readAsync.Result.Content.ReadAsStringAsync();
                json.Wait();

                dynamic datajson = JsonConvert.DeserializeObject(json.Result);
                Console.WriteLine(datajson.token);

                var factory = new ConnectionFactory()
                {
                    //HostName = "localhost" , 
                    //Port = 30724
                    HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                    Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))
                };

                Console.WriteLine(factory.HostName + ":" + factory.Port);
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "TaskQueue",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    string message = datajson.token;
                    var body = Encoding.UTF8.GetBytes(message);

                    channel.BasicPublish(exchange: "",
                                         routingKey: "TaskQueue",
                                         basicProperties: null,
                                         body: body);
                }
            }
        }
    }
}
