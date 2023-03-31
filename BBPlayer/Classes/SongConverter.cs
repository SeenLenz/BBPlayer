using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BBPlayer.Classes
{
    internal class SongConverter : JsonConverter<Song>
    {
        public override Song Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Song value, JsonSerializerOptions options)
        {
            JObject obj = new JObject();
            obj.Add("Title", value.Title);
            obj.Add("Artist", value.Artist);
            obj.Add("Path", value.Title);
            obj.Add("Album", value.Title);
            obj.Add("Year", value.Title);
            obj.Add("Title", value.Title);
        }
    }
}
