using Microsoft.VisualStudio.TestTools.UnitTesting;
using ContentService.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ContentService.Communication;
using ContentService.Database;
using Moq;
using ContentService.Model;
using StackExchange.Redis;
using Newtonsoft.Json;

namespace ContentService.Controllers.Tests
{
    [TestClass()]
    public class ContentControllerTests
    {
        private readonly ContentController _controller;
        private readonly Mock<IRabbitMQManager> _rabbitMQManagerMock;
        private readonly Mock<IDatabase> _databaseMock;
        private readonly Mock<IRedisCacheBuilder> _cacheBuilderMock;

        public ContentControllerTests()
        {
            _rabbitMQManagerMock = new Mock<IRabbitMQManager>();
            _databaseMock = new Mock<IDatabase>();
            _cacheBuilderMock = new Mock<IRedisCacheBuilder>();
            _cacheBuilderMock.Setup(builder => builder.GetDatabase()).Returns(_databaseMock.Object);

            DatabaseHelper databaseHelper = new DatabaseHelper(_cacheBuilderMock.Object);

            _controller = new ContentController
            (
                _rabbitMQManagerMock.Object,
                 databaseHelper
            );
        }

        [TestMethod()]
        public void UpdateDatabaseTest_Needed()
        {
            // Arrange
            string timestampKey = "last_update";
            var now = DateTime.Now;
            var timeStamp = now - TimeSpan.FromDays(3); // Simulate expired timestamp
            var timeStampJson = JsonConvert.SerializeObject(timeStamp);
            CancellationToken token = new CancellationToken();

            //contentItem
            string key = "contents";
            ContentItem content = new ContentItem()
            {
                Title = "Title"
            };
            string contentJson = JsonConvert.SerializeObject(content);
            RedisValue redisValue = contentJson;
            RedisValue[] redisValues = new RedisValue[] { redisValue };

            _databaseMock.Setup(db => db.ListRange(key, 0, -1, CommandFlags.None)).Returns(redisValues);
            _databaseMock.Setup(db => db.StringGet(timestampKey, CommandFlags.None)).Returns(timeStampJson);
            _rabbitMQManagerMock.Setup(manager => manager.RequestUpdate(token)).ReturnsAsync("[{\"id\": 1, \"title\": \"Test Content\"}]");

            // Act
            ActionResult result = _controller.UpdateDatabase();


            // Assert
            _rabbitMQManagerMock.Verify(manager => manager.RequestUpdate(token), Times.Once);


            var okObjectResult = result as OkObjectResult;
            Assert.IsNotNull(okObjectResult);

            var contents = okObjectResult.Value as List<ContentItem>;
            Assert.IsNotNull(contents);

            Assert.AreEqual(content.Title, contents.First().Title);

        }
        
        [TestMethod()]
        public void UpdateDatabaseTest_NotNeeded()
        {
            // Arrange
            DateTime now = DateTime.Now;
            DateTime timeStamp = now - TimeSpan.FromDays(1);
            var timeStampJson = JsonConvert.SerializeObject(timeStamp);
            CancellationToken token = new CancellationToken();

            //contentItem
            string key = "contents";
            ContentItem content = new ContentItem()
            {
                Title = "Title"
            };
            string contentJson = JsonConvert.SerializeObject(content);
            RedisValue redisValue = contentJson;
            RedisValue[] redisValues = new RedisValue[] { redisValue };

            _databaseMock.Setup(db => db.StringGet("last_update", CommandFlags.None)).Returns(timeStampJson);

            // Act
            ActionResult result = _controller.UpdateDatabase();

            // Assert
            _rabbitMQManagerMock.Verify(manager => manager.RequestUpdate(token), Times.Never);

            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod()]
        public void GetAllContent_ReturnsOkResult()
        {
            // Arrange
            List<ContentItem> dummyContents = new List<ContentItem>
            {
                new ContentItem { Id = 1, Title = "Test Content 1" },
                new ContentItem { Id = 2, Title = "Test Content 2" }
            };

            //contentItem
            string key = "contents";
            ContentItem content = new ContentItem()
            {
                Title = "Title"
            };
            RedisValue redisValue1 = JsonConvert.SerializeObject(dummyContents[0]);
            RedisValue redisValue2 = JsonConvert.SerializeObject(dummyContents[1]);
            RedisValue[] redisValues = new RedisValue[] { redisValue1, redisValue2 };

            _databaseMock.Setup(db => db.ListRange(key, 0, -1, CommandFlags.None)).Returns(redisValues);

            // Act
            ActionResult result = _controller.GetAllContent();

            // Assert
            var okObjectResult = result as OkObjectResult;
            Assert.IsNotNull(okObjectResult);

            var contents = okObjectResult.Value as List<ContentItem>;
            Assert.IsNotNull(contents);

            Assert.AreEqual(dummyContents.Count, contents.Count);
        }
    }
}