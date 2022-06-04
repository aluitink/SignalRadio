CREATE TABLE [dbo].[Users] (
    [Id]           BIGINT         IDENTITY (1, 1) NOT NULL,
    [Username]     NVARCHAR (MAX) NULL,
    [EmailAddress] NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)
);

