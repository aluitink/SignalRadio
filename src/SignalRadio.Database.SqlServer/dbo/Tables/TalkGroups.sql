CREATE TABLE [dbo].[TalkGroups] (
    [Id]            BIGINT         IDENTITY (1, 1) NOT NULL,
    [RadioGroupId]  BIGINT         NULL,
    [RadioSystemId] BIGINT         NULL,
    [Identifier]    INT            NOT NULL,
    [Mode]          TINYINT        NOT NULL,
    [Tag]           TINYINT        NOT NULL,
    [AlphaTag]      NVARCHAR (MAX) NULL,
    [Name]          NVARCHAR (MAX) NULL,
    [Description]   NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_TalkGroups] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_TalkGroups_RadioGroups_RadioGroupId] FOREIGN KEY ([RadioGroupId]) REFERENCES [dbo].[RadioGroups] ([Id]),
    CONSTRAINT [FK_TalkGroups_RadioSystems_RadioSystemId] FOREIGN KEY ([RadioSystemId]) REFERENCES [dbo].[RadioSystems] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_TalkGroups_RadioGroupId]
    ON [dbo].[TalkGroups]([RadioGroupId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_TalkGroups_RadioSystemId]
    ON [dbo].[TalkGroups]([RadioSystemId] ASC);

