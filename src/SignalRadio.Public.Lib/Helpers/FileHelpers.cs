using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using SignalRadio.Public.Lib.Models;
using SignalRadio.Public.Lib.Models.Enums;
using Stream = SignalRadio.Public.Lib.Models.Stream;

namespace SignalRadio.Public.Lib.Helpers
{
    public class FileHelpers
    {
        public static IEnumerable<TalkGroup> TalkGroupsFromCsv(string talkGroupCsvPath)
        {
            using(var csvStream = new StreamReader(talkGroupCsvPath))
            {
                while(!csvStream.EndOfStream)
                {
                    var line = csvStream.ReadLine();
                    var lineParts = line.Split(',');

                    ushort tgId = 0;
                    ushort priority = 0;
                    string hexId = null;
                    string mode = null;
                    string alphaTag = null;
                    string tgName = null;
                    string tgType = null;
                    string tgCategory = null;
                    string streamIds = null;
                    string[] streams = null;
                    if(lineParts.Length > 0)
                        if(!ushort.TryParse(lineParts[0], out tgId))
                            continue;

                    if(lineParts.Length > 1)
                        streamIds = lineParts[1];
                    if(lineParts.Length > 2)
                        mode = lineParts[2];
                    if(lineParts.Length > 3)
                        alphaTag = lineParts[3];
                    if(lineParts.Length > 4)
                        tgName = lineParts[4];
                    if(lineParts.Length > 5)
                        tgType = lineParts[5];
                    if(lineParts.Length > 6)
                        tgCategory = lineParts[6];
                    if(lineParts.Length > 7)
                        ushort.TryParse(lineParts[7], out priority);

                    if(streamIds != null)
                        streams = streamIds.Split('|');

                    var tg = new TalkGroup()
                    {
                        Identifier = tgId,
                        //Priority = priority,
                        Mode = TalkGroupMode.Digital,
                        AlphaTag = alphaTag,
                        Name = tgName,
                        
                        TalkGroupStreams = new Collection<TalkGroupStream>()
                    };

                    var tgStreams = new List<TalkGroupStream>();

                    foreach(var stream in streams)
                    {
                        var s = new Stream();
                        s.StreamIdentifier = stream;
                        tg.TalkGroupStreams.Add(new TalkGroupStream() { TalkGroup = tg, Stream = s });
                    }
                    
                    yield return tg;
                }

                yield return null;
            }
        }
    }
       
}