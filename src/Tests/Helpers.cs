using Microsoft.Extensions.Configuration;
using System;

namespace DocumentDb.Fluent.Tests
{
    static class Helpers
    {
        public static IConfigurationRoot Configuration { get; set; }

        static Helpers()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();
        }

        public static IAccount GetAccount()
        {
            return Account
                .Connect(Environment.GetEnvironmentVariable("TEST_DB_ENDPOINT"), Environment.GetEnvironmentVariable("TEST_DB_KEY"));
        }
    }

    public class TestObject : HasId
    {
        public string Text { get; set; }
        public int Int { get; set; }
        public double Double { get; set; }
    }

    public class TestObject2 : HasId
    {
        public string Text2 { get; set; }
        public int Int2 { get; set; }
        public double Double2 { get; set; }
    }
}
