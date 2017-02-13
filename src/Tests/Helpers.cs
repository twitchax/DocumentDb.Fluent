using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

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
    }

    public class TestObject : HasId
    {
        public string Text { get; set; }
        public int Int { get; set; }
        public double Double { get; set; }
    }
}
