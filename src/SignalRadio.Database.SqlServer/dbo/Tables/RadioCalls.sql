CREATE TABLE [dbo].[RadioCalls] (
    [Id]                  BIGINT         IDENTITY (1, 1) NOT NULL,
    [TalkGroupIdentifier] INT            NOT NULL,
    [CallState]           INT            NOT NULL,
    [CallRecordState]     INT            NOT NULL,
    [CallIdentifier]      NVARCHAR (MAX) NULL,
    [TalkGroupId]         BIGINT         NOT NULL,
    [TalkGroupTag]        NVARCHAR (MAX) NULL,
    [Elapsed]             BIGINT         NOT NULL,
    [Length]              BIGINT         NOT NULL,
    [IsPhase2]            BIT            NOT NULL,
    [IsConventional]      BIT            NOT NULL,
    [IsEncrypted]         BIT            NOT NULL,
    [IsAnalog]            BIT            NOT NULL,
    [IsEmergency]         BIT            NOT NULL,
    [StartTime]           DECIMAL (20)   NOT NULL,
    [StopTime]            DECIMAL (20)   NOT NULL,
    [FrequencyHz]         BIGINT         NOT NULL,
    [Frequency]           BIGINT         NOT NULL,
    [CallSerialNumber]    BIGINT         NOT NULL,
    [CallWavPath]         NVARCHAR (MAX) NULL,
    [SigmfFileName]       NVARCHAR (MAX) NULL,
    [DebugFilename]       NVARCHAR (MAX) NULL,
    [Filename]            NVARCHAR (MAX) NULL,
    [StatusFilename]      NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_RadioCalls] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_RadioCalls_TalkGroups_TalkGroupId] FOREIGN KEY ([TalkGroupId]) REFERENCES [dbo].[TalkGroups] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_RadioCalls_TalkGroupId]
    ON [dbo].[RadioCalls]([TalkGroupId] ASC);

