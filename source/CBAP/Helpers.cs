using Archipelago.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CBAP
{
    public static class Helpers
    {
        public static string OpenEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string jsonFile = reader.ReadToEnd();
                return jsonFile;
            }
        }
        public static List<Location> GetLocations()
        {
            var json = OpenEmbeddedResource("CBAP.Resources.Locations.json");
            var list = JsonConvert.DeserializeObject<List<Location>>(json);
            return list;
        }
    }
}
