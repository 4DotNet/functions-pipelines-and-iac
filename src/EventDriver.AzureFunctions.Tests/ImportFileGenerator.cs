using System.Text;
using EventDriver.AzureFunctions.Tests.Factories;
using Newtonsoft.Json;

namespace EventDriver.AzureFunctions.Tests
{
    public class ImportFileGenerator
    {
        [Fact]
        public async Task Test1()
        {
            var users = RawUserDataFactory.Create(1000);
            var usersJson = JsonConvert.SerializeObject(users);
            await File.WriteAllTextAsync("D:\\temp\\users.json", usersJson, Encoding.UTF8);
        }
    }
}