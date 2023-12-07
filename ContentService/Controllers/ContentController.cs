using ContentService.Communication;
using ContentService.Database;
using ContentService.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RabbitMQ.Client;
using StackExchange.Redis;
using System;
using System.Reflection;
using System.Xml.Linq;

namespace ContentService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ContentController : ControllerBase
    {
        private readonly IRabbitMQManager _rabbitMQManager;
        private readonly IDatabaseHelper _databaseHelper;
        private readonly TimeSpan _databaseUpdateTimeout;
        [ActivatorUtilitiesConstructor]
        public ContentController()
        {
            _rabbitMQManager = new RabbitMQManager();
            _databaseHelper = new DatabaseHelper();
            _databaseUpdateTimeout = TimeSpan.FromDays(2);
        }
        public ContentController(IRabbitMQManager rabbitMQManager, IDatabaseHelper databaseHelper)
        {
            _rabbitMQManager = rabbitMQManager;
            _databaseHelper = databaseHelper;
            _databaseUpdateTimeout = TimeSpan.FromDays(2);
        }

        [HttpGet("PingRedis")]
        public ActionResult GetOk()
        {
            return Ok(_databaseHelper.Ping());
        }

        [HttpGet("UpdateDatabase")]
        public ActionResult UpdateDatabase()
        {
            DateTime now = DateTime.Now;
            DateTime timeStamp = _databaseHelper.GetTimeStamp();

            if (now - timeStamp <= _databaseUpdateTimeout)
            {
                return Ok();
            }

            var task = _rabbitMQManager.RequestUpdate();
            task.Wait();
            string response = task.Result;

            List<ContentItem> contents = JsonConvert.DeserializeObject<List<ContentItem>>(response);

            if (contents != null)
            {
                _databaseHelper.SaveAll(contents);
                _databaseHelper.SaveTimeStamp();
            }

            return Ok(_databaseHelper.GetAll());
        }
        [HttpGet("GetAll")]
        public ActionResult GetAllContent()
        {
            return Ok(_databaseHelper.GetAll());
        }
    }
}
