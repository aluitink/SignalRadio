CREATE TABLE [dbo].[RadioSystems] (
    [Id]             BIGINT         IDENTITY (1, 1) NOT NULL,
    [Name]           NVARCHAR (MAX) NULL,
    [ShortName]      NVARCHAR (MAX) NULL,
    [City]           NVARCHAR (MAX) NULL,
    [State]          NVARCHAR (MAX) NULL,
    [County]         NVARCHAR (MAX) NULL,
    [NAC]            INT            NOT NULL,
    [WANC]           INT            NOT NULL,
    [SystemNumber]   INT            NOT NULL,
    [SystemType]     TINYINT        NOT NULL,
    [SystemVoice]    INT            NOT NULL,
    [LastUpdatedUtc] DATETIME2 (7)  NOT NULL,
    CONSTRAINT [PK_RadioSystems] PRIMARY KEY CLUSTERED ([Id] ASC)
);

