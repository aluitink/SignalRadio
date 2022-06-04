CREATE TABLE [dbo].[TalkGroupStreams] (
    [TalkGroupId] BIGINT NOT NULL,
    [StreamId]    BIGINT NOT NULL,
    CONSTRAINT [PK_TalkGroupStreams] PRIMARY KEY CLUSTERED ([TalkGroupId] ASC, [StreamId] ASC),
    CONSTRAINT [FK_TalkGroupStreams_Streams_StreamId] FOREIGN KEY ([StreamId]) REFERENCES [dbo].[Streams] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TalkGroupStreams_TalkGroups_TalkGroupId] FOREIGN KEY ([TalkGroupId]) REFERENCES [dbo].[TalkGroups] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_TalkGroupStreams_StreamId]
    ON [dbo].[TalkGroupStreams]([StreamId] ASC);

