using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SignalRadio.Database.EF;
using SignalRadio.Public.Lib.Models;
using SignalRadio.Public.Lib.Models.Enums;
using SRStream = SignalRadio.Public.Lib.Models.Stream;

namespace SignalRadio.Web.Api.Services
{
    public class BulkImportService: IDisposable
    {
        private readonly ISignalRadioDbContext _dbContext;
        
        public BulkImportService(ISignalRadioDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        protected virtual async Task<SRStream> GetOrCreateStreamAsync(SRStream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            var existing = _dbContext.Streams.FirstOrDefault(s => s.StreamIdentifier == stream.StreamIdentifier);

            if(existing is null)
            {
                var newStream = await _dbContext.Streams.AddAsync(stream);
                await _dbContext.SaveChangesAsync();
                existing = newStream.Entity;
            }

            return existing;
        }

        protected virtual async Task<TalkGroup> GetOrCreateTalkGroupAsync(TalkGroup talkGroup, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (talkGroup is null)
                throw new ArgumentNullException(nameof(talkGroup));

            var existing = default(TalkGroup);
            if(talkGroup.Id > 0)
                existing = _dbContext.TalkGroups.FirstOrDefault(tg => tg.Id == talkGroup.Id);
            else
                existing = _dbContext.TalkGroups.FirstOrDefault(tg => tg.Identifier == talkGroup.Identifier);

            if(existing is null)
            {
                var newTalkGroup = await _dbContext.TalkGroups.AddAsync(talkGroup);
                await _dbContext.SaveChangesAsync();
                existing = newTalkGroup.Entity;
            }

            return existing;
        }

        public virtual async Task<TalkGroupImportResults> ImportTalkGroupsCsvAsync(string talkGroupCsvPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(talkGroupCsvPath))
                throw new ArgumentException($"'{nameof(talkGroupCsvPath)}' cannot be null or empty.", nameof(talkGroupCsvPath));

            uint talkGroupCount = 0;

            using(var fs = new FileStream(talkGroupCsvPath, FileMode.Open, FileAccess.Read))
            using(var csvStream = new StreamReader(fs))
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
                        hexId = lineParts[1];
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

                    if(lineParts.Length > 8)
                        streamIds = lineParts[8];

                    if(streamIds != null)
                        streams = streamIds.Split('|');

                    try
                    {
                        var dbTalkGroup = await GetOrCreateTalkGroupAsync(new TalkGroup() { Identifier = tgId }, cancellationToken);

                        talkGroupCount++;

                        dbTalkGroup.Mode = TalkGroupMode.Digital;
                        dbTalkGroup.AlphaTag = alphaTag;
                        dbTalkGroup.Name = tgName;

                        if(dbTalkGroup.TalkGroupStreams is null)
                            dbTalkGroup.TalkGroupStreams = new Collection<TalkGroupStream>();

                        foreach(var stream in streams)
                        {
                            var dbStream = await GetOrCreateStreamAsync(new SRStream() { StreamIdentifier = stream });

                            if(dbTalkGroup.TalkGroupStreams.FirstOrDefault(tgs => tgs.StreamId == dbStream.Id) is null)
                                dbTalkGroup.TalkGroupStreams.Add(new TalkGroupStream() { TalkGroup = dbTalkGroup, TalkGroupId = dbTalkGroup.Id, Stream = dbStream, StreamId = dbStream.Id });
                        }
                    }
                    catch(Exception e)
                    {
                        System.Console.WriteLine(e);
                        return new TalkGroupImportResults() { IsSuccessful = false, ItemsProcessed = talkGroupCount };
                    }
                    finally
                    {
                        await _dbContext.SaveChangesAsync();
                    }
                }
            }
            return new TalkGroupImportResults()
            {
                IsSuccessful = true,
                ItemsProcessed = talkGroupCount
            };
        }

        public void Dispose() {  }
    }
}