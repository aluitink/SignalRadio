CREATE TABLE [dbo].[RadioGroups] (
    [Id]            BIGINT         IDENTITY (1, 1) NOT NULL,
    [Name]          NVARCHAR (MAX) NULL,
    [RadioSystemId] BIGINT         NOT NULL,
    CONSTRAINT [PK_RadioGroups] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_RadioGroups_RadioSystems_RadioSystemId] FOREIGN KEY ([RadioSystemId]) REFERENCES [dbo].[RadioSystems] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_RadioGroups_RadioSystemId]
    ON [dbo].[RadioGroups]([RadioSystemId] ASC);

