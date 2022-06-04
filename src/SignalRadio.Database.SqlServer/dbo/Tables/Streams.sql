CREATE TABLE [dbo].[Streams] (
    [Id]               BIGINT         IDENTITY (1, 1) NOT NULL,
    [MountPointId]     BIGINT         NULL,
    [StreamIdentifier] NVARCHAR (MAX) NULL,
    [Name]             NVARCHAR (MAX) NULL,
    [Description]      NVARCHAR (MAX) NULL,
    [Genra]            NVARCHAR (MAX) NULL,
    [OwnerUserId]      BIGINT         NULL,
    [LastCallTimeUtc]  DATETIME2 (7)  NOT NULL,
    CONSTRAINT [PK_Streams] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Streams_MountPoints_MountPointId] FOREIGN KEY ([MountPointId]) REFERENCES [dbo].[MountPoints] ([Id]),
    CONSTRAINT [FK_Streams_Users_OwnerUserId] FOREIGN KEY ([OwnerUserId]) REFERENCES [dbo].[Users] ([Id])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Streams_MountPointId]
    ON [dbo].[Streams]([MountPointId] ASC) WHERE ([MountPointId] IS NOT NULL);


GO
CREATE NONCLUSTERED INDEX [IX_Streams_OwnerUserId]
    ON [dbo].[Streams]([OwnerUserId] ASC);

