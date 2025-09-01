IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [StorageLocations] (
    [Id] int NOT NULL IDENTITY,
    [Kind] int NOT NULL,
    [LocationUri] nvarchar(max) NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    CONSTRAINT [PK_StorageLocations] PRIMARY KEY ([Id])
);

CREATE TABLE [TalkGroups] (
    [Id] int NOT NULL IDENTITY,
    [Number] int NOT NULL,
    [Name] nvarchar(max) NULL,
    [Priority] int NULL,
    CONSTRAINT [PK_TalkGroups] PRIMARY KEY ([Id])
);

CREATE TABLE [Calls] (
    [Id] int NOT NULL IDENTITY,
    [TalkGroupId] int NOT NULL,
    [RecordingTimeUtc] datetimeoffset NOT NULL,
    [FrequencyHz] float NOT NULL,
    [DurationSeconds] int NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Calls] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Calls_TalkGroups_TalkGroupId] FOREIGN KEY ([TalkGroupId]) REFERENCES [TalkGroups] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Recordings] (
    [Id] int NOT NULL IDENTITY,
    [CallId] int NOT NULL,
    [StorageLocationId] int NOT NULL,
    [FileName] nvarchar(max) NOT NULL,
    [SizeBytes] bigint NOT NULL,
    [ReceivedAtUtc] datetimeoffset NOT NULL,
    [IsProcessed] bit NOT NULL,
    CONSTRAINT [PK_Recordings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Recordings_Calls_CallId] FOREIGN KEY ([CallId]) REFERENCES [Calls] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Recordings_StorageLocations_StorageLocationId] FOREIGN KEY ([StorageLocationId]) REFERENCES [StorageLocations] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Transcriptions] (
    [Id] int NOT NULL IDENTITY,
    [RecordingId] int NOT NULL,
    [Service] nvarchar(450) NOT NULL,
    [Language] nvarchar(max) NULL,
    [FullText] nvarchar(max) NOT NULL,
    [Confidence] float NULL,
    [AdditionalDataJson] nvarchar(max) NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [IsFinal] bit NOT NULL,
    CONSTRAINT [PK_Transcriptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Transcriptions_Recordings_RecordingId] FOREIGN KEY ([RecordingId]) REFERENCES [Recordings] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Calls_TalkGroupId_RecordingTimeUtc] ON [Calls] ([TalkGroupId], [RecordingTimeUtc]);

CREATE INDEX [IX_Recordings_CallId] ON [Recordings] ([CallId]);

CREATE INDEX [IX_Recordings_ReceivedAtUtc] ON [Recordings] ([ReceivedAtUtc]);

CREATE INDEX [IX_Recordings_StorageLocationId] ON [Recordings] ([StorageLocationId]);

CREATE INDEX [IX_TalkGroups_Number] ON [TalkGroups] ([Number]);

CREATE INDEX [IX_Transcriptions_RecordingId] ON [Transcriptions] ([RecordingId]);

CREATE INDEX [IX_Transcriptions_Service] ON [Transcriptions] ([Service]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250830233221_InitialCreate', N'9.0.8');

COMMIT;
GO

