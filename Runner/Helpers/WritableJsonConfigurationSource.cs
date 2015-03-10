using Microsoft.Framework.ConfigurationModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marathon.Helpers
{
    public class WritableJsonConfigurationSource : JsonConfigurationSource
    {
        public WritableJsonConfigurationSource(string path)
            : base(path)
        {
            if (!File.Exists(path))
                using (var configFile = File.CreateText(path))
                {
                    configFile.WriteLine("{");
                    configFile.WriteLine("  ");
                    configFile.WriteLine("}");
                }
        }

        public override void Set(string key, string value)
        {
            base.Set(key, value);

            Commit();
        }

        public override void Commit()
        {
            using (var fs = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fs.Seek(0, SeekOrigin.Begin);
                fs.SetLength(0);

                var configJson = Newtonsoft.Json.JsonConvert.SerializeObject(Data, Newtonsoft.Json.Formatting.Indented);
                var configJsonBytes = Encoding.UTF8.GetBytes(configJson);
                fs.Write(configJsonBytes, 0, configJsonBytes.Length);
            }
        }
    }
}
