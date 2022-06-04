CREATE TABLE [dbo].[RadioRecorders] (
    [Id]                 BIGINT         IDENTITY (1, 1) NOT NULL,
    [RecorderIdentifier] NVARCHAR (MAX) NULL,
    [Type]               NVARCHAR (MAX) NULL,
    [SourceNumber]       INT            NOT NULL,
    [RecorderNumber]     INT            NOT NULL,
    [Count]              BIGINT         NOT NULL,
    [Duration]           REAL           NOT NULL,
    [State]              INT            NOT NULL,
    [StatusLength]       INT            NOT NULL,
    [StatusError]        INT            NOT NULL,
    [StatusSpike]        INT            NOT NULL,
    CONSTRAINT [PK_RadioRecorders] PRIMARY KEY CLUSTERED ([Id] ASC)
);

