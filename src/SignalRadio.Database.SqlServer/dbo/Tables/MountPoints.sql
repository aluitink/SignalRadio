CREATE TABLE [dbo].[MountPoints] (
    [Id]       BIGINT         IDENTITY (1, 1) NOT NULL,
    [UserId]   BIGINT         NOT NULL,
    [StreamId] BIGINT         NOT NULL,
    [Name]     NVARCHAR (MAX) NULL,
    [Host]     NVARCHAR (MAX) NULL,
    [Port]     BIGINT         NOT NULL,
    [Password] NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_MountPoints] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_MountPoints_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_MountPoints_UserId]
    ON [dbo].[MountPoints]([UserId] ASC);

